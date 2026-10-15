# Requerimientos del Core

Transcripción de trabajo del documento *Requerimientos del Core — Programación III, ITLA, 2026-C-3*, igual para los 25 proyectos. Ante cualquier duda manda el PDF original del maestro.

- **RD:** requisitos de diseño, aplican a todo el sistema (incluido el negocio). Se evalúan en el code review y en la demo final.
- **RF:** requisitos funcionales, agrupados por pieza del Core.
- **Criterio de aceptación:** cómo se comprueba. Si no pasa el criterio, el requisito no está hecho aunque el código corra.
- Ningún requisito es opcional salvo donde se indica.

## 1. Requisitos de diseño (transversales)

| ID | Requisito | Criterio de aceptación |
|----|-----------|------------------------|
| RD-01 | Cada pieza del Core es un componente con responsabilidad única e interfaz explícita. | Para cada pieza se puede decir qué hace y qué operaciones expone sin describir el funcionamiento interno de otra. |
| RD-02 | La lógica de negocio no vive en la presentación ni en el manejador de la petición. | Cambiar la interfaz de usuario no obliga a reescribir ninguna regla de negocio. |
| RD-03 | El Core no depende del módulo de negocio; la dependencia va en un solo sentido. | Si se elimina el módulo de negocio, el Core sigue construyéndose y ejecutándose. |
| RD-04 | Las transiciones de estado se resuelven en un solo punto del código. | Agregar una transición se hace modificando un único componente; no hay validaciones de estado repetidas. |
| RD-05 | Las contraseñas se almacenan con hash. | Leyendo el almacenamiento no es posible recuperar ninguna contraseña. |
| RD-06 | La autorización se verifica del lado del servidor en cada operación. | Ocultar la opción en la interfaz no basta: invocar la operación directamente también se rechaza. |
| RD-07 | Toda entrada externa se valida antes de usarse. | Datos ausentes, de tipo incorrecto o fuera de rango producen un rechazo controlado, no una excepción sin manejar. |
| RD-08 | Los errores devuelven un mensaje comprensible sin exponer detalles internos. | Ningún mensaje visible contiene trazas de pila, rutas de archivos ni consultas. |
| RD-09 | Los datos persisten fuera del proceso. | Reiniciar la aplicación no pierde información. |
| RD-10 | Las credenciales y claves se leen de variables de entorno. | No hay ninguna credencial en el repositorio ni en el historial de commits. |
| RD-11 | Las fechas y horas se registran con el mismo criterio en todo el sistema. | Dos registros creados en el mismo instante por piezas distintas muestran la misma hora. |
| RD-12 | Cada pieza del Core puede probarse sin levantar la aplicación completa. | Existe al menos una prueba de esa pieza que corre sin interfaz de usuario. |

## 2. Entidades del Core (atributos mínimos)

Se pueden agregar atributos; no se puede omitir ninguno de estos.

| Entidad | Atributos mínimos |
|---------|-------------------|
| Usuario | Nombre · correo (único) · contraseña con hash · rol · activo |
| Rol | Administrador o Estándar |
| SolicitudPermiso | Solicitante · permiso solicitado · estado · aprobador · fecha de creación · fecha de resolución · motivo del rechazo |
| CodigoRecuperacion | Usuario · código · fecha de emisión · fecha de vencimiento · usado o no usado |
| CorreoEnCola | Destinatario · asunto · cuerpo · estado · intentos · fecha de creación · fecha de envío · último error |
| Documento | Nombre · tipo · tamaño · fecha de subida · usuario que lo subió · ruta de almacenamiento · marca de eliminado |
| Notificacion | Destinatario · tipo · mensaje · leída o no leída · fecha |
| RegistroAuditoria | Usuario que actuó · acción · entidad afectada · identificador de la entidad · fecha y hora · valor anterior · valor nuevo |

> Brechas actuales frente a esta tabla: `Usuario` no tiene `Nombre`; `CorreoEnCola` no tiene `Intentos` ni `UltimoError` (llegan en la semana 11).

## 3. Pieza 1 — Control de acceso (semanas 2 a 4)

Ninguna otra pieza se considera terminada si sus operaciones no están protegidas por esta. Los IDs RF-CA-14 a RF-CA-22 los añade la Práctica 1 (ver `docs/entregas/practica-1.md`).

