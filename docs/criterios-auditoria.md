# Criterios de aceptación — Historial de auditoría (Pieza 6)

Requisitos que cubre: `RF-AUD-01` a `RF-AUD-07` (`docs/requisitos/core.md`, sección 8), con `RF-DOC-06`, `RF-CA-08`, `RF-CA-13` y `RF-CA-20`, y los RD-03, RD-06, RD-07, RD-09 y RD-11. Se escriben antes de programar: el historial se acepta solo si cumple cada uno.

## 0. Cómo se comprueban

- **Entorno:** copiar `.env.example` a `.env` y ejecutar `docker compose up --build`. La API queda en `http://localhost:5206` y los correos llegan a Mailpit (`http://localhost:8025`).
- **Cuentas:**
  - A1 y A2: el Administrador inicial de `.env.example` y un segundo Administrador creado con `POST /api/usuarios`.
  - E1 y E2: dos usuarios Estándar, activos.
- **Consulta del historial:** `GET /api/auditoria`, con los filtros opcionales:
  - `usuarioId`: el usuario que actuó;
  - `entidad`: `SolicitudPermiso`, `Documento` o `Usuario`;
  - `desde` y `hasta`: en `yyyy-MM-dd` (UTC), se usan juntos y `hasta` incluye el día completo;
  - `pagina` (desde 1) y `tamanoPagina` (de 1 a 100; por defecto 50).
- **Cada registro** muestra `id`, `usuarioId`, `usuarioNombre`, `accion`, `entidad`, `entidadId`, `fechaUtc`, `valorAnterior` y `valorNuevo`, del más reciente al más antiguo.
- **Valores:** `valorAnterior` y `valorNuevo` son objetos JSON con solo los campos que cambiaron. Nunca están vacíos: en una creación, el valor anterior es `{"estado":"NoExistia"}`.
- **Pruebas:** cada criterio tiene una petición numerada en `src/API/API.http` y, si no depende de la red, una prueba automatizada que cita su ID (`dotnet test P3-PCBuilderOps.slnx`).
- **Errores:** toda respuesta de error usa `{"mensaje": "..."}` (RD-07) y nunca expone trazas, rutas internas ni errores de SQL (RD-08).

Formato: **Dado** (estado previo) · **Cuando** (acción) · **Entonces** (resultado observable) · **Comprobación** (cómo verlo).

---

## 1. El registro

### CA-AUD-01 · RF-AUD-01 — Todo registro trae los ocho datos
- **Dado:** cualquiera de los eventos de los criterios CA-AUD-02 a CA-AUD-12.
- **Cuando:** un Administrador consulta `GET /api/auditoria`.
- **Entonces:** cada registro tiene, todos presentes y no vacíos:
  - `usuarioId` y `usuarioNombre` de quien actuó;
  - `accion`;
  - `entidad` y `entidadId` de lo afectado;
  - `fechaUtc`, terminada en `Z`;
  - `valorAnterior` y `valorNuevo`.
- **Comprobación:** la consulta después de provocar cada evento; las pruebas de cada evento verifican los ocho campos.

## 2. Solicitudes de permiso (RF-AUD-02)

### CA-AUD-02 · RF-AUD-02 — Aprobar una solicitud queda auditado
- **Dado:** una solicitud pendiente de E1 para `OperarTaller`.
- **Cuando:** A1 la aprueba (`POST /api/permisos/solicitudes/{id}/aprobar`).
- **Entonces:** hay un registro nuevo con:
  - `usuarioId` = A1;
  - `accion` = `SolicitudAprobada`;
  - `entidad` = `SolicitudPermiso` y `entidadId` = el id de la solicitud;
  - `valorAnterior` = `{"estado":"Pendiente"}`;
  - `valorNuevo` con `"estado":"Aplicada"`, el permiso y el id de E1.
- **Comprobación:** `GET /api/auditoria?entidad=SolicitudPermiso`.

