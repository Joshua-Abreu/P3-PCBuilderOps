# Bitácora de sesión con el agente - Asignación 4

Historial de auditoría (Pieza 6): quién cambió qué, cuándo y qué había antes.

## 1. Cómo se delegó

Antes de delegar se escribieron el contexto y los criterios, igual que en la Práctica 5:
- `docs/entregas/asignacion-4.md`: alcance, rúbrica y decisiones de diseño.
- `docs/criterios-auditoria.md`: 20 criterios dado / cuando / entonces, CA-AUD-01 a 20.
- La fila del archivo de criterios en el mapa de `AGENTS.md`.

Se fusionaron en el PR #45 (commit `7a57cce`) antes del primer commit del historial. Después se le dio al agente esta instrucción, copiada tal cual:

> «Implementa el historial de auditoría (Pieza 6) según docs/criterios-auditoria.md y docs/entregas/asignacion-4.md, en una rama nueva.
>
> - El registro se guarda en la misma operación que el cambio que audita, para que no pueda quedar un cambio sin su registro.
> - Audita la resolución de solicitudes, la subida y eliminación de documentos, el cambio de rol, la desactivación y reactivación de usuarios, el restablecimiento de contraseña (propio y forzado por un Administrador) y el cambio de la propia contraseña.
> - Nunca guardes contraseñas, hashes ni códigos de recuperación.
> - La consulta GET /api/auditoria es solo para el Administrador, con filtros por usuario, entidad y rango de fechas; un filtro inválido responde 400.
> - No debe existir ninguna operación que edite o borre registros.
> - El Core no puede depender del módulo de negocio (RD-03).
> - Escribe las pruebas: una por cada tipo de evento que verifique todos los campos, incluidos el valor anterior y el nuevo; una de que un Estándar no consulta; y una de que nada modifica ni borra el historial.
> - Actualiza el README (sección Auditoría y API.http).
>
> Antes de darlo por terminado, comprueba cada criterio CA-AUD-01 a 20 ejecutando la API en Docker y dime cuáles cumpliste, con la evidencia. Si algo no salió bien a la primera, dímelo y anótalo para la bitácora; no lo escondas.»

## 2. Qué devolvió el agente

En la rama `feature/historial-auditoria`:

- **Modelo** (`src/Core/Auditoria`):
  - la entidad `RegistroAuditoria`, sin setters públicos;
  - `IAuditor`, que agrega el registro al mismo `DbContext` de la operación sin guardar, así se guarda en el mismo `SaveChanges` que el cambio;
  - `IHistorialAuditoria`, que solo consulta y valida los filtros;
  - las acciones y las entidades como texto, para que el módulo pueda auditar sin que el Core lo conozca (RD-03).
- **Eventos** conectados en `GestionPermisosService`, `DocumentosService`, `UsuarioAdminService` y `PasswordService`:
  - `TokenRecuperacion` guarda ahora si el código lo pidió el usuario o un Administrador;
  - el restablecimiento forzado recibe qué Administrador lo hizo.
- **Inmutabilidad en cuatro capas:**
  - interfaces que solo registran y consultan;
  - la entidad sin setters públicos;
  - `AppDbContext` rechaza guardar un registro modificado o borrado;
  - el disparador `TR_RegistrosAuditoria_Inmutable` de SQL Server rechaza cualquier `UPDATE` o `DELETE`.
- **Consulta:** `GET /api/auditoria` (solo Administrador), con filtros por usuario, entidad, rango de fechas y paginación.
- **Migración** `AgregarHistorialAuditoria`.
- **Pruebas** (32 nuevas):
  - `EventosAuditadosTests`: una por tipo de evento, que revisa los ocho campos y la ausencia de secretos;
  - `ConsultaAuditoriaTests`: filtros, el último día del rango, un Estándar rechazado y los filtros inválidos;
  - `InmutabilidadAuditoriaTests`;
  - el proyecto nuevo `tests/API.Tests`, que revisa por reflexión que el controlador solo expone `GET` y exige el rol Administrador.
- **Documentación:**
  - README: sección «Auditoría», pruebas y guía de verificación 15;
  - `API.http`: peticiones 131 a 149.

## 3. Errores del agente, cómo se detectaron y cómo se corrigieron

### Error 1 · Ediciones que no se aplicaron por los finales de línea

**Qué hizo mal.** Para conectar el auditor en `DocumentosService`, `UsuarioAdminService` y `GestionPermisosService`, el agente usó sustituciones con `perl` cuyos patrones esperaban saltos de línea `\n`. Esos archivos usan `\r\n` (CRLF), así que ningún patrón coincidió y el comando terminó **sin error y sin cambiar nada**.

