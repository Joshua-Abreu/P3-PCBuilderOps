import type { ReactNode } from 'react'

const pines = (inicio: number, fin: number, paso: number) =>
  Array.from({ length: Math.floor((fin - inicio) / paso) + 1 }, (_, i) => inicio + i * paso)

const relleno = 'fill-acento/10'
const detalle = 'fill-acento/25'

const cpu = (
  <>
    {pines(56, 104, 8).map((x) => (
      <path key={`a${x}`} d={`M${x} 10v8M${x} 82v8`} />
    ))}
    {pines(26, 74, 8).map((y) => (
      <path key={`b${y}`} d={`M36 ${y}h8M116 ${y}h8`} />
    ))}
    <rect x="44" y="18" width="72" height="64" rx="6" className={relleno} />
    <rect x="58" y="32" width="44" height="36" rx="3" className={detalle} />
    <path d="M50 24l6 0-6 6z" className="fill-current" />
  </>
)

const placa = (
  <>
    <rect x="18" y="8" width="124" height="84" rx="5" className={relleno} />
    <rect x="34" y="20" width="30" height="30" rx="3" className={detalle} />
    <rect x="41" y="27" width="16" height="16" rx="2" />
    {pines(80, 104, 8).map((x) => (
      <rect key={x} x={x} y="18" width="4" height="40" rx="1" />
    ))}
    <rect x="30" y="64" width="80" height="6" rx="2" className={detalle} />
    <rect x="30" y="78" width="54" height="5" rx="2" />
    <rect x="118" y="64" width="14" height="16" rx="2" />
    {[24, 136].flatMap((x) =>
      [14, 86].map((y) => <circle key={`${x}-${y}`} cx={x} cy={y} r="2" />),
    )}
  </>
)

const ventilador = (cx: number) => (
  <g key={cx}>
    <circle cx={cx} cy="46" r="20" />
    <circle cx={cx} cy="46" r="5" className="fill-current" />
    {[0, 72, 144, 216, 288].map((angulo) => (
      <path key={angulo} d={`M${cx} 41c4-6 10-9 15-9`} transform={`rotate(${angulo} ${cx} 46)`} />
    ))}
  </g>
)

const grafica = (
  <>
    <rect x="12" y="16" width="136" height="60" rx="8" className={relleno} />
    {ventilador(50)}
    {ventilador(106)}
    <path d="M12 26H4v52" />
    <path d="M40 76v10h70V76" />
    {pines(46, 104, 6).map((x) => (
      <path key={x} d={`M${x} 80v6`} className="opacity-60" />
    ))}
  </>
)

const memoria = (
  <>
    <path d="M10 26h140v40H86v6h-8v-6H10z" className={relleno} />
    {pines(20, 128, 22).map((x) => (
      <rect key={x} x={x} y="34" width="16" height="20" rx="2" className={detalle} />
    ))}
    <path d="M10 66v10h140V66" />
    {pines(16, 144, 8).map((x) =>
      x > 74 && x < 90 ? null : <path key={x} d={`M${x} 70v6`} className="opacity-60" />,
    )}
  </>
)

const disco = (
  <>
    <path d="M14 36h118a6 6 0 0 1 0 28H14z" className={relleno} />
    <path d="M14 36v28M14 40h-6v6h6M14 54h-6v6h6" />
    <rect x="28" y="42" width="22" height="16" rx="2" className={detalle} />
    <rect x="58" y="42" width="22" height="16" rx="2" className={detalle} />
    <rect x="88" y="44" width="14" height="12" rx="2" />
    <circle cx="132" cy="50" r="4" />
    <path d="M30 74h80" className="opacity-60" />
  </>
)

const fuente = (
  <>
    <rect x="22" y="10" width="116" height="80" rx="6" className={relleno} />
    <circle cx="70" cy="50" r="28" />
    <circle cx="70" cy="50" r="18" className="opacity-60" />
    <path d="M42 50h56M70 22v56M50 30l40 40M90 30 50 70" className="opacity-60" />
    <rect x="110" y="22" width="18" height="14" rx="2" className={detalle} />
    <path d="M114 29h10" />
    <rect x="112" y="58" width="14" height="22" rx="3" />
    <path d="M119 62v6" />
  </>
)

export const ILUSTRACION_GENERICA = (
  <>
    <rect x="30" y="14" width="100" height="72" rx="8" className={relleno} />
    <path d="M58 40a22 22 0 1 1 44 0c0 14-22 14-22 26M80 76v2" />
  </>
)

export const ILUSTRACIONES: Record<string, ReactNode> = {
  Cpu: cpu,
  PlacaBase: placa,
  Gpu: grafica,
  Ram: memoria,
  Almacenamiento: disco,
  Fuente: fuente,
}
