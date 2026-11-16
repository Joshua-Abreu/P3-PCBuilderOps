# Decisiones de diseño

Registro breve de decisiones y su motivo, para poder defenderlas (RD-01) y para que ningún agente las deshaga por error. Cada entrada: fecha, decisión, por qué y requisitos relacionados. Las pendientes van al final.

## Tomadas

### 2026-10-13 · Sesión JWT validada contra la base de datos (RF-CA-12, RF-CA-18)
Cada token lleva `jti` = `Id` de `SesionUsuario` y `OnTokenValidated` comprueba que la sesión exista, no esté revocada ni vencida y que el usuario siga activo.
**Por qué:** un JWT por sí solo no puede revocarse; así el cierre de sesión, el cambio de contraseña y la desactivación invalidan las credenciales al instante.

### 2026-10-13 · Cola de correos con despachador independiente (RF-NOT-08, RF-NOT-09)
Las operaciones solo insertan en `CorreosEnCola` dentro de su transacción; `src/Workers/EmailDispatcher` envía los pendientes y termina.
**Por qué:** la operación no depende de que el servidor SMTP responda, y el envío puede repetirse sin duplicar correos.

### 2026-10-15 · Administrador inicial desde variables de entorno (RF-CA-04, RD-10)
Al arrancar, si no hay ningún Administrador, la API crea (o promueve) la cuenta de `ADMIN_CORREO` / `ADMIN_PASSWORD`. Si ya existe uno, no hace nada.
**Por qué:** el registro público solo crea Estándar y el maestro necesita un Administrador para verificar el bloque de roles. Hacerlo por entorno evita credenciales en el repositorio y en las migraciones.

### 2026-10-15 · Configuración JWT en un único punto, sin valor de respaldo (RD-10)
`ConfiguracionJwt.DesdeEntorno()` lee `JWT_SECRET` (mínimo 32 bytes) y la API no arranca si falta. La emisión (`JwtService`) y la validación (`Program.cs`) usan la misma instancia.
**Por qué:** una clave por defecto escrita en el código es pública y permitiría falsificar tokens; además, dos lecturas distintas de la clave causaban errores 500 cuando no coincidían.

### 2026-10-15 · Restablecimiento forzado reutiliza el flujo de recuperación (RF-CA-13)
El Administrador invalida la contraseña (hash aleatorio), revoca sesiones y códigos previos, y se encola un código nuevo que el usuario usa en `POST /api/auth/restablecer-password`.
**Por qué:** cumple "el usuario no puede seguir usando la anterior y recibe por la cola el código", sin crear un segundo flujo de cambio de contraseña.

### 2026-10-15 · URL base de los enlaces en un único helper
`UrlBaseAplicacion.Obtener(configuration)` construye la URL absoluta de todos los enlaces de correo.
**Por qué:** había tres copias de la lógica y una generaba enlaces relativos rotos.

