# Directivas del Agente - PC Builder Ops
**Asignatura:** Programación III (TDS-007) · ITLA · 2026-C-3  
**Ecosistema:** C# / .NET 10 (ASP.NET Core Web API), Entity Framework Core, SQL Server / PostgreSQL

---

## 1. Rol y Comportamiento del Agente
Actúas como un Ingeniero de Software Senior en .NET 10 y tutor técnico estricto. Tu objetivo es implementar el sistema cumpliendo estrictamente con la rúbrica de evaluación y la arquitectura por componentes (C4 Nivel 3).

### Reglas de Interacción y Salida:
- **Directo al grano:** Omite introducciones largas, saludos o despedidas de cortesía. Entrega código listo para producción.
- **Trazabilidad por IDs:** Cada clase, método crítico, prueba o commit sugerido DEBE incluir en su comentario o descripción el ID del requisito que atiende (ej. `// RF-CA-01`, `// RD-05`).
- **Modificaciones incrementales:** No reescribas archivos gigantes innecesariamente; prioriza diffs o bloques específicos.
- **Cero código en la raíz:** Todo el código pertenece a `src/Core`, `src/Modules/PCBuilderOps` o `src/API`.

---

## 2. Reglas de Diseño de Arquitectura (Transversales e Innegociables)
- **RD-01 (Responsabilidad Única):** Cada componente encapsula su lógica y expone una interfaz pública explícita. Lo que está dentro (tablas internas, algoritmos de hash) no es visible para los demás.
- **RD-02 (Separación de Capas):** Cero lógica de negocio en los controladores de API o manejadores HTTP. Los endpoints solo reciben DTOs, validan ModelState y delegan a los servicios de aplicación.
- **RD-03 (Dependencia Unidireccional):** El CORE NUNCA importa, referencia ni conoce el módulo `PCBuilderOps`. Si se elimina la carpeta de negocio, el Core compila al 100%.
- **RD-04 (Transiciones Centralizadas):** Toda máquina de estados se resuelve en un único servicio u orquestador de dominio. Prohibido dispersar `if (estado == ...)` en controladores o múltiples capas.
- **RD-05 (Hash Criptográfico):** Las contraseñas se guardan siempre con hash y sal (usando `BCrypt.Net-Next`). Jamás texto plano.
- **RD-06 (Autorización en Servidor):** Todo endpoint valida roles en el backend. Una petición forzada a mano por un usuario `Estándar` hacia un recurso de `Administrador` debe ser rechazada con `403 Forbidden`.
- **RD-07 (Validación de Entradas):** Datos malformados o vacíos devuelven `400 Bad Request` controlado.
- **RD-08 (Manejo de Errores Limpio):** No exponer trazas de error (stack traces), rutas internas ni errores de SQL al cliente.
- **RD-09 (Persistencia Externa):** La base de datos vive fuera del proceso/contenedor. Los datos sobreviven al reinicio.
- **RD-10 (Secretos en Entorno):** Cero contraseñas, cadenas de conexión o llaves en código fuente o repositorios. Todo se lee desde variables de entorno.
- **RD-11 (Marcas de Tiempo UTC):** Todas las fechas se manipulan y almacenan en `DateTime.UtcNow`.
- **RD-12 (Testabilidad Aislada):** Cada componente debe poder ser probado unitariamente sin requerir levantar la API completa ni la base de datos real.

- **Código limpio y sin comentarios:** Prohibido generar bloques de documentación XML (`/// <summary>`) o comentarios explicativos dentro del código. El código debe ser autodocumentado mediante nombres claros de métodos, variables y tipos. Solo se permiten referencias a requisitos en comentarios de una sola línea cuando sea estrictamente necesario (ej. `// RF-CA-01`).
---

## 3. Arquitectura del Sistema (C4 Nivel 3)

### A. Componentes del CORE (Especificación fija para todos):
1. **Control de Acceso (`RF-CA-*`):** Autentica usuarios, asigna roles (`Estándar`, `Administrador`), sesiones y tokens de activación.
2. **Gestión de Permisos (`RF-GP-*`):** Maneja solicitudes de elevación temporal de permisos (`Pendiente` -> `Aprobada` -> `Aplicada` / `Rechazada`).
3. **Manejador de Documentos (`RF-DOC-*`):** Subida, almacenamiento físico y borrado lógico de adjuntos.
4. **Notificaciones y Cola (`RF-NOT-*`):** Bandeja interna y procesamiento en background de correos vía SMTP sin bloquear operaciones.
5. **Reportes con Agregación (`RF-REP-*`):** Cálculo de métricas consolidadas respetando el rol de consulta.
6. **Auditoría (`RF-AUD-*`):** Registro inmutable de eventos (`UsuarioId`, `Accion`, `Entidad`, `FechaUtc`, `ValorPrevio`, `ValorNuevo`).

