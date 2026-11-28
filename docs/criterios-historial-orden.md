# Criterios de aceptación — Historial de estados de la orden de ensamble

Requisitos que cubre: lo que `docs/requisitos/negocio.md` (sección 4) promete y la Asignación 4 dejó para el proyecto final. Son la auditoría de las transiciones de la orden y el reporte de tiempo promedio de ensamble por técnico (`RF-NEG-08`, `RF-REP-02` a `RF-REP-05`). Se apoyan en `RF-AUD-01`, `RF-AUD-07`, `RF-NEG-03` a `RF-NEG-07`, `FN-02` a `FN-05` y los RD-03, RD-04, RD-06, RD-07, RD-09 y RD-11, con las reglas de la sección 12 de `AGENTS.md`. Se escriben antes de programar: el historial de la orden se acepta solo si cumple cada uno.

## 0. Cómo se comprueban

- **Entorno:** copiar `.env.example` a `.env` y ejecutar `docker compose up --build`. La API queda en `http://localhost:5206` y los correos llegan a Mailpit (`http://localhost:8025`).
- **Cuentas:**
  - A1: el Administrador inicial de `.env.example`.
  - C1 y C2: dos clientes (usuarios Estándar).
  - T1 y T2: dos técnicos (usuarios Estándar con `OperarTaller` aprobado).
- **Flujo de la orden:**
  1. C1 cotiza con `POST /api/ordenes`.
  2. A1 inicia el armado con `POST /api/ordenes/{id}/iniciar-armado` y el técnico.
  3. El técnico la pasa a pruebas con `POST /api/taller/ordenes/{id}/pasar-a-pruebas`.
  4. El técnico registra pruebas con `POST /api/taller/ordenes/{id}/pruebas`.
  5. El técnico la entrega con `POST /api/taller/ordenes/{id}/entregar`.
- **Historial de la orden:** lista de entradas en el detalle de la orden (`GET /api/ordenes/{id}`, campo `historial`). Cada entrada tiene `estadoAnterior` (vacío en la creación), `estadoNuevo` y `fechaUtc`, en orden cronológico.
- **Auditoría:** `GET /api/auditoria?entidad=OrdenEnsamble`, con el formato de `docs/criterios-auditoria.md`.
- **Reporte:** `GET /api/reportes/tiempo-ensamble?desde=yyyy-MM-dd&hasta=yyyy-MM-dd`, con las reglas de rango de los otros reportes (`docs/criterios-notificaciones-reportes.md`).
- **Pruebas:** cada criterio tiene una petición en `src/API/API.http` y, si no depende de la red, una prueba automatizada que cita su ID (`dotnet test P3-PCBuilderOps.slnx`).
- **Errores:** toda respuesta de error usa `{"mensaje": "..."}` (RD-07) y nunca expone trazas, rutas internas ni errores de SQL (RD-08).

Formato: **Dado** (estado previo) · **Cuando** (acción) · **Entonces** (resultado observable) · **Comprobación** (cómo verlo).

---

## 1. Una sola operación entre el Core y el módulo

La orden vive en el contexto de datos del módulo y la auditoría y las notificaciones en el del Core. Estos criterios fijan que una transición se guarda completa o no se guarda.

### CA-HIS-01 · RF-AUD-01, RF-NEG-07 — Una transición guarda todo junto
- **Dado:** una orden de C1 en `EnArmado` con T1 asignado.
- **Cuando:** T1 la pasa a pruebas.
- **Entonces:** la base tiene a la vez:
  - el estado `EnPruebas`;
  - una entrada nueva en el historial;
  - un registro nuevo de auditoría;
  - la notificación de C1 y su correo en la cola.
- **Comprobación:** el detalle, la auditoría y la bandeja de C1 después de la petición; la prueba de la transición verifica las cuatro tablas.

