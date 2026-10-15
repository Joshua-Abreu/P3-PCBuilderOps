# PC Builder Ops

> **Sistema de Gestión de Ensamblaje y Pruebas de Computadoras Custom Para Empresas**  
> Proyecto de la asignatura **Programación III (TDS-007)** — ITLA (2026-C-3)

---

## Descripción del Proyecto
Plataforma orientada a talleres técnicos y tiendas especializadas de hardware para gestionar el ciclo operativo completo de computadoras personalizadas a pedido. El sistema valida la compatibilidad física y eléctrica de los componentes durante la cotización, coordina las etapas de montaje físico, y garantiza el control de calidad mediante el registro de pruebas de estrés térmico y estabilidad (benchmarking) antes de autorizar la entrega final al cliente.

## Stack Tecnológico
* **Lenguaje & Framework:** C# / .NET 10 (ASP.NET Core Web API)
* **Persistencia:** Entity Framework Core con base de datos relacional (SQL Server)
* **Autenticación & Seguridad:** JWT (JSON Web Tokens) con BCrypt y hashing salado
* **Arquitectura:** Diseño modular basado en componentes (C4 Nivel 3) con desacoplamiento estricto del Core

---

## Arquitectura del Sistema (Diagrama C4 Nivel 3)
El sistema se compone de seis piezas fijas del Core y el módulo de negocio independiente:

```mermaid
flowchart TD
    subgraph Core["CORE - Especificación Fija"]
        CA["Control of Access<br/>[C# / ASP.NET Core]<br/>Authentica usuarios y provee roles"]
        GP["Gestión de Permisos<br/>[C# / ASP.NET Core]<br/>Administra solicitudes de elevación de privilegios"]
        DOC["Manejador de Documentos<br/>[C# / ASP.NET Core]<br/>Gestiona almacenamiento de archivos físicos"]
        NOTI["Notificaciones<br/>[C# / ASP.NET Core]<br/>Bandeja interna y cola de correos"]
        REP["Reportes with Agregación<br/>[C# / ASP.NET Core]<br/>Genera métricas consolidadas"]
        AUD["Auditoría<br/>[C# / ASP.NET Core]<br/>Registro inmutable de trazabilidad"]
    end

    subgraph Negocio["Módulo de Negocio"]
        MOD["PC Builder Ops<br/>[C# / ASP.NET Core]<br/>Gestiona compatibilidad, ensamblaje y benchmarking de PCs"]
    end

    GP -->|"avisa resolución"| NOTI
    GP -->|"quien autoriza"| CA
    DOC -->|"valida rol y dueño"| CA
    REP -->|"verifica rol de consulta"| CA
    REP -->|"agrega solicitudes por estado"| GP

    CA -.->|"audita cambios de rol y claves"| AUD
    GP -.->|"audita resoluciones"| AUD
    DOC -.->|"audita subidas y borrados"| AUD

    MOD -->|"quien es y que rol tiene"| CA
    MOD -->|"notificar cambio de fase"| NOTI
    MOD -->|"adjuntar benchmark"| DOC
    MOD -->|"solicita reporte de ensambles"| REP
    MOD -.->|"registrar eventos críticos"| AUD
```

---

## Variables de Entorno Requeridas

Conforme a las directivas de seguridad **RF-NOT-13** y **RD-10**, ninguna credencial ni secreto se encuentra versionado en el código fuente. Configure las siguientes variables en el entorno de ejecución, consola o en su archivo local excluido de Git (`launchSettings.json`):

### Cadena de conexión a base de datos

La aplicación resuelve la cadena de conexión en el siguiente orden de prioridad:

1. `CONNECTION_STRING` (variable de entorno directa)
2. `ConnectionStrings__DefaultConnection` (formato doble guion bajo para variables de entorno)
3. `ConnectionStrings:DefaultConnection` (formato estándar de configuración .NET)
4. Fallback local: `Server=localhost;Database=PCBuilderOps;Trusted_Connection=True;TrustServerCertificate=True`

### Clave secreta para JWT

La clave de firma de tokens se resuelve en el siguiente orden:

1. `JWT_SECRET` (variable de entorno directa)
2. `Jwt__Key` (formato doble guion bajo)
3. `Jwt:Key` (configuración .NET estándar)
4. Fallback: `ClaveSuperSecretaDeDesarrollo2026!` (solo desarrollo)

### Credenciales SMTP

