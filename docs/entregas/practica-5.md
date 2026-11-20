# Práctica 5 — Delegas al agente notificaciones y reportes

| Dato | Valor |
|------|-------|
| Bloque / semana | Bloque 5 · Semana 10 |
| Puntos | 8 · Individual |
| Fecha límite | La que indica la plataforma virtual. Tarde: hasta 24 h más con 50 % de penalidad; luego no se acepta. |
| Cómo se entrega | `git tag practica-5` + `git push origin --tags`; URL del repo y `practica-5` en Moodle. Última práctica del cuatrimestre. |
| Rama(s) | `docs/criterios-practica-5` (contexto y criterios, **se fusiona primero**); luego las ramas del motor de notificaciones, la cola y los reportes |
| Estado | ⏳ En curso |

> *Por primera vez se delega al agente una funcionalidad completa y nueva. Antes de delegar se escriben el contexto que necesita y los criterios con los que se aceptará su trabajo.*

## 1. Alcance

### 1.1 Contexto y criterios, antes de delegar

| Pide | Dónde | Cómo se cumple |
|------|-------|----------------|
| Archivo de contexto en la raíz: propósito, lenguaje y framework, cómo correr, cómo probar, convenciones y restricciones (qué no tocar, nunca escribir credenciales ni leer `.env`) | `AGENTS.md` (Claude Code lo importa desde `CLAUDE.md`) | Propósito al inicio; stack en el encabezado y la sección 5; correr y probar en la sección 7; convenciones en las secciones 3, 5 y 6; restricciones en la sección 11 |
| Criterios comprobables (dado / cuando / entonces) que citan RF-NOT y RF-REP y cubren las dos funcionalidades | `docs/criterios-notificaciones-reportes.md` | 30 criterios con su comprobación |
| El commit de contexto y criterios es **anterior** al primer commit del motor de notificaciones | `git log` | La rama `docs/criterios-practica-5` se fusiona a `main` antes de crear la rama del motor |

La cola mínima de la Práctica 1 (`CorreoEnCola`, `ColaCorreoService`, `EmailQueueWorker`, `EmailDispatcher`) es anterior a este commit. El PDF la reconoce: «desde la Práctica 1 existe la versión mínima de la cola». Lo nuevo de la práctica (bandeja interna, reintentos, estado fallido, consulta de la cola y reportes) empieza después.

### 1.2 Notificaciones (Pieza 4)

Todas las operaciones requieren sesión válida (RD-06).

| ID | Requisito | Criterio de aceptación | Criterios |
|----|-----------|------------------------|-----------|
| RF-NOT-01 | Crear una solicitud de permiso notifica a los Administradores. | Todos los Administradores la reciben; nadie más. | CA-NOT-01 |
| RF-NOT-02 | Resolver una solicitud notifica al solicitante. | Indica si fue aprobada o rechazada y, si fue rechazada, el motivo. | CA-NOT-02, 03 |
| RF-NOT-03 | Un evento del negocio dispara una notificación. | Descrito en el README y reproducible con sus pasos. | CA-NOT-04, 05 |
| RF-NOT-04 | Consultar las propias, distinguiendo leídas de no leídas. | Un usuario no ve las de otro. | CA-NOT-06, 09 |
| RF-NOT-05 | Marcar como leída. | Marcar la de otro se rechaza. | CA-NOT-07, 08 |
| RF-NOT-06 | Dos canales: interno y correo. | Bandeja interna y correo. | CA-NOT-10 |
| RF-NOT-07 | Desactivar el correo sin perder el interno. | Aparece la interna y no se encola correo. | CA-NOT-11, 12, 13 |

### 1.3 Cola de correos

| ID | Requisito | Criterio de aceptación | Criterios |
|----|-----------|------------------------|-----------|
| RF-NOT-08 | Se encolan; no se envían dentro de la operación. | La operación termina bien aunque el SMTP no responda. | CA-COLA-01 |
| RF-NOT-09 | Un proceso independiente envía los pendientes. | Corre sin que el flujo original vuelva a correr. | CA-COLA-02 |
| RF-NOT-10 | Reintentos limitados y luego fallido. | Ni infinito ni sin rastro; máximo declarado en el README. | CA-COLA-03, 04 |
| RF-NOT-11 | Estado, intentos, fechas y último error. | El Administrador consulta la cola; el Estándar recibe rechazo. | CA-COLA-05, 06 |
| RF-NOT-12 | Un enviado no se reenvía. | El procesador dos veces seguidas no duplica. | CA-COLA-07, 08 |
| RF-NOT-13 | Credenciales SMTP en variables de entorno. | Ni en el repositorio ni en el historial (RD-10). | CA-COLA-09 |

