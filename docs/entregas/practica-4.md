# Práctica 4 — Tu proyecto en un contenedor, con documentos y una API externa

| Dato | Valor |
|------|-------|
| Bloque / semana | Bloque 4 · Semana 9 |
| Puntos | 8 · Individual |
| Fecha límite | **Martes 10 de noviembre de 2026, 11:59 p. m.** Tarde: hasta 24 h más con 50 % de penalidad; luego no se acepta. |
| Cómo se entrega | `git tag practica-4` + `git push origin --tags`; URL del repo y `practica-4` en Moodle. |
| Rama(s) | `feature/practica-4-contenedor` |
| Estado | ⏳ En curso |

> *Quien clone el repositorio, copie un archivo de ejemplo y ejecute un comando tiene el proyecto corriendo, con sus documentos guardados, sin instalar nada más que Docker.*

## 1. Alcance

### 1.1 Manejador de documentos (Pieza 3 del Core)

Todas sus operaciones requieren sesión válida.

| ID | Requisito | Criterio de aceptación | Cómo se cumple |
|----|-----------|------------------------|----------------|
| RF-DOC-01 | Subir un documento registrando nombre, tipo, tamaño, fecha y quién lo subió. | El registro queda consultable con todos esos datos. | `POST /api/documentos` (formulario con el archivo). Registro en la tabla `Documentos` del Core. |
| RF-DOC-02 | Listar los documentos que el usuario puede ver. | No incluye eliminados ni de otros usuarios, salvo para el Administrador. | `GET /api/documentos`. |
| RF-DOC-03 | Descargar un documento. | Solo el dueño o un Administrador; cualquier otro recibe un rechazo. | `GET /api/documentos/{id}/descarga`; otro usuario recibe `403`. |
| RF-DOC-04 | Eliminar un documento de forma lógica. | Deja de aparecer en el listado, pero el registro sigue existiendo. | `DELETE /api/documentos/{id}`: marca el registro, no lo borra ni borra el archivo. |
| RF-DOC-05 | El archivo se guarda en un volumen persistente fuera de la imagen. | Destruir y recrear el contenedor no pierde los archivos. | Volumen con nombre `documentos`. |

Aplican además RD-06 (autorización en el servidor) y RD-07 (archivo ausente o vacío → rechazo controlado).

### 1.2 Un solo comando
- `Dockerfile` que construye la imagen y `.dockerignore` que deja fuera `bin/`, `obj/`, `.git/`, `.env` y `launchSettings.json`.
- `docker-compose.yml` con la aplicación (`api`) y SQL Server (`db`). Los servicios se encuentran por nombre (`Server=db`), no por `localhost`.
- `docker compose up --build` arranca el proyecto desde un clon limpio, sin instalar nada salvo Docker y sin pasos manuales: las migraciones se aplican al arrancar.
- Solo la aplicación publica un puerto: **5206**. La base no es accesible desde fuera de la composición.
- Sección **Contenedor** en el README: comando, `http://localhost:5206` y cómo detener el proyecto.

### 1.3 Los datos sobreviven
- Archivos de documentos en el volumen `documentos` (RF-DOC-05).
- Datos de SQL Server en el volumen `sqldata` (RD-09).
- Prueba: subir un documento, `docker compose down` (sin `-v`), `docker compose up`; el documento se lista y se descarga, y el usuario inicia sesión.

### 1.4 API externa con la clave como secreto
- **API:** ExchangeRate-API (tasa de cambio), con clave.
- **Operación:** `GET /api/ordenes/{id}/costo?moneda=DOP` convierte el costo total de la orden a otra moneda con la tasa del día. No es un reenvío: calcula con datos del negocio.
- La clave se lee de la variable `TASA_CAMBIO_API_KEY`, que llega al contenedor desde `.env` (excluido por `.gitignore`).
- `.env.example` versionado con todas las variables y un valor de ejemplo para cada una. Copiarlo a `.env` es el único paso previo.
- La clave no aparece en el código, `Dockerfile`, `docker-compose.yml`, la imagen ni ningún commit.
- Sin clave o con la API caída: rechazo controlado con mensaje comprensible; el resto del proyecto sigue funcionando.
- Sección **API externa** en el README: qué API es, qué operación la usa, el nombre de la variable y un ejemplo real de respuesta, sin la clave.

### 1.5 Revisión acumulada (seis frentes)
Desde `practica-4`, copiando `.env.example` a `.env` y siguiendo el README al pie de la letra:
1. Control de acceso: registro con activación por correo.
2. Control de acceso: inicio y cierre de sesión.
3. Control de acceso: rechazo a un Estándar en una operación de Administrador.
4. Control de acceso: recuperación de contraseña.
5. Gestión de permisos: crear, aprobar y rechazar solicitudes; las transiciones fuera de la tabla se rechazan.
6. Módulo de negocio: una operación válida funciona y la transición prohibida se rechaza.

