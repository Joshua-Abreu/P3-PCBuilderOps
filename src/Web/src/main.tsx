import '@fontsource-variable/inter'
import '@fontsource-variable/jetbrains-mono'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import { RouterProvider } from 'react-router/dom'
import { ErrorApi } from './api/errores'
import './index.css'
import { enrutador } from './rutas'
import { ProveedorSesion } from './sesion/ProveedorSesion'

const clienteConsultas = new QueryClient({
  defaultOptions: {
    queries: {
      refetchOnWindowFocus: false,
      retry: (intentos, error) =>
        intentos < 2 && !(error instanceof ErrorApi && error.estado >= 400 && error.estado < 500),
    },
  },
})

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <QueryClientProvider client={clienteConsultas}>
      <ProveedorSesion>
        <RouterProvider router={enrutador} />
      </ProveedorSesion>
    </QueryClientProvider>
  </StrictMode>,
)
