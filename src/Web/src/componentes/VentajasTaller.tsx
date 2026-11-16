const ventajas = [
  {
    titulo: 'Compatibilidad al momento.',
    texto: 'Socket de CPU y placa, y consumo frente a la fuente.',
    color: 'bg-acento/12 text-acento-texto',
    icono: <path d="M13 2 4 14h7l-1 8 9-12h-7z" />,
  },
  {
    titulo: 'Probada antes de entregar.',
    texto: 'Cada PC pasa pruebas de rendimiento en el taller.',
    color: 'bg-exito/12 text-exito',
    icono: <path d="m5 12 4 4 10-10" />,
  },
  {
    titulo: 'Sigue tu orden.',
    texto: 'Cotizada, en armado, en pruebas y entregada.',
    color: 'bg-aviso/12 text-aviso',
    icono: (
      <>
        <circle cx="12" cy="12" r="9" />
        <path d="M12 7v5l3 2" />
      </>
    ),
  },
]

export function VentajasTaller({ titulo, texto }: { titulo: string; texto: string }) {
  return (
    <div className="space-y-6">
      <h2 className="text-3xl leading-tight font-bold tracking-tight">{titulo}</h2>
      <p className="leading-relaxed text-texto-claro">{texto}</p>
      <ul className="space-y-4">
        {ventajas.map(({ titulo, texto, color, icono }) => (
          <li key={titulo} className="flex items-start gap-3">
            <span
              className={`flex size-8 shrink-0 items-center justify-center rounded-lg ${color}`}
            >
              <svg
                viewBox="0 0 24 24"
                className="size-4"
                fill="none"
                stroke="currentColor"
                strokeWidth="2"
                strokeLinecap="round"
                strokeLinejoin="round"
                aria-hidden="true"
              >
                {icono}
              </svg>
            </span>
            <span className="text-[15px] leading-normal">
              <strong className="font-semibold">{titulo}</strong>{' '}
              <span className="text-texto-suave">{texto}</span>
            </span>
          </li>
        ))}
      </ul>
    </div>
  )
}