**Cómo se detectó.** Antes de compilar, el agente contó las apariciones del auditor en cada archivo:

```text
$ grep -c "_auditor" src/Core/Documentos/Services/DocumentosService.cs src/Core/Auth/Services/UsuarioAdminService.cs
0
0
$ grep -c "_auditor" src/Core/Permisos/Services/GestionPermisosService.cs
1      <- solo la edición hecha con el editor; el constructor y la aprobación seguían sin auditor
$ file src/Core/Documentos/Services/DocumentosService.cs
... with CRLF line terminators
```

**Cómo se corrigió.** Se rehicieron todas esas ediciones con el editor del agente, que respeta los finales de línea, y se comprobó que la solución compila con 0 errores y 0 advertencias.

**Lección.** Una sustitución por patrón que no coincide no falla: hay que comprobar que el cambio está antes de seguir.

### Error 2 · Una prueba de «sin secretos» que daba falso positivo

**Qué hizo mal.** Para CA-AUD-11, la función de prueba `VerificarSinSecretos` buscaba en el registro, entre otras cosas, el texto `password"` para asegurar que no se guardara ninguna contraseña. Pero los registros de contraseñas guardan, a propósito, el campo `"debeCambiarPassword":`, que contiene ese texto.

**Cómo se detectó.** La primera ejecución de las pruebas de auditoría falló en las 4 pruebas de contraseñas, aunque ningún registro contenía un secreto:

```text
Failed EventosAuditadosTests.RestablecerConElCodigoPedidoPorElUsuario_RegistraQueLoInicioElUsuario
   Assert.DoesNotContain() Failure: Sub-string found
Failed EventosAuditadosTests.RestablecerConElCodigoDeUnRestablecimientoForzado_RegistraQueLoInicioUnAdministrador
   Assert.DoesNotContain() Failure: Sub-string found
Failed EventosAuditadosTests.RestablecimientoForzado_RegistraAlAdministradorSinContrasenaNiCodigo
   Assert.DoesNotContain() Failure: Sub-string found
Failed EventosAuditadosTests.CambiarLaPropiaContrasena_RegistraAlUsuarioSinLaContrasena
   Assert.DoesNotContain() Failure: Sub-string found
Failed!  - Failed: 4, Passed: 25, Total: 29 - Core.Tests.dll
```

**Cómo se corrigió.** El defecto estaba en la prueba, no en el historial. La comprobación busca ahora los **secretos reales** de cada caso: las contraseñas usadas, el hash guardado (`hash:...`) y el código de recuperación. Las 29 pruebas pasaron.

Para comprobar que la prueba corregida sigue detectando el defecto verdadero, se hicieron dos experimentos y luego se restauró el código:
- **Guardar el código de recuperación en el registro del restablecimiento:** `RestablecerConElCodigoPedidoPorElUsuario_...` falló en `VerificarSinSecretos` con `Assert.DoesNotContain() Failure: Sub-string found`.
- **Guardar el hash en el registro del forzado:** `RestablecimientoForzado_...` falló con `Actual: ···"Por":"Administrador","hashGuardado":"hash"···`.

**Lección.** Una prueba que busca un texto genérico puede fallar por un nombre de campo. Hay que buscar el dato sensible concreto.

### Observación · El mensaje del `403` lo da el framework

Por HTTP, un Estándar que consulta `/api/auditoria` recibe `403` con `{"mensaje":"No tienes permiso para esta operación."}`, el mensaje general de la API. El atributo `[Authorize(Roles = "Administrador")]` lo detiene antes de llegar al servicio. El servicio también rechaza a un no-Administrador con su propio mensaje, como segunda barrera que comprueban las pruebas. Cumple CA-AUD-16 (`403` con `{"mensaje"}`, sin datos), así que no se cambió.

## 4. Verificación de los criterios CA-AUD-01 a 20

Se ejecutó la API en un Docker aislado (copia del código, `.env.example` como `.env` y `docker compose up --build`), con un script que provoca cada evento por HTTP. Resultado: **52 comprobaciones, 52 correctas**.

