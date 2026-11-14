import { createBrowserRouter } from 'react-router'
import { Plantilla } from './componentes/Plantilla'
import { Inicio } from './paginas/Inicio'
import { NoEncontrada } from './paginas/NoEncontrada'
import { PaginaError } from './paginas/PaginaError'
import { Proximamente } from './paginas/Proximamente'

export const enrutador = createBrowserRouter([
  {
    path: '/',
    element: <Plantilla />,
    errorElement: <PaginaError />,
    children: [
      { index: true, element: <Inicio /> },
      { path: 'catalogo', element: <Proximamente titulo="Catálogo" fase={3} /> },
      { path: 'armar', element: <Proximamente titulo="Armar mi PC" fase={4} /> },
      { path: 'login', element: <Proximamente titulo="Iniciar sesión" fase={2} /> },
      { path: 'registro', element: <Proximamente titulo="Registrarse" fase={2} /> },
      { path: '*', element: <NoEncontrada /> },
    ],
  },
])
