# Bitácora de Sesión con el Agente de IA - Asignación 2

## 1. Tareas Delegadas al Agente

1. **Analizar el enunciado y proponer el diseño** antes de programar: qué significa "aplicar un permiso" (RF-GP-05), códigos de respuesta, duplicados y estrategia de pruebas. Las decisiones las tomé yo entre las opciones propuestas y quedaron en `docs/decisiones.md`.
2. **Construir el dominio**: `SolicitudPermiso`, `EstadoSolicitud` y `MaquinaEstadosSolicitud` como único punto de transiciones (RD-04).
3. **Crear el proyecto de pruebas** (`tests/Core.Tests`, xUnit) y **redactar las pruebas** de la máquina de estados y de la entidad.

---

## 2. Detalle de la Tarea Delegada

* **Qué se le pidió:**
  Crear el dominio de las solicitudes, la máquina de estados y el proyecto de pruebas con sus primeras pruebas, verificando que la solución compile con 0 errores y 0 advertencias (regla de `AGENTS.md`).

* **Qué devolvió el agente:**
  * Dominio en `src/Core/Permisos/` con la tabla de transiciones solo en `MaquinaEstadosSolicitud`; la entidad la consulta antes de cada cambio y valida que nadie resuelva su propia solicitud (RF-GP-07) y que el rechazo tenga motivo (RF-GP-04).
  * Proyecto `tests/Core.Tests` agregado a la solución, con 44 pruebas (preparar–actuar–verificar): todas las transiciones de la tabla, todas las combinaciones fuera de la tabla, y pruebas de la entidad que comprueban el estado y los datos registrados.

---

## 3. Registro de Error del Agente, Detección y Corrección

### A. Qué devolvió el agente (el error)
Al ejecutar la suite por primera vez, NuGet mostró advertencias NU1902/NU1903: el paquete transitivo `System.Security.Cryptography.Pkcs` 10.0.0 (que llega por `MailKit` y por `Microsoft.Data.SqlClient`) tenía vulnerabilidades conocidas de gravedad alta y moderada. El agente fijó el paquete en la versión **10.0.12** suponiendo que ya traía la corrección, **sin consultar el aviso de seguridad**.

### B. Cómo se detectó el error
Al volver a compilar con `dotnet list package --vulnerable --include-transitive`, la vulnerabilidad seguía apareciendo, ahora en 10.0.12, y la compilación mostraba 20 advertencias en lugar de 0.

### C. Cómo se corrigió
Se consultaron los avisos de GitHub (GHSA-846f-f764-76mx, CVE-2026-84008, y GHSA-2fmv-rv2c-59x7, CVE-2026-70162): el rango vulnerable llega hasta 10.0.12 y la primera versión corregida es **10.0.13**. Se fijó esa versión en `src/Core/Core.csproj` y se verificó: ninguna dependencia vulnerable, 0 advertencias y las 44 pruebas pasando.

**Lección:** una actualización de seguridad se valida contra el aviso oficial y se comprueba después con `dotnet list package --vulnerable`, no se asume.

---

> Borrador en curso: se completa durante la asignación con el resto de tareas delegadas y la evidencia de la sesión.
