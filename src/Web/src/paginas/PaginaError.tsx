import { Link } from 'react-router'

// RD-08
export function PaginaError() {
  return (
    <div className="flex min-h-screen items-center justify-center bg-fondo px-4">
      <div className="max-w-md text-center">
        <h1 className="text-3xl font-semibold tracking-tight">Algo salió mal</h1>
        <p className="mt-3 text-texto-suave">
          Ocurrió un error inesperado al mostrar esta página. Intenta de nuevo.
        </p>
        <Link
          to="/"
          className="mt-6 inline-block rounded-lg bg-acento px-4 py-2 text-sm font-semibold text-sobre-acento hover:bg-acento-claro"
        >
          Volver al inicio
        </Link>
      </div>
    </div>
  )
}
