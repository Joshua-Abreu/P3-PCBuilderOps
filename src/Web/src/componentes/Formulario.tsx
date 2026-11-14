import { useId, type ComponentProps, type ReactNode } from 'react'

export function TarjetaFormulario({
  titulo,
  descripcion,
  children,
  pie,
}: {
  titulo: string
  descripcion?: string
  children: ReactNode
  pie?: ReactNode
}) {
  return (
    <div className="mx-auto w-full max-w-md">
      <div className="rounded-xl border border-slate-200 bg-white p-6 sm:p-8">
        <h1 className="text-2xl font-semibold tracking-tight">{titulo}</h1>
        {descripcion && <p className="mt-2 text-sm text-slate-600">{descripcion}</p>}
        <div className="mt-6 space-y-4">{children}</div>
      </div>
      {pie && <div className="mt-4 space-y-1 text-center text-sm text-slate-600">{pie}</div>}
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
      <label htmlFor={id} className="block text-sm font-medium text-slate-700">
        {etiqueta}
      </label>
      <input
        id={id}
        aria-invalid={error ? true : undefined}
        aria-describedby={error ? idError : undefined}
        className={`mt-1 block w-full rounded-md border bg-white px-3 py-2 text-sm shadow-xs outline-none focus:ring-2 ${
          error
            ? 'border-red-400 focus:border-red-500 focus:ring-red-100'
            : 'border-slate-300 focus:border-teal-600 focus:ring-teal-100'
        }`}
        {...props}
      />
      {error && (
        <p id={idError} className="mt-1 text-sm text-red-700">
          {error}
        </p>
      )}
    </div>
  )
}

export function Boton({
  cargando = false,
  variante = 'primario',
  children,
  className = '',
  ...props
}: ComponentProps<'button'> & { cargando?: boolean; variante?: 'primario' | 'secundario' }) {
  const estilo =
    variante === 'primario'
      ? 'bg-teal-700 text-white hover:bg-teal-800'
      : 'border border-slate-300 bg-white text-slate-800 hover:bg-slate-100'

  return (
    <button
      disabled={cargando || props.disabled}
      className={`inline-flex items-center justify-center gap-2 rounded-md px-4 py-2 text-sm font-medium transition-colors disabled:cursor-not-allowed disabled:opacity-60 ${estilo} ${className}`}
      {...props}
    >
      {cargando && (
        <span className="size-4 animate-spin rounded-full border-2 border-current border-t-transparent" />
      )}
      {children}
    </button>
  )
}
