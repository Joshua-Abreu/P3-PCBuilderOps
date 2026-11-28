import { describe, expect, it } from 'vitest'
import { rutaDeImagen, type Componente } from '../api/componentes'
import { filtrarAdministracion } from './administracion'

const componente = (
  datos: Partial<Componente> & Pick<Componente, 'id' | 'nombre' | 'categoria'>,
): Componente => ({
  fabricante: 'AMD',
  socketOInterfaz: 'AM5',
  consumoWatts: 100,
  potenciaWatts: null,
  precio: 100,
  stockDisponible: 5,
  tipoMemoria: null,
  ranurasMemoria: null,
  estaActivo: true,
  imagenUrl: null,
  ...datos,
})

const catalogo: Componente[] = [
  componente({ id: '1', nombre: 'RM850x', categoria: 'Fuente', fabricante: 'Corsair' }),
  componente({ id: '2', nombre: 'Ryzen 7 7700X', categoria: 'Cpu' }),
  componente({ id: '3', nombre: 'Ryzen 5 5600', categoria: 'Cpu', estaActivo: false }),
  componente({ id: '4', nombre: 'GeForce RTX 4060', categoria: 'Gpu', fabricante: 'MSI' }),
]

const ids = (lista: Componente[]) => lista.map((c) => c.id)

// RF-NEG-01
describe('filtrarAdministracion', () => {
  it('ordena por categoría del catálogo y luego por nombre, con los retirados incluidos', () => {
    const lista = filtrarAdministracion(catalogo, {
      texto: '',
      categoria: '',
      mostrarRetirados: true,
    })
    expect(ids(lista)).toEqual(['3', '2', '4', '1'])
  })

  it('oculta los retirados cuando se pide', () => {
    const lista = filtrarAdministracion(catalogo, {
      texto: '',
      categoria: '',
      mostrarRetirados: false,
    })
    expect(ids(lista)).not.toContain('3')
  })

  it('filtra por categoría y por texto sin importar tildes ni mayúsculas', () => {
    expect(
      ids(filtrarAdministracion(catalogo, { texto: '', categoria: 'Cpu', mostrarRetirados: true })),
    ).toEqual(['3', '2'])
    expect(
      ids(
        filtrarAdministracion(catalogo, {
          texto: 'córsair',
          categoria: '',
          mostrarRetirados: true,
        }),
      ),
    ).toEqual(['1'])
  })
})

// RF-NEG-06
describe('rutaDeImagen', () => {
  it('quita el /api que trae la URL de la API, porque el cliente lo agrega', () => {
    expect(rutaDeImagen('/api/componentes/abc/imagen?v=foto.png')).toBe(
      '/componentes/abc/imagen?v=foto.png',
    )
    expect(rutaDeImagen('/componentes/abc/imagen')).toBe('/componentes/abc/imagen')
  })
})
