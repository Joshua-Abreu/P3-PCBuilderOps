# Módulo de negocio — PC Builder Ops

Fuente: *Propuesta de Proyecto de Dominio Propio* (aprobada) y `docs/SRS.md`. Este archivo describe el **objetivo**; el estado real del código está en la sección 0 de `AGENTS.md`.

PC Builder Ops gestiona el ciclo de computadoras personalizadas a pedido: cotización con verificación de compatibilidad, armado en taller, pruebas de estrés térmico y benchmarking, y entrega certificada al cliente.

> **IDs:** `RF-NEG-01` a `RF-NEG-10` son los requisitos genéricos del maestro (ver `docs/requisitos/core.md`, sección 9). Las funcionalidades propias del dominio usan el prefijo **`FN-`** para no confundirse con ellos, igual que en `docs/SRS.md`.

## 1. Entidades (RF-NEG-01)

| Entidad | Atributos | Relaciones |
|---------|-----------|------------|
| `OrdenEnsamble` | `Id`, `CodigoSeguimiento`, `ClienteUsuarioId`, `TecnicoAsignadoId?`, `Estado`, `CostoTotal`, `FechaCreacionUtc` | 1 cliente (Usuario) · 0..1 técnico (Usuario) · N `DetalleEnsamble` · N `PruebaRendimiento` |
| `Componente` | `Id`, `Nombre`, `Categoria`, `Fabricante`, `SocketOInterfaz`, `ConsumoWatts`, `PotenciaWatts?`, `Precio`, `StockDisponible` | N `DetalleEnsamble` |
| `DetalleEnsamble` | `Id`, `OrdenEnsambleId`, `ComponenteId`, `Cantidad`, `PrecioUnitario` | 1 `OrdenEnsamble` · 1 `Componente` |
| `PruebaRendimiento` | `Id`, `OrdenEnsambleId`, `TipoSoftwarePrueba`, `TemperaturaMaximaCpu`, `TemperaturaMaximaGpu`, `PuntajeBenchmark`, `EsAprobada`, `FechaEjecucionUtc` | 1 `OrdenEnsamble` |

`Fabricante` se agrega a `Componente` porque el reporte de negocio agrupa "piezas más utilizadas por fabricante". `PotenciaWatts` solo aplica a las fuentes de poder y permite comprobar que cubren el consumo (FN-01).

## 2. Máquina de estados de `OrdenEnsamble` (RF-NEG-03, 04, 05, 09)

| Desde | Hacia | Quién la ejecuta | Condición |
|-------|-------|------------------|-----------|
| Cotizada (1) | EnArmado (2) | Administrador | El técnico indicado tiene `OperarTaller`. Reserva stock. |
| EnArmado (2) | EnPruebas (3) | Estándar con `OperarTaller`, o Administrador | Ensamble físico terminado. |
| EnPruebas (3) | Entregada (4) | Estándar con `OperarTaller`, o Administrador | Existe al menos una `PruebaRendimiento` y la más reciente está aprobada. |
| Entregada (4) | — | — | **Estado terminal** (RF-NEG-05): ninguna transición sale de aquí. |
| Cotizada (1) | Entregada (4) | — | **Prohibida explícitamente** (RF-NEG-04): se rechaza y el estado no cambia. |
| Cualquier otra combinación | — | — | Rechazada; el estado no cambia. |

- Estados y transiciones declarados en **un solo lugar** del módulo (RD-04); independiente de la máquina de Gestión de permisos (RF-NEG-09).
- La tabla completa, fila por fila, está en `docs/maquina-de-estados.md`.
- Una prueba reprobada no cambia el estado: el técnico corrige y registra otra. La orden no vuelve a `EnArmado`.

## 3. Funcionalidades separables (RF-NEG-02)

| ID | Funcionalidad | Regla principal |
|----|---------------|-----------------|
| FN-01 | Cotización con verificación de compatibilidad | El socket de la CPU coincide con el de la tarjeta madre y la PSU cubre la suma de `ConsumoWatts` de los componentes. Incompatible → rechazo controlado. |
| FN-02 | Apertura y asignación de la orden a un técnico | El Administrador la asigna a un técnico con `OperarTaller`; pasa a `EnArmado`, reserva stock y registra el técnico responsable. |
| FN-03 | Registro de pruebas de rendimiento y estrés térmico | El técnico registra telemetría y veredicto (`EsAprobada`). Exige el permiso `OperarTaller` de Gestión de permisos. |
| FN-04 | Aprobación final de control de calidad | El pase a `Entregada` solo procede si hay pruebas y la más reciente está aprobada. |
| FN-05 | Consulta de trazabilidad para el cliente | El cliente ve componentes y resultados de **sus** órdenes, no las de otros (RF-NEG-06). |

## 4. Integración con el Core

| Pieza | Uso en PC Builder Ops |
|-------|----------------------|
| Control de acceso (RF-NEG-06) | Estándar = cliente (cotiza y consulta sus órdenes). Administrador = gestor de taller (asigna técnicos). |
| Gestión de permisos | Técnico = Estándar con `OperarTaller`, obtenido mediante una solicitud aprobada. El servicio del módulo consulta el permiso con `IVerificadorPermisos`. |
| Notificaciones (RF-NEG-07 / RF-NOT-03) | Notificación interna y correo al cliente cuando su orden pasa a `EnPruebas` y cuando queda `Entregada`. |
| Reportes (RF-NEG-08 / RF-REP-02) | Tiempo promedio de ensamble por técnico y piezas más utilizadas por fabricante, filtrables por rango de fechas. El tiempo de ensamble exige guardar la fecha de cada transición (historial de estados o auditoría). |
| Documentos (RF-NEG-10, opcional) | Capturas y reportes de benchmark adjuntos a la orden. Si se implementa, debe cumplir toda la pieza 3. |
| Auditoría | Transiciones de estado de la orden. |
