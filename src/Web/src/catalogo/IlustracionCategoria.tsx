import { ILUSTRACION_GENERICA, ILUSTRACIONES } from './ilustraciones'

export const MARCO_IMAGEN =
  'flex items-center justify-center overflow-hidden rounded-lg border border-borde bg-superficie-2'

// FN-01
export function IlustracionCategoria({
  categoria,
  className = 'aspect-[16/9]',
}: {
  categoria: string
  className?: string
}) {
  return (
    <div
      className={`${MARCO_IMAGEN} bg-[radial-gradient(rgba(34,195,224,0.08)_1px,transparent_1px)] bg-size-[16px_16px] ${className}`}
    >
      <svg
        viewBox="0 0 160 100"
        className="h-4/5 w-4/5 text-acento-texto"
        fill="none"
        stroke="currentColor"
        strokeWidth="2"
        strokeLinecap="round"
        strokeLinejoin="round"
        aria-hidden="true"
      >
        {ILUSTRACIONES[categoria] ?? ILUSTRACION_GENERICA}
      </svg>
    </div>
  )
}
