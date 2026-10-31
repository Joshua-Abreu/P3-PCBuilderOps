# Máquina de Estados - Órdenes de Ensamble

## Flujo de Estados

```
Cotizada (1) → EnArmado (2) → EnPruebas (3) → Entregada (4)
```

## Transiciones Permitidas

| Estado Origen | Estado Destino | Quién la ejecuta | Condición / Regla |
|---------------|----------------|------------------|-------------------|
| Cotizada (1) | EnArmado (2) | Administrador | El técnico indicado tiene el permiso `OperarTaller`. |
| EnArmado (2) | EnPruebas (3) | Estándar con `OperarTaller` | Ensamble físico terminado. |
| EnPruebas (3) | Entregada (4) | Estándar con `OperarTaller` | Existe al menos una prueba de rendimiento y la más reciente está aprobada. |

Al pasar a `EnArmado` se asigna el técnico y se reserva el stock; si no alcanza, la operación se rechaza y nada cambia.

## Transiciones Prohibidas

Intentar cualquiera de estas transiciones se rechaza con `409 Conflict` y el estado no cambia.

| Estado Origen | Estado Destino | Motivo |
|---------------|----------------|--------|
| Cotizada (1) | Entregada (4) | **Prohibida explícitamente:** una orden no se entrega sin armarse ni probarse. |
| Cotizada (1) | EnPruebas (3) | No se prueba lo que no se ha armado. |
| Cotizada (1) | Cotizada (1) | No es un cambio de estado. |
| EnArmado (2) | Cotizada (1) | El flujo no retrocede. |
| EnArmado (2) | EnArmado (2) | No es un cambio de estado. |
| EnArmado (2) | Entregada (4) | No se entrega sin pruebas. |
| EnPruebas (3) | Cotizada (1) | El flujo no retrocede. |
| EnPruebas (3) | EnArmado (2) | El flujo no retrocede; una corrección se resuelve registrando otra prueba. |
| EnPruebas (3) | EnPruebas (3) | No es un cambio de estado. |
| Entregada (4) | Cotizada (1) | Estado terminal. |
| Entregada (4) | EnArmado (2) | Estado terminal. |
| Entregada (4) | EnPruebas (3) | Estado terminal. |
| Entregada (4) | Entregada (4) | Estado terminal. |

## Estado Terminal

**Entregada (4)** es el estado terminal. Ninguna transición parte de él.

## Prueba Reprobada

Una prueba reprobada no cambia el estado: la orden sigue en `EnPruebas`, el técnico corrige y registra otra prueba. La entrega depende de la prueba más reciente; las anteriores se conservan como historial.

## Implementación

`MaquinaEstadosOrdenEnsamble`, en `src/Modules/PCBuilderOps/Domain/`, declara los estados permitidos en una sola tabla y es el único punto que resuelve las transiciones (RD-04). Es independiente de la máquina de Gestión de permisos (RF-NEG-09).
