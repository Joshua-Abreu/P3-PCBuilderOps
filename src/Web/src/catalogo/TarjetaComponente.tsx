import type { Componente } from '../api/componentes'
import { etiquetaCategoria } from './filtros'
import { FotoComponente } from './FotoComponente'
import { formatearPrecio } from './formato'

const POCAS_EXISTENCIAS = 3

function Existencias({ cantidad }: { cantidad: number }) {
  if (cantidad === 0) return <span className="text-sm font-medium text-error">Agotado</span>

  if (cantidad <= POCAS_EXISTENCIAS)
    return (
      <span className="text-sm font-medium text-aviso">
        {cantidad === 1 ? 'Queda 1' : `Quedan ${cantidad}`}
      </span>
    )

  return <span className="text-sm font-medium text-exito">{cantidad} en existencia</span>
}

// RF-NEG-01
export function EtiquetaRetirado() {
  return (
    <span className="rounded-full border border-borde-fuerte bg-superficie-2 px-2.5 py-0.5 text-xs font-semibold text-texto-suave">
      Retirado
    </span>
  )
}

// FN-01
export function TarjetaComponente({ componente }: { componente: Componente }) {
  const energia =
    componente.potenciaWatts !== null
      ? { titulo: 'Potencia', valor: `${componente.potenciaWatts} W` }
      : { titulo: 'Consumo', valor: `${componente.consumoWatts} W` }

  return (
    <article className="flex flex-col gap-4 rounded-xl border border-borde bg-superficie p-5 transition-colors hover:border-borde-fuerte">
      <FotoComponente componente={componente} />

      <div className="flex items-center justify-between gap-2">
        <p className="text-xs font-semibold tracking-wider text-acento-texto uppercase">
          {etiquetaCategoria(componente.categoria)}
        </p>
        {componente.estaActivo ? (
          <Existencias cantidad={componente.stockDisponible} />
        ) : (
          <EtiquetaRetirado />
        )}
      </div>

      <div>
        <p className="text-sm text-texto-suave">{componente.fabricante}</p>
        <h3 className="mt-0.5 text-lg leading-snug font-semibold text-balance">
          {componente.nombre}
        </h3>
      </div>

      <dl className="grid grid-cols-2 gap-2">
        <div className="rounded-lg bg-superficie-2 px-3 py-2.5">
          <dt className="text-xs text-texto-suave">Socket o interfaz</dt>
          <dd className="mt-0.5 font-mono text-sm font-medium">{componente.socketOInterfaz}</dd>
        </div>
        <div className="rounded-lg bg-superficie-2 px-3 py-2.5">
          <dt className="text-xs text-texto-suave">{energia.titulo}</dt>
          <dd className="mt-0.5 font-mono text-sm font-medium">{energia.valor}</dd>
        </div>
        {componente.tipoMemoria && (
          <div className="col-span-2 rounded-lg bg-superficie-2 px-3 py-2.5">
            <dt className="text-xs text-texto-suave">Memoria</dt>
            <dd className="mt-0.5 font-mono text-sm font-medium">
              {componente.tipoMemoria}
              {componente.ranurasMemoria !== null && ` · ${componente.ranurasMemoria} ranuras`}
            </dd>
          </div>
        )}
      </dl>

      <p className="mt-auto font-mono text-2xl font-semibold">
        {formatearPrecio(componente.precio)}
      </p>
    </article>
  )
}
