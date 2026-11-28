import { describe, expect, it } from 'vitest'
import type { Componente } from '../api/componentes'
import {
  aDatosComponente,
  esquemaComponente,
  problemaDeFoto,
  TAMANO_MAXIMO_FOTO,
  valoresIniciales,
  type ValoresComponente,
} from './esquemaComponente'

const fuente: ValoresComponente = {
  nombre: ' RM850x ',
  categoria: 'Fuente',
  fabricante: 'Corsair',
  socketOInterfaz: 'ATX',
  consumoWatts: '0',
  potenciaWatts: '850',
  precio: '139.99',
  stockDisponible: '10',
  tipoMemoria: 'DDR5',
  ranurasMemoria: '4',
}

const mensajes = (valores: ValoresComponente) => {
  const resultado = esquemaComponente.safeParse(valores)
  return resultado.success
    ? {}
    : Object.fromEntries(resultado.error.issues.map((i) => [i.path.join('.'), i.message]))
}

const archivo = (tipo: string, tamano: number) =>
  new File([new Uint8Array(tamano)], 'foto', { type: tipo })

// RF-NEG-01, RD-07
describe('esquemaComponente', () => {
  it('acepta una fuente completa con un precio de dos decimales', () => {
    expect(mensajes(fuente)).toEqual({})
  })

  it('exige la potencia solo a las fuentes', () => {
    expect(mensajes({ ...fuente, potenciaWatts: '' })).toHaveProperty('potenciaWatts')
    expect(mensajes({ ...fuente, categoria: 'Gpu', potenciaWatts: '' })).toEqual({})
  })

  it('rechaza un precio con tres decimales, cero o el máximo', () => {
    expect(mensajes({ ...fuente, precio: '10.999' })).toHaveProperty('precio')
    expect(mensajes({ ...fuente, precio: '0' })).toHaveProperty('precio')
    expect(mensajes({ ...fuente, precio: '100000' })).toHaveProperty('precio')
    expect(mensajes({ ...fuente, precio: '99999.99' })).toEqual({})
  })

  it('acepta los extremos de consumo y existencias y rechaza lo que se pasa', () => {
    expect(mensajes({ ...fuente, consumoWatts: '2000', stockDisponible: '100000' })).toEqual({})
    expect(mensajes({ ...fuente, consumoWatts: '2001' })).toHaveProperty('consumoWatts')
    expect(mensajes({ ...fuente, stockDisponible: '-1' })).toHaveProperty('stockDisponible')
    expect(mensajes({ ...fuente, stockDisponible: '1.5' })).toHaveProperty('stockDisponible')
  })

  it('valida las ranuras de una placa base solo si se indican', () => {
    const placa = { ...fuente, categoria: 'PlacaBase', potenciaWatts: '' }
    expect(mensajes({ ...placa, ranurasMemoria: '' })).toEqual({})
    expect(mensajes({ ...placa, ranurasMemoria: '17' })).toHaveProperty('ranurasMemoria')
  })

  it('pide los textos obligatorios y la categoría', () => {
    const errores = mensajes({ ...fuente, nombre: '  ', categoria: '', fabricante: '' })
    expect(errores).toMatchObject({
      nombre: 'El nombre es obligatorio.',
      categoria: 'Elige una categoría.',
      fabricante: 'El fabricante es obligatorio.',
    })
  })
})

// RF-NEG-01
describe('aDatosComponente', () => {
  it('convierte los números y quita la memoria de lo que no es placa base', () => {
    expect(aDatosComponente(fuente)).toEqual({
      nombre: 'RM850x',
      categoria: 'Fuente',
      fabricante: 'Corsair',
      socketOInterfaz: 'ATX',
      consumoWatts: 0,
      potenciaWatts: 850,
      precio: 139.99,
      stockDisponible: 10,
      tipoMemoria: null,
      ranurasMemoria: null,
    })
  })

  it('quita la potencia de lo que no es fuente y normaliza la memoria de la placa', () => {
    const datos = aDatosComponente({ ...fuente, categoria: 'PlacaBase', tipoMemoria: ' ddr4 ' })
    expect(datos).toMatchObject({ potenciaWatts: null, tipoMemoria: 'DDR4', ranurasMemoria: 4 })
  })

  it('carga los valores de un componente existente para editarlo', () => {
    const componente: Componente = {
      ...aDatosComponente(fuente),
      id: '1',
      estaActivo: true,
      imagenUrl: null,
    }
    expect(aDatosComponente(valoresIniciales(componente))).toEqual(aDatosComponente(fuente))
  })
})

// RD-07
describe('problemaDeFoto', () => {
  it('acepta JPG, PNG y WebP hasta 2 MB', () => {
    expect(problemaDeFoto(archivo('image/jpeg', 10))).toBeNull()
    expect(problemaDeFoto(archivo('image/webp', TAMANO_MAXIMO_FOTO))).toBeNull()
  })

  it('rechaza otros tipos, archivos vacíos y los que pasan de 2 MB', () => {
    expect(problemaDeFoto(archivo('image/gif', 10))).toBe('La foto debe ser JPG, PNG o WebP.')
    expect(problemaDeFoto(archivo('image/png', 0))).toBe('El archivo está vacío.')
    expect(problemaDeFoto(archivo('image/png', TAMANO_MAXIMO_FOTO + 1))).toBe(
      'La foto no puede pesar más de 2 MB.',
    )
  })
})
