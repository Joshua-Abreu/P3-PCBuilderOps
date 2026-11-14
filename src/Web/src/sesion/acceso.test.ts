import { describe, expect, it } from 'vitest'
import type { Usuario } from '../api/auth'
import { cumpleRequisito, describirRol, type Requisito } from './acceso'

const usuario = (rol: Usuario['rol'], permisos: string[] = []): Usuario => ({
  id: '1',
  nombre: 'Prueba',
  correo: 'prueba@itla.edu.do',
  rol,
  permisos,
})

const cliente = usuario('Estandar')
const tecnico = usuario('Estandar', ['OperarTaller'])
const administrador = usuario('Administrador')

// RF-CA-06, RF-GP-05, RF-NEG-06
describe('cumpleRequisito', () => {
  it.each<[string, Usuario | null, Requisito, boolean]>([
    ['visitante', null, 'sesion', false],
    ['visitante', null, 'cliente', false],
    ['cliente', cliente, 'sesion', true],
    ['cliente', cliente, 'cliente', true],
    ['cliente', cliente, 'taller', false],
    ['cliente', cliente, 'administrador', false],
    ['técnico', tecnico, 'cliente', true],
    ['técnico', tecnico, 'taller', true],
    ['técnico', tecnico, 'administrador', false],
    ['Administrador', administrador, 'cliente', false],
    ['Administrador', administrador, 'taller', true],
    ['Administrador', administrador, 'administrador', true],
  ])('%s con requisito %s', (_nombre, quien, requisito, esperado) => {
    // Actuar
    const cumple = cumpleRequisito(quien, requisito)

    // Verificar
    expect(cumple).toBe(esperado)
  })
})

describe('describirRol', () => {
  it('distingue cliente, técnico y Administrador', () => {
    // Actuar y verificar
    expect(describirRol(cliente)).toBe('Cliente')
    expect(describirRol(tecnico)).toBe('Cliente y técnico del taller')
    expect(describirRol(administrador)).toBe('Administrador')
  })
})
