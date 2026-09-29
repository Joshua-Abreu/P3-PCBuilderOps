using Core.Auth.Exceptions;
using Core.Auth.Services;
using Core.Data;
using Core.Notifications.Services;
using Core.Notifications.Workers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// RD-09, RD-10: Cadena de conexión desde variable de entorno con fallback local
var connectionString = Environment.GetEnvironmentVariable("CONNECTION_STRING")
    ?? "Server=localhost;Database=PCBuilderOps;Trusted_Connection=True;TrustServerCertificate=True";

// Registro de dependencias (RD-01, RD-02)
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddScoped<IPasswordHasher, BcryptPasswordHasher>();
builder.Services.AddScoped<IInputValidator, InputValidator>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddSingleton<IEmailSender, SmtpEmailSender>();

// RF-NOT-13: Servicio en segundo plano
builder.Services.AddHostedService<EmailQueueWorker>();

builder.Services.AddControllers();

var app = builder.Build();

// RD-08: Middleware global de excepciones — nunca exponer stack traces ni SQL
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
    catch (Exception)
    {
        context.Response.StatusCode = 500;
        await context.Response.WriteAsJsonAsync(new { mensaje = "Error interno del servidor." });
    }
});

app.UseHttpsRedirection();
app.MapControllers();

app.Run();
