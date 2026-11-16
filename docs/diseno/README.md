# Diseño visual del frontend

| Archivo o enlace | Qué es |
|------------------|--------|
| `stitch/*.png` y `stitch/*.html` | Pantallas generadas con Google Stitch (inicio de sesión, registro, catálogo, armador, mis órdenes y panel del taller). Son **inspiración**: el HTML no se copia al proyecto. |
| [Lienzo en Claude Design](https://claude.ai/artifact/Sjh1FoBXfTwAJz41MDsffV) | Versión afinada de Stitch con los datos reales de la API: sistema visual, inicio de sesión, catálogo, armador y mis órdenes. Es privado: solo lo abre su dueño o quien reciba acceso. |
| `src/Web/src/index.css` | Fuente única de colores y letras en el código (`@theme` de Tailwind). |

## Qué se tomó de Stitch

- Tema oscuro azul marino con un solo acento cian.
- Menú lateral cuando hay sesión; cabecera arriba para el visitante.
- Inicio de sesión y registro en una tarjeta de dos columnas, con las ventajas del taller a la izquierda.
- Tarjetas del catálogo con categoría, existencias, socket, energía y precio.
- Armador por pasos con resumen lateral; órdenes en lista con detalle al lado.

## Qué se descartó y por qué

Stitch inventa datos que la API no tiene. No se dibujan ni se programan:

- Telemetría, bancos de prueba, «muelles», RMA, garantías, descuentos ni cargos por servicio.
- Entrar con Google o GitHub, «mantener sesión», elegir rol al registrarse o código de invitación: el rol lo asigna la API y el técnico lo obtiene con una solicitud de permiso (RF-GP).
- Rastrear una orden sin sesión: RF-NEG-06 exige sesión.
- Fotos y especificaciones (núcleos, VRAM, frecuencias): el componente solo trae nombre, categoría, fabricante, socket o interfaz, consumo o potencia, precio y existencias.

## Colores

| Uso | Clase de Tailwind | Valor |
|-----|-------------------|-------|
| Fondo de la página | `bg-fondo` | `#070d1b` |
| Menú y cabecera | `bg-barra` | `#0b1222` |
| Tarjetas | `bg-superficie` | `#0d1527` |
| Controles y cajas internas | `bg-superficie-2` | `#131d33` |
| Divisiones | `border-linea`, `border-borde`, `border-borde-fuerte` | `#18233b`, `#22304b`, `#2c3d5e` |
| Texto | `text-texto`, `text-texto-claro`, `text-texto-suave`, `text-texto-tenue` | `#e7ecf6`, `#c9d2e3`, `#a6b2c8`, `#8a97b0` |
| Acción principal | `bg-acento` con `text-sobre-acento` | `#22c3e0` sobre `#03141b` |
| Enlaces y elemento activo | `text-acento-texto` | `#67dcf2` |
| Compatible, entregada | `text-exito` | `#43d29d` |
| Pocas existencias, en armado | `text-aviso` | `#f2b84b` |
| Errores de la API | `text-error` | `#ff8f87` |

Los estados de la orden usan su color **y** su nombre, para no depender solo del color: Cotizada en gris claro, En armado en ámbar, En pruebas en cian y Entregada en verde.

## Letras

- **Inter** para todo el texto.
- **JetBrains Mono** solo para cifras y códigos: precios, watts, sockets y el código de seguimiento (`ORD-…`).
- Ambas se instalan con `@fontsource-variable` y se sirven desde la propia aplicación, sin pedir nada a Google Fonts.

## Reglas al hacer pantallas nuevas

1. Usar las clases de la tabla; no escribir colores sueltos (`text-slate-…`, `#…`) en los componentes.
2. Reutilizar `Boton`, `Campo`, `TarjetaFormulario`, `Mensaje` y `Cargando` de `componentes/`, y `estiloBotonPrimario`, `estiloBotonSecundario` y `estiloEnlace` para enlaces con forma de botón.
3. Mostrar solo datos que devuelve la API; si una pantalla de Stitch pide algo que no existe, se anota en `docs/frontend.md` en vez de inventarlo.
4. Revisar cada pantalla en el ancho de un teléfono (375 px) sin desplazamiento horizontal.