### Fuera de alcance
- Auditoría de subidas y eliminaciones (RF-DOC-06, Pieza 6).
- Adjuntar documentos a una orden (RF-NEG-10, opcional).
- Notificaciones y reportes del negocio (RF-NEG-07 y RF-NEG-08).
- Bitácora (esta práctica no la pide).

## 2. Rúbrica (8 puntos)

| Criterio | Puntos | Para "Logrado" |
|----------|--------|----------------|
| Manejador de documentos RF-DOC-01 a 04 | 2.0 | Subir guarda nombre, tipo, tamaño, fecha y quién lo subió; el listado excluye eliminados y ajenos (salvo para el Administrador); solo el dueño o un Administrador descarga o elimina; la eliminación es lógica. |
| Contenedor con un solo comando | 1.5 | Desde un clon limpio, copiar `.env.example` a `.env` y ejecutar `docker compose up --build` levanta el proyecto, que responde en el puerto del README. Hay `Dockerfile`, `.dockerignore`, `docker-compose.yml` y sección «Contenedor». |
| Volumen persistente RF-DOC-05 | 1.0 | Tras `docker compose down` (sin `-v`) y volver a levantar, el documento se lista y se descarga y el usuario inicia sesión. El archivo vive en un volumen. |
| API externa con la clave como secreto | 1.5 | Una operación usa datos de la API; la clave llega por variable de entorno desde un `.env` no versionado; `.env.example` la trae con valor de ejemplo; ningún commit la contiene; sin clave o con la API caída el rechazo es controlado; el README documenta la integración. |
| Revisión acumulada | 2.0 | Funcionan los seis frentes. Con dos a cinco, la mitad. |

## 3. Qué revisa el maestro

1. `git clone` y `git checkout practica-4`.
2. Presencia de `Dockerfile`, `.dockerignore`, `docker-compose.yml`, `.env.example` y un `.gitignore` que excluye `.env`; que `.env` no exista en ningún commit.
3. `.env.example` copiado a `.env` y el comando de la sección «Contenedor»: el proyecto levanta y responde en la dirección del README.
4. Subir, listar, descargar y eliminar un documento con su dueño; otro Estándar es rechazado al descargar y al eliminar.
5. `docker compose down` y volver a levantar: el documento sigue y el usuario inicia sesión.
6. La operación de la API externa con la clave de ejemplo (rechazo controlado) y la sección «API externa».
7. El historial completo, en busca de la clave o de un `.env`.
8. Los seis frentes, arrancados desde el README.

## 4. Decisiones y riesgos

- **API externa:** tasa de cambio. La conversión es una operación aparte, no parte de cotizar, para que el negocio siga funcionando si la API cae.
- **Correos:** la API registra el `EmailQueueWorker` existente y envía la cola cada 30 s. Sin SMTP real (caso del `.env.example`), el README indica cómo leer el enlace con `docker compose exec`.
- **Puerto 5206:** el mismo de `API.http`, el README y `APP_BASE_URL`.
- **Documentos:** se acepta cualquier tipo con un límite de 10 MB; el archivo se guarda con un nombre generado, nunca con el original, y se entrega siempre como descarga.
- **Migraciones automáticas** al arrancar, con reintentos mientras SQL Server se prepara.
- **Valores del `.env.example`:** todos deben funcionar tal cual (contraseña de SQL Server, `JWT_SECRET`, Administrador inicial), salvo la clave externa.
- **Riesgos:** la imagen de SQL Server pesa unos 1.5 GB y pide unos 2 GB de memoria; en Mac con chip Apple corre por emulación. La carpeta de documentos debe tener permisos para el usuario sin privilegios de la imagen de .NET.

## 5. Checklist de estado

- [x] Ficha creada
- [x] Manejador de documentos (RF-DOC-01 a 04) con pruebas
- [x] Errores de subida (sin archivo, vacío, demasiado grande) con `{"mensaje"}`
- [ ] Migraciones automáticas al arrancar
- [ ] `EmailQueueWorker` registrado en la API
- [ ] `Dockerfile`, `.dockerignore`, `docker-compose.yml` y `.env.example`
- [ ] `docker compose up --build` desde un clon limpio responde en `http://localhost:5206`
- [ ] Volúmenes `documentos` y `sqldata`; el documento y el usuario sobreviven a `down` y `up`
- [ ] Operación con la API de tasa de cambio y rechazo controlado sin clave o con la API caída
- [ ] Ningún commit contiene la clave ni un `.env`
- [ ] README: secciones «Contenedor» y «API externa», y cómo leer los enlaces de correo
- [ ] Revisión acumulada de los seis frentes desde el contenedor
- [ ] PR fusionado, etiqueta `practica-4` subida y verificada desde un clon de GitHub
- [ ] Entrega en Moodle
- [ ] `AGENTS.md` sección 0 actualizada
