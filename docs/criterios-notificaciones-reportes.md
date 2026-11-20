# Criterios de aceptación — Notificaciones, cola de correos y reportes


Requisitos que cubre: `RF-NOT-01` a `RF-NOT-13` y `RF-REP-01` a `RF-REP-05` (`docs/requisitos/core.md`, secciones 6 y 7), con `RF-NEG-07` y `RF-NEG-08` (`docs/requisitos/negocio.md`).

## 0. Cómo se comprueban

- Entorno: copiar `.env.example` a `.env` y ejecutar `docker compose up --build`. La API queda en `http://localhost:5206`.
- El servidor de correo de pruebas (Mailpit) viene en el `docker-compose.yml`. Recibe todos los correos sin enviarlos a internet y los muestra en `http://localhost:8025`. Para simular un servidor de correo caído se ejecuta `docker compose stop mailpit`, y para recuperarlo, `docker compose start mailpit`.
- El procesador de la cola se ejecuta aparte con `docker compose run --rm despachador`: procesa una vez y termina. La API también procesa la cola cada 30 segundos (`EMAIL_WORKER_INTERVALO_SEGUNDOS`).
- Máximo de intentos por correo: **3** (`CORREO_MAX_INTENTOS`).
- Cuentas: el Administrador inicial de `.env.example` y usuarios Estándar registrados y activados con el enlace que llega a Mailpit. Un segundo Administrador se crea con `POST /api/usuarios` (rol `Administrador`).
- Cada criterio indica la petición HTTP con la que se comprueba. Las peticiones quedan numeradas en `src/API/API.http`. Además, cada criterio que no depende de la red tiene una prueba automatizada que cita su ID (`dotnet test`).
- Toda respuesta de error usa `{"mensaje": "..."}` (RD-07) y nunca expone trazas, rutas internas ni errores de SQL (RD-08).
- Las fechas de los reportes son `yyyy-MM-dd` en UTC, y `desde` y `hasta` incluyen el día completo.

Formato: **Dado** (estado previo) · **Cuando** (acción) · **Entonces** (resultado observable) · **Comprobación** (cómo verlo).

---

## 1. Notificaciones: bandeja interna (Pieza 4)

### CA-NOT-01 · RF-NOT-01 — Crear una solicitud notifica solo a los Administradores
- **Dado:** dos Administradores activos (A1, A2), un Administrador desactivado (A3) y dos Estándar (E1, E2), ninguno con notificaciones.
- **Cuando:** E1 crea una solicitud de permiso (`POST /api/permisos/solicitudes`).
- **Entonces:**
  - A1 y A2 tienen exactamente una notificación nueva, sin leer, que nombra a E1 y el permiso solicitado.
  - E1, E2 y A3 no tienen ninguna.
- **Comprobación:** `GET /api/notificaciones` con el token de cada uno.

### CA-NOT-02 · RF-NOT-02 — Aprobar una solicitud notifica al solicitante
- **Dado:** una solicitud pendiente de E1.
- **Cuando:** un Administrador la aprueba (`POST /api/permisos/solicitudes/{id}/aprobar`).
- **Entonces:**
  - E1 tiene una notificación nueva sin leer que dice que la solicitud fue **aprobada** y nombra el permiso.
  - Ningún otro usuario recibe una notificación por esta resolución.
- **Comprobación:** `GET /api/notificaciones` con E1 y con otro usuario.

### CA-NOT-03 · RF-NOT-02 — Rechazar una solicitud notifica al solicitante con el motivo
- **Dado:** una solicitud pendiente de E1.
- **Cuando:** un Administrador la rechaza con el motivo «Falta experiencia en taller» (`POST /api/permisos/solicitudes/{id}/rechazar`).
- **Entonces:** E1 tiene una notificación nueva sin leer que dice que fue **rechazada** e incluye el texto «Falta experiencia en taller».
- **Comprobación:** `GET /api/notificaciones` con E1.

### CA-NOT-04 · RF-NOT-03, RF-NEG-07 — Un evento del negocio notifica al cliente
- **Dado:** una orden de E1 en `EnArmado`, con un técnico asignado.
- **Cuando:** el técnico la pasa a `EnPruebas` (`POST /api/taller/ordenes/{id}/pasar-a-pruebas`), registra una prueba aprobada y la entrega (`POST /api/taller/ordenes/{id}/entregar`).
- **Entonces:**
  - E1 tiene dos notificaciones nuevas, una por cada cambio, con el código de seguimiento de la orden y el estado nuevo (`EnPruebas` y `Entregada`).
  - Ningún otro cliente recibe notificaciones por esa orden.
