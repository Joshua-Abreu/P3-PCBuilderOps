import { pedir } from './cliente'

export type Rol = 'Estandar' | 'Administrador'

export interface Usuario {
  id: string
  nombre: string
  correo: string
  rol: Rol
  permisos: string[]
}

export interface RespuestaLogin {
  token: string
  expiracionUtc: string
  debeCambiarPassword: boolean
}

export interface RespuestaMensaje {
  mensaje: string
}

// RF-CA-01, RF-CA-15
export const registrar = (datos: { nombre: string; correo: string; password: string }) =>
  pedir<RespuestaMensaje>('/auth/registro', { metodo: 'POST', cuerpo: datos })

// RF-CA-16
export const activar = (token: string) =>
  pedir<RespuestaMensaje>(`/auth/activar?token=${encodeURIComponent(token)}`)

// RF-CA-17
export const reenviarActivacion = (correo: string) =>
  pedir<RespuestaMensaje>('/auth/reenviar-activacion', { metodo: 'POST', cuerpo: { correo } })

// RF-CA-03
export const iniciarSesion = (datos: { correo: string; password: string }) =>
  pedir<RespuestaLogin>('/auth/login', { metodo: 'POST', cuerpo: datos })

// RF-CA-07
export const obtenerUsuarioActual = () => pedir<Usuario>('/auth/yo')

// RF-CA-18
export const cerrarSesion = () => pedir<RespuestaMensaje>('/auth/logout', { metodo: 'POST' })

// RF-CA-09
export const solicitarRecuperacion = (correo: string) =>
  pedir<RespuestaMensaje>('/auth/olvide-password', { metodo: 'POST', cuerpo: { correo } })

// RF-CA-10
export const validarCodigoRecuperacion = (token: string) =>
  pedir<RespuestaMensaje>(`/auth/restablecer-password?token=${encodeURIComponent(token)}`)

// RF-CA-11
export const restablecerPassword = (datos: { token: string; nuevaPassword: string }) =>
  pedir<RespuestaMensaje>('/auth/restablecer-password', { metodo: 'POST', cuerpo: datos })

// RF-CA-22
export const cambiarPassword = (datos: { passwordActual: string; nuevaPassword: string }) =>
  pedir<RespuestaMensaje>('/auth/cambiar-password', { metodo: 'POST', cuerpo: datos })
