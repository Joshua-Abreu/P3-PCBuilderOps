import { Link, NavLink, useNavigate } from 'react-router'
import { cumpleRequisito, type Requisito } from '../sesion/acceso'
import { useSesion } from '../sesion/contexto'
import { Logo } from './Logo'

interface Enlace {
  ruta: string
  texto: string
  requisito?: Requisito
  soloVisitantesOClientes?: boolean
}

const enlaces: Enlace[] = [
  { ruta: '/', texto: 'Inicio' },
  { ruta: '/catalogo', texto: 'Catálogo' },
  { ruta: '/armar', texto: 'Armar mi PC', soloVisitantesOClientes: true },
  { ruta: '/mis-ordenes', texto: 'Mis órdenes', requisito: 'cliente' },
  { ruta: '/taller', texto: 'Taller', requisito: 'taller' },
  { ruta: '/admin', texto: 'Administración', requisito: 'administrador' },
]

const estiloEnlace = ({ isActive }: { isActive: boolean }) =>
  `rounded-md px-3 py-2 text-sm font-medium transition-colors ${
    isActive ? 'bg-teal-50 text-teal-800' : 'text-slate-600 hover:bg-slate-100 hover:text-slate-900'
  }`

export function Cabecera() {
  const { usuario, cargando, cerrarSesion } = useSesion()
  const navegar = useNavigate()

  const visibles = enlaces.filter(({ requisito, soloVisitantesOClientes }) => {
    if (soloVisitantesOClientes) return !usuario || cumpleRequisito(usuario, 'cliente')
    return !requisito || cumpleRequisito(usuario, requisito)
  })

  const salir = async () => {
    await navegar('/', { replace: true })
    await cerrarSesion()
  }

  return (
    <header className="border-b border-slate-200 bg-white">
      <div className="mx-auto flex max-w-6xl flex-wrap items-center gap-x-6 gap-y-3 px-4 py-3">
        <Link to="/" className="flex items-center gap-2">
          <Logo />
          <span className="text-lg font-semibold tracking-tight">PC Builder Ops</span>
        </Link>

        <nav
          aria-label="Principal"
          className="order-3 flex w-full flex-wrap gap-1 lg:order-none lg:w-auto"
        >
          {visibles.map(({ ruta, texto }) => (
            <NavLink key={ruta} to={ruta} end={ruta === '/'} className={estiloEnlace}>
              {texto}
            </NavLink>
          ))}
        </nav>

        <div className="ml-auto flex items-center gap-2">
          {cargando ? null : usuario ? (
            <>
              <NavLink to="/cuenta" className={estiloEnlace}>
                {usuario.nombre}
              </NavLink>
              <button
                type="button"
                onClick={salir}
                className="rounded-md px-3 py-2 text-sm font-medium text-slate-600 hover:bg-slate-100 hover:text-slate-900"
              >
                Cerrar sesión
              </button>
            </>
          ) : (
            <>
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
            </>
          )}
        </div>
      </div>
    </header>
  )
}