- **Comprobación:** `GET /api/notificaciones` con E1. Los pasos para provocarlo están en la sección «Notificaciones» del README.

### CA-NOT-05 · RF-NOT-03 — Una transición rechazada no notifica
- **Dado:** una orden de E1 en `Cotizada`.
- **Cuando:** se intenta entregarla directamente (transición prohibida).
- **Entonces:** la API responde `409`, la orden sigue en `Cotizada` y E1 no recibe ninguna notificación nueva.
- **Comprobación:** `GET /api/notificaciones` con E1 antes y después.

### CA-NOT-06 · RF-NOT-04 — Cada usuario ve solo sus notificaciones, leídas y no leídas
- **Dado:** E1 con tres notificaciones (una leída y dos sin leer) y E2 con una.
- **Cuando:** E1 consulta `GET /api/notificaciones`.
- **Entonces:**
  - Recibe exactamente sus tres notificaciones, de la más reciente a la más antigua.
  - Cada una trae `esLeida`, y la respuesta trae el total `noLeidas = 2`.
  - Ninguna es de E2.
  - Con `?soloNoLeidas=true` recibe solo las dos sin leer.
- **Comprobación:** la misma petición con los tokens de E1 y de E2.

### CA-NOT-07 · RF-NOT-05 — Marcar como leída una notificación propia
- **Dado:** una notificación sin leer de E1.
- **Cuando:** E1 la marca (`PATCH /api/notificaciones/{id}/leida`).
- **Entonces:**
  - La API responde `200`, la notificación pasa a `esLeida = true` con su fecha de lectura y `noLeidas` baja en 1.
  - Marcarla otra vez responde `200` sin cambiar la fecha de lectura.
- **Comprobación:** `GET /api/notificaciones` con E1.

### CA-NOT-08 · RF-NOT-05 — Marcar la notificación de otro usuario se rechaza
- **Dado:** una notificación sin leer de E1.
- **Cuando:** E2, o un Administrador, intenta marcarla con su propio token.
- **Entonces:**
  - La API responde `404` con `{"mensaje": "La notificación no existe."}`, el mismo mensaje que con un id inexistente, para no revelar que existe.
  - La notificación de E1 sigue sin leer.
- **Comprobación:** `PATCH` con el token de E2 y luego `GET /api/notificaciones` con E1.

### CA-NOT-09 · RD-06 — Sin sesión no hay notificaciones
- **Dado:** ninguna sesión, o un token de una sesión cerrada con `logout`.
- **Cuando:** se llama a cualquier endpoint de `/api/notificaciones`.
- **Entonces:** la API responde `401` y no devuelve ni modifica datos.
- **Comprobación:** las peticiones sin cabecera `Authorization` y con el token revocado.

## 2. Notificaciones: canal de correo

### CA-NOT-10 · RF-NOT-06 — Toda notificación llega por los dos canales
- **Dado:** E1 con el canal de correo activo (valor por defecto).
- **Cuando:** se dispara cualquiera de los eventos de CA-NOT-01 a CA-NOT-04.
- **Entonces:**
  - Cada destinatario tiene la notificación en su bandeja interna.
  - Además se encola **un** correo a su dirección, con el mismo asunto y el mismo contenido; con Mailpit encendido, llega a `http://localhost:8025`.
- **Comprobación:** `GET /api/notificaciones`, `GET /api/correos` (Administrador) y la bandeja de Mailpit.

### CA-NOT-11 · RF-NOT-07 — Desactivar el correo conserva el canal interno
- **Dado:** E1 desactiva su canal de correo (`PUT /api/notificaciones/preferencias` con `{"correoActivo": false}`), y la consulta `GET /api/notificaciones/preferencias` devuelve `false`.
- **Cuando:** E1 crea una solicitud y un Administrador la rechaza.
- **Entonces:**
  - E1 tiene la notificación del rechazo en su bandeja interna.
  - **No** se encola ningún correo nuevo a E1: la cantidad de correos de E1 en `GET /api/correos` no cambia.
  - Al reactivar el canal (`true`), la siguiente notificación vuelve a encolar su correo.
