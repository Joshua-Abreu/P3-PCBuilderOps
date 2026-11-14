// RD-07
export class ErrorApi extends Error {
  readonly estado: number

  constructor(estado: number, mensaje: string) {
    super(mensaje)
    this.name = 'ErrorApi'
    this.estado = estado
  }
}

export const MENSAJE_SIN_CONEXION =
  'No se pudo conectar con el servidor. Revisa que la API esté encendida.'

export const mensajeDeError = (error: unknown): string =>
  error instanceof ErrorApi ? error.message : 'Ocurrió un error inesperado. Intenta de nuevo.'