### CA-AUD-03 · RF-AUD-02 — Rechazar una solicitud queda auditado con el motivo
- **Dado:** una solicitud pendiente de E1.
- **Cuando:** A1 la rechaza con el motivo «Falta experiencia en taller».
- **Entonces:** hay un registro nuevo con:
  - `accion` = `SolicitudRechazada` y `usuarioId` = A1;
  - `valorAnterior` = `{"estado":"Pendiente"}`;
  - `valorNuevo` con `"estado":"Rechazada"` y el motivo.
- **Comprobación:** `GET /api/auditoria?entidad=SolicitudPermiso`.

### CA-AUD-04 · RF-AUD-02, RF-AUD-01 — Una resolución rechazada no deja registro
- **Dado:** una solicitud ya aprobada, y otra pendiente que dos Administradores resuelven a la vez.
- **Cuando:**
  - se intenta aprobar otra vez la ya aprobada (`409`);
  - E1 intenta aprobar una solicitud (`403`);
  - A1 y A2 resuelven la pendiente al mismo tiempo (una recibe `409`).
- **Entonces:**
  - Ninguno de los rechazos genera registro.
  - De la resolución simultánea queda **exactamente un** registro: el de la que ganó.
- **Comprobación:** contar los registros de esa solicitud antes y después; prueba automatizada.

## 3. Documentos (RF-AUD-03, RF-DOC-06)

### CA-AUD-05 · RF-AUD-03 — Subir un documento queda auditado
- **Dado:** E1 con sesión.
- **Cuando:** sube `factura.pdf` (`POST /api/documentos`).
- **Entonces:**
  - Hay un registro con `usuarioId` = E1, `accion` = `DocumentoSubido`, `entidad` = `Documento` y `entidadId` = el id del documento.
  - `valorAnterior` = `{"estado":"NoExistia"}`.
  - `valorNuevo` con el nombre original, el tipo, el tamaño y `"estado":"Activo"`.
  - Una subida rechazada (sin archivo, vacía o de más de 10 MB) no genera registro.
- **Comprobación:** `GET /api/auditoria?entidad=Documento`.

### CA-AUD-06 · RF-AUD-03 — Eliminar un documento queda auditado
- **Dado:** un documento activo de E1.
- **Cuando:** E1 lo elimina (`DELETE /api/documentos/{id}`); luego A1 elimina otro documento de E1.
- **Entonces:**
  - Cada eliminación genera un registro con `accion` = `DocumentoEliminado`, `valorAnterior` = `{"estado":"Activo"}` y `valorNuevo` = `{"estado":"Eliminado"}`.
  - El `usuarioId` es el de quien eliminó: E1 en el primero y A1 en el segundo.
  - Un intento rechazado de E2 (`403`) no genera registro.
- **Comprobación:** `GET /api/auditoria?entidad=Documento`.

## 4. Usuarios (RF-AUD-04, RF-CA-08, RF-CA-20)

### CA-AUD-07 · RF-AUD-04, RF-CA-08 — Cambiar un rol muestra el rol anterior y el nuevo
- **Dado:** E1 con rol `Estandar`.
- **Cuando:** A1 le cambia el rol a `Administrador` (`PUT /api/usuarios/{id}/rol`) y luego se lo devuelve a `Estandar`.
- **Entonces:**
  - Hay dos registros con `accion` = `RolCambiado`, `entidad` = `Usuario`, `entidadId` = E1 y `usuarioId` = A1.
  - El primero va de `{"rol":"Estandar"}` a `{"rol":"Administrador"}`, y el segundo al revés.
  - Un intento de un Estándar (`403`) no genera registro.
- **Comprobación:** `GET /api/auditoria?entidad=Usuario`.

### CA-AUD-08 · RF-AUD-04, RF-CA-20 — Desactivar y reactivar un usuario quedan auditados
- **Dado:** E2 activo.
- **Cuando:** A1 lo desactiva y luego lo reactiva (`PATCH /api/usuarios/{id}/estado`).
- **Entonces:**
  - Hay dos registros con `entidadId` = E2 y `usuarioId` = A1.
  - El primero tiene `accion` = `UsuarioDesactivado`, de `{"estaActivo":true}` a `{"estaActivo":false}`.
  - El segundo tiene `accion` = `UsuarioReactivado`, de `{"estaActivo":false}` a `{"estaActivo":true}`.
  - Un intento de desactivarse a sí mismo (`403`) no genera registro.
