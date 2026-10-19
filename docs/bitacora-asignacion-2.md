# Bitácora de Sesión con el Agente de IA - Asignación 2

## 1. Tareas Delegadas al Agente

1. **Analizar el enunciado y proponer el diseño** antes de programar: qué significa "aplicar un permiso", códigos de respuesta, duplicados y estrategia de pruebas. Las decisiones las tomé yo entre las opciones propuestas y quedaron en `docs/decisiones.md`.

2. **Crear el proyecto de pruebas** (`tests/Core.Tests`, xUnit) y **redactar las pruebas** de la máquina de estados y de la entidad.

---

## 2. Detalle de la Tarea Delegada

* **Qué se le pidió:**
  Crear el proyecto de pruebas y sus primeras pruebas sobre la máquina de estados y la entidad de solicitudes, verificando que la solución compile con 0 errores y 0 advertencias (regla de `AGENTS.md`).

* **Qué devolvió el agente:**
  * Proyecto `tests/Core.Tests` agregado a la solución, con 44 pruebas en esa primera entrega (preparar–actuar–verificar): todas las transiciones de la tabla, todas las combinaciones fuera de la tabla, y pruebas de la entidad que comprueban el estado y los datos registrados.
  * Al cierre de la asignación la suite tiene 59 pruebas: se sumaron las del servicio de Gestión de permisos, que verifican lo guardado en una base SQLite en memoria.

---

## 3. Registro de Error del Agente, Detección y Corrección

### A. Qué devolvió el agente (el error)
Al ejecutar la suite por primera vez, NuGet mostró advertencias NU1902/NU1903: el paquete transitivo `System.Security.Cryptography.Pkcs` 10.0.0 (que llega por `MailKit` y por `Microsoft.Data.SqlClient`) tenía vulnerabilidades conocidas de gravedad alta y moderada. El agente fijó el paquete en la versión **10.0.12** suponiendo que ya traía la corrección, **sin consultar el aviso de seguridad**.

### B. Cómo se detectó el error
Al volver a compilar con `dotnet list package --vulnerable --include-transitive`, la vulnerabilidad seguía apareciendo, ahora en 10.0.12, y la compilación mostraba 20 advertencias en lugar de 0.

### C. Cómo se corrigió
Se consultaron los avisos de GitHub (GHSA-846f-f764-76mx, CVE-2026-84008, y GHSA-2fmv-rv2c-59x7, CVE-2026-70162): el rango vulnerable llega hasta 10.0.12 y la primera versión corregida es **10.0.13**. Se fijó esa versión en `src/Core/Core.csproj` y se verificó: ninguna dependencia vulnerable, 0 advertencias y las 44 pruebas pasando.

**Lección:** una actualización de seguridad se valida contra el aviso oficial y se comprueba después con `dotnet list package --vulnerable`, no se asume.

#### Evidencia de la sesión

Primera ejecución de `dotnet test` (paquete original):

```text
tests\Core.Tests\Core.Tests.csproj : warning NU1903: Package 'System.Security.Cryptography.Pkcs' 10.0.0 has a known high severity vulnerability, https://github.com/advisories/GHSA-846f-f764-76mx
tests\Core.Tests\Core.Tests.csproj : warning NU1902: Package 'System.Security.Cryptography.Pkcs' 10.0.0 has a known moderate severity vulnerability, https://github.com/advisories/GHSA-2fmv-rv2c-59x7
```

Después de que el agente fijara la 10.0.12 (el error):

```text
src\Core\Core.csproj : warning NU1903: El paquete "System.Security.Cryptography.Pkcs" 10.0.12 tiene una vulnerabilidad de gravedad alta conocida, https://github.com/advisories/GHSA-846f-f764-76mx
--- dotnet list P3-PCBuilderOps.slnx package --vulnerable --include-transitive:
   > System.Security.Cryptography.Pkcs      10.0.12    High       https://github.com/advisories/GHSA-846f-f764-76mx
--- dotnet build:
    0 Errores
    20 Advertencia(s)
```

Consulta de los avisos (API de GitHub Advisories):

```text
GHSA-846f-f764-76mx | high   | CVE-2026-84008 - .NET Denial of Service Vulnerability
   rango: >= 10.0.0, <= 10.0.12 | corregido en: 10.0.13
GHSA-2fmv-rv2c-59x7 | medium | CVE-2026-70162 - .NET Denial of Service Vulnerability
   rango: >= 10.0.0, <= 10.0.12 | corregido en: 10.0.13
```

Después de fijar la 10.0.13:

```text
--- vulnerables:
   ninguno
--- build:
    0 Advertencia(s)
    0 Errores
--- test (sin variables):
Correctas! - Con error:     0, Superado:    44, Omitido:     0, Total:    44
```

Commit de la corrección: `1ceed68` — `fix(deps): fijar System.Security.Cryptography.Pkcs 10.0.13 por CVE-2026-84008 y CVE-2026-70162`.

### Segundo caso: migración aplicada sin recompilar

* **Qué devolvió el agente:** generó la migración `AddGestionPermisos` y la aplicó a una base temporal con `dotnet ef database update --no-build`, sin recompilar después de crearla.
* **Cómo se detectó:** EF Core respondió `PendingModelChangesWarning` ("el modelo tiene cambios pendientes") y la base no se creó. El ensamblado compilado no contenía la migración recién generada.
* **Cómo se corrigió:** se recompiló la solución antes de aplicar la migración; se verificó en SQL Server que `SolicitudesPermiso` y `PermisosUsuario` se crean con las columnas, nulos y claves esperados, y se eliminó la base temporal.

#### Evidencia de la sesión

Aplicación con `--no-build` justo después de generar la migración (el error):

```text
System.InvalidOperationException: An error was generated for warning 'Microsoft.EntityFrameworkCore.Migrations.PendingModelChangesWarning': The model for context 'AppDbContext' has pending changes. Add a new migration before updating the database.
```

Después de recompilar y volver a aplicar:

```text
Done.
PermisosUsuario.UsuarioId uniqueidentifier null=NO
PermisosUsuario.Permiso int null=NO
PermisosUsuario.SolicitudId uniqueidentifier null=NO
PermisosUsuario.FechaOtorgadoUtc datetime2 null=NO
SolicitudesPermiso.Id uniqueidentifier null=NO
SolicitudesPermiso.SolicitanteId uniqueidentifier null=NO
SolicitudesPermiso.Permiso int null=NO
SolicitudesPermiso.Estado int null=NO
SolicitudesPermiso.AprobadorId uniqueidentifier null=YES
SolicitudesPermiso.FechaCreacionUtc datetime2 null=NO
SolicitudesPermiso.FechaResolucionUtc datetime2 null=YES
SolicitudesPermiso.MotivoRechazo nvarchar null=YES
BD temporal eliminada
```

Commit de la migración: `b1f096e` — `feat(permisos): persistir solicitudes y permisos otorgados con migración (RF-GP-05, RD-09)`.

