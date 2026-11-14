import { Link } from 'react-router'
import { Mensaje } from '../componentes/Mensaje'

export function Proximamente({ titulo, fase }: { titulo: string; fase: number }) {
  return (
    <div className="max-w-xl space-y-4">
      <h1 className="text-3xl font-semibold tracking-tight">{titulo}</h1>
      <Mensaje>Esta pantalla llega en la fase {fase} del frontend.</Mensaje>
      <Link to="/" className="inline-block text-sm font-medium text-teal-700 hover:underline">
        Volver al inicio
      </Link>
    </div>
  )
}
