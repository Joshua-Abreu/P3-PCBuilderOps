# Software Requirements Specification (SRS)
## PC Builder Ops – Sistema de Gestión de Ensamblaje y Pruebas de PCs Custom

* **Proyecto:** PC Builder Ops  
* **Asignatura:** Programación III (TDS-007) — ITLA (2026-C-3)  
* **Ecosistema Tecnológico:** C# / .NET 8 (ASP.NET Core Web API), Entity Framework Core, SQL Server / PostgreSQL  
* **Arquitectura:** Diseño por Componentes (C4 Nivel 3) con Desacoplamiento Estricto del Core  

---

### 1. Propósito y Alcance del Sistema
PC Builder Ops es una solución de software orientada a talleres técnicos y tiendas especializadas de hardware para automatizar y certificar el ciclo de ensamblaje de computadoras personalizadas a pedido. El sistema previene incompatibilidades técnicas físicas y eléctricas durante la cotización, organiza la asignación y flujo de trabajo en taller, y asegura el control de calidad mediante el registro de pruebas térmicas y de estrés (benchmarking) previo a la entrega certificada al cliente.

---

### 2. Requerimientos de Diseño de Arquitectura (Transversales)
Aplican de forma obligatoria a todo el sistema según la especificación del curso:

| ID | Requerimiento de Diseño | Criterio de Aceptación |
| :--- | :--- | :--- |
| **RD-01** | Responsabilidad única e interfaz explícita | Cada componente encapsula su lógica y expone únicamente contratos de interfaz sin acoplar detalles internos. |
| **RD-02** | Separación estricta de capas | Cero reglas de negocio en controladores de API o manejadores HTTP. Los endpoints solo delegan a servicios de aplicación. |
| **RD-03** | Dependencia unidireccional Core / Negocio | El Core jamás referencia ni importa el módulo de negocio; si se retira el módulo, el Core sigue compilando y ejecutándose. |
| **RD-04** | Transiciones centralizadas | Las transiciones de estados se resuelven en un único servicio u orquestador de dominio sin validaciones duplicadas. |
| **RD-05** | Hash criptográfico de contraseñas | No se almacenan contraseñas en texto plano bajo ninguna circunstancia (uso de BCrypt/Argon2 o ASP.NET Identity Hasher). |
| **RD-06** | Autorización del lado del servidor | Cada endpoint verifica roles en servidor; la invocación directa es rechazada si el usuario no posee el rol requerido. |
| **RD-07** | Validación de entradas en servidor | Toda entrada malformada o incompleta produce respuestas controladas (`400 Bad Request`). |
| **RD-08** | Errores limpios y controlados | No se exponen stack traces, rutas locales ni consultas SQL al cliente en entornos de producción. |
| **RD-09** | Persistencia relacional externa | La persistencia de datos reside fuera de los procesos o contenedores de la aplicación mediante volumen o servicio externo. |
| **RD-10** | Manejo de secretos en variables de entorno | Cero credenciales o cadenas de conexión en el repositorio o historial Git. |
| **RD-11** | Estandarización temporal en UTC | Todas las marcas de tiempo se almacenan y procesan bajo el formato UTC. |
| **RD-12** | Testabilidad aislada | Cada pieza cuenta con pruebas unitarias ejecutables de forma independiente sin levantar la API completa. |

---

### 3. Especificación del Módulo de Negocio: PC Builder Ops

#### 3.1 Entidades del Modelo de Datos (Entidades Base)
1. **`OrdenEnsamble`:** Registra la orden de armado y su ciclo de vida.  
   *Atributos:* `Id` (Guid), `CodigoSeguimiento` (string), `ClienteUsuarioId` (Guid), `TecnicoAsignadoId` (Guid?), `Estado` (`Cotizada`, `EnArmado`, `EnPruebas`, `Entregada`), `CostoTotal` (decimal), `FechaCreacion` (DateTime UTC).
