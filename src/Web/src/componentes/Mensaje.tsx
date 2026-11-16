import type { ReactNode } from 'react'

type Tipo = 'error' | 'info' | 'exito'

const estilos: Record<Tipo, string> = {
  error: 'border-error/40 bg-error/10 text-[#ffc9c4]',
  info: 'border-acento/35 bg-acento/10 text-[#c4effa]',
  exito: 'border-exito/40 bg-exito/10 text-[#b5f0d7]',
}

// RD-07
export function Mensaje({ tipo = 'info', children }: { tipo?: Tipo; children: ReactNode }) {
  return (
    <div
      role={tipo === 'error' ? 'alert' : 'status'}
      className={`rounded-lg border px-4 py-3 text-sm leading-relaxed ${estilos[tipo]}`}
    >
      {children}
    </div>
  )
}
