import { Link } from 'react-router'

// RF-CA-06, RD-06
export function SinAcceso() {
  return (
    <div className="mx-auto max-w-md py-16 text-center">
      <p className="text-sm font-medium text-acento-texto">403</p>
      <h1 className="mt-2 text-3xl font-semibold tracking-tight">No tienes acceso</h1>
      <p className="mt-3 text-texto-suave">Esta sección es para otro tipo de usuario.</p>
      <Link
        to="/"
        className="mt-6 inline-block rounded-lg bg-acento px-4 py-2 text-sm font-semibold text-sobre-acento hover:bg-acento-claro"
      >
        Volver al inicio
      </Link>
    </div>
  )
}
