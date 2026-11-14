export function Cargando({ texto = 'Cargando…' }: { texto?: string }) {
  return (
    <div role="status" className="flex items-center gap-3 py-10 text-sm text-slate-500">
      <span className="size-5 animate-spin rounded-full border-2 border-slate-300 border-t-teal-700" />
      {texto}
    </div>
  )
}
