# Asignación 2 — Gestión de permisos y su primera suite de pruebas

| Dato | Valor |
|------|-------|
| Bloque / semana | Bloque 3 · Semana 6 |
| Puntos | 5 · Individual |
| Se publica | Miércoles 14 de octubre de 2026 |
| Fecha límite | **Martes 20 de octubre de 2026, 11:59 p. m.** Tarde: hasta 24 h más con 50 % de penalidad; luego no se acepta. |
| Cómo se entrega | `git tag asignacion-2` + `git push origin --tags`; URL del repo y nombre de la etiqueta en Moodle («Asignación 2»). Se evalúa el punto etiquetado. |
| Rama(s) | `feature/gestion-permisos` |
| Estado | ⏳ En curso |

Tres cosas en la etiqueta: la pieza Gestión de permisos funcionando, una primera suite de pruebas sobre ella y la bitácora con el agente.

## 1. Alcance

### 1.1 Gestión de permisos funcionando

Transiciones válidas: `Pendiente → Aprobada → Aplicada` y `Pendiente → Rechazada`. Ninguna otra está permitida.

| ID | Requisito | Criterio de aceptación |
|----|-----------|------------------------|
| RF-GP-01 | Un usuario Estándar crea una solicitud de permiso. | La solicitud nace en estado Pendiente. |
| RF-GP-02 | Un Administrador consulta las solicitudes pendientes. | Un usuario Estándar no puede ver las solicitudes de otros usuarios. |
| RF-GP-03 | Un Administrador aprueba una solicitud pendiente. | Queda registrado quién aprobó y cuándo. |
| RF-GP-04 | Un Administrador rechaza una solicitud pendiente indicando el motivo. | Sin motivo escrito, el rechazo no se completa. |
| RF-GP-05 | Al aprobar, el sistema aplica el cambio de permiso automáticamente. | Tras la aprobación el solicitante puede ejecutar la acción que pidió, sin ninguna intervención manual adicional. |
| RF-GP-06 | Solo se permiten las transiciones de la tabla. | Aprobar dos veces la misma solicitud, o pasar de Rechazada a Aplicada, se rechaza y el estado no cambia. |
| RF-GP-07 | Nadie resuelve su propia solicitud. | Un Administrador que intenta aprobar o rechazar una solicitud creada por él mismo recibe un rechazo. |

Aplican además: **RD-04** (transiciones en un solo punto), **RD-06** (autorización en el servidor), **RD-07** (entrada inválida → rechazo controlado) y **RD-09** (las solicitudes sobreviven al reinicio). El README debe indicar cómo provocar cada criterio.

### 1.2 Suite de pruebas sobre Gestión de permisos

- Framework de pruebas del lenguaje, dentro del repositorio, ejercitando la pieza directamente sin levantar la aplicación (RD-12).
- Al menos una prueba de **transición válida** que verifique el estado resultante **y el dato registrado** (RF-GP-03 o RF-GP-04).
- Al menos una prueba de **transición prohibida** que verifique el rechazo **y que el estado no cambió** (RF-GP-06).
- Cada prueba: tres fases identificables (preparar, actuar, verificar), un solo comportamiento y un nombre que lo dice.
- Pruebas independientes: pasan en cualquier orden y por separado.
- Un único comando, documentado en el README bajo el título **Pruebas**, ejecuta toda la suite desde un clon limpio. La suite pasa.

### 1.3 Bitácora de sesión con el agente

`docs/bitacora-asignacion-2.md`, incluido en la etiqueta: al menos una tarea delegada al agente con qué se pidió, qué devolvió y **al menos un error del agente**, cómo se detectó y cómo se corrigió, con evidencia de la sesión. Una bitácora donde todo salió bien a la primera se devuelve.

### Fuera de alcance
- Auditoría de la resolución de solicitudes (RF-AUD-02): semana 14.
- Notificaciones al crear y resolver solicitudes (RF-NOT-01, RF-NOT-02): semanas 11–12.
- Reportes de solicitudes por estado (RF-REP-01): semana 12.
- Mover el negocio al módulo y alinear sus estados: semana 8. `GET /api/taller/ordenes` solo lee la tabla actual.

## 2. Rúbrica (5 puntos)

