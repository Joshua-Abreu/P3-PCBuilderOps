import { Link } from 'react-router'

export function NoEncontrada() {
  return (
    <div className="mx-auto max-w-md py-16 text-center">
      <p className="text-sm font-medium text-acento-texto">404</p>
      <h1 className="mt-2 text-3xl font-semibold tracking-tight">Página no encontrada</h1>
      <p className="mt-3 text-texto-suave">La dirección que abriste no existe.</p>
      <Link
        to="/"
        className="mt-6 inline-block rounded-lg bg-acento px-4 py-2 text-sm font-semibold text-sobre-acento hover:bg-acento-claro"
      >
        Volver al inicio
      </Link>
    </div>
  )
}