| Criterio | Resultado | Evidencia |
|----------|-----------|-----------|
| CA-AUD-01 | ✅ | Los 15 registros generados traen los ocho datos, ninguno vacío. |
| CA-AUD-02 | ✅ | `SolicitudAprobada` con el Administrador, `{"estado":"Pendiente"}` → `{"estado":"Aplicada","permiso":"OperarTaller","solicitanteId":...}`. |
| CA-AUD-03 | ✅ | `SolicitudRechazada` con el segundo Administrador y `"motivo":"Falta experiencia en taller"`. |
| CA-AUD-04 | ✅ | Aprobar dos veces da `409`; un Estándar que aprueba, `403`. Una aprobación y un rechazo simultáneos dan `200` y `409`, con **un solo** registro de esa solicitud. |
| CA-AUD-05 | ✅ | `DocumentoSubido` `{"estado":"NoExistia"}` → `{"estado":"Activo","nombre":"factura.pdf","tipoContenido":"application/pdf","tamanoBytes":23}`. Una subida vacía da `400` y no deja registro. |
| CA-AUD-06 | ✅ | `DocumentoEliminado` del dueño y del Administrador, cada uno con su `usuarioId`. El intento de otro Estándar da `403` y no deja registro. |
| CA-AUD-07 | ✅ | Dos `RolCambiado`, `{"rol":"Estandar"}` ↔ `{"rol":"Administrador"}`. El intento de un Estándar da `403`. |
| CA-AUD-08 | ✅ | `UsuarioDesactivado` `{"estaActivo":true}` → `false` y `UsuarioReactivado` al revés. La auto-desactivación da `403` y no deja registro. |
| CA-AUD-09 | ✅ | `RestablecimientoForzado` con el Administrador, `{"debeCambiarPassword":false}` → `{"debeCambiarPassword":true,"iniciadoPor":"Administrador"}`. |
| CA-AUD-10 | ✅ | `ContrasenaRestablecida` con `iniciadoPor: Usuario` (código de «olvidé mi contraseña») y con `Administrador` (código del forzado), cada uno con el propio usuario como actor. El código reutilizado da `400` y no deja registro. |
| CA-AUD-11 | ✅ | Ningún registro contiene las contraseñas usadas, hashes BCrypt (`$2a$`, `$2b$`), códigos ni enlaces de recuperación. |
| CA-AUD-12 | ✅ | `ContrasenaCambiada` con `iniciadoPor: Usuario`. Con la contraseña actual incorrecta da `400` y no deja registro. |
| CA-AUD-13 | ✅ | `PUT`, `PATCH`, `POST` y `DELETE` sobre `/api/auditoria` dan `404` o `405`. En SQL Server, `UPDATE` y `DELETE` directos fallan con `Msg 50001, Procedure TR_RegistrosAuditoria_Inmutable ... El historial de auditoria no se puede modificar ni borrar.`, y el historial queda idéntico. |
| CA-AUD-14 | ✅ | `?usuarioId=`, `?entidad=Documento` y la combinación de ambos dan resultados distintos y correctos, del más reciente al más antiguo. |
| CA-AUD-15 | ✅ | El rango de hoy trae todo, el que termina ayer da `total: 0` y `0001-01-01..9999-12-31` da `200`. El último segundo del día lo comprueba la prueba `RangoDeUnDia_IncluyeSuUltimoSegundoYNadaDelDiaSiguiente`. |
| CA-AUD-16 | ✅ | El Estándar recibe `403` con `{"mensaje"}`, con o sin filtros; sin sesión, `401`. |
| CA-AUD-17 | ✅ | Los 8 filtros inválidos dan `400` con su mensaje; por ejemplo, `{"mensaje":"La entidad no es válida. Usa SolicitudPermiso, Documento, Usuario."}`. |
| CA-AUD-18 | ✅ | Después de `docker compose down` (sin `-v`) y `up`, los 15 registros siguen con los mismos valores. |
| CA-AUD-19 | ✅ | `fechaUtc` termina en `Z` y coincide con el momento de cada operación. |
| CA-AUD-20 | ✅ | `grep "PCBuilderOps." src/Core` no encuentra nada y `Core.csproj` no referencia otros proyectos. La entidad se identifica con texto. CA-AUD-04 demuestra que una operación rechazada no deja registro. |

Además:
- `dotnet test P3-PCBuilderOps.slnx` pasa con **368 pruebas**: 3 de la API, 179 del Core y 186 del módulo.
- Con el mismo código en Docker pasaron las 237 comprobaciones de las entregas anteriores: reportes, cola, bandeja, evento del negocio, prácticas anteriores y los seis frentes.

## 5. Paralelo entre un registro de auditoría y un commit

Se comparan un registro real del historial, generado en la verificación, y el commit real que abrió esta asignación.

**Registro de auditoría** (`GET /api/auditoria`):

