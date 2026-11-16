import { Link, NavLink } from 'react-router'
import { Marca } from './Logo'
import { enlacesVisibles } from './navegacion'

const estiloEnlace = ({ isActive }: { isActive: boolean }) =>
  `rounded-lg px-3 py-2 text-sm font-medium transition-colors ${
    isActive
      ? 'bg-acento/12 text-acento-texto'
      : 'text-texto-claro hover:bg-superficie-2 hover:text-texto'
  }`

export function Cabecera() {
  return (
    <header className="border-b border-linea bg-barra">
      <div className="mx-auto flex max-w-6xl flex-wrap items-center gap-x-6 gap-y-3 px-4 py-3">
        <Marca />

        <nav
          aria-label="Principal"
          className="order-3 flex w-full flex-wrap gap-1 md:order-none md:w-auto"
        >
          {enlacesVisibles(null).map(({ ruta, texto }) => (
            <NavLink key={ruta} to={ruta} end={ruta === '/'} className={estiloEnlace}>
              {texto}
            </NavLink>
          ))}
        </nav>

        <div className="ml-auto flex items-center gap-2">
          <Link
            to="/login"
            className="rounded-lg px-3 py-2 text-sm font-medium text-texto-claro hover:bg-superficie-2 hover:text-texto"
          >
            Iniciar sesión
          </Link>
          <Link
            to="/registro"
            className="rounded-lg border border-borde-fuerte px-4 py-2 text-sm font-medium text-texto hover:bg-superficie-2"
          >
            Crear cuenta
          </Link>
        </div>
      </div>
    </header>
  )
}
