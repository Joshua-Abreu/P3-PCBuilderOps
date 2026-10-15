# Práctica 1 — Control de acceso completo

| Dato | Valor |
|------|-------|
| Bloque / semana | Bloque 2 · Semana 4 |
| Puntos | 8 · Individual |
| Cómo se entrega | `git tag practica-1` + `git push origin --tags`; URL del repo y nombre de la etiqueta en Moodle |
| Ramas | `feature/registro-activacion`, `feature/autenticacion-sesion`, `feature/gestion-contrasena`, `feature/administracion-usuarios`, `feature/cola-correos-smtp`, `feature/maquina-estados-negocio`, `fix/retroalimentacion-practica-1`, `fix/practica-1-pendientes` |
| Estado | ✅ **Calificada.** La etiqueta `practica-1` queda en el merge del PR #15 y no se mueve. Los arreglos posteriores están en `main` (PR #16) y cuentan para la revisión acumulada. |

## 1. Alcance

Pieza 1 del Core completa, más la estructura (sin pruebas) de la máquina de estados del negocio. Requisitos detallados en `docs/requisitos/core.md`, sección 3.

| Bloque | IDs |
|--------|-----|
| 1.1 Registro y activación | RF-CA-01, RF-CA-02, RF-CA-14 (política: 8+ caracteres con letras y números), RF-CA-15 (nace inactivo, enlace de un solo uso con vencimiento), RF-CA-16 (abrir el enlace activa), RF-CA-17 (reenvío con respuesta idéntica que invalida el anterior) |
| 1.2 Sesión | RF-CA-03, RF-CA-07, RF-CA-18 (cierre de sesión), RF-CA-19 (bloqueo de 15 min tras 5 fallos consecutivos) |
| 1.3 Roles y administración | RF-CA-04, RF-CA-05, RF-CA-06, RF-CA-08, RF-CA-20 (desactivar/reactivar; no desactivarse a sí mismo), RF-CA-21 (listado sin hashes ni tokens) |
| 1.4 Contraseñas | RF-CA-09 a RF-CA-13, RF-CA-22 (cambio con sesión exigiendo la actual) |
| 1.5 Cola mínima de correo | RF-NOT-08, RF-NOT-09, RF-NOT-12, RF-NOT-13 |
| 1.6 Máquina de estados del negocio | RF-NEG-03, RF-NEG-04, RF-NEG-05, RD-04 + tabla en `docs/maquina-de-estados.md` |
| Diseño | RD-05, RD-06, RD-07, RD-08, RD-09, RD-10 |

### Fuera de alcance
- Reintentos, estado fallido, último error y consulta de la cola por el Administrador (semana 11, pieza 4).
- Auditoría de RF-CA-08, RF-CA-13 y RF-CA-20 (semana 14, pieza 6).
- Pruebas de la máquina de estados del negocio (semana 8).

## 2. Rúbrica (8 puntos)

| Criterio | Puntos | Para "Logrado" |
|----------|--------|----------------|
| Registro y activación | 1.5 | Correo único, hash con sal, política, cuenta inactiva hasta abrir el enlace, enlace de un solo uso con vencimiento y reenvío con respuesta idéntica. |
| Sesión | 1.5 | Rechazos idénticos, consulta del autenticado protegida, cierre de sesión que invalida la credencial y bloqueo tras cinco fallos. |
| Roles y administración | 1.5 | Exigencia de rol legible en un punto, rechazo en servidor aunque la petición sea manual, cambio de rol y desactivación reservados al Administrador, listado sin datos sensibles. |
| Contraseñas | 1.5 | Código de un solo uso con vencimiento, respuesta idéntica, sesiones anteriores invalidadas, restablecimiento forzado por Administrador y cambio con sesión que exige la actual. |
| Correo por cola | 0.5 | La operación termina sin SMTP, el enviador independiente entrega el correo real, no duplica envíos y las credenciales vienen del entorno. |
| Máquina de estados del negocio | 0.5 | Entidad con estado, 3–5 estados en un lugar, transiciones en un lugar con una prohibida y un terminal, tabla documentada. |
| Historial, PRs y README | 1.0 | 4+ PRs con las cuatro secciones, commits atómicos que citan el requisito, nada indebido en el historial y un README que permite ejecutar y verificar cada criterio sin preguntar. |

**Revisión acumulada:** desde la Práctica 2, 2 de los 8 puntos de cada práctica verifican que registro con activación, inicio de sesión, rechazo por rol y recuperación de contraseña sigan funcionando.

## 3. Qué revisa el maestro

1. `git clone`, `git checkout practica-1`, variables según el README y ejecución.
2. Registrarse con su correo; iniciar sesión antes de activar; abrir el enlace dos veces; registrar el mismo correo otra vez.
3. Contraseña de 5 caracteres y correo mal formado: rechazo controlado.
4. Leer el almacenamiento: la contraseña no aparece; dos usuarios con la misma contraseña tienen valores distintos.
5. Contraseña incorrecta y correo inexistente: rechazos idénticos. Cinco fallos y luego la contraseña correcta.
6. Cerrar sesión y reutilizar la credencial.
7. Como Estándar, invocar a mano una operación de Administrador; intentar cambiar el rol propio.
8. Como Administrador: listar, cambiar un rol, desactivar un usuario con sesión abierta y probar esa sesión, intentar desactivarse a sí mismo.
9. Recuperación con correo inexistente y existente; usar el código dos veces; probar contraseña vieja y nueva; probar una credencial anterior al cambio.
10. Forzar el restablecimiento como Administrador. Cambiar la contraseña con sesión indicando una actual incorrecta.
11. Sin SMTP, registrar un usuario: la operación termina y el correo queda pendiente. Ejecutar el enviador dos veces.
12. Reiniciar la aplicación: los usuarios siguen ahí.
13. Localizar el punto único de estados y transiciones del negocio y leer `docs/maquina-de-estados.md`.
14. `git log --oneline --graph --all`, `git ls-files` y `git log -p` buscando credenciales; los PRs en GitHub.

## 4. Decisiones y hallazgos

- El endpoint de RF-CA-13 se había borrado por accidente en `f94e141`; se restauró en el PR #16 con el correo por la cola.
- No había forma de obtener el primer Administrador → se crea desde `ADMIN_CORREO` / `ADMIN_PASSWORD`.
- Un `JWT_SECRET` quedó en commits antiguos de `launchSettings.json`; se invalidó y se reemplazó. No se reescribió el historial.
- Se eliminó la clave JWT de respaldo del código y del README.
- Ningún commit anterior al PR #16 citaba IDs de requisitos; desde el PR #16 todos lo hacen.

## 5. Checklist de estado (verificado en `main` tras el PR #16)

- [x] 1.1 Registro y activación (RF-CA-01, 02, 14, 15, 16, 17)
- [x] 1.2 Sesión (RF-CA-03, 07, 18, 19)
- [x] 1.3 Roles y administración (RF-CA-04, 05, 06, 08, 20, 21) — con Administrador inicial por entorno
- [x] 1.4 Contraseñas (RF-CA-09 a 13, 22) — RF-CA-13 restaurado en el PR #16
- [x] 1.5 Cola mínima (RF-NOT-08, 09, 12, 13)
- [x] 1.6 Estructura de la máquina de estados del negocio — con estados distintos a la propuesta (se alinean en la semana 8)
- [x] README con cómo provocar cada criterio
- [x] Commits con IDs y PRs con las cuatro secciones (desde el PR #16)
- [ ] Historial sin credenciales — queda un `JWT_SECRET` ya invalidado en commits antiguos (decisión: no reescribir)
