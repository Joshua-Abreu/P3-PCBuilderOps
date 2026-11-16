export function Cargando({ texto = 'Cargando…' }: { texto?: string }) {
  return (
    <div role="status" className="flex items-center gap-3 py-10 text-sm text-texto-suave">
      <span className="size-5 animate-spin rounded-full border-2 border-borde-fuerte border-t-acento" />
      {texto}
    </div>
  )
}