- **Comprobación:** `GET /api/auditoria?entidad=Usuario`.

## 5. Contraseñas (RF-AUD-05, RF-CA-13)

### CA-AUD-09 · RF-AUD-05, RF-CA-13 — El restablecimiento forzado registra al Administrador
- **Dado:** E1 activo.
- **Cuando:** A1 fuerza el restablecimiento (`POST /api/usuarios/{id}/forzar-restablecimiento`).
- **Entonces:** hay un registro con:
  - `usuarioId` = A1, `accion` = `RestablecimientoForzado`, `entidad` = `Usuario` y `entidadId` = E1;
  - `valorAnterior` = `{"debeCambiarPassword":false}`;
  - `valorNuevo` = `{"debeCambiarPassword":true,"iniciadoPor":"Administrador"}`.
- **Comprobación:** `GET /api/auditoria?usuarioId=<A1>`.

### CA-AUD-10 · RF-AUD-05 — Restablecer con el código registra quién lo inició
- **Dado:**
  - E1 pidió recuperar su contraseña (`POST /api/auth/olvide-password`);
  - E2 recibió un restablecimiento forzado por A1 (CA-AUD-09).
- **Cuando:** cada uno define su contraseña nueva con el código que le llegó a Mailpit (`POST /api/auth/restablecer-password`).
- **Entonces:**
  - Cada uno genera un registro con `accion` = `ContrasenaRestablecida`, `entidad` = `Usuario`, y `usuarioId` y `entidadId` = el propio usuario.
  - El de E1 lleva `"iniciadoPor":"Usuario"` en su `valorNuevo`.
  - El de E2 lleva `"iniciadoPor":"Administrador"`, porque su código viene del restablecimiento forzado.
  - Un código vencido o ya usado (`400`) no genera registro.
- **Comprobación:** `GET /api/auditoria?entidad=Usuario`.

### CA-AUD-11 · RF-AUD-05 — Ningún registro guarda contraseñas ni códigos
- **Dado:** los registros de CA-AUD-09, CA-AUD-10 y CA-AUD-12.
- **Cuando:** se revisa su `valorAnterior` y su `valorNuevo`.
- **Entonces:** no aparecen la contraseña anterior ni la nueva, ningún hash de contraseña ni el código de recuperación o su enlace.
- **Comprobación:** la consulta, comparada con las contraseñas usadas y con el código del correo en Mailpit; prueba automatizada que busca esos textos en el registro.

### CA-AUD-12 · RF-AUD-05 — Cambiar la propia contraseña queda auditado
- **Dado:** E1 con sesión.
- **Cuando:** cambia su contraseña (`POST /api/auth/cambiar-password`) con la actual correcta.
- **Entonces:**
  - Hay un registro con `usuarioId` y `entidadId` = E1, `accion` = `ContrasenaCambiada` y `valorNuevo` con `"iniciadoPor":"Usuario"`, sin la contraseña.
  - Con la contraseña actual incorrecta (`400`) no se genera registro.
- **Comprobación:** `GET /api/auditoria?usuarioId=<E1>`.

## 6. Inmutabilidad (RF-AUD-06)

### CA-AUD-13 · RF-AUD-06 — Nada edita ni borra el historial
- **Dado:** registros en el historial.
- **Cuando:**
  - se intenta `PUT`, `PATCH` o `DELETE` sobre `/api/auditoria` o `/api/auditoria/{id}`, con un token de Administrador;
  - se revisa el código.
- **Entonces:**
  - Las peticiones responden `405` o `404`, y el historial no cambia.
  - Ninguna interfaz pública ni controlador tiene una operación de edición o de borrado del historial.
  - Si un código interno intenta modificar o borrar un registro a través del `DbContext`, el guardado se rechaza.
- **Comprobación:** las peticiones; prueba automatizada que revisa los métodos públicos del servicio y del controlador, y que un intento directo de modificar o borrar un registro falla.

## 7. Consulta (RF-AUD-07, RD-06, RD-07)