| ID | Requisito | Criterio de aceptación |
|----|-----------|------------------------|
| RF-CA-01 | Registro de usuario con correo único. | Un segundo registro con un correo existente se rechaza. |
| RF-CA-02 | La contraseña se guarda con hash, nunca en texto plano. | El valor almacenado no coincide con la contraseña y no puede revertirse a ella. |
| RF-CA-03 | Inicio de sesión que entrega una credencial de sesión. | Credenciales correctas abren sesión; incorrectas se rechazan sin revelar cuál de los dos datos falló. |
| RF-CA-04 | Dos roles: Administrador y Estándar. | Todo usuario tiene exactamente un rol. |
| RF-CA-05 | Cada operación declara qué rol puede ejecutarla. | Existe un punto del código donde se lee la exigencia de rol de cada operación. |
| RF-CA-06 | Un Estándar que invoca una operación de Administrador recibe un rechazo explícito. | El rechazo ocurre también cuando la petición se construye a mano. |
| RF-CA-07 | Consulta del usuario autenticado y su rol. | Sin sesión válida la consulta se rechaza. |
| RF-CA-08 | Cambio de rol, reservado al Administrador. | Un Estándar no puede cambiar ningún rol, ni el propio. El cambio queda auditado (RF-AUD-04). |
| RF-CA-09 | El usuario inicia la recuperación indicando su correo. | La respuesta es idéntica exista o no el correo. |
| RF-CA-10 | La recuperación genera un código de un solo uso con vencimiento y lo envía por la cola. | Usarlo dos veces o vencido se rechaza y la contraseña no cambia. El correo sale por la cola (RF-NOT-08). |
| RF-CA-11 | Con un código válido el usuario define una contraseña nueva, guardada con hash. | La contraseña anterior deja de servir. |
| RF-CA-12 | Las sesiones abiertas antes del cambio de contraseña dejan de ser válidas. | Una credencial emitida antes del restablecimiento es rechazada. |
| RF-CA-13 | Un Administrador puede forzar el restablecimiento de la contraseña de un usuario. | El usuario no puede seguir usando la anterior y la acción queda auditada (RF-AUD-05). |

**Dependencia:** la recuperación usa la cola de correos de la pieza 4 (semana 11). Hasta entonces el correo queda encolado y se verifica leyendo la cola. RF-CA-13 no depende del correo y debe funcionar desde la semana 4.

## 4. Pieza 2 — Gestión de permisos (semanas 6 a 8)

Es la máquina de estados del Core, igual para los 25. Sobre ella se aplica la **regla del mutante** en la semana 7.

| Desde | Hacia | Quién la ejecuta | Condición |
|-------|-------|------------------|-----------|
| Pendiente | Aprobada | Administrador | La solicitud está en Pendiente. |
| Aprobada | Aplicada | El sistema | Automática, inmediatamente después de aprobar. |
| Pendiente | Rechazada | Administrador | Exige un motivo escrito. |
| Cualquier otra combinación | — | — | Prohibida: el sistema la rechaza y el estado no cambia. |

| ID | Requisito | Criterio de aceptación |
|----|-----------|------------------------|
| RF-GP-01 | Un Estándar crea una solicitud de permiso. | La solicitud nace en Pendiente. |
| RF-GP-02 | Un Administrador consulta las solicitudes pendientes. | Un Estándar no puede ver las solicitudes de otros usuarios. |
| RF-GP-03 | Un Administrador aprueba una solicitud pendiente. | Queda registrado quién aprobó y cuándo. |
| RF-GP-04 | Un Administrador rechaza una solicitud pendiente indicando el motivo. | Sin motivo escrito el rechazo no se completa. |
| RF-GP-05 | Al aprobar, el sistema aplica el cambio de permiso automáticamente. | Tras la aprobación el solicitante puede ejecutar la acción pedida sin intervención manual. |
| RF-GP-06 | Solo se permiten las transiciones de la tabla. | Aprobar dos veces, o pasar de Rechazada a Aplicada, se rechaza y el estado no cambia. |
| RF-GP-07 | Nadie resuelve su propia solicitud. | Un Administrador que aprueba o rechaza una solicitud creada por él mismo recibe un rechazo. |

