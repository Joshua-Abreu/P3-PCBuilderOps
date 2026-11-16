import { useId, type ComponentProps, type ReactNode } from 'react'

export function TarjetaFormulario({
  titulo,
  descripcion,
  children,
  pie,
  lateral,
}: {
  titulo: string
  descripcion?: string
  children: ReactNode
  pie?: ReactNode
  lateral?: ReactNode
}) {
  return (
    <div
      className={`mx-auto flex w-full flex-col overflow-hidden rounded-2xl border border-borde bg-superficie shadow-[0_0_60px_-20px_rgba(34,195,224,0.25)] md:flex-row ${
        lateral ? 'max-w-5xl' : 'max-w-md'
      }`}
    >
      {lateral && (
        <div className="border-b border-borde bg-[#0a1120] p-8 md:w-5/12 md:border-r md:border-b-0 md:p-10">
          {lateral}
        </div>
      )}
      <div className="flex-1 p-6 sm:p-8 md:p-10">
        <h1 className="text-2xl font-bold tracking-tight">{titulo}</h1>
        {descripcion && <p className="mt-2 text-[15px] text-texto-suave">{descripcion}</p>}
        <div className="mt-6 space-y-4">{children}</div>
        {pie && (
          <div className="mt-6 space-y-1.5 border-t border-borde pt-5 text-sm text-texto-suave">
            {pie}
          </div>
        )}
      </div>
    </div>
  )
}

export function Campo({
  etiqueta,
  error,
  ...props
}: ComponentProps<'input'> & { etiqueta: string; error?: string }) {
  const id = useId()
  const idError = `${id}-error`

  return (
    <div>
      <label htmlFor={id} className="block text-sm font-medium">
        {etiqueta}
      </label>
      <input
        id={id}
        aria-invalid={error ? true : undefined}
        aria-describedby={error ? idError : undefined}
        className={`mt-1.5 block h-11 w-full rounded-lg border bg-fondo px-3.5 text-[15px] text-texto outline-none placeholder:text-texto-tenue focus:ring-2 ${
          error
            ? 'border-error focus:ring-error/25'
            : 'border-borde-fuerte focus:border-acento focus:ring-acento/25'
        }`}
        {...props}
      />
      {error && (
        <p id={idError} className="mt-1.5 text-sm text-error">
          {error}
        </p>
      )}
    </div>
  )
}

export const estiloBotonPrimario =
  'inline-flex items-center justify-center gap-2 rounded-lg bg-acento px-4 py-2.5 text-[15px] font-semibold text-sobre-acento transition-colors hover:bg-acento-claro'

export const estiloBotonSecundario =
  'inline-flex items-center justify-center gap-2 rounded-lg border border-borde-fuerte bg-superficie-2 px-4 py-2.5 text-[15px] font-medium text-texto transition-colors hover:border-acento/60'

export const estiloEnlace = 'font-medium text-acento-texto hover:underline'

export function Boton({
  cargando = false,
  variante = 'primario',
  children,
  className = '',
  ...props
}: ComponentProps<'button'> & { cargando?: boolean; variante?: 'primario' | 'secundario' }) {
  const estilo = variante === 'primario' ? estiloBotonPrimario : estiloBotonSecundario

  return (
    <button
      disabled={cargando || props.disabled}
      className={`${estilo} disabled:cursor-not-allowed disabled:opacity-60 ${className}`}
      {...props}
    >
      {cargando && (
        <span className="size-4 animate-spin rounded-full border-2 border-current border-t-transparent" />
      )}
      {children}
    </button>
  )
}
