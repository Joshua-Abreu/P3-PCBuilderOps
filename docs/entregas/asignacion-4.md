# Asignación 4 — Historial de auditoría: quién cambió qué y cuándo

| Dato | Valor |
|------|-------|
| Bloque / semana | Bloque 5 · Semana 12 |
| Puntos | 5 · Individual |
| Fecha límite | La que indica la plataforma virtual. Tarde: hasta 24 h más con 50 % de penalidad; luego no se acepta. |
| Cómo se entrega | `git tag asignacion-4` + `git push origin --tags`; URL del repo y `asignacion-4` en Moodle. Última asignación del cuatrimestre. |
| Rama(s) | `docs/asignacion-4` (entrega y criterios, antes de programar); luego la rama del historial de auditoría |
| Estado | ⏳ En curso |

> *Se construye la Pieza 6 del Core: el historial de auditoría. Hace con los datos de la aplicación lo que `git log` hace con el repositorio: guardar quién cambió qué, cuándo y qué había antes.*

## 1. Alcance

### 1.1 Historial de auditoría

| ID | Requisito | Criterio de aceptación | Criterios |
|----|-----------|------------------------|-----------|
| RF-AUD-01 | Cada registro guarda usuario que actuó, acción, entidad, identificador, fecha y hora, valor anterior y valor nuevo. | Un registro al que le falte cualquiera de esos datos no cumple. | CA-AUD-01 |
| RF-AUD-02 | Se audita toda resolución de solicitud de permiso. | Queda registrado quién aprobó o rechazó qué y cuándo. | CA-AUD-02, 03, 04 |
| RF-AUD-03 | Se audita toda subida y eliminación de documento (RF-DOC-06). | Ambas acciones generan registro. | CA-AUD-05, 06 |
| RF-AUD-04 | Se audita todo cambio de rol y toda desactivación o reactivación de usuario (RF-CA-08, RF-CA-20). | El registro muestra el valor anterior y el nuevo. | CA-AUD-07, 08 |
| RF-AUD-05 | Se audita todo restablecimiento de contraseña (RF-CA-13). | Identifica si lo inició el propio usuario o un Administrador. Nunca guarda la contraseña ni el código. | CA-AUD-09 a 12 |
| RF-AUD-06 | El registro no se modifica ni se elimina desde la aplicación. | No existe ninguna operación de edición ni de borrado sobre él. | CA-AUD-13 |
| RF-AUD-07 | Consulta filtrable por usuario, por entidad y por rango de fechas. | Reservada al Administrador; un Estándar recibe rechazo. | CA-AUD-14 a 17 |

Aplican además RD-06 (la consulta se rechaza también si se invoca a mano), RD-07 (un filtro inválido produce un rechazo controlado), RD-09 (el historial sobrevive a reiniciar y a `docker compose down`), RD-11 (fecha y hora con el mismo criterio que el resto del sistema) y RD-03 (el Core no depende del módulo al auditar). Criterios CA-AUD-18 a 20.

Los criterios comprobables están en [`docs/criterios-auditoria.md`](../criterios-auditoria.md) y se escriben **antes** de programar.

README, sección **Auditoría**: cómo provocar cada evento y cómo consultar el historial con cada filtro.

### 1.2 Pruebas sobre la auditoría

- Una prueba por cada tipo de evento auditado (RF-AUD-02 a 05) que verifica los campos del registro, incluidos el valor anterior y el nuevo.
- Una prueba de que un Estándar no consulta el historial (RF-AUD-07).
- Una prueba de que ninguna operación expuesta modifica o borra registros (RF-AUD-06).
- Sin levantar la aplicación (RD-12), con preparar, actuar y verificar, independientes y con nombre que dice el comportamiento.
- Un único comando en la sección «Pruebas» del README ejecuta toda la suite desde un clon limpio: `dotnet test P3-PCBuilderOps.slnx`.

### 1.3 Bitácora con el agente y paralelo con git

`docs/bitacora-asignacion-4.md`, incluida en el tag:
- al menos una tarea delegada al agente: qué se le pidió y qué devolvió;
- al menos un caso en que se equivocó, cómo se detectó y cómo se corrigió, con evidencia de la sesión;
- una tabla que compara un registro de auditoría de la aplicación con un commit del repositorio, campo por campo: qué campo corresponde a autor, mensaje, fecha, identificador del commit y diff, y qué guarda uno que el otro no.

### Fuera de alcance (no implementar todavía)
- Auditar las transiciones de la orden de ensamble y el reporte de tiempo promedio de ensamble por técnico (`docs/requisitos/negocio.md`). La asignación solo exige eventos del Core; queda para el proyecto final.
- Una pantalla de auditoría en el frontend.

## 2. Rúbrica