## 5. Pieza 3 — Manejador de documentos (semana 9)

| ID | Requisito | Criterio de aceptación |
|----|-----------|------------------------|
| RF-DOC-01 | Subir un documento registrando nombre, tipo, tamaño, fecha y quién lo subió. | El registro queda consultable con todos esos datos. |
| RF-DOC-02 | Listar los documentos que el usuario puede ver. | No incluye eliminados ni de otros usuarios, salvo para el Administrador. |
| RF-DOC-03 | Descargar un documento. | Solo el dueño o un Administrador lo logran; cualquier otro recibe rechazo. |
| RF-DOC-04 | Eliminar un documento de forma lógica. | Deja de aparecer en el listado, pero el registro sigue existiendo. |
| RF-DOC-05 | El archivo se guarda en un volumen persistente fuera de la imagen del contenedor. | Destruir y recrear el contenedor no pierde los archivos. |
| RF-DOC-06 | Toda subida y eliminación queda auditada. | Cada una genera su registro de auditoría (RF-AUD-03). |

## 6. Pieza 4 — Notificaciones y cola de correos (semanas 11 y 12)

Es la primera funcionalidad completa que se delega al agente: sus reglas deben quedar documentadas sin ambigüedad.

| ID | Requisito | Criterio de aceptación |
|----|-----------|------------------------|
| RF-NOT-01 | Crear una solicitud de permiso notifica a los Administradores. | Todos los Administradores la reciben; nadie más. |
| RF-NOT-02 | Resolver una solicitud notifica al solicitante. | Indica si fue aprobada o rechazada y, si fue rechazada, el motivo. |
| RF-NOT-03 | Al menos un evento del módulo de negocio dispara una notificación. | El evento está documentado y se puede provocar en la demo. |
| RF-NOT-04 | Consultar las notificaciones propias distinguiendo leídas de no leídas. | Un usuario no ve las de otro. |
| RF-NOT-05 | Marcar una notificación como leída. | Marcar la de otro usuario se rechaza. |
| RF-NOT-06 | Toda notificación se entrega por dos canales: interno y correo. | El destinatario la ve en su bandeja y recibe el correo. |
| RF-NOT-07 | El destinatario puede desactivar el canal de correo sin perder el interno. | Con el correo desactivado la notificación aparece dentro del sistema y no se encola correo. |
| RF-NOT-08 | Los correos no se envían dentro de la operación que los origina: se encolan. | La operación termina bien aunque el servidor de correo no responda. |
| RF-NOT-09 | Un proceso independiente toma los pendientes y los envía. | El envío ocurre fuera del flujo que creó el correo y puede ejecutarse sin que ese flujo vuelva a correr. |
| RF-NOT-10 | Un correo que falla se reintenta un número limitado de veces y luego queda fallido. | No se reintenta indefinidamente ni desaparece sin rastro. |
| RF-NOT-11 | Cada correo registra estado (pendiente, enviado, fallido), intentos, fechas y último error. | Un Administrador puede consultar la cola y saber qué pasó con cualquier correo. |
| RF-NOT-12 | Un correo enviado no se vuelve a enviar. | Ejecutar el procesador dos veces seguidas no duplica envíos. |
| RF-NOT-13 | Las credenciales del servidor de correo se leen de variables de entorno. | Cumple RD-10. |

## 7. Pieza 5 — Reportes con agregación (semana 12)

| ID | Requisito | Criterio de aceptación |
|----|-----------|------------------------|
| RF-REP-01 | Un reporte sobre el Core: solicitudes por estado y período, o documentos por usuario. | Devuelve valores agrupados, no el listado de registros. |
| RF-REP-02 | Un reporte sobre el módulo de negocio. | Responde una pregunta real del dominio, no un conteo de filas. |
| RF-REP-03 | Ambos usan agregación real: agrupaciones, totales o promedios. | El resultado tiene menos filas que los registros que resume. |
| RF-REP-04 | Los reportes respetan el rol de quien consulta. | Un Estándar no obtiene datos de otros usuarios a través de un reporte. |
| RF-REP-05 | Ambos se pueden filtrar por rango de fechas. | Dos rangos distintos producen resultados distintos sobre los mismos datos. |

