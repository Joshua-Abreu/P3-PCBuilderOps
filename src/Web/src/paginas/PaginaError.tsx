import { Link } from 'react-router'

// RD-08
export function PaginaError() {
  return (
    <div className="flex min-h-screen items-center justify-center bg-slate-50 px-4">
      <div className="max-w-md text-center">
        <h1 className="text-3xl font-semibold tracking-tight">Algo salió mal</h1>
        <p className="mt-3 text-slate-600">
          Ocurrió un error inesperado al mostrar esta página. Intenta de nuevo.
        </p>
        <Link
          to="/"
          className="mt-6 inline-block rounded-md bg-teal-700 px-4 py-2 text-sm font-medium text-white hover:bg-teal-800"
        >
          Volver al inicio
        </Link>
      </div>
    </div>
  )
}
