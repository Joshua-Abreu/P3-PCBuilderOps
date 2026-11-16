import { Link } from 'react-router'

export function Logo({ tamano = 'size-9' }: { tamano?: string }) {
  return (
    <svg viewBox="0 0 32 32" className={tamano} aria-hidden="true">
      <rect width="32" height="32" rx="8" className="fill-acento" />
      <g className="stroke-sobre-acento" fill="none" strokeWidth="2" strokeLinecap="round">
        <rect x="10" y="10" width="12" height="12" rx="2" />
        <path d="M13 5v5M19 5v5M13 22v5M19 22v5M5 13h5M5 19h5M22 13h5M22 19h5" />
      </g>
    </svg>
  )
}

export function Marca() {
  return (
    <Link to="/" className="flex items-center gap-2.5 rounded-lg">
      <Logo />
      <span className="text-lg font-bold tracking-tight">
        PC Builder <span className="text-acento-texto">Ops</span>
      </span>
    </Link>
  )
}
