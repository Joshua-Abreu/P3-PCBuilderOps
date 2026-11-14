import { createBrowserRouter } from 'react-router'
import { Plantilla } from './componentes/Plantilla'
import { Activar } from './paginas/Activar'
import { Cuenta } from './paginas/Cuenta'
import { Inicio } from './paginas/Inicio'
import { Login } from './paginas/Login'
import { NoEncontrada } from './paginas/NoEncontrada'
import { PaginaError } from './paginas/PaginaError'
import { Proximamente } from './paginas/Proximamente'
import { OlvidePassword, ReenviarActivacion } from './paginas/Recuperacion'
import { Registro } from './paginas/Registro'
import { Restablecer } from './paginas/Restablecer'
import { RutaProtegida, SoloVisitantes } from './sesion/RutaProtegida'

export const enrutador = createBrowserRouter([
  {
    path: '/',
    element: <Plantilla />,
    errorElement: <PaginaError />,
    children: [
      { index: true, element: <Inicio /> },
      { path: 'catalogo', element: <Proximamente titulo="Catálogo" fase={3} /> },
      { path: 'activar', element: <Activar /> },
      { path: 'restablecer', element: <Restablecer /> },
      {
        element: <SoloVisitantes />,
        children: [
          { path: 'login', element: <Login /> },
          { path: 'registro', element: <Registro /> },
          { path: 'reenviar-activacion', element: <ReenviarActivacion /> },
          { path: 'olvide-password', element: <OlvidePassword /> },
        ],
      },
      {
        element: <RutaProtegida />,
        children: [{ path: 'cuenta', element: <Cuenta /> }],
      },
      {
        element: <RutaProtegida requisito="cliente" />,
        children: [
          { path: 'armar', element: <Proximamente titulo="Armar mi PC" fase={4} /> },
          { path: 'mis-ordenes', element: <Proximamente titulo="Mis órdenes" fase={5} /> },
        ],
      },
      {
        element: <RutaProtegida requisito="taller" />,
        children: [{ path: 'taller', element: <Proximamente titulo="Taller" fase={6} /> }],
      },
      {
        element: <RutaProtegida requisito="administrador" />,
        children: [{ path: 'admin', element: <Proximamente titulo="Administración" fase={6} /> }],
      },
      { path: '*', element: <NoEncontrada /> },
    ],
  },
])
