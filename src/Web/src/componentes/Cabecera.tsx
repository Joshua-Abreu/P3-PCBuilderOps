import { Link, NavLink } from 'react-router'
import { Logo } from './Logo'

const enlaces = [
  { ruta: '/', texto: 'Inicio' },
  { ruta: '/catalogo', texto: 'Catálogo' },
  { ruta: '/armar', texto: 'Armar mi PC' },
]

const estiloEnlace = ({ isActive }: { isActive: boolean }) =>
  `rounded-md px-3 py-2 text-sm font-medium transition-colors ${
    isActive ? 'bg-teal-50 text-teal-800' : 'text-slate-600 hover:bg-slate-100 hover:text-slate-900'
  }`

export function Cabecera() {
  return (
    <header className="border-b border-slate-200 bg-white">
      <div className="mx-auto flex max-w-6xl flex-wrap items-center gap-x-6 gap-y-3 px-4 py-3">
        <Link to="/" className="flex items-center gap-2">
          <Logo />
          <span className="text-lg font-semibold tracking-tight">PC Builder Ops</span>
        </Link>

        <nav aria-label="Principal" className="order-3 flex w-full gap-1 sm:order-none sm:w-auto">
          {enlaces.map(({ ruta, texto }) => (
            <NavLink key={ruta} to={ruta} end={ruta === '/'} className={estiloEnlace}>
              {texto}
            </NavLink>
          ))}
        </nav>

        <div className="ml-auto flex items-center gap-2">
          <Link
            to="/login"
            className="rounded-md px-3 py-2 text-sm font-medium text-slate-700 hover:bg-slate-100"
          >
            Iniciar sesión
          </Link>
          <Link
            to="/registro"
            className="rounded-md bg-teal-700 px-3 py-2 text-sm font-medium text-white hover:bg-teal-800"
          >
            Registrarse
          </Link>
        </div>
      </div>
    </header>
  )
}