| Variable | Alternativa | Propósito |
|----------|-------------|-----------|
| `SMTP_HOST` | — | Host del servidor SMTP (ej. smtp.gmail.com). |
| `SMTP_PORT` | — | Puerto de conexión al servidor SMTP (465 para SSL, 587 para STARTTLS). |
| `SMTP_USER` | — | Usuario o cuenta de correo autenticada ante el servidor SMTP. |
| `SMTP_PASS` | `SMTP_PASSWORD` | Contraseña de aplicación o token de autenticación SMTP. |
| `SMTP_FROM` | — | Dirección de remitente que figurará en el encabezado de los correos salientes. |

---

## Cómo ejecutar el proyecto

### 1. Clonar y preparar

```bash
git clone https://github.com/Joshua-Abreu/P3-PCBuilderOps.git
cd P3-PCBuilderOps
git checkout practica-1
```

### 2. Restaurar dependencias

```bash
dotnet restore
```

### 3. Aplicar migraciones de base de datos

El proyecto Core contiene el `AppDbContext` y las migraciones. No existe el proyecto `src/Infrastructure`. Use el siguiente comando:

```bash
dotnet ef database update --project src/Core --startup-project src/API
```

Para generar una nueva migración después de cambios en el modelo:

```bash
dotnet ef migrations add <NombreMigracion> --project src/Core --startup-project src/API
```

### 4. Compilar

```bash
dotnet build P3-PCBuilderOps.slnx
```

### 5. Ejecutar

**API Web:**
```bash
dotnet run --project src/API
```

La API quedará escuchando en `http://localhost:5206` (o el puerto configurado en su perfil de host).

**Despachador independiente de correos:**
```bash
dotnet run --project src/Workers/EmailDispatcher
```

Este proceso independiente lee los correos pendientes de la cola, los envía vía SMTP y termina su ejecución limpiamente.

---

## Guía de Verificación de Criterios de Aceptación

Para reproducir y calificar cada criterio de aceptación de la Práctica 1, se provee el archivo de pruebas integrado `src/API/API.http`, el cual puede ejecutarse secuencialmente desde Visual Studio Code (extensión REST Client) o importarse en Postman.

### 1. Registro, Validación and Activación (Bloque 1.1)

| Criterio | Descripción | Resultado esperado |
|----------|-------------|-------------------|
| **RF-CA-01** (Registro exitoso) | Enviar `POST /api/auth/registro` con datos válidos. | Retorna `201 Created` y coloca un correo en estado Pendiente en `CorreosEnCola`. |
| **RF-CA-02** (Unicidad) | Volver a enviar el registro con el mismo correo. | Retorna rechazo controlado con mensaje neutro. |
| **RF-CA-14** (Política de contraseñas y validación) | Enviar registro con contraseña menor a 8 caracteres o correo sin formato RFC 5322. | Retorna `400 Bad Request` con el listado de fallos. |
| **RF-CA-15 / RF-CA-16** (Activación de cuenta) | Abrir el enlace recibido por correo o invocar `GET /api/auth/activar?token={token}`. | La cuenta pasa a estado activo (`200 OK`). Si se invoca por segunda vez el mismo token, se rechaza de forma controlada (`400 Bad Request`). |
| **RF-CA-17** (Reenvío neutro) | `POST /api/auth/reenviar-activacion` | Retorna siempre `200 OK` con mensaje neutro independientemente de si el correo existe, ya estaba activo o no existe. |

### 2. Autenticación and Control de Sesiones (Bloque 1.2)

| Criterio | Descripción | Resultado esperado |
|----------|-------------|-------------------|
| **RF-CA-03** (Credenciales inválidas) | `POST /api/auth/login` con credenciales incorrectas. | Responde con mensaje neutro sin revelar si falló el correo o la contraseña. |
| **RF-CA-07** (Consulta de identidad) | `GET /api/auth/yo` with cabecera `Authorization: Bearer <TOKEN>`. | Retorna el identificador, correo y rol del usuario en sesión. |
| **RF-CA-18** (Logout and revocación) | `POST /api/auth/logout` | Invalida el token en la tabla `SesionesUsuario`. Consultas subsecuentes a endpoints protegidos con dicho token son rechazadas con `401 Unauthorized`. |
| **RF-CA-19** (Protección contra fuerza bruta) | Enviar 5 intentos consecutivos con contraseña incorrecta para una misma cuenta. | El quinto intento bloquea temporalmente la cuenta por 15 minutos; cualquier intento posterior durante esa ventana es rechazado indicando el bloqueo. |

### 3. Roles and Administración de Usuarios (Bloques 1.3 y 1.4)

