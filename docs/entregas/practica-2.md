# Práctica 2 — Una suite que detecta defectos: Gestión de permisos

| Dato | Valor |
|------|-------|
| Bloque / semana | Bloque 3 · Semana 7 |
| Puntos | 8 · Individual |
| Fecha límite | **Domingo 25 de octubre de 2026, 11:59 p. m.** Tarde: hasta 24 h más con 50 % de penalidad. |
| Cómo se entrega | `git tag practica-2` + `git tag practica-2-defecto-N <commit>` (N = 1 a 6) + `git push origin --tags`; URL del repo y `practica-2` en Moodle. |
| Variante | **C** (matrícula terminada en 7) |
| Rama(s) | `feature/practica-2-suite`; los commits de defecto no van a ninguna rama, solo a sus etiquetas. |
| Estado | ⏳ En curso |

> *Una suite se mide por si falla cuando el código tiene un defecto, no por cuánto código recorre.*

## 1. Alcance

### 1.1 La suite sobre Gestión de permisos
Cada RF-GP-01 a 07 con al menos una prueba que lo identifica por su ID (nombre o comentario):

| ID | Lo que la suite verifica |
|----|--------------------------|
| RF-GP-01 | La solicitud nace en estado Pendiente. |
| RF-GP-02 | Un Estándar no ve las solicitudes de otros usuarios. |
| RF-GP-03 | Queda registrado quién aprobó y cuándo. |
| RF-GP-04 | Sin motivo (ausente o vacío) el rechazo no se completa y el estado no cambia. |
| RF-GP-05 | Tras aprobar, la solicitud llega a Aplicada y el permiso pedido queda efectivo. |
| RF-GP-06 | Aprobar dos veces, pasar de Rechazada a Aplicada y cualquier otra transición fuera de la tabla se rechazan y el estado no cambia. |
| RF-GP-07 | Un Administrador que aprueba o rechaza una solicitud creada por él recibe un rechazo. |

Mismas condiciones que la Asignación 2: sin levantar la aplicación, independientes, preparar–actuar–verificar y un único comando en la sección «Pruebas» del README. En `practica-2` la suite pasa.

### 1.2 Defectos plantados — Variante C

| N | Requisito | Qué plantar | Comportamiento con el defecto |
|---|-----------|-------------|-------------------------------|
| 1 | RF-GP-02 | En la consulta de pendientes, el filtro «estado es Pendiente» pasa a «estado no es Rechazada». | La consulta de pendientes devuelve también aprobadas y aplicadas. |
| 2 | RF-GP-06 | En la declaración de transiciones, agregar `Aplicada → Aprobada`. | Una solicitud ya aplicada puede volver a Aprobada. |
| 3 | RF-GP-05 | En la operación que aprueba, eliminar la llamada que aplica el cambio de permiso. | La solicitud queda Aprobada y el solicitante no obtiene el permiso. |
| 4 | RF-GP-04 | En la operación que rechaza, la condición «estado es Pendiente» pasa a «estado no es Aplicada». | Una solicitud ya aprobada puede rechazarse. |
| 5 | RF-GP-01 | En la declaración de estados, el estado inicial pasa de Pendiente a Aprobada. | Toda solicitud nace ya aprobada. |
| 6 | RF-GP-07 | En la operación que rechaza, eliminar la verificación de solicitud propia. | Un Administrador puede rechazar su propia solicitud, aunque aprobarla siga bloqueado. |

Por cada defecto N: etiqueta `practica-2-defecto-N` sobre un commit **cuyo padre es `practica-2`**, que modifica **solo código de Gestión de permisos** (ningún archivo de pruebas) y cuyo mensaje dice **qué archivo y qué línea** cambió. Sobre ese commit, el comando del README hace fallar al menos una prueba.

El README tiene una sección **Defectos** con una línea por defecto: la prueba que lo detecta y el comportamiento que comprueba.

### 1.3 Revisión acumulada: Control de acceso
Desde `practica-2` y siguiendo el README: registro con activación por correo, inicio y cierre de sesión, rechazo a un Estándar en una operación de Administrador y recuperación de contraseña.

### Fuera de alcance
- Bitácora (esta práctica no la pide).
- Auditoría, notificaciones y reportes de Gestión de permisos.

## 2. Rúbrica (8 puntos)

| Criterio | Puntos | Para "Logrado" |
|----------|--------|----------------|
| Suite sobre RF-GP-01 a 07 | 1.5 | Pasa con un solo comando documentado, cada RF-GP tiene al menos una prueba que lo identifica, pruebas independientes y sin levantar la aplicación. |
| Detección de los seis defectos | 3.0 | Con cada defecto plantado falla al menos una prueba (0.5 por defecto) y sobre `practica-2` todas pasan. |
| Forma de los defectos plantados | 1.5 | Los seis commits cuelgan de `practica-2`, plantan exactamente el defecto de la variante sin tocar pruebas, el mensaje dice archivo y línea, y el README identifica la prueba que detecta cada uno. |
| Revisión acumulada: Control de acceso | 2.0 | Funcionan los cuatro frentes. |

## 3. Qué revisa el maestro

1. `git clone` y `git checkout practica-2`.
2. El comando de la sección «Pruebas» del README: todas pasan.
3. Por cada defecto: que el padre de `practica-2-defecto-N` sea `practica-2`; `git diff practica-2 practica-2-defecto-N --stat` (solo código de Gestión de permisos); el mensaje del commit; y el mismo comando de pruebas sobre la etiqueta, que debe reportar al menos una prueba fallida.
4. La sección «Defectos» del README contra las pruebas que fallan, y los nombres de las pruebas contra los requisitos RF-GP.
5. Control de acceso, arrancado desde el README, en los cuatro frentes.

## 4. Decisiones y hallazgos

- **Análisis previo (suite de la Asignación 2, 59 pruebas):** detectaba 5 de 6 defectos. El defecto 4 no lo detectaba ninguna prueba, porque ninguna intentaba rechazar una solicitud Aprobada o ya Rechazada.
- **Defecto 3:** aplicar el permiso estaba repartido en dos líneas (`solicitud.Aplicar()` y el otorgamiento). Se unifica en una sola llamada para que eliminarla produzca exactamente el comportamiento descrito: queda Aprobada y sin permiso.
- **Defecto 4:** la regla "solo se rechaza una Pendiente" vive en la tabla de transiciones (`ValidarTransicion`), no en un `if`. Se planta sustituyendo esa validación por la condición equivocada «estado no es Aplicada».
- Los commits de defecto se crean en modo *detached* sobre `practica-2` y solo existen en sus etiquetas; `main` nunca contiene un defecto.

## 5. Checklist de estado

- [x] Ficha creada
- [x] Aplicar permiso unificado en una sola llamada (defecto 3)
- [x] Pruebas nuevas que detectan el defecto 4
- [x] Los 6 defectos detectados en una copia de prueba antes de etiquetar
- [x] README: sección «Defectos»
- [ ] Revisión acumulada de los 4 frentes de Control de acceso
- [ ] PR fusionado y etiqueta `practica-2` subida
- [ ] Etiquetas `practica-2-defecto-1` a `6` subidas, cada una con padre `practica-2`
- [ ] Verificación desde un clon limpio de GitHub: `practica-2` pasa; cada defecto falla y solo toca código de permisos
- [ ] Entrega en Moodle
- [ ] `AGENTS.md` sección 0 actualizada
