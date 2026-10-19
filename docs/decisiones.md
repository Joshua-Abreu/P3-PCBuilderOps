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
Listado de solo lectura de la tabla actual `OrdenesServicio`, protegido por el permiso. Sirve para demostrar RF-GP-05: `403` antes de la aprobación y `200` después, sin volver a iniciar sesión.
**Por qué:** es la operación mínima del dominio que un técnico necesita y no adelanta el trabajo del negocio de la semana 8; el endpoint se mueve con el negocio cuando este pase al módulo.

### 2026-10-18 · Reglas y códigos de respuesta de Gestión de permisos (RF-GP-04, RF-GP-06, RF-GP-07, RD-07)
- Aprobar ejecuta `Pendiente → Aprobada → Aplicada` en una sola transacción: si aplicar falla, no queda nada a medias.
- Transición fuera de la tabla → `409 Conflict`. Resolver la propia solicitud → `403`. Rechazo sin motivo o permiso inexistente → `400`. Siempre con `{"mensaje"}`.
- Se rechaza crear una solicitud de un permiso que el usuario ya tiene o que ya tiene pendiente.
**Por qué:** `409` distingue "petición válida que choca con el estado actual" de un error de datos (`400`) o de autorización (`403`). Rechazar duplicados evita aprobaciones sin efecto.

### 2026-10-18 · Pruebas con xUnit, SQLite en memoria y reloj inyectado (RD-11, RD-12)
Proyecto de pruebas en `tests/`, incluido en la solución, ejecutable con `dotnet test` sin variables de entorno. La máquina de estados se prueba sin base de datos; el servicio, con SQLite en memoria (una base nueva por prueba). Los servicios reciben `TimeProvider` en lugar de usar `DateTime.UtcNow` directamente.
**Por qué:** SQLite es relacional y permite verificar lo que realmente quedó guardado (estado, aprobador, fecha, motivo), cosa que EF Core InMemory no garantiza. Una base por prueba las hace independientes del orden. El reloj inyectado permite comprobar el "cuándo" exacto que exige RF-GP-03.

## Pendientes

- **Retrabajo `EnPruebas → EnArmado`.** Más realista cuando una prueba falla y da más casos a la suite de la semana 8. Decidir antes de la semana 8.
- **Fechas leídas de la base sin `Z` (RD-11).** `fechaCreacionUtc` sale con `Z` al crearse pero sin ella al leerse de SQL Server (`DateTimeKind.Unspecified`). Unificar marcando las fechas como UTC al leerlas.