| Criterio | Descripción | Resultado esperado |
|----------|-------------|-------------------|
| **RF-CA-05 / RF-CA-06 / RF-CA-13** (Protección por rol) | Intentar consultar `GET /api/usuarios` sin token o con un token de usuario estándar. | Sin token: `401 Unauthorized`. Con token estándar: `403 Forbidden`. Solo el rol Administrador (Rol = 2) tiene acceso. |
| **RF-CA-08 / RF-CA-21** (Consulta paginada segura) | `GET /api/usuarios?pagina=1&tamanoPagina=10` | Lista usuarios aplicando paginación y filtros (activo, rol) sin exponer contraseñas, hashes ni tokens. |
| **RF-CA-04** (Creación administrativa) | `POST /api/usuarios` | Permite a un Administrador crear usuarios asignando directamente su rol. |
| **RF-CA-20** (Desactivación and reglas de autoprotección) | `PATCH /api/usuarios/{id}/estado` | Permite desactivar cuentas, revocando inmediatamente todas sus sesiones activas. Si un Administrador intenta auto-desactivar su propia cuenta o remover su propio rol de administrador, la solicitud es rechazada para prevenir el bloqueo total del sistema. |
| **RF-CA-22** (Cambio voluntario de contraseña) | Con sesión activa, `POST /api/auth/cambiar-password` | Exige ingresar la contraseña actual válida para poder establecer una nueva. Invalida las demás sesiones abiertas. |

### 4. Recuperación de Contraseña (Bloque 1.3)

| Criterio | Descripción | Resultado esperado |
|----------|-------------|-------------------|
| **RF-CA-09 / RF-CA-10** (Solicitud de recuperación) | `POST /api/auth/olvide-password` | Responde siempre de manera neutra (`200 OK`) y encola el token de recuperación en `CorreosEnCola`. |
| **RF-CA-11 / RF-CA-12** (Nueva contraseña con código) | `POST /api/auth/restablecer-password` | Recibe el token emitido and la nueva contraseña. Al procesarse con éxito, revoca todas las sesiones anteriores and marca el token como consumido impidiendo reutilizaciones. |
| **RF-CA-13** (Restablecimiento forzado por Administrador) | Con sesión de Administrador, `POST /api/usuarios/{id}/forzar-restablecimiento` (petición 14 de `API.http`). Luego intentar iniciar sesión con la contraseña anterior (petición 15) y ejecutar el despachador de correos. | Retorna `200 OK`. La contraseña anterior deja de servir (`401`), las sesiones abiertas del usuario se revocan, los códigos de recuperación previos se invalidan y se encola en `CorreosEnCola` un correo con un nuevo código para definir la contraseña mediante `POST /api/auth/restablecer-password`. Un usuario Estándar recibe `403`. |

### 5. Cola de Correos and Despacho SMTP (Bloque 1.5)

| Criterio | Descripción | Resultado esperado |
|----------|-------------|-------------------|
| **RF-NOT-08 / RF-NOT-09** (Tolerancia a fallos) | El sistema encola notificaciones en `CorreosEnCola` sin interrumpir las operaciones de registro o recuperación aunque el servidor SMTP esté fuera de línea. | Las operaciones principales no se ven afectadas por la caída del servidor SMTP. |
| **RF-NOT-12** (Despacho and Idempotencia) | El despachador independiente (`dotnet run --project src/Workers/EmailDispatcher`) o el endpoint `POST /api/admin/correos/despachar` | Procesa los registros con estado `Pendiente`, los envía vía SMTP and actualiza el registro con `Estado = 'Enviado'` and su correspondiente `FechaEnvioUtc`. Si se vuelve a ejecutar el despachador, los correos ya enviados no se duplican. |

### 6. Estructura de la Máquina de Estados de Negocio (Bloque 1.6)

| Criterio | Descripción | Resultado esperado |
|----------|-------------|-------------------|
| **RF-NEG-03 / RD-04** | Las entidades del dominio central (`OrdenServicio`), sus estados (`EstadoOrdenServicio`) and la lógica de transición se encuentran centralizados en `src/Core/Ordenes/Domain/`. | Estructura centralizada and desacoplada. |
| **RF-NEG-04 / RF-NEG-05** | Se definen los 4 estados del taller (Recibida, EnDiagnostico, EnProceso, Entregada), la restricción terminal de Entregada, and la regla explícita de rechazo para el salto prohibido Recibida → Entregada. | Las transiciones inválidas son rechazadas con excepción controlada. |

Para consultar la matriz de transición completa and responsabilidades por rol, revise el documento formal: [`docs/maquina-de-estados.md`](docs/maquina-de-estados.md).
