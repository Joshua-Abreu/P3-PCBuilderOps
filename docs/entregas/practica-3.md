# Práctica 3 — Tu módulo de negocio, sus pruebas y su integración

| Dato | Valor |
|------|-------|
| Bloque / semana | Bloque 3 · Semana 8 |
| Puntos | 8 · Individual |
| Fecha límite | La que indica Moodle (**por confirmar**). Tarde: hasta 24 h más con 50 % de penalidad; luego no se acepta. |
| Cómo se entrega | `git tag practica-3` + `git tag practica-3-defecto-N <commit>` (N = 1 a 4) + `git push origin --tags`; URL del repo y `practica-3` en Moodle. |
| Rama(s) | `feature/practica-3-negocio`; los commits de defecto no van a ninguna rama, solo a sus etiquetas. |
| Estado | ⏳ En curso |

## 1. Alcance

### 1.1 Módulo de negocio implementado

| ID | Requisito | Criterio de aceptación |
|----|-----------|------------------------|
| RF-NEG-02 | Al menos cinco funcionalidades separables. | Cada una se puede probar por separado y el README explica cómo probarla. |
| RF-NEG-03 | Máquina de estados propia, entre 3 y 5 estados. | Los estados están declarados en un solo lugar del código. |
| RF-NEG-04 | Al menos una transición prohibida explícita. | Intentarla se rechaza y el estado no cambia. |
| RF-NEG-05 | Al menos un estado terminal. | Ninguna transición parte de ese estado. |
| RF-NEG-06 | Operaciones protegidas por Control de acceso. | Ninguna se ejecuta sin sesión válida y sin verificación de rol. |
| RF-NEG-09 | Máquina independiente de la de Gestión de permisos. | Cambiar los estados de permisos no obliga a cambiar los del negocio. |

Aplican además RD-04, RD-07 y RD-09. `docs/maquina-de-estados.md` debe reflejar lo implementado.

Al menos una operación requiere un permiso de Gestión de permisos: el Estándar lo solicita, un Administrador lo aprueba y solo entonces puede ejecutarla. Aquí es **FN-03, registrar una prueba de rendimiento**, con el permiso `OperarTaller`.

### Funcionalidades

| ID | Operación | Quién | Regla |
|----|-----------|-------|-------|
| FN-01 | Cotizar una orden | Estándar (cliente) | Socket de CPU y placa coincide y la fuente cubre el consumo; si no, `400`. |
| FN-02 | Asignar técnico e iniciar armado | Administrador | El técnico tiene `OperarTaller`; se reserva stock. |
| FN-03 | Registrar una prueba de rendimiento | Estándar con `OperarTaller`, o Administrador | La orden está en `EnPruebas`. |
| FN-04 | Pasar a pruebas y aprobar la entrega | Estándar con `OperarTaller`, o Administrador | Para entregar: al menos una prueba y la más reciente aprobada. |
| FN-05 | Consultar órdenes | Cliente | Solo ve las suyas. |

### 1.2 Suite de la máquina de estados
- Proyecto `tests/PCBuilderOps.Tests`, sin levantar la aplicación (RD-12).
- Al menos una prueba por cada fila de las tablas de `docs/maquina-de-estados.md`. En las prohibidas se verifica el rechazo y que el estado no cambió.
- Una prueba de que del estado terminal no se sale.
- Tres fases identificables, un solo comportamiento por prueba, nombre que lo dice, pruebas independientes.
- Un único comando en la sección «Pruebas» del README, desde un clon limpio.

### 1.3 Defectos plantados

| N | Qué plantar | Comportamiento con el defecto | Lo detecta |
|---|-------------|-------------------------------|------------|
| 1 | Permitir la primera prohibida de la tabla: `Cotizada → Entregada`. | Una transición prohibida pasa a ser permitida. | Prueba de esa fila |
| 2 | Agregar una transición que parta de `Entregada`. | El estado terminal deja de serlo. | Prueba del estado terminal |
| 3 | Eliminar la verificación de la condición de `Cotizada → EnArmado` (el técnico tiene `OperarTaller`). | La transición se ejecuta sin cumplir su condición. | Prueba de esa condición |
| 4 | En FN-03, eliminar la consulta a Gestión de permisos. | La operación se permite sin permiso aprobado. | **Prueba de integración** |

Por cada defecto N: etiqueta `practica-3-defecto-N` sobre un commit **cuyo padre es `practica-3`**, que modifica **solo código de producción** y cuyo mensaje dice **qué archivo y qué línea** cambió. Sobre ese commit, el comando del README hace fallar al menos una prueba. El README tiene una sección **Defectos** con la prueba que detecta cada uno y el comportamiento que comprueba.

### 1.4 Prueba de integración
Usa Gestión de permisos y el módulo reales, sin dobles que reemplacen a ninguno y sin la aplicación levantada. Verifica las dos caras: FN-03 se rechaza mientras la solicitud no está aprobada y se permite después de que un Administrador la aprueba.

