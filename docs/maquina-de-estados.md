# Máquina de Estados - Órdenes de Servicio

## Flujo de States

```
Recibida (1) → EnDiagnostico (2) → EnProceso (3) → Entregada (4)
```

## Tabla de Transiciones

| Estado Origen | Estado Destino | Quién la ejecuta | Condición / Regla |
|---------------|----------------|------------------|-------------------|
| Recibida (1) | EnDiagnostico (2) | Técnico | Orden recibida y asignada a técnico |
| EnDiagnostico (2) | EnProceso (3) | Técnico | Diagnóstico completado, inicio de reparación |
| EnProceso (3) | Entregada (4) | Técnico | Reparación finalizada, lista para entrega |
| Entregada (4) | — | — | **Estado terminal**: no permite transiciones |

## Estado Terminal

**Entregada (4)** es el estado terminal. Desde este estado, ninguna transición es válida. La orden ha completado su ciclo de vida.

## Transición Prohibida Explícita

**Recibida (1) → Entregada (4)** está prohibida explícitamente. No se puede saltar directamente de Recibida a Entregada sin pasar por los estados intermedios (EnDiagnostico y EnProceso).

Cualquier salto no definido en la tabla de transiciones es inválido y lanzará una excepción `InvalidOperationException`.

## Implementación

La clase `MaquinaEstadosOrden` centraliza todas las transiciones válidas en un diccionario estático. El método `ValidarTransicion` verifica si una transición es permitida antes de ejecutarla.