- **Comprobación:** `GET /api/correos?destinatario=<correo de E1>` antes y después, y la bandeja de Mailpit.

### CA-NOT-12 · RF-NOT-07 — La preferencia es personal
- **Dado:** E1 con el correo desactivado y E2 con el correo activo.
- **Cuando:** ocurre un evento que notifica a los dos (por ejemplo, ambos son Administradores y alguien crea una solicitud).
- **Entonces:** se encola correo solo para E2, y los dos tienen la notificación interna.
- **Comprobación:** `GET /api/correos`.

### CA-NOT-13 · RF-NOT-07 — Los correos de la cuenta no dependen de la preferencia
- **Dado:** un usuario con el canal de correo desactivado.
- **Cuando:** pide recuperar su contraseña (`POST /api/auth/olvide-password`).
- **Entonces:** el correo de recuperación se encola igual. La activación y la recuperación son correos de seguridad de Control de acceso, no notificaciones, y no aparecen en la bandeja interna.
- **Comprobación:** `GET /api/correos`.

## 3. Cola de correos

### CA-COLA-01 · RF-NOT-08 — La operación no espera al servidor de correo
- **Dado:** Mailpit detenido (`docker compose stop mailpit`).
- **Cuando:** un visitante se registra (`POST /api/auth/registro`) o un Estándar crea una solicitud de permiso.
- **Entonces:**
  - La operación responde con éxito (`200`/`201`) sin errores de SMTP.
  - El usuario o la solicitud quedan guardados y el correo queda `Pendiente` con `intentos = 0`.
- **Comprobación:** la respuesta de la petición y `GET /api/correos`.

### CA-COLA-02 · RF-NOT-09 — Un proceso independiente envía los pendientes
- **Dado:** correos `Pendiente` en la cola y Mailpit encendido.
- **Cuando:** se ejecuta `docker compose run --rm despachador`, sin repetir el registro ni la solicitud que crearon los correos.
- **Entonces:**
  - El proceso termina por sí solo e informa cuántos correos envió.
  - Los correos pasan a `Enviado` con `fechaEnvioUtc` y aparecen en Mailpit.
- **Comprobación:** la salida del comando, `GET /api/correos` y la bandeja de Mailpit.

### CA-COLA-03 · RF-NOT-10 — Reintentos limitados y luego `Fallido`
- **Dado:** Mailpit detenido, `CORREO_MAX_INTENTOS = 3` y un correo nuevo `Pendiente`.
- **Cuando:** el procesador se ejecuta 4 veces seguidas.
- **Entonces:**
  - Después de la 1.ª y la 2.ª ejecución, el correo sigue `Pendiente`, con `intentos` igual a 1 y 2, `fechaUltimoIntentoUtc` actualizada y `ultimoError` no vacío.
  - Después de la 3.ª queda `Fallido` con `intentos = 3`.
  - La 4.ª no lo intenta: `intentos` sigue en 3 y el correo sigue en la cola.
- **Comprobación:** `GET /api/correos?estado=Fallido` después de cada ejecución.

### CA-COLA-04 · RF-NOT-10 — Un correo que se recupera a tiempo se envía
- **Dado:** un correo `Pendiente` con `intentos = 1` por un fallo anterior.
- **Cuando:** se enciende Mailpit (`docker compose start mailpit`) y se ejecuta el procesador.
- **Entonces:** el correo pasa a `Enviado`, conserva `intentos = 2` (contando el envío correcto) y llega a Mailpit.
- **Comprobación:** `GET /api/correos` y Mailpit.

### CA-COLA-05 · RF-NOT-11 — El Administrador consulta la cola
- **Dado:** correos en los tres estados.
- **Cuando:** un Administrador consulta `GET /api/correos`, y luego `?estado=Pendiente|Enviado|Fallido`.
- **Entonces:**
  - Cada correo muestra destinatario, asunto, estado, intentos, `fechaCreacionUtc`, `fechaUltimoIntentoUtc`, `fechaEnvioUtc` y `ultimoError`, del más reciente al más antiguo.
  - El filtro devuelve solo los del estado pedido.
  - La respuesta **no** incluye el cuerpo del correo, porque contiene enlaces de activación y recuperación.
  - `ultimoError` es un texto breve, sin trazas ni credenciales (RD-08).
