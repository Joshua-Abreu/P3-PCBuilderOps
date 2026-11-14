# Frontend — PC Builder Ops Web

| Dato | Valor |
|------|-------|
| Tipo de trabajo | Extra, no calificado por ninguna rúbrica. Las entregas del curso tienen prioridad. |
| Carpeta | `src/Web` (proyecto de Node, fuera de `P3-PCBuilderOps.slnx`) |
| Ramas | Una por fase: `feature/web-...`; los cambios del backend que pida el frontend van en su propia rama `feature/...`. |
| Estado | ⏳ En diseño (esta ficha) |

> *Un cliente arma su computadora en el navegador viendo en vivo si las piezas son compatibles, la cotiza y sigue su orden hasta la entrega; el técnico y el Administrador operan el taller desde la misma aplicación.*

## 1. Principios

1. **El servidor manda.** El frontend no decide reglas de negocio: muestra lo que la API permite y explica lo que la API rechaza (RD-02). Ocultar un botón es comodidad, no seguridad (RD-06).
2. **Toda validación del cliente se repite en el servidor.** El frontend puede avisar antes (contraseña sin números, placa con otro socket), pero la decisión final siempre es la respuesta de la API.
3. **Los errores se muestran tal como llegan.** Toda respuesta de error de la API tiene la forma `{"mensaje": "..."}` (RD-07); el frontend la muestra al usuario sin inventar textos propios para reglas del negocio.
4. **El backend no se acopla al frontend.** La API sigue probándose igual con `API.http` y Postman; ninguna ruta existente cambia de forma incompatible.
5. **Las fechas llegan en UTC** (RD-11) y el frontend las muestra en la hora local del navegador.

## 2. Tecnologías

| Para qué | Herramienta |
|----------|-------------|
| Proyecto, servidor de desarrollo y compilación | Vite |
| Interfaz | React + TypeScript (modo estricto) |
| Rutas y pantallas protegidas por rol | React Router |
| Datos de la API (carga, error, caché, recarga) | TanStack Query |
| Estilos | Tailwind CSS |
| Formularios y validación | React Hook Form + Zod |
| Pruebas | Vitest (cuando haya lógica que probar) |
| Calidad | ESLint y Prettier |

Las peticiones se hacen con `fetch` envuelto en un cliente propio (`api/cliente.ts`) que agrega el token, convierte `{"mensaje"}` en un error tipado y cierra la sesión ante un `401`. Las versiones se fijan al crear el proyecto, con las estables de ese momento.

## 3. Estructura propuesta de `src/Web`

```text
src/Web/
├── src/
│   ├── api/            cliente HTTP y una función por endpoint, con sus tipos
│   ├── sesion/         token, usuario actual y rutas protegidas
│   ├── componentes/    piezas reutilizables (botones, tablas, mensajes)
│   ├── paginas/        una carpeta por pantalla
│   └── main.tsx
├── index.html
├── package.json
├── vite.config.ts      proxy de /api hacia http://localhost:5206
├── Dockerfile          compila y sirve con nginx
└── nginx.conf          sirve la aplicación y reenvía /api al servicio api
```

## 4. Conexión con la API

| Entorno | Cómo llega el frontend a la API |
|---------|---------------------------------|
| Desarrollo | `npm run dev` (Vite en `http://localhost:5173`). Vite reenvía `/api/*` a `http://localhost:5206`, así que el navegador ve un solo origen y no hace falta CORS. |
| Docker | Un tercer servicio `web` en `docker-compose.yml`: nginx sirve la aplicación compilada y reenvía `/api/*` al servicio `api`. `docker compose up --build` sigue levantando todo con un solo comando. El puerto público se decide en la fase 7. |

**Sesión:** el token JWT de `POST /api/auth/login` se guarda en `localStorage` y viaja en la cabecera `Authorization: Bearer`. Al cerrar sesión se llama a `POST /api/auth/logout` (la API revoca la sesión) y se borra el token. Ante cualquier `401` se borra el token y se vuelve al inicio de sesión: así se respeta la revocación que la API ya aplica al cambiar la contraseña o desactivar un usuario. React escapa todo el texto que muestra y no se usa HTML crudo, lo que reduce el riesgo de que un script lea el token.

## 5. Roles y navegación

| Rol en la interfaz | Cómo se reconoce | Menú |
|--------------------|------------------|------|
| Visitante | Sin sesión | Inicio, Catálogo, Iniciar sesión, Registrarse |
| Cliente | Rol `Estandar` | Catálogo, Armar mi PC, Mis órdenes, Mis documentos, Mis solicitudes de permiso, Mi cuenta |
| Técnico | Rol `Estandar` con el permiso `OperarTaller` aprobado | Lo del cliente, más Taller |
| Administrador | Rol `Administrador` | Catálogo, Taller, Asignar técnicos, Solicitudes de permiso, Usuarios, Documentos, Mi cuenta |