2. **`Componente`:** Catálogo de piezas de hardware disponibles.  
   *Atributos:* `Id` (Guid), `Nombre` (string), `Categoria` (string), `SocketOInterfaz` (string: AM5, LGA1700, DDR5, etc.), `ConsumoWatts` (int), `Precio` (decimal), `StockDisponible` (int).
3. **`DetalleEnsamble`:** Ítems asignados a una orden específica.  
   *Atributos:* `Id` (Guid), `OrdenEnsambleId` (Guid), `ComponenteId` (Guid), `Cantidad` (int), `PrecioUnitario` (decimal).
4. **`PruebaRendimiento`:** Telemetría de control de calidad.  
   *Atributos:* `Id` (Guid), `OrdenEnsambleId` (Guid), `TipoSoftwarePrueba` (string: Cinebench, FurMark, etc.), `TemperaturaMaximaCpu` (decimal), `TemperaturaMaximaGpu` (decimal), `PuntajeBenchmark` (int), `EsAprobada` (bool), `FechaEjecucion` (DateTime UTC).

#### 3.2 Máquina de Estados del Negocio
* **Estados (4):**  
  `Cotizada` $\longrightarrow$ `En Armado` $\longrightarrow$ `En Pruebas` $\longrightarrow$ `Entregada`
* **Transición prohibida explícita:**  
  Intentar pasar directamente de `Cotizada` a `Entregada` (rechazo controlado con excepción de dominio).
* **Estado terminal:**  
  `Entregada` (no se admiten transiciones salientes ni modificaciones sobre la orden).

#### 3.3 Requerimientos Funcionales del Dominio (RF-NEG)
* **RF-NEG-01 (Validación técnica de compatibilidad):** El sistema valida que el procesador coincida con el socket de la tarjeta madre y que la fuente (PSU) cubra la demanda energética sumada antes de generar la cotización.
* **RF-NEG-02 (Apertura y reserva de taller):** El sistema permite iniciar la orden en `En Armado`, reservando el inventario y asignando la orden a un técnico responsable.
* **RF-NEG-03 (Registro de estrés térmico y telemetría):** El técnico registra los resultados de pruebas de estrés térmico indicando si la configuración es térmicamente estable.
* **RF-NEG-04 (Certificación de control de calidad):** El pase a `Entregada` solo procede si existen pruebas registradas y aprobadas en su totalidad.
* **RF-NEG-05 (Consulta de expediente de hardware):** El cliente consulta en línea las especificaciones completas, seriales y resultados de pruebas de su ensamble.

---

### 4. Matriz de Integración con el CORE
El módulo de negocio interactúa con las piezas del Core respetando las interfaces provistas:

| Pieza del Core | Período | Punto de Contacto con PC Builder Ops |
| :--- | :--- | :--- |
| **Pieza 1: Control de Acceso** | Semanas 2–4 | Protege endpoints: rol `Estándar` para clientes (cotizan y consultan); rol `Administrador` para técnicos (arman, prueban y certifican). |
| **Pieza 2: Gestión de Permisos** | Semanas 6–8 | Máquina de estados independiente para solicitudes de acceso elevado (`Pendiente` $\to$ `Aprobada` $\to$ `Aplicada` / `Rechazada`). Utilizada para solicitar permisos temporales o autorizar garantías. |
| **Pieza 3: Manejador de Documentos** | Semana 9 | Almacenamiento fuera de contenedor de capturas de pruebas térmicas y reportes en PDF adjuntos a la orden. |
| **Pieza 4: Notificaciones y Cola de Correos** | Semanas 11–12 | Emite alertas internas y encola correos al cliente cuando la orden pasa a `En Pruebas` o queda `Entregada`. |
| **Pieza 5: Reportes con Agregación** | Semana 12 | Genera consultas agrupadas: tiempo promedio de ensamble por técnico y piezas más utilizadas por categoría/fabricante en rangos de fechas. |
| **Pieza 6: Auditoría** | Semana 14 | Registro inmutable de transiciones de estado de la orden y cambios de permisos administrativos. |