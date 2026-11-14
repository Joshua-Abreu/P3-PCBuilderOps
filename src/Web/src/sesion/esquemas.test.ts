import { describe, expect, it } from 'vitest'
import { esquemaCambiarPassword, esquemaLogin, esquemaRegistro } from './esquemas'

const primerError = (resultado: { success: boolean; error?: { issues: { message: string }[] } }) =>
  resultado.error?.issues[0]?.message

const registroValido = {
  nombre: 'Ana Pérez',
  correo: 'ana@itla.edu.do',
  password: 'Clave12345',
  confirmacion: 'Clave12345',
}

// RF-CA-14
describe('esquemaRegistro', () => {
  it('acepta datos válidos y recorta espacios del correo', () => {
    // Actuar
    const resultado = esquemaRegistro.safeParse({ ...registroValido, correo: '  ana@itla.edu.do ' })

    // Verificar
    expect(resultado.success).toBe(true)
    expect(resultado.data?.correo).toBe('ana@itla.edu.do')
  })

  it.each([
    ['corta', 'Ab1', 'La contraseña debe tener al menos 8 caracteres.'],
    ['sin números', 'SoloLetras', 'La contraseña debe contener al menos un número.'],
    ['sin letras', '12345678', 'La contraseña debe contener al menos una letra.'],
  ])('rechaza una contraseña %s con el mismo mensaje que la API', (_caso, password, mensaje) => {
    // Actuar
    const resultado = esquemaRegistro.safeParse({
      ...registroValido,
      password,
      confirmacion: password,
    })

    // Verificar
    expect(primerError(resultado)).toBe(mensaje)
  })

  it.each([
    ['vacío', '', 'El correo es obligatorio.'],
    ['sin dominio', 'ana@', 'El formato del correo no es válido.'],
  ])('rechaza un correo %s', (_caso, correo, mensaje) => {
    // Actuar
    const resultado = esquemaRegistro.safeParse({ ...registroValido, correo })

    // Verificar
    expect(primerError(resultado)).toBe(mensaje)
  })

  it('rechaza un nombre de un carácter', () => {
    // Actuar
    const resultado = esquemaRegistro.safeParse({ ...registroValido, nombre: 'A' })

    // Verificar
    expect(primerError(resultado)).toBe('El nombre debe tener entre 2 y 100 caracteres.')
  })

  it('rechaza una confirmación distinta en el campo confirmacion', () => {
    // Actuar
    const resultado = esquemaRegistro.safeParse({ ...registroValido, confirmacion: 'Otra12345' })

    // Verificar
    expect(resultado.error?.issues[0]?.path).toEqual(['confirmacion'])
    expect(primerError(resultado)).toBe('Las contraseñas no coinciden.')
  })
})

describe('esquemaLogin', () => {
  it('no aplica la política de contraseña al iniciar sesión', () => {
    // Actuar
    const resultado = esquemaLogin.safeParse({ correo: 'ana@itla.edu.do', password: 'x' })

    // Verificar
    expect(resultado.success).toBe(true)
  })
})

// RF-CA-22
describe('esquemaCambiarPassword', () => {
  it('exige la contraseña actual', () => {
    // Actuar
    const resultado = esquemaCambiarPassword.safeParse({
      passwordActual: '',
      nuevaPassword: 'Nueva12345',
      confirmacion: 'Nueva12345',
    })

    // Verificar
    expect(primerError(resultado)).toBe('La contraseña actual es obligatoria.')
  })
})
