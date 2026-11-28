import { pedir, pedirArchivo } from './cliente'

export interface Componente {
  id: string
  nombre: string
  categoria: string
  fabricante: string
  socketOInterfaz: string
  consumoWatts: number
  potenciaWatts: number | null
  precio: number
  stockDisponible: number
  tipoMemoria: string | null
  ranurasMemoria: number | null
  estaActivo: boolean
  imagenUrl: string | null
}

export interface DatosComponente {
  nombre: string
  categoria: string
  fabricante: string
  socketOInterfaz: string
  consumoWatts: number
  potenciaWatts: number | null
  precio: number
  stockDisponible: number
  tipoMemoria: string | null
  ranurasMemoria: number | null
}

// FN-01, RF-NEG-06
export const listarComponentes = () => pedir<Componente[]>('/componentes')

// RF-NEG-01
export const crearComponente = (datos: DatosComponente) =>
  pedir<Componente>('/componentes', { metodo: 'POST', cuerpo: datos })

// RF-NEG-01
export const actualizarComponente = (id: string, datos: DatosComponente) =>
  pedir<Componente>(`/componentes/${id}`, { metodo: 'PUT', cuerpo: datos })

// RF-NEG-01
export const cambiarEstadoComponente = (id: string, estaActivo: boolean) =>
  pedir<Componente>(`/componentes/${id}/estado`, { metodo: 'PATCH', cuerpo: { estaActivo } })

// RF-DOC-01
export function subirImagenComponente(id: string, archivo: File) {
  const formulario = new FormData()
  formulario.append('archivo', archivo)
  return pedir<Componente>(`/componentes/${id}/imagen`, { metodo: 'PUT', cuerpo: formulario })
}

// RF-DOC-04
export const quitarImagenComponente = (id: string) =>
  pedir<Componente>(`/componentes/${id}/imagen`, { metodo: 'DELETE' })

export const rutaDeImagen = (imagenUrl: string) => imagenUrl.replace(/^\/api(?=\/)/, '')

// FN-01, RF-NEG-06
export const descargarImagen = (imagenUrl: string, senal?: AbortSignal) =>
  pedirArchivo(rutaDeImagen(imagenUrl), senal)
