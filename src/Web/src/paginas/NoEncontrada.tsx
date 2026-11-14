import { Link } from 'react-router'

export function NoEncontrada() {
  return (
    <div className="mx-auto max-w-md py-16 text-center">
      <p className="text-sm font-medium text-teal-700">404</p>
      <h1 className="mt-2 text-3xl font-semibold tracking-tight">Página no encontrada</h1>
      <p className="mt-3 text-slate-600">La dirección que abriste no existe.</p>
      <Link
        to="/"
        className="mt-6 inline-block rounded-md bg-teal-700 px-4 py-2 text-sm font-medium text-white hover:bg-teal-800"
      >
        Volver al inicio
      </Link>
    </div>
  )
}
