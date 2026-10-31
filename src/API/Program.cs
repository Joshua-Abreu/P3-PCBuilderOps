using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using API.Validacion;
using Core.Auth.Exceptions;
using Core.Auth.Services;
using Core.Data;
using Core.Notifications.Services;
using Core.Permisos.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using PCBuilderOps.Data;
using PCBuilderOps.Services;

var builder = WebApplication.CreateBuilder(args);

var connectionString = CadenaConexion.Obtener(builder.Configuration.GetConnectionString("DefaultConnection"));

var configuracionJwt = ConfiguracionJwt.DesdeEntorno();

// Mantener igual que Core/Data/AppDbContextFactory.cs (migraciones): toda opción nueva de UseSqlServer va en ambos.
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString));

// Mantener igual que PCBuilderOpsDbContextFactory (migraciones): toda opción nueva de UseSqlServer va en ambos.
builder.Services.AddDbContext<PCBuilderOpsDbContext>(options =>
    options.UseSqlServer(
        connectionString,
        sql => sql.MigrationsHistoryTable(PCBuilderOpsDbContext.TablaHistorialMigraciones)));

builder.Services.AddScoped<IPasswordHasher, BcryptPasswordHasher>();
builder.Services.AddScoped<IInputValidator, InputValidator>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IPasswordService, PasswordService>();
builder.Services.AddScoped<IUsuarioAdminService, UsuarioAdminService>();
builder.Services.AddScoped<IAdministradorInicialService, AdministradorInicialService>();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<GestionPermisosService>();
builder.Services.AddScoped<IGestionPermisosService>(sp => sp.GetRequiredService<GestionPermisosService>());
builder.Services.AddScoped<IVerificadorPermisos>(sp => sp.GetRequiredService<GestionPermisosService>());
builder.Services.AddScoped<IOrdenesService, OrdenesService>();
builder.Services.AddSingleton(configuracionJwt);
builder.Services.AddSingleton<IJwtService, JwtService>();
builder.Services.AddSingleton<ISmtpEmailSender, SmtpEmailSender>();
builder.Services.AddScoped<IColaCorreoService, ColaCorreoService>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = configuracionJwt.ParametrosValidacion();

        options.Events = new JwtBearerEvents
        {
            // RD-07
            OnChallenge = async context =>
            {
                context.HandleResponse();
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                context.Response.Headers.WWWAuthenticate = JwtBearerDefaults.AuthenticationScheme;
                await context.Response.WriteAsJsonAsync(new { mensaje = "Necesitas iniciar sesión." });
            },
            // RD-07
            OnForbidden = context =>
                context.Response.WriteAsJsonAsync(new { mensaje = "No tienes permiso para esta operación." }),
            OnTokenValidated = async context =>
            {
                var dbContext = context.HttpContext.RequestServices.GetRequiredService<AppDbContext>();
                var principal = context.Principal;

                var jtiClaim = principal?.FindFirst(JwtRegisteredClaimNames.Jti)?.Value;

                if (string.IsNullOrEmpty(jtiClaim) || !Guid.TryParse(jtiClaim, out var sesionId))
                {
                    context.Fail("Token sin identificador de sesión válido.");
                    return;
                }

                var sesion = await dbContext.SesionesUsuario
                    .FirstOrDefaultAsync(s => s.Id == sesionId);

                if (sesion is null)
                {
                    context.Fail("Sesión no encontrada.");
                    return;
                }

                if (sesion.EstaRevocada)
                {
                    context.Fail("Sesión revocada.");
                    return;
                }

                if (sesion.ExpiracionUtc <= DateTime.UtcNow)
                {
                    context.Fail("Sesión expirada.");
                    return;
                }

                var usuario = await dbContext.Usuarios
                    .FirstOrDefaultAsync(u => u.Id == sesion.UsuarioId);

                if (usuario is null || !usuario.EstaActivo)
                {
                    context.Fail("Usuario inactivo o no encontrado.");
                    return;
                }
            }
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(options =>
        options.InvalidModelStateResponseFactory = RespuestaModeloInvalido.Crear);

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    var administradorInicial = scope.ServiceProvider.GetRequiredService<IAdministradorInicialService>();

    try
    {
        var resultado = await administradorInicial.AsegurarAsync(
            Environment.GetEnvironmentVariable("ADMIN_NOMBRE"),
            Environment.GetEnvironmentVariable("ADMIN_CORREO"),
            Environment.GetEnvironmentVariable("ADMIN_PASSWORD"));

        if (resultado == ResultadoAdministradorInicial.SinConfiguracion)
            logger.LogWarning("No existe ningún Administrador. Define ADMIN_NOMBRE, ADMIN_CORREO y ADMIN_PASSWORD para crear el primero.");
        else if (resultado != ResultadoAdministradorInicial.YaExisteAdministrador)
            logger.LogInformation("Administrador inicial {Resultado} a partir de ADMIN_CORREO.", resultado);
    }
    catch (ValidacionException ex)
    {
        logger.LogWarning("No se creó el Administrador inicial: {Motivo}", ex.Message);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "No se pudo verificar el Administrador inicial. ¿Aplicaste las migraciones?");
    }
}

app.Use(async (context, next) =>
{
    try
    {
        await next();
    }
    catch (ValidacionException ex)
    {
        context.Response.StatusCode = 400;
        await context.Response.WriteAsJsonAsync(new { mensaje = ex.Message });
    }
    catch (AutenticacionException ex)
    {
        context.Response.StatusCode = 400;
        await context.Response.WriteAsJsonAsync(new { mensaje = ex.Message });
    }
    catch (ReglaNegocioException ex)
    {
        context.Response.StatusCode = 403;
        await context.Response.WriteAsJsonAsync(new { mensaje = ex.Message });
    }
    catch (Exception)
    {
        context.Response.StatusCode = 500;
        await context.Response.WriteAsJsonAsync(new { mensaje = "Error interno del servidor." });
    }
});

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
