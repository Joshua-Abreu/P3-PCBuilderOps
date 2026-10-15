# Decisiones de diseño

Registro breve de decisiones y su motivo, para poder defenderlas (RD-01) y para que ningún agente las deshaga por error. Cada entrada: fecha, decisión, por qué y requisitos relacionados. Las pendientes van al final.

## Tomadas

### 2026-10-13 · Sesión JWT validada contra la base de datos (RF-CA-12, RF-CA-18)
Cada token lleva `jti` = `Id` de `SesionUsuario` y `OnTokenValidated` comprueba que la sesión exista, no esté revocada ni vencida y que el usuario siga activo.
**Por qué:** un JWT por sí solo no puede revocarse; así el cierre de sesión, el cambio de contraseña y la desactivación invalidan las credenciales al instante.

### 2026-10-13 · Cola de correos con despachador independiente (RF-NOT-08, RF-NOT-09)
Las operaciones solo insertan en `CorreosEnCola` dentro de su transacción; `src/Workers/EmailDispatcher` envía los pendientes y termina.
**Por qué:** la operación no depende de que el servidor SMTP responda, y el envío puede repetirse sin duplicar correos.

### 2026-10-15 · Administrador inicial desde variables de entorno (RF-CA-04, RD-10)
Al arrancar, si no hay ningún Administrador, la API crea (o promueve) la cuenta de `ADMIN_CORREO` / `ADMIN_PASSWORD`. Si ya existe uno, no hace nada.
**Por qué:** el registro público solo crea Estándar y el maestro necesita un Administrador para verificar el bloque de roles. Hacerlo por entorno evita credenciales en el repositorio y en las migraciones.

### 2026-10-15 · Configuración JWT en un único punto, sin valor de respaldo (RD-10)
`ConfiguracionJwt.DesdeEntorno()` lee `JWT_SECRET` (mínimo 32 bytes) y la API no arranca si falta. La emisión (`JwtService`) y la validación (`Program.cs`) usan la misma instancia.
**Por qué:** una clave por defecto escrita en el código es pública y permitiría falsificar tokens; además, dos lecturas distintas de la clave causaban errores 500 cuando no coincidían.

### 2026-10-15 · Restablecimiento forzado reutiliza el flujo de recuperación (RF-CA-13)
El Administrador invalida la contraseña (hash aleatorio), revoca sesiones y códigos previos, y se encola un código nuevo que el usuario usa en `POST /api/auth/restablecer-password`.
**Por qué:** cumple "el usuario no puede seguir usando la anterior y recibe por la cola el código", sin crear un segundo flujo de cambio de contraseña.

### 2026-10-15 · URL base de los enlaces en un único helper
`UrlBaseAplicacion.Obtener(configuration)` construye la URL absoluta de todos los enlaces de correo.
**Por qué:** había tres copias de la lógica y una generaba enlaces relativos rotos.

### 2026-10-15 · La etiqueta `practica-1` no se mueve
Los arreglos posteriores están en `main` (PR #16), no en la etiqueta.
**Por qué:** la entrega ya fue calificada.

### 2026-10-15 · Funcionalidades del dominio con prefijo `FN-`
Las funcionalidades propias de PC Builder Ops se identifican como FN-01 a FN-05 en `docs/SRS.md` y `docs/requisitos/negocio.md`. `RF-NEG-01` a `RF-NEG-10` quedan reservados para los requisitos del maestro.
**Por qué:** el SRS usaba RF-NEG-01 a 05 con otro significado; los IDs del maestro mandan porque son los que se citan en commits, PRs y en la calificación.

### 2026-10-15 · Instrucciones del agente en `AGENTS.md`
`CLAUDE.md` solo importa `AGENTS.md`; los requisitos viven en `docs/requisitos/` y cada entrega tiene su archivo en `docs/entregas/`.
**Por qué:** una sola fuente compartida por Claude Code y opencode, y contexto completo sin depender de los PDFs.

## Pendientes

- **Rol de los técnicos.** Solo existen dos roles (RF-CA-04). Opción A: los técnicos son Administradores (simple, pero pueden cambiar roles y ver auditoría). Opción B: los técnicos son Estándar y obtienen el permiso "operar órdenes del taller" mediante Gestión de permisos (da un caso real a RF-GP-05). Decidir antes de la Práctica 2.
- **Retrabajo `EnPruebas → EnArmado`.** Más realista cuando una prueba falla y da más casos a la suite de la semana 8. Decidir antes de la semana 8.
- **Atributo `Nombre` en `Usuario`.** La tabla de entidades del Core lo exige y hoy no existe.