## 8. Pieza 6 — Auditoría (semana 14)

| ID | Requisito | Criterio de aceptación |
|----|-----------|------------------------|
| RF-AUD-01 | Cada registro guarda usuario, acción, entidad, identificador, fecha y hora, valor anterior y valor nuevo. | Un registro al que le falte cualquiera de esos datos no cumple. |
| RF-AUD-02 | Se audita toda resolución de solicitud de permiso. | Queda registrado quién aprobó o rechazó qué y cuándo. |
| RF-AUD-03 | Se audita toda subida y eliminación de documento. | Ambas acciones generan registro. |
| RF-AUD-04 | Se audita todo cambio de rol. | El registro muestra el rol anterior y el nuevo. |
| RF-AUD-05 | Se audita todo restablecimiento de contraseña. | Identifica si lo inició el usuario o un Administrador. Nunca guarda la contraseña ni el código. |
| RF-AUD-06 | La auditoría no se modifica ni se elimina desde la aplicación. | No existe ninguna operación de edición ni borrado sobre ella. |
| RF-AUD-07 | Consulta filtrable por usuario, entidad y rango de fechas. | Reservada al Administrador; un Estándar recibe rechazo. |

## 9. Módulo de negocio y su conexión con el Core

Aplica igual a los 25 dominios. El detalle propio de PC Builder Ops está en `docs/requisitos/negocio.md`.

| ID | Requisito | Criterio de aceptación |
|----|-----------|------------------------|
| RF-NEG-01 | Al menos cuatro entidades relacionadas entre sí. | Las relaciones existen en el modelo de datos, no solo en el diagrama. |
| RF-NEG-02 | Al menos cinco funcionalidades separables. | Cada una se puede probar por separado. |
| RF-NEG-03 | Máquina de estados propia, entre 3 y 5 estados. | Los estados están declarados en un solo lugar del código. |
| RF-NEG-04 | Al menos una transición prohibida explícita. | Intentarla se rechaza y el estado no cambia. |
| RF-NEG-05 | Al menos un estado terminal. | Ninguna transición parte de ese estado. |
| RF-NEG-06 | Sus operaciones están protegidas por Control de acceso. | Ninguna operación se ejecuta sin sesión válida y sin verificación de rol. |
| RF-NEG-07 | Dispara al menos una notificación propia del dominio. | Cumple RF-NOT-03. |
| RF-NEG-08 | Alimenta al menos un reporte con agregación. | Cumple RF-REP-02. |
| RF-NEG-09 | Su máquina de estados es independiente de la de Gestión de permisos. | Cambiar los estados de permisos no obliga a cambiar los del negocio. |
| RF-NEG-10 | Adjuntar documentos a una entidad del negocio es opcional. | Si se implementa, cumple todos los requisitos de la pieza 3. |

A partir de la semana 8 hay **dos máquinas de estados probadas por separado**: Gestión de permisos y la del negocio. Son dos suites de pruebas, no una.

## 10. Cronograma (no negociable: cada pieza se apoya en la anterior)

| Pieza | Semanas | Dónde se evalúa |
|-------|---------|-----------------|
| 1. Control de acceso | 2 a 4 | Asignación 1 (S3) · Práctica 1 (S4) |
| Máquina de estados del negocio (estructura) | 4 | Práctica 1 (S4) |
| 2. Gestión de permisos | 6 a 8 | Asignación 2 (S6) · Práctica 2 (S7) |
| Máquina de estados del negocio (pruebas) | 8 | Práctica 3 (S8) |
| 3. Manejador de documentos | 9 | Asignación 3 (S9) |
| 4. Notificaciones y cola de correos | 11 y 12 | Práctica 4 (S11) · Práctica 5 (S12) |
| 5. Reportes | 12 | Práctica 5 (S12) |
| 6. Auditoría | 14 | Asignación 4 (S14) |
| Todo integrado | 15 | Proyecto final (S15) |

**Revisión acumulada:** desde la Práctica 2, dos de los ocho puntos de cada práctica verifican que Control de acceso siga funcionando (registro con activación, inicio de sesión, rechazo por rol y recuperación de contraseña).
