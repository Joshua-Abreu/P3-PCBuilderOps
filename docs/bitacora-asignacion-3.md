# Bitácora de delegación - Asignación 3

Auditoría de lo que el agente entregó en la Práctica 5 (notificaciones, cola de correos y reportes), comparado contra `docs/criterios-notificaciones-reportes.md`, los RD de `AGENTS.md` y los casos que los criterios no cubrían.

## Cómo se auditó

- **Entorno:** copia de `main` en `f355224` (lo etiquetado como `practica-5`), con `.env.example` copiado a `.env` y `docker compose up --build`. Se levantó en un proyecto de Docker aparte (`pcbnot`, API en el puerto 5208) para no tocar la base del usuario.
- **Cómo se provocaron los casos:** con peticiones HTTP (Node `fetch`). Las peticiones «simultáneas» se lanzaron con `Promise.all`.
- **Qué se probó:**
  - entradas vacías o mal formadas en notificaciones, cola y reportes;
  - fechas en los bordes del calendario;
  - dos solicitudes simultáneas;
  - dos Administradores resolviendo la misma solicitud a la vez;
  - ningún Administrador activo;
  - destinatarios de correo raros;
  - el formato de las fechas en cada pieza.
- **Lo que funcionó bien y no es hallazgo:**
  - parámetros mal formados (`?soloNoLeidas=abc`, `correoActivo: "no"`, cuerpo vacío) responden `400 {"mensaje"}`;
  - un id inexistente o vacío responde `404`;
  - un rango de fechas sin datos devuelve filas vacías y `total: 0`;
  - no se puede quedar sin Administradores activos, porque la API impide desactivarse a sí mismo y quitarse el rol siendo el único;
  - un destinatario inválido queda registrado con su error en la cola, sin tumbar al procesador.

| # | Hallazgo | Pieza | Gravedad | Requisito o criterio incumplido |
|---|----------|-------|----------|---------------------------------|
| 1 | Dos Administradores resuelven la misma solicitud a la vez: queda `Rechazada` con el permiso otorgado y el solicitante recibe dos avisos contradictorios | Notificaciones | Alta | RF-NOT-02, CA-NOT-02, CA-NOT-03, RF-GP-06 |
| 2 | Dos solicitudes simultáneas del mismo usuario crean varias pendientes y avisan varias veces a cada Administrador | Notificaciones | Media | RF-NOT-01, CA-NOT-01, RF-GP-01 |
| 3 | Un reporte con `hasta=9999-12-31` responde `500` en vez de `400` | Reportes | Media | RF-REP-05, CA-REP-06, RD-07 |
| 4 | Las fechas salen con `Z` en la bandeja y sin ella en la cola, las solicitudes y los usuarios | Notificaciones y cola | Media | RD-11 |

---

## Hallazgo 1 · Resolución simultánea: solicitud `Rechazada` con el permiso otorgado y dos avisos contradictorios · Gravedad alta

**Qué se pidió.** En la Práctica 5, «resolver una solicitud notifica al solicitante con el resultado y, si fue rechazada, el motivo» (RF-NOT-02). CA-NOT-02 y CA-NOT-03 piden **una** notificación nueva que diga `aprobada` o `rechazada`, y que «ningún otro usuario reciba una notificación por esta resolución». El agente agregó los avisos dentro de `GestionPermisosService.AprobarAsync` y `RechazarAsync` (commit `0a57339`).

**Qué entregó el agente.** El servicio lee la solicitud, valida la transición en memoria y guarda. No hay ningún control de concurrencia en la fila, así que dos resoluciones que leen la solicitud `Pendiente` al mismo tiempo pasan las dos. Evidencia: dos Administradores resuelven la misma solicitud con `Promise.all`:

```text
aprobar + rechazar simultáneos
códigos = 200, 200
estado final = Rechazada, motivo = "Motivo 1"
permisos del solicitante = ["OperarTaller"]
notificaciones del solicitante (2) = ["... fue rechazada. Motivo: Motivo 1", "... fue aprobada. Ya tienes el permiso."]

rechazar + rechazar simultáneos
códigos = 200, 200
estado final = Rechazada, motivo = "Motivo 1"
notificaciones del solicitante (2) = ["... rechazada. Motivo: Motivo 1", "... rechazada. Motivo: Motivo 0"]

aprobar + aprobar simultáneos
códigos = 200, 500 {"mensaje":"Error interno del servidor."}
```

