import { describe, expect, it } from 'vitest'
import type { Componente } from '../api/componentes'
import {
  escribirFiltros,
  filtrarComponentes,
  leerFiltros,
  opcionesDeFiltro,
  type FiltrosCatalogo,
} from './filtros'

const componente = (
  datos: Partial<Componente> & Pick<Componente, 'id' | 'nombre'>,
): Componente => ({
  categoria: 'Cpu',
  fabricante: 'AMD',
  socketOInterfaz: 'AM5',
  consumoWatts: 100,
  potenciaWatts: null,
  precio: 100,
  stockDisponible: 5,
  tipoMemoria: null,
  ranurasMemoria: null,
  ...datos,
})

const catalogo: Componente[] = [
  componente({ id: '1', nombre: 'Ryzen 7 7700X', precio: 299 }),
  componente({
    id: '2',
    nombre: 'Core i5-13600K',
    fabricante: 'Intel',
    socketOInterfaz: 'LGA1700',
    precio: 279,
  }),
  componente({
    id: '3',
    nombre: 'TUF Gaming B650-Plus',
    categoria: 'PlacaBase',
    fabricante: 'ASUS',
    precio: 189,
  }),
  componente({
    id: '4',
    nombre: 'GeForce RTX 4090 TUF',
    categoria: 'Gpu',
    fabricante: 'ASUS',
    socketOInterfaz: 'PCIe 4.0',
    precio: 1799,
    stockDisponible: 0,
  }),
  componente({
    id: '5',
    nombre: 'Fuente Económica',
    categoria: 'Fuente',
    fabricante: 'EVGA',
    socketOInterfaz: 'ATX',
    precio: 49,
  }),
]

const sinFiltros: FiltrosCatalogo = {
  categoria: null,
  fabricante: null,
  socket: null,
  texto: '',
  soloDisponibles: false,
  orden: 'categoria',
}

const ids = (lista: Componente[]) => lista.map((c) => c.id)

// FN-01
describe('filtrarComponentes', () => {
  it('sin filtros devuelve todo en el orden de la API', () => {
    // Actuar
    const resultado = filtrarComponentes(catalogo, sinFiltros)

    // Verificar
    expect(ids(resultado)).toEqual(['1', '2', '3', '4', '5'])
  })

  it('combina categoría, fabricante y socket', () => {
    // Actuar
    const resultado = filtrarComponentes(catalogo, {
      ...sinFiltros,
      categoria: 'Cpu',
      fabricante: 'Intel',
      socket: 'LGA1700',
    })

    // Verificar
    expect(ids(resultado)).toEqual(['2'])
  })

  it('busca por nombre o fabricante sin importar mayúsculas ni tildes', () => {
    // Actuar y verificar
    expect(ids(filtrarComponentes(catalogo, { ...sinFiltros, texto: 'ECONOMICA' }))).toEqual(['5'])
    expect(ids(filtrarComponentes(catalogo, { ...sinFiltros, texto: ' asus ' }))).toEqual([
      '3',
      '4',
    ])
  })

  it('con solo disponibles oculta los agotados', () => {
    // Actuar
    const resultado = filtrarComponentes(catalogo, { ...sinFiltros, soloDisponibles: true })

    // Verificar
    expect(ids(resultado)).not.toContain('4')
  })

  it('ordena por precio en ambos sentidos sin modificar la lista original', () => {
    // Actuar
    const ascendente = filtrarComponentes(catalogo, { ...sinFiltros, orden: 'precio-asc' })
    const descendente = filtrarComponentes(catalogo, { ...sinFiltros, orden: 'precio-desc' })

    // Verificar
    expect(ids(ascendente)).toEqual(['5', '3', '2', '1', '4'])
    expect(ids(descendente)).toEqual(['4', '1', '2', '3', '5'])
    expect(ids(catalogo)).toEqual(['1', '2', '3', '4', '5'])
  })
})

describe('opcionesDeFiltro', () => {
  it('lista fabricantes y sockets únicos y ordenados de la categoría elegida', () => {
    // Actuar
    const deCpu = opcionesDeFiltro(catalogo, 'Cpu')
    const deTodo = opcionesDeFiltro(catalogo, null)

    // Verificar
    expect(deCpu).toEqual({ fabricantes: ['AMD', 'Intel'], sockets: ['AM5', 'LGA1700'] })
    expect(deTodo.fabricantes).toEqual(['AMD', 'ASUS', 'EVGA', 'Intel'])
  })
})

describe('filtros en la URL', () => {
  it('ida y vuelta conserva los filtros y omite los valores por defecto', () => {
    // Preparar
    const filtros: FiltrosCatalogo = {
      categoria: 'Gpu',
      fabricante: 'ASUS',
      socket: null,
      texto: 'rtx',
      soloDisponibles: true,
      orden: 'precio-desc',
    }

    // Actuar
    const parametros = escribirFiltros(filtros)

    // Verificar
    expect(parametros.toString()).toBe(
      'categoria=Gpu&fabricante=ASUS&buscar=rtx&disponibles=1&orden=precio-desc',
    )
    expect(leerFiltros(parametros)).toEqual(filtros)
    expect(escribirFiltros(sinFiltros).toString()).toBe('')
  })

  it('un orden desconocido en la URL vuelve al orden por categoría', () => {
    // Actuar
    const filtros = leerFiltros(new URLSearchParams('orden=inventado'))

    // Verificar
    expect(filtros.orden).toBe('categoria')
  })
})
