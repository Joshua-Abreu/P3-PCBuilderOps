import type { Componente } from '../api/componentes'
import { CATEGORIAS, normalizar } from './filtros'

export interface FiltrosAdministracion {
  texto: string
  categoria: string
  mostrarRetirados: boolean
}

const posicionCategoria = (categoria: string) => {
  const posicion = CATEGORIAS.findIndex((c) => c.valor === categoria)
  return posicion === -1 ? CATEGORIAS.length : posicion
}

// RF-NEG-01
export function filtrarAdministracion(
  componentes: readonly Componente[],
  { texto, categoria, mostrarRetirados }: FiltrosAdministracion,
): Componente[] {
  const buscado = normalizar(texto)

  return componentes
    .filter(
      (c) =>
        (mostrarRetirados || c.estaActivo) &&
        (!categoria || c.categoria === categoria) &&
        (!buscado || normalizar(`${c.nombre} ${c.fabricante}`).includes(buscado)),
    )
    .sort(
      (a, b) =>
        posicionCategoria(a.categoria) - posicionCategoria(b.categoria) ||
        a.nombre.localeCompare(b.nombre, 'es'),
    )
}
