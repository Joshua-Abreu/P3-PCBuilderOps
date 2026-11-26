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

**Corrección.** *(pendiente)*

**Qué cambias.** *(pendiente)*

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

**Corrección.** *(pendiente)*

**Qué cambias.** *(pendiente)*

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

**Corrección.** *(pendiente)*

**Qué cambias.** *(pendiente)*

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

**Corrección.** *(pendiente)*

**Qué cambias.** *(pendiente)*
