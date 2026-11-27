# Directivas del Agente - PC Builder Ops
**Asignatura:** Programación III (TDS-007) · ITLA · 2026-C-3  
**Ecosistema:** C# / .NET 10 (ASP.NET Core Web API), Entity Framework Core, SQL Server

**Propósito:** PC Builder Ops es una API web para un taller de computadoras armadas a pedido. El cliente cotiza un equipo con verificación de compatibilidad, el taller lo arma y lo somete a pruebas de estrés y benchmarking, y lo entrega certificado. Se apoya en un Core reutilizable de seis piezas (control de acceso, permisos, documentos, notificaciones, reportes y auditoría).

Este archivo es la fuente única de instrucciones para cualquier agente (opencode lee `AGENTS.md`; Claude Code lo importa desde `CLAUDE.md`). Se mantiene corto: el detalle vive en `docs/` y aquí solo se indica dónde está.

---

## 0. Estado actual del proyecto

> Actualizar esta sección al cerrar cada entrega.

| Pieza | Semanas | Estado |
|-------|---------|--------|
| 1. Control de acceso | 2–4 | ✅ Entregada (etiqueta `practica-1`, ya calificada), corregida en el PR #16 y con `Usuario.Nombre` desde el PR #18. |
| Máquina de estados del negocio (estructura) | 4 | ✅ Entregada en `practica-1`; sus desviaciones se corrigieron en la Práctica 3. |
| 2. Gestión de permisos | 6–8 | ✅ Asignación 2 (etiqueta `asignacion-2`, PR #20) y Práctica 2, variante C (etiqueta `practica-2`, PR #21). |
| Máquina de estados del negocio (pruebas) | 8 | ✅ Práctica 3 (etiqueta `practica-3`, PR #22). Ver `docs/entregas/practica-3.md`. Su notificación y su reporte (RF-NEG-07 y RF-NEG-08) llegaron en la Práctica 5. |
| 3. Manejador de documentos | 9 | ✅ Práctica 4 (etiqueta `practica-4`, PR #24): documentos RF-DOC-01 a 05, proyecto en contenedores con `docker compose up --build` (puerto 5206, volúmenes `documentos` y `sqldata`) y API externa de tasa de cambio. Ver `docs/entregas/practica-4.md`. Auditoría de documentos (RF-DOC-06) pendiente con la Pieza 6. |
| 4. Notificaciones y cola de correos | 11–12 | ✅ Práctica 5 (etiqueta `practica-5`, PR #37 a #41): bandeja interna con preferencia de correo, avisos de permisos y del negocio (`EnPruebas`, `Entregada`), cola con 3 reintentos, estado `Fallido`, reserva atómica y consulta del Administrador. Mailpit (`http://localhost:8025`) y `docker compose run --rm despachador` vienen en el contenedor. Criterios en `docs/criterios-notificaciones-reportes.md`; ver `docs/entregas/practica-5.md`. |
| 5. Reportes con agregación | 12 | ✅ Práctica 5: solicitudes por estado y mes, y fabricantes por piezas y valor, agregados en SQL, con rango de fechas obligatorio y alcance por rol. El reporte de tiempo de ensamble por técnico espera la Auditoría. |
| Auditoría de lo delegado en la Práctica 5 | 11 | ✅ Asignación 3 (etiqueta `asignacion-3`): 4 hallazgos corregidos, cada uno en su commit y con su prueba de regresión; reglas nuevas en la sección 12 y criterios CA-NOT-14 a 16 y CA-REP-08. Ver `docs/bitacora-asignacion-3.md` y `docs/entregas/asignacion-3.md`. |
| 6. Auditoría | 14 | Pendiente. |
| Frontend web (extra, no calificado) | — | ⏳ Fases 1 (base), 2 (sesión) y 3 (catálogo con sesión) hechas, con el diseño visual de `docs/diseno/README.md`; sigue la fase 4 (armador de PC, con el cambio B2): ver `docs/frontend.md`. Cede ante cualquier entrega del curso. |

### Desviaciones conocidas (no corregir sin que el usuario lo pida)
- Gestión de permisos (`tests/Core.Tests`) y el negocio (`tests/PCBuilderOps.Tests`) tienen pruebas automatizadas; Control de acceso solo las tiene para los enlaces de correo y el bloqueo por intentos (`tests/Core.Tests/Auth`), no para registro, activación ni sesiones (RD-12).
- Las etiquetas `practica-2-defecto-N` y `practica-3-defecto-N` contienen defectos plantados a propósito: nunca se fusionan a `main` ni se "corrigen".
- Las etiquetas de entregas (`practica-1`, `asignacion-2`, `practica-2`, `practica-3`, `practica-4`, `practica-5`, `asignacion-3`) no se mueven: se evalúa el punto etiquetado.

---

## 1. Mapa de documentación

| Archivo | Para qué sirve |
|---------|----------------|
| `docs/requisitos/core.md` | Todos los RD y RF del Core con su criterio de aceptación. Fuente de verdad del contrato del curso. |
| `docs/requisitos/negocio.md` | Dominio propio: entidades, máquina de estados objetivo, funcionalidades, notificación y reporte del negocio. |
| `docs/criterios-notificaciones-reportes.md` | Criterios de aceptación de notificaciones, cola de correos y reportes (Práctica 5), escritos antes de programarlos. Un cambio de esas piezas se acepta solo si los cumple. |
| `docs/criterios-auditoria.md` | Criterios de aceptación del historial de auditoría (Asignación 4, Pieza 6), escritos antes de programarlo. Un cambio de la auditoría se acepta solo si los cumple. |
| `docs/entregas/<entrega>.md` | Una por asignación o práctica: alcance, rúbrica, qué revisa el maestro y checklist de estado. |
| `docs/entregas/_plantilla.md` | Plantilla para crear el archivo de una entrega nueva. |
| `docs/decisiones.md` | Decisiones de diseño tomadas y por qué. Consultar antes de cambiar algo que parezca "raro". |
| `docs/maquina-de-estados.md` | Tabla de transiciones exigida por el maestro (refleja el código actual). |
| `docs/frontend.md` | Frontend web: tecnologías, pantallas por rol, endpoints que usa, cambios que pide al backend y fases. |
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
- **Cero código en la raíz:** Todo el código pertenece a `src/Core`, `src/Modules/PCBuilderOps`, `src/API`, `src/Workers` o `src/Web` (frontend React con TypeScript; no contiene reglas de negocio).

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
- Ejecutar todo en contenedores (el comando que usa el maestro): copiar `.env.example` a `.env` y `docker compose up --build`. Detener con `docker compose down` (sin `-v`, para conservar datos y documentos).
- Compilar solución: `dotnet build P3-PCBuilderOps.slnx`
- Ejecutar pruebas: `dotnet test P3-PCBuilderOps.slnx` (sin variables de entorno ni SQL Server; SQLite en memoria)
- Migraciones del Core (Crear): `dotnet ef migrations add <Nombre> --project src/Core --startup-project src/API --output-dir Data/Migrations --context AppDbContext`
- Migraciones del módulo (Crear): `dotnet ef migrations add <Nombre> --project src/Modules/PCBuilderOps --startup-project src/API --output-dir Data/Migrations --context PCBuilderOpsDbContext`
- Migraciones (Aplicar, primero el Core porque el módulo referencia `Usuarios`): `dotnet ef database update --project src/Core --startup-project src/API --context AppDbContext` y `dotnet ef database update --project src/Modules/PCBuilderOps --startup-project src/API --context PCBuilderOpsDbContext`. Usan `AppDbContextFactory` y `PCBuilderOpsDbContextFactory`: no arrancan la API, solo necesitan la cadena de conexión y **no leen** `launchSettings.json`.
- Ejecutar API: `dotnet run --project src/API`
- Ejecutar Worker independiente: `dotnet run --project src/Workers/EmailDispatcher`
- Frontend (desde `src/Web`): `npm install`, `npm run dev` (puerto 5173, reenvía `/api` a `http://localhost:5206` o a `API_URL`; para pruebas manuales del agente, `API_URL` apunta a la API temporal), `npm test`, `npm run lint`, `npm run format:check` y `npm run build`. Antes de dar por terminado un cambio del frontend, los cuatro últimos deben pasar sin errores.

### Pruebas manuales del agente
- Usar siempre una base de datos temporal (`PCBuilderOps_Prueba...`) apuntando `CONNECTION_STRING` a ella; **nunca** la base `PCBuilderOps` del usuario. Borrarla al terminar.
- Ejecutar la API de prueba en un puerto distinto (ej. `--urls http://localhost:5299`) y detenerla por su PID al terminar: lanzada con `dotnet API.dll`, el proceso se llama `dotnet`, no `API`, y si queda vivo bloquea `dotnet build`.

## 8. Variables de Entorno y Enlaces Externos
- **Variables:** `CONNECTION_STRING`, `JWT_SECRET` (obligatoria, mínimo 32 bytes), `JWT_ISSUER`, `JWT_AUDIENCE`, `JWT_EXPIRACION_MINUTOS`, `APP_BASE_URL`, `WEB_BASE_URL` (opcional, dirección del frontend), `ADMIN_NOMBRE`, `ADMIN_CORREO`, `ADMIN_PASSWORD` (las tres `ADMIN_*` son obligatorias para crear el Administrador inicial) y `SMTP_*`. Localmente están en `src/API/Properties/launchSettings.json`, ignorado por git.
- **Cadena de conexión:** se obtiene únicamente con `CadenaConexion.Obtener()` (API, despachador y migraciones). Toda opción nueva de `UseSqlServer` va tanto en `src/API/Program.cs` como en la fábrica del contexto (`AppDbContextFactory` o `PCBuilderOpsDbContextFactory`).
- **JWT:** se lee únicamente mediante `ConfiguracionJwt.DesdeEntorno()`; emisión y validación comparten esa configuración.
- **URLs en Correos:** Toda URL de activación o restablecimiento DEBE ser absoluta y se genera solo con `UrlBaseAplicacion.EnlaceActivacion` o `UrlBaseAplicacion.EnlaceRecuperacion`: apuntan al frontend si hay `WEB_BASE_URL` y a la API (`APP_BASE_URL`) si no. Queda prohibido generar rutas relativas tipo `/api/...`.
- **Negociación SMTP Dinámica:** `SmtpEmailSender` evalúa el puerto: 465 -> `SslOnConnect`, 587 -> `StartTls`, otros -> `Auto`.

## 9. Seguridad de Sesiones y Endpoints
- **Vinculación 1:1:** Cada token JWT contiene el claim `jti` mapeado directamente al `Id` de `SesionUsuario`.
- **Validación en Base de Datos:** En `OnTokenValidated` se verifica que la sesión exista, no esté revocada (`!EstaRevocada`) y que el usuario siga activo (`Usuario.EstaActivo`). Si no, `context.Fail()`.
- **Revocación:** Cambiar contraseña, restablecer credenciales (incluido el forzado por Administrador) o desactivar un usuario revoca inmediatamente todas las sesiones activas en BD.
- **Identidad:** el usuario actual se obtiene siempre de los claims de la petición en curso, nunca de "la última sesión" en la base de datos.
- **Validación GET para correos:** Los enlaces de recuperación deben soportar peticiones `GET` para validar el token y no arrojar HTTP 405 en navegadores.

## 10. Regla Operativa del Asistente
- Antes de dar por terminada cualquier tarea o refactorización, ejecuta siempre `dotnet build P3-PCBuilderOps.slnx` y confirma 0 errores y 0 advertencias.
- Un trabajo delegado con criterios de aceptación escritos (por ejemplo, `docs/criterios-notificaciones-reportes.md`) está terminado solo cuando cada criterio se comprobó. Si un criterio no se puede cumplir tal como está escrito, se avisa al usuario; nunca se reescribe el criterio para que coincida con el código.

## 11. Restricciones del Agente (innegociables)
- **Secretos:** nunca leer, mostrar, copiar ni modificar `.env` ni `src/API/Properties/launchSettings.json`. Para conocer las variables, usar `.env.example` y la sección 8. Nunca escribir contraseñas, cadenas de conexión, llaves de API ni credenciales SMTP en código, pruebas, documentación, commits ni en el chat; los valores de ejemplo van solo en `.env.example` y deben ser falsos.
- **Etiquetas:** nunca crear, mover ni borrar etiquetas de entregas (`practica-*`, `asignacion-*`); las crea el usuario. Nunca fusionar a `main` ni "corregir" las etiquetas `*-defecto-N`.
- **Migraciones:** nunca editar ni borrar una migración que ya está en `main`; los cambios de esquema van en una migración nueva.
- **Git:** nunca hacer commit, push, PR ni fusión sin la confirmación del usuario, ni reescribir el historial publicado (`push --force`, `rebase` de ramas subidas).
- **Datos del usuario:** nunca usar la base `PCBuilderOps` del usuario ni su volumen `sqldata` para pruebas (ver "Pruebas manuales del agente").
- **Alcance:** no tocar piezas fuera de la entrega en curso; lo que no entra se anota en `docs/entregas/<entrega>.md`. `src/Web` es trabajo extra y no se modifica durante una práctica salvo que el usuario lo pida.
- **Configuración del equipo:** no cambiar configuraciones de seguridad del sistema operativo (por ejemplo, Smart App Control).

## 12. Reglas aprendidas en la Asignación 3 (auditoría de la Práctica 5)
Cada regla nace de un hallazgo de `docs/bitacora-asignacion-3.md`. Se aplican a todo código nuevo o modificado.
- **Hallazgo 1 · Concurrencia en las transiciones:** toda entidad con máquina de estados (`SolicitudPermiso`, `OrdenEnsamble`) lleva un token de concurrencia (`Version`) que cambia en cada transición. Si dos peticiones resuelven o transicionan la misma fila a la vez, la base rechaza la segunda, el servicio responde `409` y no se guarda ni se notifica nada de ella. Toda prueba de una transición incluye el caso de dos peticiones que leyeron la fila antes de que la otra guardara.
- **Hallazgo 2 · Unicidad garantizada por la base:** una regla de «no puede haber dos» (una solicitud pendiente por usuario y permiso, una preferencia por usuario) se garantiza con un índice único o una clave en la base, no solo con una consulta previa. El servicio captura el choque y responde como el caso normal (`409` si es un duplicado, o actualiza si es un *upsert*), nunca con `500`. Al agregar una consulta del tipo «¿ya existe?» antes de insertar, se pregunta qué pasa si dos peticiones la hacen a la vez.
- **Hallazgo 3 · Valores límite en las entradas:** toda entrada del usuario se prueba también con los valores extremos válidos de su tipo (la primera y la última fecha del calendario, cero, el máximo), no solo con valores ausentes o mal escritos. No se calcula con la entrada del usuario algo que pueda desbordarse (por ejemplo, «hasta + 1 día»): el día de `hasta` se incluye comparando con su último instante (`<=`). Una entrada válida nunca produce `500`.
- **Hallazgo 4 · Fechas UTC también al devolverlas:** RD-11 incluye la salida. Toda fecha que devuelve la API lleva `Z`, igual al crearse que al leerse de la base. La regla vive en un solo lugar, `ConvencionFechasUtc`, que usan `AppDbContext` y `PCBuilderOpsDbContext`; está prohibido corregirla endpoint por endpoint con `DateTime.SpecifyKind`. Un defecto que aparece en una pieza se corrige en su causa y se busca en las demás; no se deja como «pendiente» después de arreglar un solo lugar.
