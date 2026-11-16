import { useQuery } from '@tanstack/react-query'
import { Link, useSearchParams } from 'react-router'
import { listarComponentes } from '../api/componentes'
import { mensajeDeError } from '../api/errores'
import {
  CATEGORIAS,
  ORDENES,
  escribirFiltros,
  filtrarComponentes,
  leerFiltros,
  opcionesDeFiltro,
  type FiltrosCatalogo,
  type Orden,
} from '../catalogo/filtros'
import { TarjetaComponente } from '../catalogo/TarjetaComponente'
import { Cargando } from '../componentes/Cargando'
import { Boton } from '../componentes/Formulario'
import { Mensaje } from '../componentes/Mensaje'
import { cumpleRequisito } from '../sesion/acceso'
import { useSesion } from '../sesion/contexto'

const estiloCampo =
  'rounded-md border border-slate-300 bg-white px-3 py-2 text-sm outline-none focus:border-teal-600 focus:ring-2 focus:ring-teal-100'

const estiloCategoria = (activa: boolean) =>
  `rounded-full px-3 py-1.5 text-sm font-medium transition-colors ${
    activa
      ? 'bg-teal-700 text-white'
      : 'bg-white text-slate-700 ring-1 ring-slate-200 hover:bg-slate-100'
  }`

// FN-01, RF-NEG-06
export function Catalogo() {
  const { usuario } = useSesion()
  const [parametros, setParametros] = useSearchParams()
  const filtros = leerFiltros(parametros)

  const consulta = useQuery({ queryKey: ['componentes'], queryFn: listarComponentes })

  const actualizar = (cambios: Partial<FiltrosCatalogo>) =>
    setParametros(escribirFiltros({ ...filtros, ...cambios }), { replace: true })

  const elegirCategoria = (categoria: string | null) =>
    actualizar({ categoria, fabricante: null, socket: null })

  const hayFiltros = parametros.toString() !== ''

  if (consulta.isPending) return <Cargando texto="Cargando el catálogo…" />

  if (consulta.isError)
    return (
      <div className="max-w-xl space-y-4">
        <h1 className="text-3xl font-semibold tracking-tight">Catálogo</h1>
        <Mensaje tipo="error">{mensajeDeError(consulta.error)}</Mensaje>
        <Boton variante="secundario" onClick={() => consulta.refetch()}>
          Reintentar
        </Boton>
      </div>
    )

  const componentes = filtrarComponentes(consulta.data, filtros)
  const { fabricantes, sockets } = opcionesDeFiltro(consulta.data, filtros.categoria)

  return (
    <div className="space-y-6">
      <div className="flex flex-wrap items-end justify-between gap-4">
        <div>
          <h1 className="text-3xl font-semibold tracking-tight">Catálogo</h1>
          <p className="mt-1 text-sm text-slate-600">
            {componentes.length === 1 ? '1 componente' : `${componentes.length} componentes`}
            {hayFiltros && ` de ${consulta.data.length}`}
          </p>
        </div>
        {cumpleRequisito(usuario, 'cliente') && (
          <Link
            to="/armar"
            className="rounded-md bg-teal-700 px-4 py-2 text-sm font-medium text-white hover:bg-teal-800"
          >
            Armar mi PC
          </Link>
        )}
      </div>

      <div role="group" aria-label="Categorías" className="flex flex-wrap gap-2">
        <button
          type="button"
          aria-pressed={!filtros.categoria}
          className={estiloCategoria(!filtros.categoria)}
          onClick={() => elegirCategoria(null)}
        >
          Todos
        </button>
        {CATEGORIAS.map(({ valor, etiqueta }) => (
          <button
            key={valor}
            type="button"
            aria-pressed={filtros.categoria === valor}
            className={estiloCategoria(filtros.categoria === valor)}
            onClick={() => elegirCategoria(valor)}
          >
            {etiqueta}
          </button>
        ))}
      </div>

      <div className="grid gap-3 rounded-xl border border-slate-200 bg-white p-4 sm:grid-cols-2 lg:grid-cols-4">
        <label className="flex flex-col gap-1 text-sm font-medium text-slate-700">
          Buscar
          <input
            type="search"
            value={filtros.texto}
            placeholder="Nombre o fabricante"
            onChange={(evento) => actualizar({ texto: evento.target.value })}
            className={estiloCampo}
          />
        </label>
        <label className="flex flex-col gap-1 text-sm font-medium text-slate-700">
          Fabricante
          <select
            value={filtros.fabricante ?? ''}
            onChange={(evento) => actualizar({ fabricante: evento.target.value || null })}
            className={estiloCampo}
          >
            <option value="">Todos</option>
            {fabricantes.map((fabricante) => (
              <option key={fabricante}>{fabricante}</option>
            ))}
          </select>
        </label>
        <label className="flex flex-col gap-1 text-sm font-medium text-slate-700">
          Socket o interfaz
          <select
            value={filtros.socket ?? ''}
            onChange={(evento) => actualizar({ socket: evento.target.value || null })}
            className={estiloCampo}
          >
            <option value="">Todos</option>
            {sockets.map((socket) => (
              <option key={socket}>{socket}</option>
            ))}
          </select>
        </label>
        <label className="flex flex-col gap-1 text-sm font-medium text-slate-700">
          Ordenar por
          <select
            value={filtros.orden}
            onChange={(evento) => actualizar({ orden: evento.target.value as Orden })}
            className={estiloCampo}
          >
            {ORDENES.map(({ valor, etiqueta }) => (
              <option key={valor} value={valor}>
                {etiqueta}
              </option>
            ))}
          </select>
        </label>
        <div className="flex flex-wrap items-center justify-between gap-3 sm:col-span-2 lg:col-span-4">
          <label className="flex items-center gap-2 text-sm text-slate-700">
            <input
              type="checkbox"
              checked={filtros.soloDisponibles}
              onChange={(evento) => actualizar({ soloDisponibles: evento.target.checked })}
              className="size-4 accent-teal-700"
            />
            Solo con existencias
          </label>
          {hayFiltros && (
            <button
              type="button"
              onClick={() => setParametros(new URLSearchParams(), { replace: true })}
              className="text-sm font-medium text-teal-700 hover:underline"
            >
              Limpiar filtros
            </button>
          )}
        </div>
      </div>

      {componentes.length === 0 ? (
        <div className="rounded-xl border border-dashed border-slate-300 bg-white p-10 text-center">
          <p className="font-medium">Ningún componente coincide con los filtros.</p>
          <button
            type="button"
            onClick={() => setParametros(new URLSearchParams(), { replace: true })}
            className="mt-3 text-sm font-medium text-teal-700 hover:underline"
          >
            Limpiar filtros
          </button>
        </div>
      ) : (
        <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
          {componentes.map((componente) => (
            <TarjetaComponente key={componente.id} componente={componente} />
          ))}
        </div>
      )}
    </div>
  )
}