### 1.5 Revisión acumulada
Desde `practica-3` y siguiendo el README al pie de la letra:
1. Control de acceso: registro con activación por correo.
2. Control de acceso: inicio y cierre de sesión.
3. Control de acceso: rechazo a un Estándar en una operación de Administrador.
4. Control de acceso: recuperación de contraseña.
5. Gestión de permisos: las solicitudes se crean, se aprueban y se rechazan, y las transiciones fuera de la tabla se rechazan.

### Fuera de alcance
- Notificaciones y reportes del negocio (RF-NEG-07 y RF-NEG-08, semana 10).
- Adjuntar documentos a una entidad (RF-NEG-10, opcional).
- Bitácora (esta práctica no la pide).

## 2. Rúbrica (8 puntos)

| Criterio | Puntos | Para "Logrado" |
|----------|--------|----------------|
| Módulo de negocio (RF-NEG-02 a 06 y 09) | 1.5 | Al menos cinco funcionalidades separables, protegidas por Control de acceso, con estados y transiciones en un solo lugar, la prohibida y el terminal rechazados, y el README explica cómo provocar cada una. |
| Suite de la máquina de estados | 1.0 | Un comando documentado la ejecuta desde un clon limpio, pasa, y hay al menos una prueba por cada fila de las tablas y por el estado terminal. |
| Detección de los defectos 1 a 3 y su forma | 1.5 | Con los tres falla al menos una prueba; sobre `practica-3` todas pasan; los commits cuelgan de `practica-3`, no tocan pruebas, el mensaje dice archivo y línea, y el README identifica la prueba que detecta cada uno. |
| Prueba de integración | 2.0 | Usa los componentes reales, verifica el rechazo antes de aprobar y la ejecución después, y falla con el defecto 4 plantado (etiqueta con la forma pedida). |
| Revisión acumulada | 2.0 | Funcionan los cinco frentes. Con dos a cuatro, la mitad. |

## 3. Qué revisa el maestro

1. `git clone` y `git checkout practica-3`.
2. El comando de la sección «Pruebas» del README: todas pasan.
3. Por cada defecto: que el padre de `practica-3-defecto-N` sea `practica-3`; `git diff practica-3 practica-3-defecto-N --stat` (solo código de producción); el mensaje del commit; y el mismo comando sobre la etiqueta, que debe reportar al menos una prueba fallida.
4. El README: la sección «Defectos» contra las pruebas que fallan, y cómo provocar cada funcionalidad.
5. `docs/maquina-de-estados.md` contra el código, y la prueba de integración.
6. Control de acceso y Gestión de permisos, arrancados desde el README, en los cinco frentes.

## 4. Decisiones y hallazgos

- **Sin retrabajo:** la máquina conserva los estados de la propuesta aprobada y la entrega depende de la prueba más reciente. Ver `docs/decisiones.md`.
- **El permiso se comprueba en el servicio del módulo, no en una política de `src/API`:** la prueba de integración corre sin HTTP y no pasaría por la política; con ella el defecto 4 tampoco permitiría la operación en la aplicación real.
- **Punto de partida:** el módulo está vacío, el negocio vive en `src/Core/Ordenes/` con otros estados y `MaquinaEstadosOrden` no la usa ninguna operación.
- **Filas sin operación propia:** ninguna operación pública lleva una orden a `Cotizada`, así que la suite ejerce todas las filas por `OrdenEnsamble.TransicionarA`, el único punto que cambia el estado. Es `internal` y solo lo ve el proyecto de pruebas.
- **Análisis previo de defectos (copia aparte, sin etiquetas todavía):** los cuatro hacen fallar al menos una prueba; el 4 lo detecta la prueba de integración.
- **README:** antes de etiquetar, cambiar `git checkout asignacion-2` por `git checkout practica-3`.

## 5. Checklist de estado

- [x] Ficha creada
- [x] Tablas de transiciones permitidas y prohibidas en `docs/maquina-de-estados.md`
- [x] Entidades, máquina de estados y migración en `src/Modules/PCBuilderOps`
- [x] `src/Core/Ordenes/` eliminado; `Core.csproj` no referencia al módulo (RD-03)
- [x] FN-01 a FN-05 con sus endpoints, protegidos (RF-NEG-06)
- [x] Suite con una prueba por cada fila y por el estado terminal
- [x] Prueba de integración con las dos caras
- [ ] README: cómo provocar cada funcionalidad, migraciones, «Pruebas», «Defectos» y `git checkout practica-3`
- [ ] `API.http` con el flujo del negocio
- [ ] Los 4 defectos detectados en una copia de prueba antes de etiquetar
- [ ] PR fusionado y etiqueta `practica-3` subida
- [ ] Etiquetas `practica-3-defecto-1` a `4` subidas, cada una con padre `practica-3`
- [ ] Verificación desde un clon limpio de GitHub: `practica-3` pasa; cada defecto falla y no toca pruebas
- [ ] Revisión acumulada de los 5 frentes
- [ ] Entrega en Moodle
- [ ] `AGENTS.md` sección 0 actualizada
