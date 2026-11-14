import { Link } from 'react-router'
import { esAdministrador } from '../sesion/acceso'
import { useSesion } from '../sesion/contexto'

const ventajas = [
  {
    titulo: 'Compatibilidad verificada',
    texto:
      'Antes de cotizar se comprueba que la CPU y la placa compartan socket y que la fuente cubra el consumo.',
  },
  {
    titulo: 'Ensamblada por técnicos',
    texto: 'Solo los técnicos autorizados por el taller arman y prueban tu equipo.',
  },
  {
    titulo: 'Probada antes de entregar',
    texto:
      'Cada equipo pasa pruebas de rendimiento y temperatura. Si la última no aprueba, no se entrega.',
  },
]

const etapas = [
  { estado: 'Cotizada', texto: 'Eliges tus piezas y recibes el costo.' },
  { estado: 'En armado', texto: 'Un técnico reserva las piezas y ensambla.' },
  { estado: 'En pruebas', texto: 'Se mide rendimiento y temperaturas.' },
  { estado: 'Entregada', texto: 'Tu equipo aprobado está listo.' },
]

export function Inicio() {
  const { usuario } = useSesion()
  const principal =
    usuario && esAdministrador(usuario)
      ? { ruta: '/taller', texto: 'Ir al taller' }
      : { ruta: '/armar', texto: 'Armar mi PC' }

  return (
    <div className="space-y-16">
      <section className="max-w-3xl">
        <p className="text-sm font-medium text-teal-700">Taller de computadoras a medida</p>
        <h1 className="mt-2 text-4xl font-semibold tracking-tight text-balance sm:text-5xl">
          Arma tu PC a medida, sin sorpresas
        </h1>
        <p className="mt-4 text-lg text-slate-600">
          Elige cada componente, recibe tu cotización al instante y sigue tu equipo desde el armado
          hasta la entrega.
        </p>
        <div className="mt-8 flex flex-wrap gap-3">
          <Link
            to={principal.ruta}
            className="rounded-md bg-teal-700 px-5 py-2.5 font-medium text-white hover:bg-teal-800"
          >
            {principal.texto}
          </Link>
          <Link
            to="/catalogo"
            className="rounded-md border border-slate-300 bg-white px-5 py-2.5 font-medium text-slate-800 hover:bg-slate-100"
          >
            Ver catálogo
          </Link>
        </div>
      </section>

      <section aria-labelledby="ventajas" className="grid gap-4 sm:grid-cols-3">
        <h2 id="ventajas" className="sr-only">
          Ventajas
        </h2>
        {ventajas.map(({ titulo, texto }) => (
          <article key={titulo} className="rounded-xl border border-slate-200 bg-white p-5">
            <h3 className="font-semibold">{titulo}</h3>
            <p className="mt-2 text-sm text-slate-600">{texto}</p>
          </article>
        ))}
      </section>

      <section aria-labelledby="como-funciona">
        <h2 id="como-funciona" className="text-2xl font-semibold tracking-tight">
          Cómo funciona
        </h2>
        <ol className="mt-6 grid gap-4 sm:grid-cols-4">
          {etapas.map(({ estado, texto }, indice) => (
            <li key={estado} className="rounded-xl border border-slate-200 bg-white p-5">
              <span className="flex size-7 items-center justify-center rounded-full bg-teal-700 text-sm font-semibold text-white">
                {indice + 1}
              </span>
              <p className="mt-3 font-semibold">{estado}</p>
              <p className="mt-1 text-sm text-slate-600">{texto}</p>
            </li>
          ))}
        </ol>
      </section>
    </div>
  )
}