### CA-HIS-02 · RF-AUD-01 — Si falla el guardado del Core, la transición se deshace
- **Dado:** una orden en `EnArmado`, y el guardado de la auditoría o de la notificación falla después de que el módulo guardó la orden.
- **Cuando:** se intenta pasar la orden a pruebas.
- **Entonces:**
  - la orden sigue en `EnArmado`;
  - no queda entrada de historial, registro de auditoría, notificación ni correo de esa transición.
- **Comprobación:** prueba automatizada con un notificador que falla. No se puede provocar desde la API.

### CA-HIS-03 · RF-NEG-04, RF-NEG-05, FN-04, RF-GP-05 — Una transición rechazada no deja rastro
- **Dado:** órdenes en distintos estados.
- **Cuando:** se intenta cada una de estas transiciones:
  - entregar una orden `Cotizada` (prohibida);
  - entregar una orden sin prueba aprobada;
  - pasar a pruebas sin el permiso `OperarTaller`;
  - iniciar el armado sin existencias;
  - cualquier transición sobre una orden `Entregada`.
- **Entonces:**
  - la API responde el error de siempre (`409`, `403` o `400`, con su mensaje);
  - el estado no cambia;
  - no hay entrada nueva de historial, registro de auditoría ni notificación.
- **Comprobación:** contar historial, auditoría y bandeja antes y después de cada intento.

### CA-HIS-04 · Hallazgo 1 (Asignación 3) — Dos transiciones simultáneas dejan un solo rastro
- **Dado:** una orden en `EnArmado`, leída por dos peticiones antes de que cualquiera guarde.
- **Cuando:** ambas la pasan a pruebas a la vez.
- **Entonces:**
  - una responde `200` y la otra `409`;
  - quedan exactamente una entrada de historial, un registro de auditoría y una notificación de esa transición.
- **Comprobación:** prueba automatizada con dos contextos que leen la fila antes de guardar; en Docker, dos peticiones en paralelo.

### CA-HIS-05 · RD-03 — El Core no conoce el módulo
- **Dado:** la operación compartida que ofrece el Core.
- **Cuando:** se revisa el código.
- **Entonces:**
  - `src/Core` no importa ni usa ningún tipo del módulo (`using PCBuilderOps`, `PCBuilderOps.<Tipo>`) y `Core.csproj` no referencia su proyecto. Los textos que solo llevan el nombre del producto, como el emisor del JWT, no cuentan;
  - la operación recibe cualquier contexto de datos (`DbContext`), no el del módulo;
  - si ese contexto no comparte la conexión del Core, la operación se rechaza antes de guardar nada, en vez de guardar a medias.
- **Comprobación:** búsqueda de `using PCBuilderOps` y `PCBuilderOps.` en `src/Core` y de referencias en `Core.csproj`; prueba automatizada del tipo del parámetro y del rechazo con un contexto de otra conexión.

## 2. Historial de la orden (FN-05, RD-04)

### CA-HIS-06 · FN-01, FN-05 — Cotizar abre el historial
- **Dado:** C1 con sesión.
- **Cuando:** cotiza una PC compatible.
- **Entonces:** el detalle trae una sola entrada, sin estado anterior, con `estadoNuevo` = `Cotizada`, la fecha de la cotización y C1 como responsable.
- **Comprobación:** respuesta de `POST /api/ordenes`; prueba automatizada.

### CA-HIS-07 · RF-NEG-03, RD-04, RD-11 — Cada transición agrega su entrada
- **Dado:** una orden recorrida completa: cotizada, en armado, en pruebas y entregada.
- **Cuando:** C1 consulta el detalle.
- **Entonces:**
  - el historial tiene cuatro entradas en orden: `—→Cotizada`, `Cotizada→EnArmado`, `EnArmado→EnPruebas`, `EnPruebas→Entregada`;
  - cada una trae su fecha en UTC con `Z`;
  - la base guarda también quién la ejecutó: C1, A1, T1 y T1;
  - la entrada que llega a `EnPruebas` guarda cuánto tiempo estuvo la orden en `EnArmado`;
  - las entradas se crean en el mismo lugar que valida la transición (RD-04), no en el servicio ni en el controlador.