README, sección **Notificaciones**: cómo provocar cada evento, cómo ejecutar el procesador, cómo provocar un fallo de envío y el máximo de intentos.

### 1.4 Reportes con agregación (Pieza 5)

| ID | Requisito | Criterio de aceptación | Criterios |
|----|-----------|------------------------|-----------|
| RF-REP-01 | Reporte del Core: solicitudes por estado y período. | Valores agrupados, no el listado. | CA-REP-01 |
| RF-REP-02 | Reporte del negocio. | Responde una pregunta real del dominio. | CA-REP-02 |
| RF-REP-03 | Agregación real. | Menos filas que los registros que resume. | CA-REP-01, 02, 03 |
| RF-REP-04 | Respetan el rol. | Un Estándar no obtiene datos de otros. | CA-REP-04 |
| RF-REP-05 | Filtro por rango de fechas. | Dos rangos distintos dan resultados distintos. | CA-REP-05, 06 |

README, sección **Reportes**: qué pregunta responde el reporte del negocio y cómo consultar ambos con un rango de fechas.

### 1.5 Revisión acumulada

Se copia `.env.example` a `.env` y se arranca con el comando del README. Se comprueban seis frentes: registro con activación por la cola; login, logout y 403; recuperación de contraseña; solicitudes de permiso; operación válida y transición prohibida del negocio; un documento que sobrevive a `docker compose down` (CA-ACU-01).

### Fuera de alcance (no implementar todavía)
- Auditoría (Pieza 6), incluida la de documentos (RF-DOC-06).
- Historial de fechas por estado de la orden y el reporte de tiempo promedio de ensamble por técnico.
- Notificaciones en tiempo real (WebSocket/SignalR), plantillas HTML de correo y la bandeja en el frontend.
- Documentos adjuntos a las órdenes (RF-NEG-10).

## 2. Rúbrica

| Criterio | Puntos | Qué se necesita para "Logrado" |
|----------|--------|--------------------------------|
| Archivo de contexto y criterios | 1.5 | El contexto declara propósito, lenguaje, cómo correr y probar, convenciones y restricciones (incluido no tocar secretos). Los criterios cubren notificaciones y reportes, cada uno con su condición y su comprobación, y citan los RF. Su commit es anterior al primer commit de notificaciones. |
| Notificaciones RF-NOT-01 a 07 | 2.0 | Crear una solicitud notifica solo a los Administradores; resolverla notifica al solicitante con el resultado y el motivo; el evento del negocio notifica y se provoca con el README; cada usuario consulta y marca solo las suyas; cada notificación llega por los dos canales; con el correo desactivado queda la interna y no se encola correo. |
| Cola RF-NOT-08 a 13 | 1.5 | Los correos se encolan y los envía un procesador independiente; un fallo se reintenta el máximo del README y queda fallido con su último error; el Administrador consulta la cola; el procesador dos veces no duplica; credenciales SMTP en variables de entorno. |
| Reportes RF-REP-01 a 05 | 1.5 | Uno del Core y uno del negocio con valores agrupados; el del negocio responde una pregunta real; un Estándar solo ve lo suyo; dos rangos distintos dan resultados distintos. |
| Revisión acumulada | 1.5 | Funcionan los seis frentes. |
| **Total** | **8.0** | |

## 3. Qué revisa el maestro exactamente

1. `git clone` y `git checkout practica-5`.
2. El archivo de contexto y `docs/criterios-notificaciones-reportes.md`, y en `git log` que su commit es anterior al primer commit del motor de notificaciones.
3. `.env.example` copiado a `.env` y el comando de la sección «Contenedor» del README: el proyecto levanta.
4. Crear una solicitud con un Estándar y comprobar la notificación de los Administradores; resolverla y comprobar la del solicitante; provocar el evento del negocio.
5. Consultar y marcar notificaciones propias, y comprobar que las de otro se rechazan. Desactivar el correo y comprobar que no se encola ninguno.
6. Provocar un fallo de envío según el README: reintentos, estado fallido y último error; consultar la cola como Administrador y como Estándar; ejecutar el procesador dos veces.
7. Los dos reportes con dos rangos de fechas distintos y con un Estándar.
8. Todo el historial, buscando credenciales SMTP o la clave de la API externa.
9. Los seis frentes de la revisión acumulada, arrancados desde el README.