La solicitud queda `Rechazada` pero el usuario tiene el permiso, lo que rompe RF-GP-06. Recibe dos avisos que se contradicen, y uno cita un motivo que no quedó guardado. Si los dos aprueban, el segundo recibe `500`.

**Por qué pasó.** **Criterio ausente.** Los criterios de notificaciones solo describen una resolución a la vez, y ningún criterio ni regla de `AGENTS.md` pide que una transición resista dos peticiones simultáneas. La revisión de la Práctica 5 tampoco lo probó: todas las pruebas y comprobaciones resolvían una sola vez.

**Corrección.** La hizo el agente, con esta instrucción del usuario (copiada tal cual):

> «Corrige el hallazgo 1: cuando 2 administradores resuelven la misma solicitud al imsmo tiempo solo una resolucion debe ganar, la otra debe recibir 409, no cambiar nada y no enviar ninguna notificacion. El solicitante o usuario no puede quedar con una solicitud rechazada y otra aprobada ni recibir 2 avisos. Resuélvelo en la base de datos, no con una comprobación en memoria, y revisa si la aprobación y el rechazo tienen el mismo problema. Agrega una prueba que falle con el código actual y pase con la corrección.»

Qué hizo:
- `SolicitudPermiso` tiene ahora una columna `Version`, configurada como token de concurrencia. Cambia en cada transición, y SQL Server solo guarda la fila si nadie la cambió desde que se leyó.
- Si el guardado choca, `GestionPermisosService` vuelve a leer el estado y responde con `TransicionNoPermitidaException`, que el controlador ya traduce a `409`. Como la notificación y el permiso iban en el mismo `SaveChanges`, no queda nada de la resolución perdedora.
- **La misma causa en otro lugar:** las transiciones de `OrdenEnsamble` (iniciar armado, pasar a pruebas, entregar) tenían el mismo patrón, y dos técnicos pasando la misma orden a pruebas le enviaban al cliente dos avisos. Recibieron el mismo token y el mismo manejo.
- Migraciones nuevas: `AgregarVersionSolicitudesPermiso` (Core) y `AgregarVersionOrdenesEnsamble` (módulo).

Commit: `5725f13`.

Pruebas: `ResolucionSimultaneaTests` (3) y `TransicionSimultaneaOrdenTests` (1). Sobre el código anterior a la corrección:

```text
Failed ResolucionSimultaneaTests.AprobarYRechazarALaVez_SoloGanaLaPrimeraYElSolicitanteRecibeUnSoloAviso
   Assert.Throws() Failure: No exception was thrown
Failed ResolucionSimultaneaTests.RechazarDosVecesALaVez_ConservaElPrimerMotivoYUnSoloAviso
   Assert.Throws() Failure: No exception was thrown
Failed ResolucionSimultaneaTests.AprobarDosVecesALaVez_LaSegundaSeRechazaComoTransicionYNoComoErrorInterno
   Expected: TransicionNoPermitidaException   Actual: Microsoft.EntityFrameworkCore.DbUpdateException
Failed TransicionSimultaneaOrdenTests.PasarAPruebasDosVecesALaVez_LaSegundaSeRechazaYElClienteRecibeUnSoloAviso
   Assert.Throws() Failure: No exception was thrown
```

Después de la corrección pasan las 4. Con la API en Docker, las mismas peticiones simultáneas dan:

```text
aprobar + rechazar   -> 200, 409 {"mensaje":"La solicitud está Aplicada y no puede pasar a Rechazada."}; 1 aviso
rechazar + rechazar  -> 409, 200; motivo guardado = el del que ganó; 1 aviso
aprobar + aprobar    -> 409, 200 (antes 500); 1 aviso
```

**Qué cambias.**
- `AGENTS.md`, sección 12: regla «Hallazgo 1 · Concurrencia en las transiciones». Toda entidad con máquina de estados lleva un token de concurrencia, y toda prueba de una transición incluye el caso de dos peticiones simultáneas.
- `docs/criterios-notificaciones-reportes.md`: criterio nuevo **CA-NOT-14** (dos resoluciones simultáneas: solo gana la primera y hay un solo aviso).

