import type { ReactNode } from 'react'

type Tipo = 'error' | 'info' | 'exito'

const estilos: Record<Tipo, string> = {
  error: 'border-red-200 bg-red-50 text-red-800',
  info: 'border-sky-200 bg-sky-50 text-sky-800',
  exito: 'border-emerald-200 bg-emerald-50 text-emerald-800',
}

// RD-07
export function Mensaje({ tipo = 'info', children }: { tipo?: Tipo; children: ReactNode }) {
  return (
    <div
      role={tipo === 'error' ? 'alert' : 'status'}
      className={`rounded-lg border px-4 py-3 text-sm ${estilos[tipo]}`}
    >
      {children}
    </div>
  )
}