### CA-AUD-14 · RF-AUD-07 — El Administrador filtra por usuario, por entidad y combinando filtros
- **Dado:** registros de A1 y A2 sobre `SolicitudPermiso`, `Documento` y `Usuario`.
- **Cuando:** A1 consulta `?usuarioId=<A2>`, luego `?entidad=Documento` y luego `?usuarioId=<A1>&entidad=Usuario`.
- **Entonces:**
  - Cada consulta devuelve solo los registros que cumplen todos sus filtros, del más reciente al más antiguo.
  - Las tres dan resultados distintos.
  - La respuesta trae el `total` de registros que cumplen los filtros.
- **Comprobación:** las tres peticiones; prueba automatizada.

### CA-AUD-15 · RF-AUD-07, RD-11 — El rango de fechas incluye completo el último día
- **Dado:**
  - un registro a las `23:59:59` UTC del 30 de noviembre;
  - otro a las `00:00:00` UTC del 1 de diciembre;
  - registros de hoy.
- **Cuando:** se consulta con `desde=2026-11-30&hasta=2026-11-30`, luego con un rango que termina ayer, y luego con `desde=0001-01-01&hasta=9999-12-31`.
- **Entonces:**
  - El primero incluye el registro del 30 y no el del 1 de diciembre.
  - El rango que termina ayer no trae los registros de hoy.
  - El rango completo responde `200` con todos los registros, sin `500`.
- **Comprobación:** prueba automatizada con fechas fijas; peticiones de `API.http`.

### CA-AUD-16 · RF-AUD-07, RD-06 — Solo el Administrador consulta
- **Dado:** E1 con sesión, y una petición sin sesión.
- **Cuando:** E1 consulta `GET /api/auditoria`, con o sin filtros, y lo mismo sin token.
- **Entonces:** E1 recibe `403` con `{"mensaje": "..."}`; sin sesión, `401`. Ninguno recibe datos.
- **Comprobación:** las peticiones; prueba automatizada.

### CA-AUD-17 · RF-AUD-07, RD-07 — Un filtro inválido se rechaza de forma controlada
- **Dado:** A1 con sesión.
- **Cuando:** consulta con:
  - `usuarioId=abc`;
  - `entidad=Factura`;
  - `desde=2026-13-01`;
  - solo `desde` o solo `hasta`;
  - `desde` posterior a `hasta`;
  - `pagina=0`;
  - `tamanoPagina=1000`.
- **Entonces:** cada consulta responde `400` con `{"mensaje": "..."}`, que dice qué filtro está mal, y sin datos.
- **Comprobación:** las peticiones de `API.http`; prueba automatizada.

## 8. Transversales

### CA-AUD-18 · RD-09 — El historial sobrevive al reinicio
- **Dado:** registros en el historial.
- **Cuando:** se ejecuta `docker compose down` (sin `-v`) y luego `docker compose up`.
- **Entonces:** `GET /api/auditoria` devuelve los mismos registros, con los mismos valores.
- **Comprobación:** la consulta antes y después.

### CA-AUD-19 · RD-11 — La fecha del registro sigue el criterio del sistema
- **Dado:** cualquier registro.
- **Cuando:** se consulta.
- **Entonces:** `fechaUtc` está en UTC y termina en `Z`, igual que el resto de las fechas de la API, y coincide con el momento de la operación.
- **Comprobación:** la consulta; prueba automatizada con un reloj fijo.

### CA-AUD-20 · RD-03, RF-AUD-01 — Auditar no hace que el Core dependa del módulo
- **Dado:** el código del Core.
- **Cuando:** se revisa cómo registra la auditoría.
- **Entonces:**
  - El Core no importa ni referencia nada de `PCBuilderOps`.
  - La entidad auditada se identifica con un texto, así el módulo podría auditar sus propias entidades sin que el Core cambie.
  - El registro se guarda en la misma operación que el cambio que audita, así que no puede quedar un cambio sin su registro ni un registro sin su cambio.
- **Comprobación:** revisión de `src/Core`; las pruebas de CA-AUD-04 demuestran que una operación rechazada no deja registro.
