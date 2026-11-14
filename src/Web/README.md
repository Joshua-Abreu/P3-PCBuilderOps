# PC Builder Ops Web

Frontend en React con TypeScript que consume la API de PC Builder Ops. El diseño, las pantallas y las fases están en [`docs/frontend.md`](../../docs/frontend.md).

## Requisitos

- Node.js 24 o superior (incluye npm).
- La API corriendo en `http://localhost:5206`, con `docker compose up` o `dotnet run --project src/API`.

## Comandos

Desde `src/Web`:

| Comando          | Qué hace                                                                                                              |
| ---------------- | --------------------------------------------------------------------------------------------------------------------- |
| `npm install`    | Instala las dependencias (la primera vez).                                                                            |
| `npm run dev`    | Abre el frontend en `http://localhost:5173`. Las peticiones a `/api` se reenvían a `http://localhost:5206`, sin CORS. |
| `npm test`       | Ejecuta las pruebas con Vitest.                                                                                       |
| `npm run lint`   | Revisa el código con oxlint.                                                                                          |
| `npm run format` | Da formato al código con Prettier (`npm run format:check` solo lo revisa).                                            |
| `npm run build`  | Comprueba los tipos y genera la versión de producción en `dist/`.                                                     |

## Estructura

| Carpeta           | Contenido                                                    |
| ----------------- | ------------------------------------------------------------ |
| `src/api`         | Cliente HTTP (`pedir`) y los errores de la API (`ErrorApi`). |
| `src/sesion`      | Token de la sesión y aviso de cierre de sesión.              |
| `src/componentes` | Piezas reutilizables: plantilla, cabecera, pie y mensajes.   |
| `src/paginas`     | Una pantalla por archivo.                                    |
| `src/rutas.tsx`   | Rutas de la aplicación.                                      |