El Administrador no cotiza (la API responde `403` a `POST /api/ordenes`), por eso no ve «Armar mi PC».

## 6. Pantallas

| Ruta | Rol | Qué hace | Endpoints |
|------|-----|----------|-----------|
| `/` | Todos | Presentación del taller y acceso al catálogo y al armador. | — |
| `/catalogo` | Todos | Componentes con filtros por categoría, fabricante y socket, precio y existencia. | `GET /api/componentes` (**cambio B1**) |
| `/registro` | Visitante | Registro con nombre, correo y contraseña; indica que llegó un correo de activación. | `POST /api/auth/registro` |
| `/activar?token=` | Visitante | Destino del enlace del correo: activa la cuenta y lleva al inicio de sesión. | `GET /api/auth/activar` (**cambio B3**) |
| `/reenviar-activacion` | Visitante | Pide otro correo de activación. | `POST /api/auth/reenviar-activacion` |
| `/login` | Visitante | Inicio de sesión; muestra el bloqueo tras cinco intentos tal como lo informa la API. | `POST /api/auth/login`, `GET /api/auth/yo` |
| `/olvide-password` | Visitante | Pide el correo de recuperación; la respuesta es la misma exista o no el correo. | `POST /api/auth/olvide-password` |
| `/restablecer?token=` | Visitante | Destino del enlace del correo: valida el código y pide la contraseña nueva. | `GET` y `POST /api/auth/restablecer-password` (**cambio B3**) |
| `/armar` | Cliente | **Armador de PC** (sección 7). Termina en la cotización. | `GET /api/componentes`, `POST /api/componentes/validar` (**cambio B2**), `POST /api/ordenes` |
| `/mis-ordenes` | Cliente | Órdenes propias con estado y costo. | `GET /api/ordenes/mias` |
| `/mis-ordenes/:id` | Cliente | Componentes, línea de estados, pruebas de rendimiento y costo convertido a otra moneda. | `GET /api/ordenes/{id}`, `GET /api/ordenes/{id}/costo?moneda=` |
| `/documentos` | Cliente, Administrador | Subir, listar, descargar y eliminar documentos. El Administrador ve los de todos. | `GET`, `POST`, `DELETE /api/documentos`, `GET /api/documentos/{id}/descarga` |
| `/solicitudes` | Cliente | Solicitar `OperarTaller` y ver el estado de las solicitudes propias, con el motivo si fue rechazada. | `POST /api/permisos/solicitudes`, `GET /api/permisos/solicitudes/mias` |
| `/cuenta` | Con sesión | Datos propios, cambiar contraseña y cerrar sesión. | `GET /api/auth/yo`, `POST /api/auth/cambiar-password`, `POST /api/auth/logout` |
| `/taller` | Técnico, Administrador | Órdenes del taller por estado. | `GET /api/taller/ordenes` |
| `/taller/:id` | Técnico, Administrador | Detalle de la orden: pasar a pruebas, registrar una prueba de rendimiento y entregar. Los rechazos de la máquina de estados (`409`) se muestran con su mensaje. | `GET /api/taller/ordenes/{id}` (**cambio B5**), `POST .../pasar-a-pruebas`, `POST .../pruebas`, `POST .../entregar` |
| `/admin/asignar` | Administrador | Órdenes `Cotizada` y asignación de un técnico para iniciar el armado. | `GET /api/taller/ordenes`, `GET /api/taller/tecnicos` (**cambio B6**), `POST /api/ordenes/{id}/iniciar-armado` |
| `/admin/solicitudes` | Administrador | Solicitudes pendientes: aprobar o rechazar con motivo. | `GET .../pendientes`, `POST .../{id}/aprobar`, `POST .../{id}/rechazar` |
| `/admin/usuarios` | Administrador | Listado paginado, crear usuario, cambiar rol, activar o desactivar y forzar restablecimiento. | `GET`, `POST /api/usuarios`, `PUT .../rol`, `PATCH .../estado`, `POST .../forzar-restablecimiento` |

## 7. Flujo del armador de PC

1. **CPU.** El usuario elige una de las CPU del catálogo.
2. **Placa base.** Se muestran primero las placas con el mismo socket que la CPU; las demás aparecen deshabilitadas con el motivo.
3. **RAM, GPU y almacenamiento.** Opcionales, con cantidad.
4. **Fuente de poder.** Se muestra el consumo total y se marcan las fuentes que no alcanzan.
5. **Resumen.** Lista de piezas, consumo frente a potencia, total en USD y la conversión opcional a otra moneda tras cotizar.
6. **Cotizar.** `POST /api/ordenes` crea la orden `Cotizada` y lleva a su detalle.

Durante todo el armado se muestran en vivo el consumo, la potencia y el total. Cada cambio pide a la API que valide el armado (**cambio B2**), así que la regla de compatibilidad vive solo en `VerificadorCompatibilidad`: el filtrado del paso 2 y las marcas del paso 4 son ayudas visuales, y la respuesta del servidor es la que decide. La falta de existencias se informa al iniciar el armado (`409`), como hoy.

