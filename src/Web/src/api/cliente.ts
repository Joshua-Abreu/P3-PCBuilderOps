import { cerrarSesionLocal, leerToken } from '../sesion/almacenToken'
import { ErrorApi, MENSAJE_SIN_CONEXION } from './errores'

type Metodo = 'GET' | 'POST' | 'PUT' | 'PATCH' | 'DELETE'

const ESTADOS_SIN_SERVIDOR = [502, 503, 504]

export interface OpcionesPeticion {
  metodo?: Metodo
  cuerpo?: unknown
  senal?: AbortSignal
}

// RD-07, RD-08
export async function pedir<T>(
  ruta: string,
  { metodo = 'GET', cuerpo, senal }: OpcionesPeticion = {},
): Promise<T> {
  const esFormulario = cuerpo instanceof FormData
  const cabeceras = new Headers({ Accept: 'application/json' })

  if (cuerpo !== undefined && !esFormulario) cabeceras.set('Content-Type', 'application/json')

  const respuesta = await enviar(ruta, {
    method: metodo,
    headers: cabeceras,
    body: cuerpo === undefined ? undefined : esFormulario ? cuerpo : JSON.stringify(cuerpo),
    signal: senal,
  })

  return (await leerCuerpo(respuesta)) as T
}

// RF-NEG-06
export async function pedirArchivo(ruta: string, senal?: AbortSignal): Promise<Blob> {
  const respuesta = await enviar(ruta, { method: 'GET', headers: new Headers(), signal: senal })
  return respuesta.blob()
}

async function enviar(
  ruta: string,
  opciones: RequestInit & { headers: Headers },
): Promise<Response> {
  const token = leerToken()
  if (token) opciones.headers.set('Authorization', `Bearer ${token}`)

  let respuesta: Response
  try {
    respuesta = await fetch(`/api${ruta}`, opciones)
  } catch (error) {
    if (error instanceof DOMException && error.name === 'AbortError') throw error
    throw new ErrorApi(0, MENSAJE_SIN_CONEXION)
  }

  // RF-CA-12
  if (respuesta.status === 401 && token) cerrarSesionLocal()

  if (!respuesta.ok)
    throw new ErrorApi(
      respuesta.status,
      extraerMensaje(await leerCuerpo(respuesta), respuesta.status),
    )

  return respuesta
}

async function leerCuerpo(respuesta: Response): Promise<unknown> {
  const texto = await respuesta.text()
  if (!texto) return undefined

  try {
    return JSON.parse(texto)
  } catch {
    return texto
  }
}

function extraerMensaje(datos: unknown, estado: number): string {
  if (
    typeof datos === 'object' &&
    datos !== null &&
    'mensaje' in datos &&
    typeof datos.mensaje === 'string'
  )
    return datos.mensaje

  if (ESTADOS_SIN_SERVIDOR.includes(estado)) return MENSAJE_SIN_CONEXION

  return `El servidor respondió con un error inesperado (${estado}).`
}