### 2026-10-15 · La etiqueta `practica-1` no se mueve
Los arreglos posteriores están en `main` (PR #16), no en la etiqueta.
**Por qué:** la entrega ya fue calificada.

### 2026-10-15 · Funcionalidades del dominio con prefijo `FN-`
Las funcionalidades propias de PC Builder Ops se identifican como FN-01 a FN-05 en `docs/SRS.md` y `docs/requisitos/negocio.md`. `RF-NEG-01` a `RF-NEG-10` quedan reservados para los requisitos del maestro.
**Por qué:** el SRS usaba RF-NEG-01 a 05 con otro significado; los IDs del maestro mandan porque son los que se citan en commits, PRs y en la calificación.

### 2026-10-15 · Instrucciones del agente en `AGENTS.md`
`CLAUDE.md` solo importa `AGENTS.md`; los requisitos viven en `docs/requisitos/` y cada entrega tiene su archivo en `docs/entregas/`.
**Por qué:** una sola fuente compartida por Claude Code y opencode, y contexto completo sin depender de los PDFs.

### 2026-10-15 · `Usuario.Nombre` obligatorio (atributo mínimo del Core, RD-07)
Entre 2 y 100 caracteres (`LongitudesUsuario`), sin espacios al inicio ni al final. Lo exigen el registro, el alta por Administrador y el Administrador inicial (`ADMIN_NOMBRE` obligatoria, igual que `ADMIN_CORREO` y `ADMIN_PASSWORD`). La migración `AddNombreUsuario` asigna a los usuarios existentes la parte del correo antes de la `@`, recortada a 100 caracteres.
**Por qué:** es un atributo mínimo de `Usuario` en los Requerimientos del Core. Rellenar con la parte del correo deja a cada usuario existente con un nombre identificable sin pedir datos a mano.

### 2026-10-15 · Errores de validación siempre en formato `{"mensaje"}` (RD-07, RD-08)
`RespuestaModeloInvalido` reemplaza la respuesta automática de ASP.NET (`ProblemDetails`) por `"Datos de entrada inválidos. Revisa: <campos>."`, nombrando solo los campos que fallaron.
**Por qué:** controlar el formato de todos los errores y no exponer `traceId` ni nombres de tipos internos como `API.DTOs.CrearUsuarioRequest`.

### 2026-10-15 · Migraciones sin arrancar la API y cadena de conexión en un único punto (RD-09, RD-10)
`AppDbContextFactory` crea el `DbContext` en tiempo de diseño solo con `CadenaConexion.Obtener()`; la API, el despachador y las migraciones comparten esa resolución.
**Por qué:** `dotnet ef` no lee `launchSettings.json` y, al arrancar la API, exigía `JWT_SECRET` sin necesitarlo. Cada herramienta pide solo lo que usa. Costo: la configuración de `UseSqlServer` vive en dos lugares (`Program.cs` y la fábrica); ambos tienen un comentario que lo recuerda, como excepción aprobada a la regla de no comentar.

### 2026-10-18 · Permisos específicos; los técnicos son Estándar con `OperarTaller` (RF-GP-05, RF-CA-04)
Gestión de permisos otorga permisos de un catálogo (inicialmente `OperarTaller`), guardados en una tabla de permisos por usuario. Aplicar una solicitud aprobada = otorgar ese permiso al solicitante. Los técnicos tienen rol Estándar y obtienen `OperarTaller` mediante una solicitud aprobada.
**Por qué:** ascender a Administrador daría al solicitante todo el poder del sistema (cambiar roles, resolver solicitudes ajenas). Un permiso concreto otorga solo lo pedido y da a RF-GP-05 un caso real del dominio. Resuelve el pendiente sobre el rol de los técnicos.

### 2026-10-18 · Los permisos se comprueban contra la base de datos en cada petición (RF-GP-05)
Las operaciones que exigen un permiso consultan la tabla de permisos del usuario en cada petición; el permiso no viaja dentro del JWT.
**Por qué:** si el permiso estuviera en el token, tras la aprobación el usuario tendría que cerrar sesión y volver a entrar, y eso es la "intervención manual" que RF-GP-05 prohíbe. Sigue el mismo criterio que la validación de sesiones contra la base de datos.

### 2026-10-18 · `GET /api/taller/ordenes` como operación protegida por `OperarTaller`
Listado de solo lectura de las órdenes (hoy `OrdenesEnsamble`; en esa fecha, `OrdenesServicio`), protegido por el permiso. Sirve para demostrar RF-GP-05: `403` antes de la aprobación y `200` después, sin volver a iniciar sesión.
**Por qué:** es la operación mínima del dominio que un técnico necesita y no adelanta el trabajo del negocio de la semana 8; el endpoint se mueve con el negocio cuando este pase al módulo.

### 2026-10-18 · Reglas y códigos de respuesta de Gestión de permisos (RF-GP-04, RF-GP-06, RF-GP-07, RD-07)
- Aprobar ejecuta `Pendiente → Aprobada → Aplicada` en una sola transacción: si aplicar falla, no queda nada a medias.
- Transición fuera de la tabla → `409 Conflict`. Resolver la propia solicitud → `403`. Rechazo sin motivo o permiso inexistente → `400`. Siempre con `{"mensaje"}`.
- Se rechaza crear una solicitud de un permiso que el usuario ya tiene o que ya tiene pendiente.
**Por qué:** `409` distingue "petición válida que choca con el estado actual" de un error de datos (`400`) o de autorización (`403`). Rechazar duplicados evita aprobaciones sin efecto.

### 2026-10-18 · Pruebas con xUnit, SQLite en memoria y reloj inyectado (RD-11, RD-12)
Proyecto de pruebas en `tests/`, incluido en la solución, ejecutable con `dotnet test` sin variables de entorno. La máquina de estados se prueba sin base de datos; el servicio, con SQLite en memoria (una base nueva por prueba). Los servicios reciben `TimeProvider` en lugar de usar `DateTime.UtcNow` directamente.
**Por qué:** SQLite es relacional y permite verificar lo que realmente quedó guardado (estado, aprobador, fecha, motivo), cosa que EF Core InMemory no garantiza. Una base por prueba las hace independientes del orden. El reloj inyectado permite comprobar el "cuándo" exacto que exige RF-GP-03.

### 2026-10-18 · Comentarios `// Preparar`, `// Actuar`, `// Verificar` en las pruebas
Cada prueba marca sus tres fases con esos comentarios, como excepción a la regla de no comentar el código.
**Por qué:** la rúbrica de la Asignación 2 exige que cada prueba tenga "tres fases identificables (preparar, actuar, verificar)". Los marcadores lo hacen explícito sin depender de la lectura de quien califica.

### 2026-10-31 · Sin retrabajo: la entrega depende de la prueba más reciente (RF-NEG-03, FN-04)
No existe `EnPruebas → EnArmado`. Una prueba reprobada deja la orden en `EnPruebas`; el técnico corrige y registra otra. Se entrega si hay al menos una prueba y la más reciente está aprobada. Las anteriores se conservan como historial.
**Por qué:** con la regla anterior («todas aprobadas»), una sola prueba reprobada bloqueaba la orden para siempre. Este cambio lo resuelve sin alterar los estados ni las transiciones de la propuesta aprobada. Costo aceptado: mientras se corrige, la orden aparece en `EnPruebas`.

### 2026-10-31 · El permiso del negocio se consulta en el servicio del módulo (RF-GP-05, RF-NEG-06)
Las operaciones del taller consultan `IVerificadorPermisos` dentro del servicio del módulo y rechazan con `403`. Reemplaza a la política `OperarTaller` de `src/API`. El Administrador pasa sin el permiso, igual que con la política.
**Por qué:** la Práctica 3 exige detectar la falta de la consulta con una prueba de integración sin HTTP, y esa prueba nunca pasaría por una política. Con la política activa, quitar la consulta tampoco permitiría la operación en la aplicación real. Sigue comprobándose contra la base de datos en cada petición.

### 2026-10-31 · El módulo de negocio tiene su propio `DbContext` y migraciones (RD-03, RD-09)
Las entidades del negocio viven en `src/Modules/PCBuilderOps`, con su `DbContext` y su historial de migraciones, en la misma base de SQL Server. `src/Core/Ordenes/` se elimina y una migración del Core borra la tabla `OrdenesServicio`.
**Por qué:** si el `DbContext` del Core conociera las entidades del negocio, el Core dependería del módulo. Costo: aplicar las migraciones requiere un comando por contexto, y el README lo documenta.

### 2026-10-31 · Errores sin cuerpo y `415` también en formato `{"mensaje"}` (RD-07, RD-08)
`SuppressMapClientErrors` desactiva el `ProblemDetails` automático de ASP.NET y `RespuestaEstadoHttp` escribe el mensaje de `404`, `405` y `415` cuando nadie más respondió. `401` y `403` se escriben en los eventos de JWT.
**Por qué:** la auditoría de la Práctica 3 encontró que una petición sin `Content-Type` devolvía `traceId` y el enlace de la especificación, y que las rutas inexistentes respondían sin cuerpo.

### 2026-10-31 · Códigos de respuesta del negocio (RD-07)
Compatibilidad o datos inválidos → `400`. Sin rol o sin permiso → `403`. Orden que no existe o de otro cliente → `404`. Transición fuera de la tabla o stock insuficiente → `409`. Siempre con `{"mensaje"}`.
**Por qué:** mantiene el criterio de Gestión de permisos, y `404` en órdenes ajenas no revela que existen.

### 2026-11-07 · Tasa de cambio como API externa, en una operación aparte (RD-07, RD-10)
`GET /api/ordenes/{id}/costo?moneda=XXX` convierte el costo total de una orden con la tasa de ExchangeRate-API. La clave se lee de `TASA_CAMBIO_API_KEY`, que llega desde `.env`. Sin clave o con la API caída responde un rechazo controlado.
**Por qué:** encaja con la cotización (FN-01) y calcula con datos del negocio en lugar de reenviar la respuesta. Va aparte de cotizar para que el negocio siga funcionando si la API externa falla.

### 2026-11-07 · Contenedores: puerto 5206, migraciones al arrancar y envío automático de correos (RD-09)
La aplicación publica el puerto 5206 (el mismo de `API.http` y `APP_BASE_URL`); SQL Server no publica ninguno. La API aplica las migraciones de los dos contextos al arrancar, con reintentos, y registra el `EmailQueueWorker` existente para enviar la cola cada 30 s.
**Por qué:** la Práctica 4 exige levantar el proyecto con `docker compose up --build` sin pasos manuales. Sin SMTP real, el README explica cómo leer los enlaces con `docker compose exec`, porque la base no es accesible desde fuera.

### 2026-11-07 · Documentos: nombre generado, cualquier tipo, 10 MB y `403` a otros usuarios (RF-DOC-01 a 05, RD-06, RD-07)
El archivo se guarda con un nombre generado en la carpeta del volumen; el nombre original solo vive en el registro. Se acepta cualquier tipo hasta 10 MB y la descarga se entrega como adjunto. Descargar o eliminar un documento ajeno responde `403`; uno inexistente o eliminado, `404`.
**Por qué:** el nombre generado impide escribir fuera de la carpeta. No limitar el tipo evita rechazar el archivo con el que se pruebe. `403` deja claro el rechazo que revisa la rúbrica.

### 2026-11-07 · `launchSettings.json` en el historial temprano, sin reescribirlo (RD-10)
El commit `6b7f304` (2 de octubre) versionó `src/API/Properties/launchSettings.json` con un `JWT_SECRET` de desarrollo y una cadena de conexión con autenticación de Windows, sin contraseña. El commit `554480a` lo retiró y lo excluyó con `.gitignore`. Ese `JWT_SECRET` ya no se usa en ningún entorno; ningún commit contiene un `.env`, la clave de la API externa ni las contraseñas actuales.
**Por qué:** reescribir el historial cambiaría los commits de las etiquetas ya evaluadas (`practica-1` a `practica-3`). El secreto expuesto se retiró de uso, que es lo que lo deja sin efecto.

### 2026-11-14 · Frontend en React con TypeScript, como proyecto aparte en `src/Web` (RD-02, RD-06)
Se agrega un frontend web con Vite, React, TypeScript, React Router, TanStack Query, Tailwind CSS y React Hook Form con Zod. Vive en `src/Web`, fuera de la solución de .NET, y solo consume la API: en desarrollo por el proxy de Vite y en Docker por nginx, ambos en el mismo origen, sin CORS. El diseño, las pantallas y los cambios que pide al backend están en `docs/frontend.md`.
**Por qué:** el proyecto debe poder usarse como un sistema completo, no solo por Postman. React con TypeScript se elige para aprender ese lenguaje en vez de Blazor. Ser un proyecto aparte demuestra RD-02 (cambiar la interfaz no toca las reglas) y mantiene RD-06 (la autorización sigue en el servidor). No lo califica ninguna rúbrica, así que se trabaja en ramas propias y cede ante cualquier entrega.

### 2026-11-14 · Enlaces de correo hacia el frontend con `WEB_BASE_URL` opcional y permisos en `GET /api/auth/yo` (RF-CA-07, RF-CA-10, RF-CA-16, RF-GP-05)
Los enlaces de activación y recuperación se generan solo en `UrlBaseAplicacion`. Si existe `WEB_BASE_URL`, abren las pantallas `/activar` y `/restablecer` del frontend, que llaman a la API; si no, apuntan a la API como antes y los `GET` siguen funcionando. `GET /api/auth/yo` agrega `permisos` con los permisos aprobados como texto (`["OperarTaller"]`); al Administrador no se le agregan permisos implícitos: el frontend decide el menú del taller con el rol o el permiso.
**Por qué:** sin el frontend levantado, el enlace del correo debe seguir sirviendo (revisión acumulada y guía del README), por eso la variable es opcional y no está en `.env.example` hasta que el frontend entre en Docker. Los permisos en `yo` evitan que el frontend tenga que provocar un `403` para saber si muestra el taller.

### 2026-11-14 · El bloqueo indica los minutos y se quita al restablecer o cambiar la contraseña; `DebeCambiarPassword` marca el restablecimiento pendiente (RF-CA-11, RF-CA-13, RF-CA-19, RF-CA-22)
El rechazo por bloqueo dice cuántos minutos faltan («Intenta de nuevo en 15 minutos.»), como promete la guía del README para RF-CA-19. Restablecer la contraseña con el enlace del correo o cambiarla con sesión limpia `BloqueadoHastaUtc` además de `IntentosFallidos`. `DebeCambiarPassword` se mantiene como indicador de restablecimiento forzado pendiente, visible en el listado de usuarios del Administrador: ningún flujo lo devuelve en `true` al iniciar sesión, porque el restablecimiento forzado cambia la contraseña por una aleatoria y el enlace del correo lo apaga.
**Por qué:** el bloqueo frena a quien adivina contraseñas; quien usa el enlace demostró que controla el correo, que es una prueba más fuerte. El código ya reiniciaba los intentos al restablecer y olvidaba la fecha de bloqueo, así que el usuario no podía entrar con su contraseña nueva durante 15 minutos. Activar `DebeCambiarPassword` al crear usuarios desde el Administrador cambiaría las peticiones de `API.http` que usa la revisión acumulada, por eso queda para más adelante.

### 2026-11-14 · El catálogo del frontend exige sesión; se descarta el cambio B1 (RF-NEG-06, FN-01)
`GET /api/componentes` sigue respondiendo `401` sin sesión y la pantalla `/catalogo` del frontend lleva al inicio de sesión. La portada invita al visitante a crear una cuenta para ver el catálogo.
**Por qué:** RF-NEG-06 exige que ninguna operación del módulo de negocio se ejecute sin sesión válida, y el catálogo es parte de FN-01. Un catálogo público, como el de una tienda, podría leerse como incumplimiento en la revisión acumulada o el proyecto final. Si el profesor confirma que un catálogo público no rompe RF-NEG-06, basta con `[AllowAnonymous]` en ese endpoint y quitar la ruta protegida en el frontend.

### 2026-11-16 · Validar un armado sin cotizar con `VerificadorCompatibilidad.Evaluar` (FN-01, RD-02, RF-NEG-06)
`VerificadorCompatibilidad.Evaluar` devuelve un diagnóstico completo (faltantes, todos los problemas, consumo y potencia) sin lanzar excepción, y `Validar` lo usa para lanzar el primer error con el mismo mensaje y en el mismo orden que antes. `POST /api/componentes/validar` expone ese diagnóstico más el total, exige sesión como el resto del negocio y no crea órdenes ni reserva existencias. Un armado incompleto se informa en `faltantes`, no como error.
**Por qué:** el armador del frontend necesita avisar mientras el usuario elige piezas sin duplicar la regla de compatibilidad en el navegador (RD-02). Una sola fuente de la regla evita que la validación previa y la cotización digan cosas distintas; una prueba de caracterización fija los mensajes de `Validar` para que la cotización no cambie.

### 2026-11-16 · Diseño visual oscuro a partir de Google Stitch, solo con datos de la API (RD-02, RF-NEG-06)
El frontend usa un solo tema oscuro (azul marino con acento cian), Inter para el texto y JetBrains Mono para cifras y códigos, ambas servidas desde la aplicación. Con sesión, la navegación es un menú lateral que en el teléfono se abre con un botón; sin sesión, una cabecera. Los colores viven solo en el `@theme` de `src/Web/src/index.css`, y `docs/diseno/README.md` explica qué se tomó de Stitch y qué se descartó.
**Por qué:** el usuario eligió el estilo de Stitch y lo revisó afinado en Claude Design. Stitch inventa datos que la API no tiene (telemetría, fotos, elegir rol, rastreo sin sesión); dibujarlos crearía pantallas que el backend no respalda o que contradicen RF-NEG-06, así que se quitaron. Un solo tema evita mantener dos paletas en un trabajo que no se califica; las fuentes propias evitan depender de un servicio externo dentro de Docker.

## Pendientes

- **Fechas leídas de la base sin `Z` (RD-11).** `fechaCreacionUtc` sale con `Z` al crearse pero sin ella al leerse de SQL Server (`DateTimeKind.Unspecified`). Unificar marcando las fechas como UTC al leerlas.
