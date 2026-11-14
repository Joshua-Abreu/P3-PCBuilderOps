import { Navigate, Outlet, useLocation } from 'react-router'
import { Cargando } from '../componentes/Cargando'
import { SinAcceso } from '../paginas/SinAcceso'
import { cumpleRequisito, type Requisito } from './acceso'
import { useSesion } from './contexto'

// RD-06
export function RutaProtegida({ requisito = 'sesion' }: { requisito?: Requisito }) {
  const { usuario, cargando } = useSesion()
  const ubicacion = useLocation()

  if (cargando) return <Cargando />

  if (!usuario)
    return (
      <Navigate to="/login" replace state={{ desde: `${ubicacion.pathname}${ubicacion.search}` }} />
    )

  return cumpleRequisito(usuario, requisito) ? <Outlet /> : <SinAcceso />
}

// RF-CA-03, RF-CA-13
export function SoloVisitantes() {
  const { usuario, cargando, debeCambiarPassword } = useSesion()
  const desde = (useLocation().state as { desde?: string } | null)?.desde

  if (cargando) return <Cargando />

  if (!usuario) return <Outlet />

  return <Navigate to={debeCambiarPassword ? '/cuenta' : (desde ?? '/')} replace />
}