## 8. Cambios necesarios en el backend

Cada uno va en su propia rama, con pruebas, sin romper ninguna petición de `API.http` ni la guía del README.

| Id | Cambio | Por qué lo necesita el frontend | Requisito |
|----|--------|---------------------------------|-----------|
| B1 | `GET /api/componentes` sin sesión (`[AllowAnonymous]`). | El catálogo se puede consultar sin registrarse. Solo expone datos públicos del catálogo. | FN-01, RF-NEG-06 (las operaciones siguen protegidas) |
| B2 | `POST /api/componentes/validar`: recibe las líneas del armado y responde si es compatible, el consumo, la potencia, el total y el problema si lo hay, sin crear la orden. Reutiliza `VerificadorCompatibilidad`. | Avisar en vivo sin duplicar la regla en el navegador. Un armado incompleto se informa como pendiente, no como error. | FN-01, RD-02 |
| B3 | Los enlaces de los correos apuntan a las pantallas del frontend (`/activar?token=`, `/restablecer?token=`) cuando se configura una URL del frontend; sin ella siguen apuntando a la API como hoy. Los `GET` de la API se mantienen. | Que el enlace abra una pantalla y no un JSON. | RF-CA-16, RF-CA-10, sección 8 de `AGENTS.md` |
| B4 | `GET /api/auth/yo` incluye los permisos aprobados del usuario. | Saber si mostrar el menú Taller sin provocar un `403`. | RF-GP-05 |
| B5 | `GET /api/taller/ordenes/{id}`: detalle de cualquier orden para el técnico con `OperarTaller` o el Administrador. | Hoy el detalle solo lo ve el cliente dueño; el técnico necesita ver piezas y pruebas para trabajar. | FN-03, FN-04, RF-GP-05 |
| B6 | `GET /api/taller/tecnicos` (Administrador): usuarios activos con `OperarTaller`. | Elegir técnico de una lista en vez de pegar un identificador. | FN-02 |
| B7 | Nombres de cliente y técnico en `OrdenResumen` y `OrdenDetalle`. | Mostrar «Técnico: Juan Pérez» en vez de un identificador. | FN-05 |

Opcional, más adelante: descripción e imagen de cada componente para un catálogo más visual.

## 9. Fuera de alcance por ahora

- Pantallas de Notificaciones, Reportes y Auditoría: se agregan cuando existan sus endpoints (Piezas 4, 5 y 6).
- Pagos, carrito con varias órdenes, valoraciones o cualquier funcionalidad que el backend no tenga.
- Adjuntar documentos a una orden (RF-NEG-10, opcional en el backend).
- Aplicación móvil.

## 10. Orden de trabajo

| Fase | Contenido | Depende de |
|------|-----------|------------|
| 1 | Crear `src/Web` con Vite, TypeScript, Tailwind, ESLint, Prettier y el proxy; cliente HTTP con manejo de `{"mensaje"}` y `401`; diseño base (cabecera, menú, mensajes). | — |
| 2 | Sesión: login, registro, activación, reenvío, recuperación, cambio de contraseña, cierre de sesión y rutas protegidas por rol. | B3, B4 |
| 3 | Catálogo con filtros. | B1 |
| 4 | Armador de PC y cotización. | B2 |
| 5 | Mis órdenes con detalle, línea de estados, pruebas y conversión de moneda; Mis documentos; Mis solicitudes. | B7 |
| 6 | Taller y pantallas del Administrador. | B5, B6, B7 |
| 7 | Servicio `web` en Docker con nginx, README y verificación desde un clon limpio. | Fases 1 a 6 |

## 11. Riesgos

- **Tiempo frente a las entregas:** el frontend se pausa cuando se habilite una práctica o asignación.
- **Cambios de la API por las Piezas 4 a 6:** se agregan endpoints nuevos; los existentes no cambian de forma incompatible, así que las pantallas hechas no se rehacen.
- **El token en `localStorage`:** aceptable para este proyecto por el escape de React y la revocación en el servidor; si se exige más, se cambia a una cookie `HttpOnly`, lo que obliga a modificar la autenticación de la API.
- **Revisión acumulada:** cada cambio del backend (B1 a B7) se verifica con las peticiones de `API.http` antes de fusionarlo.

## 12. Checklist

- [x] Ficha creada
- [ ] Fase 1: base del proyecto
- [ ] Fase 2: sesión (con B3 y B4)
- [ ] Fase 3: catálogo (con B1)
- [ ] Fase 4: armador de PC (con B2)
- [ ] Fase 5: pantallas del cliente (con B7)
- [ ] Fase 6: taller y Administrador (con B5 y B6)
- [ ] Fase 7: Docker, README y verificación desde un clon limpio