- **Comprobación:** el detalle tras el flujo completo; prueba automatizada que avanza el reloj entre pasos.

### CA-HIS-08 · RF-NEG-06, FN-05 — Cada cliente ve solo el historial de sus órdenes
- **Dado:** una orden de C1.
- **Cuando:** C2 pide su detalle.
- **Entonces:** recibe `404`, como hoy, y no ve el historial.
- **Comprobación:** `GET /api/ordenes/{id}` con el token de C2.

### CA-HIS-09 · Hallazgo 2 (Asignación 3) — La base impide dos entradas del mismo estado
- **Dado:** una orden que ya entró a `EnPruebas`.
- **Cuando:** se intenta guardar otra entrada de la misma orden hacia `EnPruebas`.
- **Entonces:** la base la rechaza con un índice único (orden y estado nuevo).
- **Comprobación:** prueba automatizada que inserta la entrada duplicada directamente.

### CA-HIS-10 · RD-09 — Las órdenes que ya existían reciben su entrada de creación
- **Dado:** órdenes guardadas antes de esta migración.
- **Cuando:** se aplica la migración.
- **Entonces:**
  - cada orden recibe una entrada `—→Cotizada` con su fecha de creación y su cliente;
  - no se inventan las transiciones que no se registraron ni su duración.
- **Comprobación:** levantar la versión anterior, crear órdenes, actualizar y consultar el detalle.

## 3. Auditoría de las transiciones (RF-AUD-01, RF-AUD-07)

### CA-HIS-11 · RF-AUD-01 — Cada transición queda auditada con los ocho datos
- **Dado:** el flujo completo de CA-HIS-07.
- **Cuando:** A1 consulta `GET /api/auditoria?entidad=OrdenEnsamble`.
- **Entonces:** hay tres registros de esa orden, cada uno con los ocho datos de CA-AUD-01:

  | `accion` | `usuarioId` | `valorAnterior` | `valorNuevo` |
  |----------|-------------|-----------------|--------------|
  | `ArmadoIniciado` | A1 | `estado` = `Cotizada`, `tecnicoAsignadoId` = vacío | `estado` = `EnArmado`, `tecnicoAsignadoId` = T1 |
  | `OrdenEnPruebas` | T1 | `estado` = `EnArmado` | `estado` = `EnPruebas` |
  | `OrdenEntregada` | T1 | `estado` = `EnPruebas` | `estado` = `Entregada` |

  En todos, `entidad` = `OrdenEnsamble` y `entidadId` = el id de la orden.
- **Comprobación:** la consulta tras el flujo; una prueba automatizada por acción que verifica todos los campos.

### CA-HIS-12 · RF-AUD-07, RD-07 — La consulta de auditoría acepta la entidad nueva
- **Dado:** registros del Core y de la orden.
- **Cuando:** A1 consulta:
  - con `?entidad=OrdenEnsamble`;
  - con `?entidad=ordenensamble`;
  - con `?entidad=Factura`.
- **Entonces:**
  - las dos primeras traen solo registros de la orden;
  - la tercera responde `400` con un mensaje que lista las entidades válidas, `OrdenEnsamble` incluida.
- **Comprobación:** las tres peticiones.

### CA-HIS-13 · RF-AUD-01 — Lo que no es una transición no se audita
- **Dado:** una orden.
- **Cuando:** C1 la cotiza y T1 registra una prueba de rendimiento.
- **Entonces:** no se crea ningún registro de auditoría. La cotización queda en el historial (CA-HIS-06) y la prueba en la orden, como hoy.
- **Comprobación:** contar registros de auditoría antes y después.

## 4. Reporte de tiempo de ensamble (RF-NEG-08, RF-REP-02 a 05)

