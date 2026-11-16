import { useState } from 'react'
import { NavLink, useLocation, useNavigate } from 'react-router'
import { describirRol } from '../sesion/acceso'
import { useSesion } from '../sesion/contexto'
import { Marca } from './Logo'
import { enlacesVisibles } from './navegacion'

const estiloEnlace = ({ isActive }: { isActive: boolean }) =>
  `flex min-h-11 items-center rounded-lg px-3 text-[15px] font-medium transition-colors ${
    isActive
      ? 'bg-acento/12 text-acento-texto'
      : 'text-texto-claro hover:bg-superficie-2 hover:text-texto'
  }`

export function MenuLateral() {
  const { usuario, cerrarSesion } = useSesion()
  const navegar = useNavigate()
  const { pathname } = useLocation()
  const [abiertoEn, setAbiertoEn] = useState<string | null>(null)
  const abierto = abiertoEn === pathname

  if (!usuario) return null

  const salir = async () => {
    await navegar('/', { replace: true })
    await cerrarSesion()
  }

  return (
    <aside className="border-b border-linea bg-barra lg:sticky lg:top-0 lg:flex lg:h-screen lg:w-60 lg:shrink-0 lg:flex-col lg:border-r lg:border-b-0">
      <div className="flex items-center justify-between gap-3 px-4 py-3 lg:px-4 lg:pt-5 lg:pb-6">
        <Marca />
        <button
          type="button"
          aria-expanded={abierto}
          aria-controls="menu-principal"
          onClick={() => setAbiertoEn(abierto ? null : pathname)}
          className="rounded-lg border border-borde-fuerte px-3 py-2 text-sm font-medium text-texto hover:bg-superficie-2 lg:hidden"
        >
          {abierto ? 'Cerrar menú' : 'Menú'}
        </button>
      </div>

      <div
        id="menu-principal"
        className={`${abierto ? 'flex' : 'hidden'} flex-1 flex-col gap-6 px-3 pb-4 lg:flex`}
      >
        <nav aria-label="Principal" className="flex flex-col gap-1">
          {enlacesVisibles(usuario).map(({ ruta, texto }) => (
            <NavLink key={ruta} to={ruta} end={ruta === '/'} className={estiloEnlace}>
              {texto}
            </NavLink>
          ))}
          <NavLink to="/cuenta" className={estiloEnlace}>
            Mi cuenta
          </NavLink>
        </nav>

        <div className="mt-auto flex flex-col gap-3 rounded-xl border border-borde bg-superficie p-3">
          <div className="min-w-0">
            <p className="truncate text-sm font-semibold">{usuario.nombre}</p>
            <p className="text-sm text-texto-suave">{describirRol(usuario)}</p>
          </div>
          <button
            type="button"
            onClick={salir}
            className="rounded-lg border border-borde-fuerte bg-superficie-2 px-3 py-2 text-sm font-medium text-texto hover:border-acento/60"
          >
            Cerrar sesión
          </button>
        </div>
      </div>
    </aside>
  )
}