---

## Hallazgo 2 · Solicitudes simultáneas: varias pendientes y avisos repetidos a cada Administrador · Gravedad media

**Qué se pidió.** «Crear una solicitud de permiso notifica a los Administradores» (RF-NOT-01). CA-NOT-01 pide que cada Administrador tenga «**exactamente una** notificación nueva». La regla de que un usuario no tiene dos solicitudes pendientes del mismo permiso viene de RF-GP-01. El agente agregó el aviso en `GestionPermisosService.CrearAsync` (commit `0a57339`), sobre la comprobación de duplicados que ya existía.

**Qué entregó el agente.** La regla de duplicados se comprueba con una consulta (`AnyAsync`) antes de insertar, y en la base no hay nada que la garantice. Evidencia: un mismo Estándar envía 8 `POST /api/permisos/solicitudes` a la vez:

```text
8 POST simultáneos: códigos = 201,201,201,409,409,409,409,409
solicitudes pendientes de E2 tras la ráfaga = 3
notificaciones nuevas del Administrador = 3
```

Tres peticiones pasan la comprobación antes de que la primera se guarde. Quedan tres solicitudes pendientes, y cada Administrador recibe tres avisos y tres correos por lo que el usuario hizo una sola vez.

**Por qué pasó.** **Criterio ausente** y **revisión insuficiente**. CA-NOT-01 se comprobó con una sola petición, y ningún criterio pedía que la regla de duplicados la garantizara la base de datos. El agente reutilizó una comprobación en memoria sin preguntarse qué pasa con peticiones simultáneas, y nadie se lo pidió.

**Corrección.** La hizo el agente, con esta instrucción del usuario (copiada tal cual):

> «Corrige el Hallazgo 2: Corrige el hallazgo 2: un usuario no puede tener dos solicitudes pendientes del mismo permiso aunque las envíe al mismo tiempo. Si llegan dos a la vez, una se crea y la otra recibe 409, y cada Administrador recibe un solo aviso. La regla debe garantizarla la base de datos, no solo la consulta previa. El cambio de esquema va en una migración nueva. Agrega una prueba que falle con el código actual y pase con la corrección.»

Qué hizo:
- Un índice único filtrado `UX_SolicitudesPermiso_UnaPendientePorPermiso` sobre (`SolicitanteId`, `Permiso`) con `WHERE [Estado] = 1`. Solo puede haber una pendiente; las resueltas no cuentan, y se puede volver a solicitar después de un rechazo.
- Si el guardado choca con el índice, `CrearAsync` responde con `SolicitudDuplicadaException` (`409`) y el mismo mensaje del caso normal. Los avisos de esa petición no se guardan, porque iban en el mismo `SaveChanges`.
- La migración `AgregarIndiceUnaSolicitudPendiente` conserva el índice de la clave foránea `IX_SolicitudesPermiso_SolicitanteId`. La primera versión que generó EF lo borraba, porque creía que el índice nuevo lo cubría, y el agente lo corrigió antes de aceptarla.
- **La misma causa en otro lugar:** `ActualizarPreferenciasAsync` (Práctica 5) también buscaba y luego insertaba. Dos `PUT /api/notificaciones/preferencias` simultáneos de un usuario sin preferencia chocaban con la clave primaria y uno respondía `500`. Ahora, si choca, actualiza la que se acaba de crear.

Commit: `8cc50a0`.

Pruebas: `CreacionSimultaneaTests` y `PreferenciaSimultaneaTests`. Simulan la segunda petición con un interceptor que la ejecuta justo antes del `SaveChanges` de la primera, sin levantar la API. Sobre el código anterior a la corrección:

```text
Failed CreacionSimultaneaTests.DosSolicitudesALaVez_SoloSeCreaUnaYCadaAdministradorRecibeUnSoloAviso
   Assert.Throws() Failure: No exception was thrown
Failed PreferenciaSimultaneaTests.PrimeraPreferenciaGuardadaDosVecesALaVez_NoFallaYQuedaLaUltima
   at Core.Notifications.Services.NotificacionesService.ActualizarPreferenciasAsync(...)  (DbUpdateException)
```