- **Comprobación:** la petición con el token del Administrador.

### CA-COLA-06 · RF-NOT-11, RD-06 — Un Estándar no ve la cola
- **Dado:** un Estándar con sesión.
- **Cuando:** consulta `GET /api/correos`.
- **Entonces:** la API responde `403` con `{"mensaje": "..."}`. Sin sesión responde `401`.
- **Comprobación:** la petición con el token del Estándar y sin token.

### CA-COLA-07 · RF-NOT-12 — Ejecutar el procesador dos veces no duplica
- **Dado:** dos correos `Pendiente` y Mailpit encendido y vacío.
- **Cuando:** se ejecuta `docker compose run --rm despachador` dos veces seguidas.
- **Entonces:**
  - La primera ejecución informa 2 enviados y la segunda 0.
  - Mailpit tiene exactamente 2 correos, uno por destinatario.
  - Ningún correo `Enviado` cambia su `fechaEnvioUtc`.
- **Comprobación:** la salida de cada ejecución y Mailpit.

### CA-COLA-08 · RF-NOT-12 — Dos procesadores a la vez no duplican
- **Dado:** correos `Pendiente`, con el procesador de la API y el despachador independiente corriendo al mismo tiempo.
- **Cuando:** ambos toman la cola en el mismo momento.
- **Entonces:** cada correo se envía una sola vez. Un procesador reclama el correo de forma atómica antes de enviarlo, y el otro lo salta.
- **Comprobación:** prueba automatizada con dos procesadores concurrentes sobre la misma base; el remitente falso cuenta un envío por correo.

### CA-COLA-09 · RF-NOT-13, RD-10 — Credenciales SMTP solo en variables de entorno
- **Dado:** el repositorio clonado.
- **Cuando:** se busca en todo el historial (`git log -p --all`) y en los archivos versionados.
- **Entonces:**
  - No aparecen credenciales SMTP reales ni la clave de la API externa.
  - `SmtpEmailSender` y el despachador leen `SMTP_HOST`, `SMTP_PORT`, `SMTP_USER`, `SMTP_PASS` y `SMTP_FROM` del entorno, sin valores de respaldo para la contraseña.
  - `.env.example` solo trae los valores de Mailpit, que no requiere usuario ni contraseña.
- **Comprobación:** `git log -p --all -S "SMTP_PASS"` y la lectura de `.env.example`.

## 4. Reportes con agregación (Pieza 5)

### Reporte del Core · RF-REP-01
`GET /api/reportes/solicitudes?desde=&hasta=` responde la pregunta **«¿cuántas solicitudes de permiso hay en cada estado, mes por mes?»**. Cada fila es `{ periodo: "2026-11", estado, cantidad }`, y la respuesta incluye el `total`. Una solicitud cae en el período de su `FechaCreacionUtc`.

### Reporte del negocio · RF-REP-02, RF-NEG-08
`GET /api/reportes/fabricantes?desde=&hasta=` responde la pregunta **«¿qué fabricantes concentran más piezas y más valor en las órdenes del período?»**, que sirve para decidir con qué marcas negociar stock. Cada fila es `{ fabricante, unidades, valorUsd, ordenes }`, ordenada por `unidades` de mayor a menor:
- `unidades`: suma de `Cantidad`;
- `valorUsd`: suma de `Cantidad × PrecioUnitario`;
- `ordenes`: órdenes distintas en que aparece el fabricante.

Una orden cae en el período de su `FechaCreacionUtc`.

### CA-REP-01 · RF-REP-01, RF-REP-03 — El reporte del Core agrupa
- **Dado:** 5 solicitudes de este mes: 2 pendientes, 2 aprobadas y 1 rechazada.
- **Cuando:** un Administrador consulta el reporte con un rango que cubre este mes.
- **Entonces:**
  - Recibe 3 filas para este mes (`Pendiente` 2, `Aprobada` 2, `Rechazada` 1) con `total = 5`.
  - Son menos filas que las 5 solicitudes, y ninguna fila trae ids ni datos de una solicitud individual.
- **Comprobación:** la petición de `API.http` y la prueba automatizada.