| Criterio | Puntos | Para "Logrado" |
|----------|--------|----------------|
| Gestión de permisos funcionando (RF-GP-01 a 07) | 1.5 | Las siete reglas cumplen su criterio, las solicitudes sobreviven al reinicio y el README explica cómo provocar cada criterio. |
| Estructura y ejecución de la suite | 1.0 | Un solo comando documentado ejecuta la suite desde un clon limpio, todas pasan, son independientes, tienen preparar–actuar–verificar y nombres que describen el comportamiento. |
| Transición válida | 0.5 | Al menos una prueba verifica el estado resultante y el dato registrado (quién y cuándo, o el motivo). |
| Transición prohibida | 1.0 | Al menos una prueba intenta una transición fuera de la tabla, verifica el rechazo y verifica que el estado no cambió. |
| Bitácora con el agente | 1.0 | Qué se pidió, qué devolvió y al menos un error del agente detectado y corregido, con evidencia. |

Errores que bajan a "Parcialmente logrado": falta RF-GP-05, RF-GP-06 o RF-GP-07; falta el comando documentado; pruebas que dependen del orden, sin tres fases o con nombres vagos; prueba válida que solo comprueba "no falla"; prueba prohibida que no comprueba el estado; bitácora sin error del agente.

**Semana 7:** sobre esta pieza se aplica la **regla del mutante**; las pruebas deben detectar cambios intencionales en el código.

## 3. Qué revisa el maestro

1. `git clone` y `git checkout asignacion-2`.
2. El README: arranque de la pieza y cómo provocar cada criterio RF-GP, seguido al pie de la letra.
3. Crear, aprobar, rechazar con motivo y aprobar dos veces una solicitud.
4. El comando de la sección «Pruebas» del README, ejecutado tal como está escrito.
5. Las pruebas de transición válida y prohibida: qué preparan, qué ejecutan y qué verifican.
6. `docs/bitacora-asignacion-2.md`.

## 4. Decisiones de esta entrega

Detalle y motivos en `docs/decisiones.md`.

- **Permisos específicos**, no ascenso a Administrador. Catálogo inicial: `OperarTaller`. Aplicar = otorgar el permiso al solicitante (tabla de permisos por usuario).
- **El permiso se comprueba contra la base de datos en cada petición**, no desde el JWT, para que funcione al instante tras la aprobación (RF-GP-05).
- **Operación protegida para la demo:** `GET /api/taller/ordenes` (solo lectura) exige `OperarTaller`. Estándar sin permiso → `403`; tras la aprobación → `200`.
- **Aprobar** = `Pendiente → Aprobada → Aplicada` en una sola transacción.
- **Errores:** transición prohibida → `409 Conflict`; resolver la propia solicitud (RF-GP-07) → `403`; rechazo sin motivo o permiso inexistente → `400`; todos con `{"mensaje"}`.
- **Duplicados:** se rechaza pedir un permiso que ya se tiene o que ya está pendiente.
- **Pruebas:** proyecto xUnit en `tests/`, incluido en la solución. Máquina de estados probada sin base de datos; servicio probado con **SQLite en memoria**, una base por prueba. Reloj inyectado con `TimeProvider` para verificar el "cuándo" exacto.

## 5. Checklist de estado

Marcar cada punto solo después de verificarlo contra el código en ejecución.

- [ ] Dominio: `SolicitudPermiso`, estados y `MaquinaEstadosSolicitud` como único punto de transiciones (RD-04)
- [ ] Persistencia y migración de solicitudes y permisos de usuario (RD-09)
- [ ] RF-GP-01 Crear solicitud (nace Pendiente; rechaza duplicados y permisos inexistentes)
- [ ] RF-GP-02 Admin lista pendientes; Estándar solo ve las suyas
- [ ] RF-GP-03 Aprobar registra aprobador y fecha
- [ ] RF-GP-04 Rechazar exige motivo y lo registra
- [ ] RF-GP-05 Aprobar aplica el permiso; `GET /api/taller/ordenes` pasa de 403 a 200 sin volver a iniciar sesión
- [ ] RF-GP-06 Transiciones fuera de la tabla → 409 y estado sin cambios
- [ ] RF-GP-07 Resolver la propia solicitud → 403
- [ ] Proyecto de pruebas en la solución; `dotnet test` pasa desde un clon limpio sin variables de entorno
- [ ] Prueba de transición válida con estado + dato registrado
- [ ] Prueba de transición prohibida con rechazo + estado sin cambios
- [ ] Pruebas independientes, con tres fases y nombres descriptivos
- [ ] README: cómo provocar cada RF-GP y sección **Pruebas** con el comando
- [ ] `API.http` con el flujo completo
- [ ] `docs/bitacora-asignacion-2.md` con al menos un error real del agente
- [ ] Revisión: Control de acceso sigue funcionando
- [ ] Commits citan sus IDs; PR con las cuatro secciones
- [ ] Etiqueta `asignacion-2` creada y subida antes del cierre
- [ ] `AGENTS.md` sección 0 actualizada