`UnaSolicitudResuelta_NoImpideVolverASolicitarElMismoPermiso` pasa antes y después a propósito: comprueba que el índice no bloquea una solicitud nueva tras un rechazo. Después de la corrección pasan todas. Con la API en Docker, 8 `POST` simultáneos dan `201` una vez y `409` siete veces, con 1 pendiente y 1 aviso por Administrador (antes, 3 y 3).

**Qué cambias.**
- `AGENTS.md`, sección 12: regla «Hallazgo 2 · Unicidad garantizada por la base». Toda regla de «no puede haber dos» lleva índice único o clave, el servicio maneja el choque sin `500`, y toda consulta «¿ya existe?» se piensa con dos peticiones a la vez.
- `docs/criterios-notificaciones-reportes.md`: criterio nuevo **CA-NOT-15** (peticiones simultáneas no duplican avisos ni dan `500`).

---

## Hallazgo 3 · Reporte con `hasta=9999-12-31` responde `500` · Gravedad media

**Qué se pidió.** RF-REP-05 pide filtrar por rango de fechas. CA-REP-06 dice: «sin `desde` o sin `hasta`, con una fecha mal escrita o con `desde` posterior a `hasta`, la API responde `400` con `{"mensaje": "..."}`». RD-07 pide que una entrada inválida dé `400` controlado. El agente creó `RangoFechas` (commit `6ef2f23`).

**Qué entregó el agente.** `RangoFechas` valida el formato y el orden, pero calcula el fin del rango como `Hasta.AddDays(1)`. Con el último día del calendario, esa suma se desborda y lanza una excepción que nadie captura. Evidencia:

```text
GET /api/reportes/solicitudes?desde=0001-01-01&hasta=9999-12-31   -> 500 {"mensaje":"Error interno del servidor."}
GET /api/reportes/fabricantes?desde=0001-01-01&hasta=9999-12-31   -> 500 {"mensaje":"Error interno del servidor."}
GET /api/reportes/solicitudes?desde=2026-01-01&hasta=9999-12-31   -> 500
GET /api/reportes/solicitudes?desde=9999-12-31&hasta=9999-12-31   -> 500
```

La fecha es válida y bien escrita, y el usuario recibe un error del servidor en vez de un resultado o un `400`. El manejador global evita la traza (RD-08 se cumple), pero RD-07 no.

**Por qué pasó.** **Criterio incompleto.** CA-REP-06 enumera fechas ausentes, mal escritas y en desorden, pero no las fechas válidas en los bordes del calendario. Las pruebas de `RangoFechas` cubrieron exactamente esa lista y nada más.

**Corrección.** La hizo el agente, con esta instrucción del usuario (copiada tal cual):

> «Corrige hallazgo 3: Corrige el hallazgo 3: los dos reportes deben aceptar cualquier fecha válida en formato yyyy-MM-dd, incluidas 0001-01-01 y 9999-12-31, sin responder 500. El día de hasta se sigue incluyendo completo. Corrige el cálculo del rango, no agregues un límite arbitrario de años, y revisa si hay otra suma de fechas que pueda desbordarse. Agrega una prueba que falle con el código actual y pase con la corrección.»

Qué hizo:
- `RangoFechas.HastaExclusivoUtc` (`Hasta + 1 día`) se reemplazó por `HastaInclusivoUtc`, el último instante del día de `hasta` (`23:59:59.9999999`). Los dos reportes comparan ahora con `<=`. El día de `hasta` se sigue incluyendo completo y nada del día siguiente; la prueba existente `Solicitudes_HastaIncluyeElDiaCompletoYNadaDelSiguiente` lo sigue comprobando.
- No se agregó ningún límite de años.
- **Otras sumas de fechas:** se revisaron todas las de `src`. Las demás suman una cantidad fija a la hora actual (vencimiento de tokens y sesiones, bloqueo de 15 minutos, reserva de 5 minutos de la cola), así que ninguna depende de una entrada del usuario.
- `RangoFechasTests` dejó de comprobar la propiedad del límite y ahora verifica las fechas leídas, para que compile también sobre el código anterior.

Commit: `13ea487`.

Pruebas: `Solicitudes_ConLasFechasDeLosBordesDelCalendario_Responde` y `Fabricantes_ConLasFechasDeLosBordesDelCalendario_Responde`. Sobre el código anterior a la corrección:

```text
Failed ReportesServiceTests.Solicitudes_ConLasFechasDeLosBordesDelCalendario_Responde
   System.ArgumentOutOfRangeException : Value to add was out of range.
   at Core.Reportes.Domain.RangoFechas.get_HastaExclusivoUtc() in RangoFechas.cs:line 21
Failed ReporteFabricantesServiceTests.Fabricantes_ConLasFechasDeLosBordesDelCalendario_Responde
   System.ArgumentOutOfRangeException : Value to add was out of range.
```

Después de la corrección pasan las dos. Con la API en Docker, `desde=0001-01-01&hasta=9999-12-31` responde `200` con todos los datos, y `desde=9999-12-31&hasta=9999-12-31` responde `200` con filas vacías (antes, `500` en ambos casos).

**Qué cambias.**
- `AGENTS.md`, sección 12: regla «Hallazgo 3 · Valores límite en las entradas». Toda entrada se prueba con los extremos válidos de su tipo, nada se calcula con la entrada si puede desbordarse, y una entrada válida nunca da `500`.
- `docs/criterios-notificaciones-reportes.md`: criterio nuevo **CA-REP-08** (fechas de los bordes del calendario).

---

## Hallazgo 4 · Fechas con distinto criterio de zona horaria entre piezas · Gravedad media

**Qué se pidió.** RD-11: «todas las fechas se manipulan y almacenan en UTC». En la Práctica 5 el agente notó que las fechas leídas de SQL Server salían sin `Z`. Lo anotó como pendiente en `docs/decisiones.md` («Fechas leídas de la base sin `Z`») y lo resolvió **solo en la bandeja**, con un `SpecifyKind` local en `NotificacionesService` (commit `0a57339`). La cola (`GET /api/correos`, commit `e6f85d0`) quedó sin corregir.

**Qué entregó el agente.** La misma API devuelve la misma clase de dato con dos criterios distintos. Evidencia:

```text
POST /api/permisos/solicitudes (recién creada)   fechaCreacionUtc = "2026-11-26T22:11:15.1578818Z"
GET  /api/permisos/solicitudes/mias (la misma)   fechaCreacionUtc = "2026-11-26T22:11:15.1578818"
GET  /api/notificaciones                         fechaCreacionUtc = "2026-11-26T22:11:15.2347296Z"
GET  /api/correos                                fechaCreacionUtc = "2026-11-26T22:11:15.2414316"
GET  /api/usuarios/{id}                          fechaCreacionUtc = "2026-11-26T22:11:14.2611058"
```

Una fecha sin `Z` la interpreta cualquier cliente (un navegador, por ejemplo) como hora **local**. En República Dominicana (UTC-4), un correo creado a las 22:11 UTC se muestra 4 horas desfasado, mientras su notificación se muestra bien. Incluso la misma solicitud cambia de formato entre su creación y su lectura.

**Por qué pasó.** **Revisión insuficiente** e **instrucción incompleta**. El agente corrigió el síntoma donde lo vio y dejó el resto como «pendiente» en lugar de corregir la causa, que es que EF Core lee las fechas sin marcarlas como UTC. Se aceptó así porque ningún criterio pedía comprobar el formato de las fechas en las respuestas, y RD-11 solo habla de cómo se guardan, no de cómo se devuelven.

**Corrección.** La hizo el agente, con esta instrucción del usuario (copiada tal cual):

> «Corrige el hallazgo 4: todas las fechas que devuelve la API deben salir en UTC con la Z al final, igual en todas las piezas (bandeja, cola, solicitudes, usuarios, órdenes y documentos), tanto al crearse como al leerse de la base. Corrige la causa en un solo lugar para el Core y para el módulo, no endpoint por endpoint, y quita el arreglo local que quedó en la bandeja. Agrega una prueba que falle con el código actual y pase con la corrección.»

