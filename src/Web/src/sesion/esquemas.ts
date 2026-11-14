import { z } from 'zod'

const FORMATO_CORREO = /^[^@\s]+@[^@\s]+\.[^@\s]+$/

// RF-CA-14
export const campoCorreo = z
  .string()
  .trim()
  .min(1, 'El correo es obligatorio.')
  .regex(FORMATO_CORREO, 'El formato del correo no es válido.')

// RF-CA-14
export const campoPassword = z
  .string()
  .min(1, 'La contraseña es obligatoria.')
  .min(8, 'La contraseña debe tener al menos 8 caracteres.')
  .regex(/\p{L}/u, 'La contraseña debe contener al menos una letra.')
  .regex(/\d/, 'La contraseña debe contener al menos un número.')

const campoNombre = z
  .string()
  .trim()
  .min(1, 'El nombre es obligatorio.')
  .min(2, 'El nombre debe tener entre 2 y 100 caracteres.')
  .max(100, 'El nombre debe tener entre 2 y 100 caracteres.')

const confirmacionIgual = <T extends { confirmacion: string }>(
  datos: T,
  password: string,
  contexto: z.RefinementCtx,
) => {
  if (datos.confirmacion !== password)
    contexto.addIssue({
      code: 'custom',
      path: ['confirmacion'],
      message: 'Las contraseñas no coinciden.',
    })
}

export const esquemaLogin = z.object({
  correo: campoCorreo,
  password: z.string().min(1, 'La contraseña es obligatoria.'),
})

export const esquemaRegistro = z
  .object({
    nombre: campoNombre,
    correo: campoCorreo,
    password: campoPassword,
    confirmacion: z.string(),
  })
  .superRefine((datos, contexto) => confirmacionIgual(datos, datos.password, contexto))

export const esquemaCorreo = z.object({ correo: campoCorreo })

export const esquemaRestablecer = z
  .object({ nuevaPassword: campoPassword, confirmacion: z.string() })
  .superRefine((datos, contexto) => confirmacionIgual(datos, datos.nuevaPassword, contexto))

export const esquemaCambiarPassword = z
  .object({
    passwordActual: z.string().min(1, 'La contraseña actual es obligatoria.'),
    nuevaPassword: campoPassword,
    confirmacion: z.string(),
  })
  .superRefine((datos, contexto) => confirmacionIgual(datos, datos.nuevaPassword, contexto))

export type DatosLogin = z.infer<typeof esquemaLogin>
export type DatosRegistro = z.infer<typeof esquemaRegistro>
export type DatosCorreo = z.infer<typeof esquemaCorreo>
export type DatosRestablecer = z.infer<typeof esquemaRestablecer>
export type DatosCambiarPassword = z.infer<typeof esquemaCambiarPassword>