### CA-HIS-14 · RF-REP-02, RF-REP-03 — Tiempo promedio de ensamble por técnico
- **Dado:** órdenes en las que se registró el armado completo:
  - T1 armó dos, de 2 h y de 4 h;
  - T2 armó una, de 1 h.
- **Cuando:** A1 consulta el reporte con un rango que las incluye.
- **Entonces:** cada fila trae el técnico (id y nombre), las órdenes armadas, y el promedio, el mínimo y el máximo en minutos:
  - T1: 2 órdenes, promedio 180, mínimo 120, máximo 240;
  - T2: 1 orden, 60 en los tres valores.
  
  Además:
  - la respuesta trae el total de órdenes y el promedio general;
  - las filas van del promedio más alto al más bajo;
  - la agrupación se hace en la base, no en C#.
- **Comprobación:** prueba automatizada que avanza el reloj y revisa la consulta SQL; en Docker, con tiempos reales cortos.

### CA-HIS-15 · RF-REP-05, Hallazgo 3 (Asignación 3) — El rango se aplica a la fecha en que terminó el armado
- **Dado:** los datos de CA-HIS-14, con armados terminados en días distintos.
- **Cuando:** se consulta:
  - con dos rangos distintos;
  - sin `desde` o sin `hasta`, o con una fecha mal escrita;
  - con `desde` mayor que `hasta`;
  - con los bordes del calendario (`0001-01-01` a `9999-12-31`).
- **Entonces:**
  - los dos rangos dan resultados distintos, y el día de `hasta` se incluye completo;
  - las consultas incompletas, mal escritas o invertidas responden `400` con el mensaje de los otros reportes;
  - los bordes del calendario responden `200`.
- **Comprobación:** las peticiones; las pruebas del rango.

### CA-HIS-16 · RF-REP-04, RD-06 — Cada rol ve lo suyo
- **Dado:** los datos de CA-HIS-14.
- **Cuando:** consultan:
  - A1;
  - T1;
  - C1, que no es técnico;
  - alguien sin sesión.
- **Entonces:**
  - A1 ve las filas de T1 y T2;
  - T1 ve solo su fila;
  - C1 recibe `200` sin filas;
  - sin sesión responde `401`.

  El alcance sale de la sesión, nunca de un parámetro.
- **Comprobación:** las cuatro peticiones; prueba automatizada del alcance.

### CA-HIS-17 · RF-REP-03 — Sin tiempo de armado conocido, la orden no entra
- **Dado:** una orden anterior a la migración que pasa a `EnPruebas`, cuyo inicio del armado no se registró.
- **Cuando:** se consulta el reporte.
- **Entonces:** esa orden no cuenta, ni con tiempo cero ni con el tiempo desde la cotización.
- **Comprobación:** prueba automatizada con una orden sin la entrada `EnArmado`.

## 5. Transversales

### CA-HIS-18 · RD-09 — El historial sobrevive al reinicio
- **Dado:** el flujo completo de CA-HIS-07.
- **Cuando:** se ejecuta `docker compose down` (sin `-v`) y otra vez `docker compose up`.
- **Entonces:** el historial, los registros de auditoría y el reporte siguen iguales.
- **Comprobación:** comparar las respuestas antes y después.

### CA-HIS-19 · Revisión acumulada — Lo anterior sigue funcionando
- **Dado:** el cambio terminado.
- **Cuando:** se ejecutan las pruebas automatizadas y las comprobaciones de las entregas anteriores en Docker.
- **Entonces:**
  - todas pasan;
  - las respuestas existentes solo ganan campos (`historial`), no cambian ni pierden ninguno;
  - las peticiones de `API.http` siguen funcionando.
- **Comprobación:** `dotnet test P3-PCBuilderOps.slnx` y las baterías de las Prácticas 3 a 5 y las Asignaciones 3 y 4.