## 4. Decisiones y dudas de esta entrega

- **Servidor de correo de pruebas (Mailpit) en `docker-compose.yml`.** Con `.env.example` ningún correo saldría, así que no se podría demostrar que el correo llega (RF-NOT-06), que no se duplica (RF-NOT-12) ni el contraste entre envío y fallo (RF-NOT-10). Apagarlo simula el fallo.
- **Despachador independiente en el contenedor** (`docker compose run --rm despachador`). El maestro debe poder ejecutar el procesador siguiendo el README sin instalar .NET.
- **Máximo de 3 intentos**, configurable con `CORREO_MAX_INTENTOS`.
- **Reclamo atómico del correo antes de enviarlo.** Así el procesador de la API y el despachador no envían dos veces el mismo correo.
- **Reporte del negocio: fabricantes por piezas y valor.** El tiempo promedio por técnico necesita las fechas de cada transición, que hoy no se guardan; queda para la Auditoría.
- **Los correos de activación y recuperación no son notificaciones.** Se encolan siempre, sin pasar por la preferencia ni por la bandeja: un usuario sin activar no tiene sesión para verlos.
- **Control de acceso deja de escribir en `CorreosEnCola` directamente** y encola con `IEncoladorCorreos`, la interfaz pública de Notificaciones (RD-01). Está separada de `IColaCorreoService` (despacho y consulta) para que quien encola no dependa del SMTP.
- **CA-REP-01 corregido a `Aplicada`.** El criterio esperaba una fila `Aprobada`, pero en este sistema una solicitud aprobada pasa a `Aplicada` en la misma operación (RF-GP-05), así que `Aprobada` nunca queda guardada. Lo detectó el agente al implementar el reporte. El usuario decidió corregir el criterio en un commit propio (`docs(criterios)`), visible en `git log`, en lugar de deformar el reporte.
- **Marcar la notificación de otro usuario responde 404**, igual que un id inexistente, para no revelar que existe.
- **La notificación se guarda con la operación que la origina.** `INotificador` agrega la notificación y su correo al mismo `SaveChanges` de la solicitud: si la operación falla, no queda aviso.
- **La preferencia vive en Notificaciones** (`PreferenciasNotificacion`), no en `Usuario`; sin fila, el correo está activo.

## 5. Checklist de estado

Marcar cada punto solo después de verificarlo contra el código en ejecución.

- [x] `AGENTS.md` y `docs/criterios-notificaciones-reportes.md` fusionados a `main` antes del primer commit del motor (PR #37, commit `340acb9`)
- [x] Cola: intentos, último error, estado `Fallido`, reserva atómica y consulta del Administrador (CA-COLA-01 a 08), con pruebas y verificada contra SQL Server y Mailpit en un contenedor aislado
- [x] Mailpit y despachador en `docker-compose.yml`
- [x] Bandeja interna, preferencia de correo y notificaciones de permisos (CA-NOT-01 a 03, 06 a 13), con pruebas y verificada contra SQL Server y Mailpit en un contenedor aislado
- [x] Evento del negocio: `EnPruebas` y `Entregada` (CA-NOT-04, 05), con pruebas y verificado contra SQL Server y Mailpit en un contenedor aislado
- [x] Reportes del Core y del negocio (CA-REP-01 a 07), con pruebas y verificados contra SQL Server en un contenedor aislado
- [x] Pruebas automatizadas que citan cada criterio
- [x] README: secciones Notificaciones y Reportes, y la guía de verificación (secciones 13 y 14)
- [ ] Búsqueda de credenciales en todo el historial
- [ ] Revisión acumulada: los seis frentes (CA-ACU-01)
- [ ] Commits citan sus IDs; PR con las cuatro secciones
- [ ] `AGENTS.md` sección 0 actualizada; etiqueta `practica-5` creada por el usuario
