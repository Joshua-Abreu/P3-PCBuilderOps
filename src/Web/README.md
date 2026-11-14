# PC Builder Ops Web

Frontend en React con TypeScript que consume la API de PC Builder Ops. El diseño, las pantallas y las fases están en [`docs/frontend.md`](../../docs/frontend.md).

## Requisitos

- Node.js 24 o superior (incluye npm).
- La API corriendo en `http://localhost:5206`, con `docker compose up` o `dotnet run --project src/API`.

## Comandos

Desde `src/Web`:

| Comando          | Qué hace                                                                                                                                                                      |
| ---------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `npm install`    | Instala las dependencias (la primera vez).                                                                                                                                    |
| `npm run dev`    | Abre el frontend en `http://localhost:5173`. Las peticiones a `/api` se reenvían a `http://localhost:5206`, sin CORS. Con la variable `API_URL` se reenvían a otra dirección. |
| `npm test`       | Ejecuta las pruebas con Vitest.                                                                                                                                               |
| `npm run lint`   | Revisa el código con oxlint.                                                                                                                                                  |
| `npm run format` | Da formato al código con Prettier (`npm run format:check` solo lo revisa).                                                                                                    |
| `npm run build`  | Comprueba los tipos y genera la versión de producción en `dist/`.                                                                                                             |

Para que los enlaces de los correos de activación y recuperación abran las pantallas del frontend, la API necesita `WEB_BASE_URL=http://localhost:5173` en su configuración (`.env` o `launchSettings.json`).

## Estructura

| Carpeta           | Contenido                                                                                                   |
| ----------------- | ----------------------------------------------------------------------------------------------------------- |
| `src/api`         | Cliente HTTP (`pedir`) y los errores de la API (`ErrorApi`).                                                |
| `src/sesion`      | Token, contexto de la sesión, reglas de acceso por rol, rutas protegidas y validaciones de los formularios. |
| `src/componentes` | Piezas reutilizables: plantilla, cabecera, pie y mensajes.                                                  |
| `src/paginas`     | Una pantalla por archivo.                                                                                   |
| `src/rutas.tsx`   | Rutas de la aplicación.                                                                                     |
