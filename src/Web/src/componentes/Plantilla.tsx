import { Outlet } from 'react-router'
import { useSesion } from '../sesion/contexto'
import { Cabecera } from './Cabecera'
import { Cargando } from './Cargando'
import { MenuLateral } from './MenuLateral'
import { Pie } from './Pie'

export function Plantilla() {
  const { usuario, cargando } = useSesion()

  if (cargando)
    return (
      <div className="flex min-h-screen items-center justify-center">
        <Cargando />
      </div>
    )

  if (usuario)
    return (
      <div className="min-h-screen lg:flex">
        <MenuLateral />
        <main className="mx-auto w-full max-w-6xl min-w-0 flex-1 px-4 py-8 sm:px-8 lg:py-10">
          <Outlet />
        </main>
      </div>
    )

  return (
    <div className="flex min-h-screen flex-col">
      <Cabecera />
      <main className="mx-auto w-full max-w-6xl flex-1 px-4 py-10">
        <Outlet />
      </main>
      <Pie />
    </div>
  )
}
