import { z } from 'zod'
import type { Componente, DatosComponente } from '../api/componentes'
import { CATEGORIAS } from './filtros'

export const LIMITES = {
  nombre: 150,
  fabricante: 100,
  socketOInterfaz: 50,
  tipoMemoria: 20,
  consumoWatts: 2000,
  potenciaWatts: 3000,
  ranurasMemoria: 16,
  precio: 100_000,
  stockDisponible: 100_000,
} as const

export const TAMANO_MAXIMO_FOTO = 2 * 1024 * 1024
export const TIPOS_FOTO = ['image/jpeg', 'image/png', 'image/webp'] as const

const VALORES_CATEGORIA = CATEGORIAS.map((c) => c.valor) as [string, ...string[]]

const FORMATO_PRECIO = /^\d+(\.\d{1,2})?$/

const leerNumero = (texto: string) => (texto.trim() === '' ? Number.NaN : Number(texto.trim()))

const texto = (campo: string, maximo: number) =>
  z
    .string()
    .trim()
    .min(1, `${campo} es obligatorio.`)
    .max(maximo, `${campo} no puede superar ${maximo} caracteres.`)

const entero = (mensaje: string, minimo: number, maximo: number) =>
  z.string().refine((valor) => {
    const numero = leerNumero(valor)
    return Number.isInteger(numero) && numero >= minimo && numero <= maximo
  }, mensaje)

// RF-NEG-01, RD-07
export const esquemaComponente = z
  .object({
    nombre: texto('El nombre', LIMITES.nombre),
    categoria: z.enum(VALORES_CATEGORIA, 'Elige una categoría.'),
    fabricante: texto('El fabricante', LIMITES.fabricante),
    socketOInterfaz: texto('El socket o interfaz', LIMITES.socketOInterfaz),
    consumoWatts: entero(
      `El consumo debe estar entre 0 y ${LIMITES.consumoWatts} W.`,
      0,
      LIMITES.consumoWatts,
    ),
    potenciaWatts: z.string(),
    precio: z.string().refine((valor) => {
      const numero = leerNumero(valor)
      return FORMATO_PRECIO.test(valor.trim()) && numero > 0 && numero < LIMITES.precio
    }, `El precio debe ser mayor que 0 y menor que ${LIMITES.precio}, con dos decimales como máximo.`),
    stockDisponible: entero(
      `Las existencias deben estar entre 0 y ${LIMITES.stockDisponible}.`,
      0,
      LIMITES.stockDisponible,
    ),
    tipoMemoria: z
      .string()
      .trim()
      .max(LIMITES.tipoMemoria, 'El tipo de memoria es demasiado largo.'),
    ranurasMemoria: z.string(),
  })
  .superRefine((datos, contexto) => {
    if (datos.categoria === 'Fuente') {
      const potencia = leerNumero(datos.potenciaWatts)
      if (!Number.isInteger(potencia) || potencia < 1 || potencia > LIMITES.potenciaWatts)
        contexto.addIssue({
          code: 'custom',
          path: ['potenciaWatts'],
          message: `La potencia de la fuente es obligatoria y debe estar entre 1 y ${LIMITES.potenciaWatts} W.`,
        })
    }

    if (datos.categoria === 'PlacaBase' && datos.ranurasMemoria.trim() !== '') {
      const ranuras = leerNumero(datos.ranurasMemoria)
      if (!Number.isInteger(ranuras) || ranuras < 1 || ranuras > LIMITES.ranurasMemoria)
        contexto.addIssue({
          code: 'custom',
          path: ['ranurasMemoria'],
          message: `Las ranuras de memoria deben estar entre 1 y ${LIMITES.ranurasMemoria}.`,
        })
    }
  })

export type ValoresComponente = z.input<typeof esquemaComponente>

export const valoresIniciales = (componente?: Componente): ValoresComponente => ({
  nombre: componente?.nombre ?? '',
  categoria: componente?.categoria ?? '',
  fabricante: componente?.fabricante ?? '',
  socketOInterfaz: componente?.socketOInterfaz ?? '',
  consumoWatts: componente ? String(componente.consumoWatts) : '',
  potenciaWatts: componente?.potenciaWatts?.toString() ?? '',
  precio: componente ? String(componente.precio) : '',
  stockDisponible: componente ? String(componente.stockDisponible) : '',
  tipoMemoria: componente?.tipoMemoria ?? '',
  ranurasMemoria: componente?.ranurasMemoria?.toString() ?? '',
})

// RF-NEG-01
export function aDatosComponente(valores: ValoresComponente): DatosComponente {
  const esFuente = valores.categoria === 'Fuente'
  const esPlaca = valores.categoria === 'PlacaBase'
  const tipoMemoria = valores.tipoMemoria.trim().toUpperCase()

  return {
    nombre: valores.nombre.trim(),
    categoria: valores.categoria,
    fabricante: valores.fabricante.trim(),
    socketOInterfaz: valores.socketOInterfaz.trim(),
    consumoWatts: leerNumero(valores.consumoWatts),
    potenciaWatts: esFuente ? leerNumero(valores.potenciaWatts) : null,
    precio: leerNumero(valores.precio),
    stockDisponible: leerNumero(valores.stockDisponible),
    tipoMemoria: esPlaca && tipoMemoria !== '' ? tipoMemoria : null,
    ranurasMemoria:
      esPlaca && valores.ranurasMemoria.trim() !== '' ? leerNumero(valores.ranurasMemoria) : null,
  }
}

// RD-07
export function problemaDeFoto(archivo: File): string | null {
  if (!(TIPOS_FOTO as readonly string[]).includes(archivo.type))
    return 'La foto debe ser JPG, PNG o WebP.'
  if (archivo.size === 0) return 'El archivo está vacío.'
  if (archivo.size > TAMANO_MAXIMO_FOTO) return 'La foto no puede pesar más de 2 MB.'
  return null
}