```json
{
  "id": "7bf38b0b-7986-4868-9532-ceeb5c018258",
  "usuarioId": "e36f3c2d-9685-480d-96c8-0e5b02d3d93e",
  "usuarioNombre": "Administrador PCBuilderOps",
  "accion": "RestablecimientoForzado",
  "entidad": "Usuario",
  "entidadId": "75a34933-5ed8-4aec-8288-aeff4ce7b62b",
  "fechaUtc": "2026-11-27T17:42:40.6781058Z",
  "valorAnterior": { "debeCambiarPassword": false },
  "valorNuevo": { "debeCambiarPassword": true, "iniciadoPor": "Administrador" }
}
```

**Commit** (`git show --stat 7a57cce`):

```text
commit 7a57cce3dc3f67d6b46f1d3eea0955e508a383c6
Author: Joshua Sebastian Abreu Trinidad
Date:   2026-11-27T13:25:45-04:00

    docs(asignacion-4): escribir la entrega y los criterios del historial de auditoría antes de programarlo (RF-AUD-01 a 07)

 AGENTS.md                     |   1 +
 docs/criterios-auditoria.md   | 226 ++++++++++++++++++++++++++++++++++++++++++
 docs/entregas/asignacion-4.md | 102 +++++++++++++++++++
```

| Campo del commit | Campo del registro | Cómo se parecen y en qué se diferencian |
|------------------|--------------------|------------------------------------------|
| **Autor** (`Author: Joshua Sebastian Abreu Trinidad`) | `usuarioId` + `usuarioNombre` | Los dos dicen **quién** hizo el cambio. En git, el autor es un texto que se configura a mano y se puede falsear. En la auditoría sale de la sesión autenticada (el token), no de lo que el usuario escriba. |
| **Mensaje** (`docs(asignacion-4): escribir la entrega...`) | `accion` (`RestablecimientoForzado`) | Los dos dicen **qué** se hizo. El mensaje es texto libre que explica también el **porqué**. La acción es un valor de una lista cerrada, pensado para filtrar, y no explica el motivo (salvo el `motivo` del rechazo, en el valor nuevo). |
| **Fecha** (`2026-11-27T13:25:45-04:00`) | `fechaUtc` (`2026-11-27T17:42:40.6781058Z`) | Los dos dicen **cuándo**. Git guarda la hora local con su zona (`-04:00`) y la toma del reloj del equipo, que se puede cambiar. La auditoría guarda siempre UTC con `Z` (RD-11), con el reloj del servidor. |
| **Identificador** (`7a57cce3dc3f...`) | `id` (`7bf38b0b-...`) | Los dos identifican el cambio de forma única. El hash del commit **se calcula a partir del contenido y del commit padre**: si algo cambia, cambia el hash. El `id` del registro es un `Guid` aleatorio que no depende del contenido. |
| **Diff** (`+329` líneas en 3 archivos) | `valorAnterior` → `valorNuevo` | Los dos muestran **qué había antes y qué hay después**. El diff es línea a línea y se calcula comparando con el commit padre. El registro guarda los dos valores ya escritos, como JSON con solo los campos que cambiaron. |
| Archivos del diff (`docs/criterios-auditoria.md`, ...) | `entidad` + `entidadId` (`Usuario` / `75a34933-...`) | Los dos dicen **sobre qué** se hizo el cambio: git nombra archivos y la auditoría nombra un objeto de la aplicación y su id. |

**Lo que guarda el commit y el registro no:**
- El **commit padre**, que encadena la historia completa y permite volver a cualquier punto (`git checkout`).
- La **foto completa** del proyecto en ese momento (el árbol), no solo lo que cambió.
- El **porqué**, en el mensaje libre.
- La distinción entre autor y quien hizo el commit, y las ramas y fusiones.

**Lo que guarda el registro y el commit no:**
- Acciones de **usuarios de la aplicación** que no tienen acceso al repositorio (un cliente que sube un documento).
- Un **actor verificado** por la sesión, no declarado a mano.
- Una **consulta por filtros** (usuario, entidad, rango de fechas) reservada al Administrador.
- El valor anterior **escrito explícitamente**, sin reconstruirlo desde otro punto.

**Inmutabilidad.** En git, la historia **se puede reescribir** (`rebase`, `push --force`), aunque se nota porque cambian los hashes. Por eso el proyecto prohíbe reescribir el historial publicado (`AGENTS.md`, sección 11). En el historial de auditoría **no se puede** desde la aplicación: no hay operaciones de edición ni de borrado, `AppDbContext` rechaza modificar un registro, y el disparador de SQL Server rechaza `UPDATE` y `DELETE` incluso por fuera de la API.