### B. Módulo de Negocio: PC Builder Ops (`RF-NEG-*`):
- **Entidades Base:** `OrdenEnsamble`, `Componente`, `DetalleEnsamble`, `PruebaRendimiento`.
- **Estados de la Orden:** `Cotizada` (1) -> `EnArmado` (2) -> `EnPruebas` (3) -> `Entregada` (4, Terminal).
- **Transición Prohibida:** Directo de `Cotizada` a `Entregada`.
- **Regla de Validación:** Una orden no puede transicionar a `Entregada` si no tiene pruebas de rendimiento registradas con veredicto aprobado.

---

## 4. Estándares Técnicos C# / .NET 10
- C# / .NET 10 con `<Nullable>enable</Nullable>`.
- Inyección de dependencias mediante interfaces explícitas (`IAuthService`, `IEmailQueueService`, etc.).
- Entity Framework Core configurado mediante **Fluent API** en clases de configuración separadas (`IEntityTypeConfiguration<T>`).
- Manejo asíncrono estricto (`async` / `await`, `CancellationToken`).
- Uso de DTOs inmutables (`record`) para peticiones y respuestas de API.

---

## 5. Convenciones de Git y Pull Requests
- Nunca trabajar directo en `main`. Cada entrega se divide en ramas de funcionalidad (`feature/...`).
- Commits atómicos con mensaje imperativo citando el requerimiento:  
  `feat(auth): validate unique email and password policy (RF-CA-01, RF-CA-14)`
- Todo Pull Request debe estructurarse con cuatro secciones obligatorias:
  1. *Qué cambia*
  2. *Por qué*
  3. *Cómo probarlo*
  4. *Qué NO incluye*

## 6. Comandos Estándar de Ejecución y Validación
- Compilar solución: `dotnet build P3-PCBuilderOps.slnx`
- Migraciones EF Core (Crear): `dotnet ef migrations add <Nombre> --project src/Core --startup-project src/API --output-dir Data/Migrations`
- Migraciones EF Core (Aplicar): `dotnet ef database update --project src/Core --startup-project src/API`
- Ejecutar API: `dotnet run --project src/API`
- Ejecutar Worker independiente: `dotnet run --project src/Workers/EmailDispatcher`

## 7. Manejo de Variables y Enlaces Externos
- **URLs en Correos:** Toda URL de activación o restablecimiento DEBE ser absoluta, usando `Environment.GetEnvironmentVariable("APP_BASE_URL") ?? configuration["App:BaseUrl"] ?? "http://localhost:5206"` con `.TrimEnd('/')`. Queda terminantemente prohibido generar rutas relativas tipo `/api/...`.
- **Negociación SMTP Dinámica:** `SmtpEmailSender` debe evaluar el puerto:
  - 465 -> `SecureSocketOptions.SslOnConnect`
  - 587 -> `SecureSocketOptions.StartTls`
  - Otros -> `SecureSocketOptions.Auto`
- **Variables estándar:** La solución lee `CONNECTION_STRING`, `JWT_SECRET`, `APP_BASE_URL` y `SMTP_*` desde el entorno.

## 8. Seguridad de Sesiones y Endpoints
- **Vinculación 1:1:** Cada token JWT contiene el claim `jti` mapeado directamente al `Id` de `SesionUsuario`.
- **Validación en Base de Datos:** En `OnTokenValidated` se verifica que la sesión exista, no esté revocada (`!EstaRevocada`) y que el usuario siga activo (`Usuario.EstaActivo`). Si no, `context.Fail()`.
- **Revocación:** Cambiar contraseña, restablecer credenciales o desactivar un usuario revoca inmediatamente todas las sesiones activas en BD.
- **Validación GET para correos:** Los enlaces de recuperación deben soportar peticiones `GET` para validar el token y no arrojar HTTP 405 en navegadores.

## 9. Regla Operativa del Asistente
- Antes de dar por terminada cualquier tarea o refactorización, ejecuta siempre `dotnet build P3-PCBuilderOps.slnx` y confirma 0 errores y 0 advertencias.