| Criterio | Puntos | Qué se necesita para "Logrado" |
|----------|--------|--------------------------------|
| Eventos auditados RF-AUD-01 a 05 | 1.5 | Se auditan resolución de solicitudes, subida y eliminación de documentos, cambio de rol, restablecimiento de contraseña (propio y forzado por un Administrador), desactivación y reactivación de usuarios. Cada registro trae usuario, acción, entidad, identificador, fecha y hora, valor anterior y valor nuevo, y ninguno guarda contraseñas ni códigos. |
| Consulta e inmutabilidad RF-AUD-06 y 07 | 1.0 | El Administrador filtra por usuario, por entidad y por rango de fechas, con resultados distintos y correctos; el Estándar recibe rechazo; no existe ninguna operación de edición ni de borrado sobre el historial. |
| Pruebas sobre la auditoría | 1.0 | Una prueba por tipo de evento que verifica los campos (incluidos el valor anterior y el nuevo), una de que un Estándar no consulta, y una de que nada modifica o borra registros. Un comando del README las ejecuta y la suite pasa. |
| Bitácora y paralelo con git | 1.5 | Qué se pidió, qué devolvió, al menos un error del agente detectado y corregido con evidencia de la sesión, y el paralelo campo por campo entre un registro de auditoría y un commit. |
| **Total** | **5.0** | |

## 3. Qué revisa el maestro exactamente

1. `git clone` y `git checkout asignacion-4`.
2. El README: cómo provocar cada evento y cómo consultar con cada filtro, seguido al pie de la letra.
3. Aprobar y rechazar una solicitud, subir y eliminar un documento, cambiar un rol, desactivar y reactivar un usuario, y restablecer una contraseña como el propio usuario y como Administrador; en cada caso, el registro resultante con todos sus campos.
4. Que ningún registro contiene una contraseña ni un código de recuperación.
5. La consulta como Administrador con cada filtro y con un rango de fechas, y como Estándar (rechazo).
6. Que no hay en el código ninguna operación que edite o borre registros del historial.
7. `docker compose down` (sin `-v`) y volver a levantar: el historial sigue ahí.
8. El comando de la sección «Pruebas» del README, ejecutado tal como está escrito.
9. `docs/bitacora-asignacion-4.md`, incluida la tabla de paralelo con el historial de git.

## 4. Decisiones y dudas de esta entrega

- **El registro se escribe en el mismo `SaveChanges` que el cambio.** Un `IAuditor` del Core agrega el registro al `DbContext` de la operación sin guardar, como `INotificador`. Si el cambio falla, no queda registro; si el registro falla, no queda el cambio. Todos los eventos exigidos viven en el Core y comparten `AppDbContext`.
- **Valor anterior y valor nuevo como objetos JSON** con solo los campos que cambian (`{"rol":"Estandar"}` → `{"rol":"Administrador"}`). Nunca vacíos: en una creación el anterior es `{"estado":"NoExistia"}`.
- **RD-03:** `IAuditor` recibe la entidad como texto (`"SolicitudPermiso"`, `"Documento"`, `"Usuario"`), así el Core no conoce tipos del módulo y el módulo podría auditar sin que el Core cambie.
- **Inmutabilidad en tres capas:** la entidad sin setters públicos, un servicio que solo registra y consulta, y un interceptor de EF Core que rechaza modificar o borrar un registro.
- **El filtro de fechas reutiliza `RangoFechas`** (incluye completo el día de `hasta` y acepta los bordes del calendario, Hallazgo 3 de la Asignación 3).
- **Se audita también el cambio de la propia contraseña** (`POST /api/auth/cambiar-password`), aunque el PDF solo nombra el restablecimiento: es el mismo tipo de evento y no auditarlo dejaría un cambio de credenciales sin rastro.
- **El restablecimiento con el código recuerda quién lo inició.** Hoy el token de recuperación no guarda si lo pidió el usuario («olvidé mi contraseña») o un Administrador (forzado), y el restablecimiento forzado no recibe qué Administrador lo hizo. Ambos datos se agregan para cumplir RF-AUD-05.

## 5. Checklist de estado

Marcar cada punto solo después de verificarlo contra el código en ejecución.

- [ ] Entrega y criterios (`docs/criterios-auditoria.md`) fusionados antes del primer commit del historial
- [ ] Registro de auditoría con todos los campos de RF-AUD-01 (CA-AUD-01)
- [ ] Resolución de solicitudes auditada (CA-AUD-02 a 04)
- [ ] Subida y eliminación de documentos auditadas (CA-AUD-05, 06)
- [ ] Cambio de rol, desactivación y reactivación auditados (CA-AUD-07, 08)
- [ ] Restablecimiento propio y forzado, y cambio de contraseña, sin secretos (CA-AUD-09 a 12)
- [ ] Inmutabilidad (CA-AUD-13)
- [ ] Consulta con filtros, solo Administrador, filtros inválidos `400` (CA-AUD-14 a 17)
- [ ] Persistencia, UTC y RD-03 (CA-AUD-18 a 20)
- [ ] Pruebas que citan cada criterio; la suite pasa con el comando del README
- [ ] README: sección Auditoría y guía de verificación; peticiones en `API.http`
- [ ] Revisión acumulada: el proyecto levanta con el README y siguen pasando los seis frentes
- [ ] `docs/bitacora-asignacion-4.md` con el error del agente y el paralelo con git
- [ ] `AGENTS.md` sección 0 actualizada; etiqueta `asignacion-4` creada por el usuario
