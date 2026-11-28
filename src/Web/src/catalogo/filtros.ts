import type { Componente } from '../api/componentes'

export const CATEGORIAS = [
  { valor: 'Cpu', etiqueta: 'Procesadores' },
  { valor: 'PlacaBase', etiqueta: 'Placas base' },
  { valor: 'Gpu', etiqueta: 'Tarjetas gráficas' },
  { valor: 'Ram', etiqueta: 'Memoria RAM' },
  { valor: 'Almacenamiento', etiqueta: 'Almacenamiento' },
  { valor: 'Fuente', etiqueta: 'Fuentes de poder' },
] as const

export const ORDENES = [
  { valor: 'categoria', etiqueta: 'Categoría' },
  { valor: 'precio-asc', etiqueta: 'Precio: menor a mayor' },
  { valor: 'precio-desc', etiqueta: 'Precio: mayor a menor' },
] as const

export type Orden = (typeof ORDENES)[number]['valor']

export interface FiltrosCatalogo {
  categoria: string | null
  fabricante: string | null
  socket: string | null
  texto: string
  soloDisponibles: boolean
  orden: Orden
}

const esOrden = (valor: string | null): valor is Orden => ORDENES.some((o) => o.valor === valor)

export const etiquetaCategoria = (categoria: string) =>
  CATEGORIAS.find((c) => c.valor === categoria)?.etiqueta ?? categoria

export const normalizar = (texto: string) =>
  texto
    .normalize('NFD')
    .replace(/\p{Diacritic}/gu, '')
    .toLowerCase()
    .trim()

export function leerFiltros(parametros: URLSearchParams): FiltrosCatalogo {
  const orden = parametros.get('orden')
  return {
    categoria: parametros.get('categoria'),
    fabricante: parametros.get('fabricante'),
    socket: parametros.get('socket'),
    texto: parametros.get('buscar') ?? '',
    soloDisponibles: parametros.get('disponibles') === '1',
    orden: esOrden(orden) ? orden : 'categoria',
  }
}

export function escribirFiltros(filtros: FiltrosCatalogo): URLSearchParams {
  const parametros = new URLSearchParams()
  if (filtros.categoria) parametros.set('categoria', filtros.categoria)
  if (filtros.fabricante) parametros.set('fabricante', filtros.fabricante)
  if (filtros.socket) parametros.set('socket', filtros.socket)
  if (filtros.texto.trim()) parametros.set('buscar', filtros.texto)
  if (filtros.soloDisponibles) parametros.set('disponibles', '1')
  if (filtros.orden !== 'categoria') parametros.set('orden', filtros.orden)
  return parametros
}

// FN-01
export function filtrarComponentes(
  componentes: readonly Componente[],
  filtros: FiltrosCatalogo,
): Componente[] {
  const texto = normalizar(filtros.texto)

  const visibles = componentes.filter(
    (c) =>
      (!filtros.categoria || c.categoria === filtros.categoria) &&
      (!filtros.fabricante || c.fabricante === filtros.fabricante) &&
      (!filtros.socket || c.socketOInterfaz === filtros.socket) &&
      (!filtros.soloDisponibles || c.stockDisponible > 0) &&
      (!texto || normalizar(`${c.nombre} ${c.fabricante}`).includes(texto)),
  )

  if (filtros.orden === 'precio-asc') return visibles.sort((a, b) => a.precio - b.precio)
  if (filtros.orden === 'precio-desc') return visibles.sort((a, b) => b.precio - a.precio)
  return visibles
}

export function opcionesDeFiltro(componentes: readonly Componente[], categoria: string | null) {
  const deLaCategoria = componentes.filter((c) => !categoria || c.categoria === categoria)
  const unicos = (valores: string[]) =>
    [...new Set(valores)].sort((a, b) => a.localeCompare(b, 'es'))

  return {
    fabricantes: unicos(deLaCategoria.map((c) => c.fabricante)),
    sockets: unicos(deLaCategoria.map((c) => c.socketOInterfaz)),
  }
}