### CA-REP-02 · RF-REP-02, RF-REP-03 — El reporte del negocio agrupa por fabricante
- **Dado:** 3 órdenes: la 1 con 1 CPU AMD y 2 RAM Kingston; la 2 con 1 CPU AMD; la 3 con 1 GPU NVIDIA.
- **Cuando:** un Administrador consulta el reporte con un rango que las cubre.
- **Entonces:**
  - Recibe 3 filas, ordenadas así: AMD (`unidades` 2, `ordenes` 2), Kingston (`unidades` 2, `ordenes` 1) y NVIDIA (`unidades` 1, `ordenes` 1).
  - Cada `valorUsd` es la suma de cantidad × precio unitario guardado en la orden.
  - Son menos filas que los 5 detalles que resume.
- **Comprobación:** la petición de `API.http` y la prueba automatizada.

### CA-REP-03 · RF-REP-03 — La agregación la hace el motor de datos
- **Dado:** cualquiera de los dos reportes.
- **Cuando:** se revisa su implementación.
- **Entonces:**
  - La agrupación y las sumas se traducen a SQL (`GroupBy` de EF Core, ejecutado en SQL Server).
  - El servicio no carga la lista completa de registros para resumirla en memoria, y la interfaz no agrega nada.
- **Comprobación:** revisión del servicio del reporte y la prueba automatizada.

### CA-REP-04 · RF-REP-04 — Un Estándar solo obtiene sus datos
- **Dado:** E1 con 2 solicitudes y 1 orden, y E2 con 3 solicitudes y 2 órdenes.
- **Cuando:** E1 consulta los dos reportes con un rango que lo cubre todo.
- **Entonces:**
  - El reporte de solicitudes suma `total = 2`: solo cuenta las suyas.
  - El de fabricantes solo resume los componentes de su orden.
  - Agregar a la URL un parámetro con el id de E2 (`usuarioId`, `clienteId`) no cambia el resultado.
  - Un Administrador, con el mismo rango, ve los totales de todos.
- **Comprobación:** la misma petición con E1, con E1 y el parámetro extra, y con el Administrador.

### CA-REP-05 · RF-REP-05 — El rango de fechas cambia el resultado
- **Dado:** los mismos datos de CA-REP-01 y CA-REP-02, todos creados hoy.
- **Cuando:** se consulta cada reporte con un rango que incluye hoy y luego con un rango que termina ayer.
- **Entonces:**
  - El primer rango devuelve las filas de los criterios anteriores; el segundo, ninguna fila y `total = 0`.
  - Las pruebas automatizadas comprueban además datos repartidos en dos meses, con los dos meses dando resultados distintos y no vacíos.
- **Comprobación:** las dos peticiones de `API.http` y las pruebas.

### CA-REP-06 · RF-REP-05, RD-07 — El rango es obligatorio y válido
- **Dado:** un usuario con sesión.
- **Cuando:** consulta un reporte sin `desde` o sin `hasta`, con una fecha mal escrita o con `desde` posterior a `hasta`.
- **Entonces:** la API responde `400` con `{"mensaje": "..."}`, que explica el problema, y sin datos.
- **Comprobación:** las peticiones de `API.http`.

### CA-REP-07 · RD-06 — Sin sesión no hay reportes
- **Dado:** ninguna sesión.
- **Cuando:** se consulta cualquier reporte.
- **Entonces:** la API responde `401`.
- **Comprobación:** la petición sin token.

## 5. Lo que no se puede romper (revisión acumulada)

### CA-ACU-01 — Lo anterior sigue funcionando
- **Dado:** el proyecto levantado desde la etiqueta `practica-5` con el comando de la sección «Contenedor» del README.
- **Cuando:** se repiten las pruebas de las entregas anteriores.
- **Entonces:**
  - `dotnet test` pasa completo.
  - Funcionan los seis frentes:
    1. registro con activación, con el correo enviado por la cola y visible en Mailpit;
    2. login, logout y `403` a un Estándar en una operación de Administrador;
    3. recuperación de contraseña;
    4. solicitudes que se crean, aprueban y rechazan, con las transiciones inválidas rechazadas;
    5. una orden válida que avanza, y `Cotizada → Entregada` rechazada;
    6. un documento que sobrevive a `docker compose down` (sin `-v`) y `up`.
- **Comprobación:** la guía de verificación del README, de principio a fin.
