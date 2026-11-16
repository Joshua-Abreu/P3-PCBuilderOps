import { describe, expect, it } from 'vitest'
import { CATEGORIAS } from './filtros'
import { ILUSTRACIONES } from './ilustraciones'

describe('ILUSTRACIONES', () => {
  it('tiene una ilustración para cada categoría del catálogo', () => {
    // Actuar
    const sinIlustracion = CATEGORIAS.filter(({ valor }) => !(valor in ILUSTRACIONES))

    // Verificar
    expect(sinIlustracion).toEqual([])
  })
})
