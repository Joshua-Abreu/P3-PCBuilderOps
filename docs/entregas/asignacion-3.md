# Asignación 3 — Auditas lo que el agente entregó y lo corriges

| Dato | Valor |
|------|-------|
| Bloque / semana | Bloque 5 · Semana 11 |
| Puntos | 5 · Individual |
| Fecha límite | La que indica la plataforma virtual. Tarde: hasta 24 h más con 50 % de penalidad; luego no se acepta. |
| Cómo se entrega | `git tag asignacion-3` + `git push origin --tags`; URL del repo y `asignacion-3` en Moodle |
| Rama(s) | `docs/asignacion-3` (entrega, auditoría y bitácora); una rama `fix/...` por hallazgo |
| Estado | ⏳ En curso |

> *El agente entregó notificaciones, cola de correos y reportes en la Práctica 5. Que el código corra no significa que esté bien: se revisa como el trabajo de otra persona, contra los criterios propios, ejecutándolo, y se corrige lo encontrado.*

## 1. Alcance

### 1.1 Auditoría

Se audita el código actual de notificaciones, cola de correos y reportes (lo que el agente escribió en la Práctica 5), ejecutando y provocando casos, contra tres referencias:

| Referencia | Dónde |
|------------|-------|
| Criterios de aceptación y requisitos RF-NOT y RF-REP | `docs/criterios-notificaciones-reportes.md`, `docs/requisitos/core.md` |
| Requisitos de diseño RD-02, RD-03, RD-06, RD-07, RD-08, RD-10 y RD-11 | `AGENTS.md` sección 3 |
| Casos que los criterios no cubrían | Entradas vacías o fuera de rango; ningún Administrador activo; dos solicitudes simultáneas; rango de fechas sin datos; correo con destinatario inválido |

Un hallazgo se puede reproducir, no es una opinión de estilo. Mínimo **tres**, al menos uno de notificaciones y uno de reportes. Un criterio incompleto que dejó pasar el defecto también es un hallazgo.

### 1.2 Correcciones

- Se corrigen todos los hallazgos declarados como corregidos; uno de gravedad baja puede quedar sin corregir si se declara y se explica.
- **Un commit por hallazgo**, que cita su número en el mensaje.
- Se corrige la causa: si la regla falló en un lugar, se revisa si se repite en otro.
- Si la corrección la hace el agente, el usuario le da la instrucción y verifica el resultado antes de aceptarlo.
- El proyecto sigue levantando con el README y los seis frentes de la Práctica 5 siguen pasando.

### 1.3 Una prueba por hallazgo

- Cada hallazgo corregido tiene una prueba nueva que **falla sobre el código anterior** a la corrección y **pasa sobre el corregido**.
- xUnit, sin levantar la aplicación (RD-12), con preparar, actuar y verificar, independientes y con nombre que dice el comportamiento.
- El nombre o un comentario cita el hallazgo.
- Un único comando en la sección «Pruebas» del README ejecuta toda la suite desde un clon limpio: `dotnet test P3-PCBuilderOps.slnx`.

### 1.4 Bitácora de delegación

`docs/bitacora-asignacion-3.md`, organizada por hallazgo:

| Campo | Qué debe decir |
|-------|----------------|
| Hallazgo | Número, título corto y gravedad (alta, media o baja) |
| Qué se pidió | La instrucción o el criterio con que se delegó esa parte en la Práctica 5 |
| Qué entregó el agente | Qué hacía realmente, con la evidencia: salida, captura o commit |
| Por qué pasó | Criterio ausente, criterio ambiguo, instrucción incompleta o revisión insuficiente |
| Corrección | Quién la hizo, la instrucción dada al agente y el commit |
| Qué cambias | Qué se agregó al archivo de contexto o a los criterios; debe verse en el tag |

### Fuera de alcance
- Funcionalidad nueva (Auditoría, Pieza 6) y el frontend.
- Reescribir criterios existentes para que coincidan con el código: los cambios a los criterios solo **agregan** lo que faltó.

## 2. Rúbrica

| Criterio | Puntos | Qué se necesita para "Logrado" |
|----------|--------|--------------------------------|
| Auditoría contra los criterios | 1.5 | Al menos tres hallazgos que se confirman al ejecutar; al menos uno de notificaciones y uno de reportes; cada uno cita el criterio o requisito incumplido, la evidencia y su gravedad. |
| Correcciones en el repositorio | 1.5 | Cada hallazgo corregido se reproduce antes de su commit y no en el tag; un commit propio por hallazgo; el proyecto levanta y los seis frentes funcionan. |
| Prueba de regresión por hallazgo | 1.0 | Cada hallazgo corregido tiene una prueba que falla antes y pasa después; un solo comando del README las ejecuta y toda la suite pasa. |
| Bitácora de delegación | 1.0 | Por hallazgo: qué se pidió, qué entregó, por qué el criterio no lo detectó, cómo se corrigió (con la instrucción al agente) y qué cambió en el contexto o los criterios, con evidencia de la sesión. |
| **Total** | **5.0** | |

## 3. Qué revisa el maestro exactamente

1. `git clone` y `git checkout asignacion-3`.
2. `docs/bitacora-asignacion-3.md`: los hallazgos, su evidencia y los campos de cada uno.
3. Reproducir cada hallazgo en el commit anterior a su corrección y comprobar que ya no se reproduce en el tag.
4. `git log`: un commit por hallazgo, con el hallazgo citado en el mensaje.
5. Cada prueba de regresión sobre el código anterior a la corrección (debe fallar) y sobre el tag (debe pasar).
6. El comando de la sección «Pruebas» del README, tal como está escrito.
7. El archivo de contexto y los criterios: que reflejan los cambios declarados en la bitácora.
8. El proyecto arrancado desde el README y los seis frentes de la revisión acumulada de la Práctica 5.

## 4. Decisiones y dudas de esta entrega

- **La prueba va en el mismo commit que la corrección.** Antes del commit se corre sobre el código sin corregir y la salida con el fallo se copia en la bitácora; el maestro la reproduce llevando el archivo de la prueba al commit anterior.
- **Cada corrección la pide el usuario con una instrucción al agente**, que se copia tal cual en la bitácora.

## 5. Checklist de estado

- [x] Auditoría ejecutada en un entorno aislado y hallazgos registrados con evidencia (4 hallazgos: 2 de notificaciones, 1 de reportes y 1 de fechas en notificaciones y cola)
- [x] Un commit por hallazgo, con su prueba de regresión
- [x] Cada prueba falla antes de su corrección y pasa después (código anterior + pruebas finales, en un contenedor)
- [x] Contexto y criterios actualizados según «Qué cambias» (`AGENTS.md` sección 12; CA-NOT-14, 15, 16 y CA-REP-08)
- [x] `docs/bitacora-asignacion-3.md` completa
- [x] `dotnet test P3-PCBuilderOps.slnx` pasa: 336 pruebas
- [x] Revisión acumulada: el proyecto levanta con el README y pasan los seis frentes (y las 237 comprobaciones de la Práctica 5)
- [x] `AGENTS.md` sección 0 actualizada
- [ ] Etiqueta `asignacion-3` creada por el usuario

## 6. Anotado para después (fuera del alcance)

- **Registro simultáneo con el mismo correo** (Control de acceso, Práctica 1): sigue el patrón del Hallazgo 2 y podría responder con error del servidor en lugar de `400`.
- **Reserva simultánea de la misma existencia por dos órdenes distintas** (FN-02, Práctica 3): el componente no tiene token de concurrencia.
