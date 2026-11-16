import type { Componente } from '../api/componentes'
import { etiquetaCategoria } from './filtros'
import { formatearPrecio } from './formato'

const POCAS_EXISTENCIAS = 3

function Existencias({ cantidad }: { cantidad: number }) {
  if (cantidad === 0) return <span className="text-sm font-medium text-red-700">Agotado</span>

  if (cantidad <= POCAS_EXISTENCIAS)
    return (
      <span className="text-sm font-medium text-amber-700">
        {cantidad === 1 ? 'Queda 1' : `Quedan ${cantidad}`}
      </span>
    )

  return <span className="text-sm font-medium text-emerald-700">Disponible ({cantidad})</span>
}

// FN-01
export function TarjetaComponente({ componente }: { componente: Componente }) {
  const energia =
    componente.potenciaWatts !== null
      ? `Entrega ${componente.potenciaWatts} W`
      : `Consume ${componente.consumoWatts} W`

  return (
    <article className="flex flex-col rounded-xl border border-slate-200 bg-white p-5">
      <p className="text-xs font-medium tracking-wide text-teal-700 uppercase">
        {etiquetaCategoria(componente.categoria)}
      </p>
      <h3 className="mt-1 font-semibold text-balance">{componente.nombre}</h3>
      <p className="text-sm text-slate-600">{componente.fabricante}</p>

      <dl className="mt-4 flex flex-wrap gap-2 text-xs">
        <div className="rounded-md bg-slate-100 px-2 py-1">
          <dt className="sr-only">Socket o interfaz</dt>
          <dd>{componente.socketOInterfaz}</dd>
        </div>
        <div className="rounded-md bg-slate-100 px-2 py-1">
          <dt className="sr-only">Energía</dt>
          <dd>{energia}</dd>
        </div>
      </dl>

      <div className="mt-auto flex items-end justify-between gap-2 pt-5">
        <p className="text-xl font-semibold">{formatearPrecio(componente.precio)}</p>
        <Existencias cantidad={componente.stockDisponible} />
      </div>
    </article>
  )
}
