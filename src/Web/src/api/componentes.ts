import { pedir } from './cliente'

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
}

// FN-01, RF-NEG-06
export const listarComponentes = () => pedir<Componente[]>('/componentes')
