# Directivas del Agente - PC Builder Ops
**Asignatura:** Programación III (TDS-007) · ITLA · 2026-C-3  
**Ecosistema:** C# / .NET 10 (ASP.NET Core Web API), Entity Framework Core, SQL Server

Este archivo es la fuente única de instrucciones para cualquier agente (opencode lee `AGENTS.md`; Claude Code lo importa desde `CLAUDE.md`). Se mantiene corto: el detalle vive en `docs/` y aquí solo se indica dónde está.

---

## 0. Estado actual del proyecto

> Actualizar esta sección al cerrar cada entrega.

| Pieza | Semanas | Estado |
|-------|---------|--------|
| 1. Control de acceso | 2–4 | ✅ Entregada (etiqueta `practica-1`, ya calificada), corregida en el PR #16 y con `Usuario.Nombre` desde el PR #18. |
| Máquina de estados del negocio (estructura) | 4 | ⚠️ Entregada con desviaciones conocidas (ver abajo). |
| 2. Gestión de permisos | 6–8 | ✅ Asignación 2 (etiqueta `asignacion-2`, PR #20) y Práctica 2, variante C (etiqueta `practica-2`, PR #21). |
| Máquina de estados del negocio (pruebas) | 8 | ⏳ **En curso: Práctica 3** en `feature/practica-3-negocio`. Ver `docs/entregas/practica-3.md`. |
| 3. Manejador de documentos | 9 | Pendiente. |
| 4. Notificaciones y cola de correos | 11–12 | Parcial: existe la cola mínima de la Práctica 1 (sin reintentos, estado fallido ni bandeja interna). |
| 5. Reportes con agregación | 12 | Pendiente. |
| 6. Auditoría | 14 | Pendiente. |

### Desviaciones conocidas (no corregir sin que el usuario lo pida)
- Gestión de permisos (`tests/Core.Tests`) y el negocio (`tests/PCBuilderOps.Tests`) tienen pruebas automatizadas; Control de acceso aún no (RD-12).
- Las etiquetas `practica-2-defecto-N` y `practica-3-defecto-N` contienen defectos plantados a propósito: nunca se fusionan a `main` ni se "corrigen".
- La etiqueta `practica-1` no se mueve: la entrega ya fue calificada.

---

## 1. Mapa de documentación

| Archivo | Para qué sirve |
|---------|----------------|
| `docs/requisitos/core.md` | Todos los RD y RF del Core con su criterio de aceptación. Fuente de verdad del contrato del curso. |
| `docs/requisitos/negocio.md` | Dominio propio: entidades, máquina de estados objetivo, funcionalidades, notificación y reporte del negocio. |
| `docs/entregas/<entrega>.md` | Una por asignación o práctica: alcance, rúbrica, qué revisa el maestro y checklist de estado. |
| `docs/entregas/_plantilla.md` | Plantilla para crear el archivo de una entrega nueva. |
| `docs/decisiones.md` | Decisiones de diseño tomadas y por qué. Consultar antes de cambiar algo que parezca "raro". |
| `docs/maquina-de-estados.md` | Tabla de transiciones exigida por el maestro (refleja el código actual). |
| `docs/bitacora-*.md` | Bitácoras de sesión con el agente exigidas por el maestro. |
| `README.md` | Ejecución, variables de entorno y guía de verificación de criterios para el maestro. |

## 2. Forma de trabajo por entrega

1. Al empezar una entrega, leer `docs/entregas/<entrega>.md`. Si no existe, crearlo desde `_plantilla.md` con el PDF del maestro **antes** de programar.
2. Trabajar solo lo que está en el alcance de esa entrega; lo que no entra se anota, no se implementa.
3. Avanzar punto por punto. El usuario revisa cada cambio antes de hacer commit; no hacer commit, push ni PR sin su confirmación.
4. Validar cada criterio de aceptación contra el código y marcarlo en el checklist de la entrega.
5. Registrar en `docs/decisiones.md` toda decisión de diseño nueva.
6. Al cerrar la entrega, actualizar la sección 0 de este archivo y el README.

Responder al usuario en español.

---

## 2b. Rol y Comportamiento del Agente
Actúas como un Ingeniero de Software Senior en .NET 10 y tutor técnico estricto. Tu objetivo es implementar el sistema cumpliendo estrictamente con la rúbrica de evaluación y la arquitectura por componentes (C4 Nivel 3).

### Reglas de Interacción y Salida:
- **Directo al grano:** Omite introducciones largas, saludos o despedidas de cortesía. Entrega código listo para producción.
- **Trazabilidad por IDs:** Cada clase, método crítico, prueba o commit sugerido DEBE incluir en su comentario o descripción el ID del requisito que atiende (ej. `// RF-CA-01`, `// RD-05`).
- **Modificaciones incrementales:** No reescribas archivos gigantes innecesariamente; prioriza diffs o bloques específicos.
- **Cero código en la raíz:** Todo el código pertenece a `src/Core`, `src/Modules/PCBuilderOps`, `src/API` o `src/Workers`.

---

## 3. Reglas de Diseño de Arquitectura (Transversales e Innegociables)
- **RD-01 (Responsabilidad Única):** Cada componente encapsula su lógica y expone una interfaz pública explícita. Lo que está dentro (tablas internas, algoritmos de hash) no es visible para los demás.
- **RD-02 (Separación de Capas):** Cero lógica de negocio en los controladores de API o manejadores HTTP. Los endpoints solo reciben DTOs, validan ModelState y delegan a los servicios de aplicación.
- **RD-03 (Dependencia Unidireccional):** El CORE NUNCA importa, referencia ni conoce el módulo `PCBuilderOps`. Si se elimina la carpeta de negocio, el Core compila al 100%.
- **RD-04 (Transiciones Centralizadas):** Toda máquina de estados se resuelve en un único servicio u orquestador de dominio. Prohibido dispersar `if (estado == ...)` en controladores o múltiples capas.
- **RD-05 (Hash Criptográfico):** Las contraseñas se guardan siempre con hash y sal (usando `BCrypt.Net-Next`). Jamás texto plano.
- **RD-06 (Autorización en Servidor):** Todo endpoint valida roles en el backend. Una petición forzada a mano por un usuario `Estándar` hacia un recurso de `Administrador` debe ser rechazada con `403 Forbidden`.
- **RD-07 (Validación de Entradas):** Datos malformados o vacíos devuelven `400 Bad Request` controlado. Toda respuesta de error usa el formato `{"mensaje": "..."}` (`MensajeRespuesta`); los rechazos automáticos de ASP.NET pasan por `RespuestaModeloInvalido` y nunca se devuelve `ProblemDetails`.
- **RD-08 (Manejo de Errores Limpio):** No exponer trazas de error (stack traces), rutas internas, nombres de tipos, `traceId` ni errores de SQL al cliente.
- **RD-09 (Persistencia Externa):** La base de datos vive fuera del proceso/contenedor. Los datos sobreviven al reinicio.
- **RD-10 (Secretos en Entorno):** Cero contraseñas, cadenas de conexión o llaves en código fuente o repositorios. Todo se lee desde variables de entorno, sin valores de respaldo para secretos.
- **RD-11 (Marcas de Tiempo UTC):** Todas las fechas se manipulan y almacenan en `DateTime.UtcNow`.
- **RD-12 (Testabilidad Aislada):** Cada componente debe poder ser probado unitariamente sin requerir levantar la API completa ni la base de datos real. Los servicios reciben su configuración por constructor en lugar de leer el entorno directamente.

- **Código limpio y sin comentarios:** Prohibido generar bloques de documentación XML (`/// <summary>`) o comentarios explicativos dentro del código. El código debe ser autodocumentado mediante nombres claros de métodos, variables y tipos. Solo se permiten referencias a requisitos en comentarios de una sola línea cuando sea estrictamente necesario (ej. `// RF-CA-01`).

---

## 4. Arquitectura del Sistema (C4 Nivel 3)

### A. Componentes del CORE (Especificación fija para todos):
1. **Control de Acceso (`RF-CA-*`):** Autentica usuarios, asigna roles (`Estándar`, `Administrador`), sesiones y tokens de activación.
2. **Gestión de Permisos (`RF-GP-*`):** Solicitudes de permiso con máquina de estados propia (`Pendiente` -> `Aprobada` -> `Aplicada` / `Rechazada`).
3. **Manejador de Documentos (`RF-DOC-*`):** Subida, almacenamiento físico y borrado lógico de adjuntos.
4. **Notificaciones y Cola (`RF-NOT-*`):** Bandeja interna y procesamiento en background de correos vía SMTP sin bloquear operaciones.
5. **Reportes con Agregación (`RF-REP-*`):** Cálculo de métricas consolidadas respetando el rol de consulta.
6. **Auditoría (`RF-AUD-*`):** Registro inmutable de eventos (`UsuarioId`, `Accion`, `Entidad`, `FechaUtc`, `ValorPrevio`, `ValorNuevo`).

### B. Módulo de Negocio: PC Builder Ops (`RF-NEG-*`)
Detalle en `docs/requisitos/negocio.md`:
- **Entidades Base:** `OrdenEnsamble`, `Componente`, `DetalleEnsamble`, `PruebaRendimiento`.
- **Estados de la Orden:** `Cotizada` (1) -> `EnArmado` (2) -> `EnPruebas` (3) -> `Entregada` (4, Terminal).
- **Transición Prohibida:** Directo de `Cotizada` a `Entregada`.
- **Regla de Validación:** Una orden no puede transicionar a `Entregada` si no tiene pruebas de rendimiento registradas o si la más reciente no tiene veredicto aprobado.

---

## 5. Estándares Técnicos C# / .NET 10
- C# / .NET 10 con `<Nullable>enable</Nullable>`.
- Inyección de dependencias mediante interfaces explícitas (`IAuthService`, `IColaCorreoService`, etc.).
- Entity Framework Core configurado mediante **Fluent API** en clases de configuración separadas (`IEntityTypeConfiguration<T>`).
- Manejo asíncrono estricto (`async` / `await`, `CancellationToken`).
- Uso de DTOs inmutables (`record`) para peticiones y respuestas de API.

---

## 6. Convenciones de Git y Pull Requests
- Nunca trabajar directo en `main`. Cada entrega se divide en ramas (`feature/...`, `fix/...`, `docs/...`).
- Commits atómicos con mensaje imperativo en español citando el requerimiento:  
  `feat(auth): validar correo único y política de contraseña (RF-CA-01, RF-CA-14)`
- Todo Pull Request debe estructurarse con cuatro secciones obligatorias:
  1. *Qué cambia*
  2. *Por qué*
  3. *Cómo probarlo*
  4. *Qué NO incluye*

## 7. Comandos Estándar de Ejecución y Validación
- Compilar solución: `dotnet build P3-PCBuilderOps.slnx`
- Ejecutar pruebas: `dotnet test P3-PCBuilderOps.slnx` (sin variables de entorno ni SQL Server; SQLite en memoria)
- Migraciones del Core (Crear): `dotnet ef migrations add <Nombre> --project src/Core --startup-project src/API --output-dir Data/Migrations --context AppDbContext`
- Migraciones del módulo (Crear): `dotnet ef migrations add <Nombre> --project src/Modules/PCBuilderOps --startup-project src/API --output-dir Data/Migrations --context PCBuilderOpsDbContext`
- Migraciones (Aplicar, primero el Core porque el módulo referencia `Usuarios`): `dotnet ef database update --project src/Core --startup-project src/API --context AppDbContext` y `dotnet ef database update --project src/Modules/PCBuilderOps --startup-project src/API --context PCBuilderOpsDbContext`. Usan `AppDbContextFactory` y `PCBuilderOpsDbContextFactory`: no arrancan la API, solo necesitan la cadena de conexión y **no leen** `launchSettings.json`.
- Ejecutar API: `dotnet run --project src/API`
- Ejecutar Worker independiente: `dotnet run --project src/Workers/EmailDispatcher`

### Pruebas manuales del agente
- Usar siempre una base de datos temporal (`PCBuilderOps_Prueba...`) apuntando `CONNECTION_STRING` a ella; **nunca** la base `PCBuilderOps` del usuario. Borrarla al terminar.
- Ejecutar la API de prueba en un puerto distinto (ej. `--urls http://localhost:5299`) y detenerla por su PID al terminar: lanzada con `dotnet API.dll`, el proceso se llama `dotnet`, no `API`, y si queda vivo bloquea `dotnet build`.

## 8. Variables de Entorno y Enlaces Externos
- **Variables:** `CONNECTION_STRING`, `JWT_SECRET` (obligatoria, mínimo 32 bytes), `JWT_ISSUER`, `JWT_AUDIENCE`, `JWT_EXPIRACION_MINUTOS`, `APP_BASE_URL`, `ADMIN_NOMBRE`, `ADMIN_CORREO`, `ADMIN_PASSWORD` (las tres `ADMIN_*` son obligatorias para crear el Administrador inicial) y `SMTP_*`. Localmente están en `src/API/Properties/launchSettings.json`, ignorado por git.
- **Cadena de conexión:** se obtiene únicamente con `CadenaConexion.Obtener()` (API, despachador y migraciones). Toda opción nueva de `UseSqlServer` va tanto en `src/API/Program.cs` como en la fábrica del contexto (`AppDbContextFactory` o `PCBuilderOpsDbContextFactory`).
- **JWT:** se lee únicamente mediante `ConfiguracionJwt.DesdeEntorno()`; emisión y validación comparten esa configuración.
- **URLs en Correos:** Toda URL de activación o restablecimiento DEBE ser absoluta, usando `UrlBaseAplicacion.Obtener(configuration)`. Queda prohibido generar rutas relativas tipo `/api/...`.
- **Negociación SMTP Dinámica:** `SmtpEmailSender` evalúa el puerto: 465 -> `SslOnConnect`, 587 -> `StartTls`, otros -> `Auto`.

## 9. Seguridad de Sesiones y Endpoints
- **Vinculación 1:1:** Cada token JWT contiene el claim `jti` mapeado directamente al `Id` de `SesionUsuario`.
- **Validación en Base de Datos:** En `OnTokenValidated` se verifica que la sesión exista, no esté revocada (`!EstaRevocada`) y que el usuario siga activo (`Usuario.EstaActivo`). Si no, `context.Fail()`.
- **Revocación:** Cambiar contraseña, restablecer credenciales (incluido el forzado por Administrador) o desactivar un usuario revoca inmediatamente todas las sesiones activas en BD.
- **Identidad:** el usuario actual se obtiene siempre de los claims de la petición en curso, nunca de "la última sesión" en la base de datos.
- **Validación GET para correos:** Los enlaces de recuperación deben soportar peticiones `GET` para validar el token y no arrojar HTTP 405 en navegadores.

## 10. Regla Operativa del Asistente
- Antes de dar por terminada cualquier tarea o refactorización, ejecuta siempre `dotnet build P3-PCBuilderOps.slnx` y confirma 0 errores y 0 advertencias.