Qué hizo:
- `src/Core/Data/ConvencionFechasUtc.cs` define `FechaUtcConverter` y la convención `UsarFechasUtc()`. La aplican `AppDbContext` y `PCBuilderOpsDbContext` en `ConfigureConventions`, así que toda propiedad `DateTime` o `DateTime?` de los dos contextos se lee marcada como UTC y se guarda en UTC.
- Se quitó el `DateTime.SpecifyKind` local de `NotificacionesService`. Ya no queda ningún `SpecifyKind` fuera de la convención.
- La convención no cambia las columnas: `dotnet ef migrations has-pending-model-changes` responde «No changes have been made to the model» en los dos contextos, así que no necesita migración.
- El frontend no interpreta esas fechas (solo usa `expiracionUtc` del inicio de sesión, que ya traía `Z`), así que no se rompe nada.

Commit: `262770e`.

Pruebas: `FechasUtcTests` del Core y del módulo. Cada uno revisa que **toda** propiedad de fecha del contexto se lea como UTC, de modo que una fecha nueva sin la convención también falla. Además leen datos reales de la base. Sobre el código anterior a la corrección:

```text
Failed Core FechasUtcTests.SolicitudCorreoYNotificacion_LeidasDeLaBase_SalenConZ
   Expected: "2026-11-26T22:11:15Z"   Actual: "2026-11-26T22:11:15"
Failed Core FechasUtcTests.TodaPropiedadDeFechaDelCore_SeLeeDeLaBaseComoUtc
   Assert.Empty() Failure: Collection was not empty
Failed Módulo FechasUtcTests.TodaPropiedadDeFechaDelModulo_SeLeeDeLaBaseComoUtc
   Assert.Empty() Failure: Collection was not empty
Failed Módulo FechasUtcTests.OrdenYPrueba_LeidasDeLaBase_SonUtc
   Expected: Utc   Actual: Unspecified
```

Después de la corrección pasan las 4. Con la API en Docker, la solicitud al crearse y al leerse, la bandeja, la cola y el usuario devuelven `...Z`.

**Qué cambias.**
- `AGENTS.md`, sección 12: regla «Hallazgo 4 · Fechas UTC también al devolverlas». RD-11 incluye la salida, la regla vive solo en `ConvencionFechasUtc`, y está prohibido corregir endpoint por endpoint o dejar un defecto como «pendiente» tras arreglar un solo lugar.
- `docs/criterios-notificaciones-reportes.md`: criterio nuevo **CA-NOT-16** (fechas con `Z` en todas las piezas).
- `docs/decisiones.md`: el pendiente «Fechas leídas de la base sin `Z`» queda resuelto.

---

## Verificación final

- **Pruebas:** `dotnet test P3-PCBuilderOps.slnx` pasa con **336 pruebas** (150 del Core y 186 del módulo), 13 de ellas nuevas por los hallazgos. `dotnet build` termina con 0 advertencias.
- **Cada prueba falla antes y pasa después.** Se armó, para cada hallazgo, el código anterior a su corrección con las pruebas finales. Todas compilan, y las del hallazgo fallan (salidas arriba); con el código final pasan. La sección «Pruebas» del README explica cómo repetirlo.
- **Migraciones sobre datos existentes.** Se levantó `main` antes de los cambios, se le cargaron datos (usuarios, solicitudes, una orden y un documento) y se actualizó la misma base al código corregido. Las tres migraciones se aplicaron y los datos siguieron ahí.
- **Revisión acumulada**, en un Docker limpio con el código corregido:
  - reportes: 24 de 24;
  - cola de correos: 19 de 19;
  - bandeja: 46 de 46;
  - evento del negocio: 19 de 19;
  - prácticas anteriores: 92 de 92;
  - los seis frentes, con el documento sobreviviendo a `docker compose down` y `up`: 37 de 37.

## Observaciones fuera del alcance (no corregidas)

- **Registro simultáneo con el mismo correo.** `AuthService` busca el correo con `AnyAsync` antes de insertar y no captura el choque. El índice único de la base evita el duplicado, pero la segunda petición probablemente recibiría un error del servidor en lugar de `400` (no se reprodujo). Es el patrón del Hallazgo 2, pero en Control de acceso (Práctica 1), fuera del alcance de esta auditoría de la Práctica 5; queda anotado en `docs/entregas/asignacion-3.md`.
- **Reserva de existencias de dos órdenes distintas al mismo tiempo.** El token de la orden impide transicionar dos veces la misma orden, pero no protege la existencia de un componente que reservan dos órdenes distintas (FN-02, Práctica 3). Queda anotado para una entrega futura.
