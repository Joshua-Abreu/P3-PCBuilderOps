import type { Usuario } from '../api/auth'
import { cumpleRequisito, type Requisito } from '../sesion/acceso'

export interface Enlace {
  ruta: string
  texto: string
  requisito?: Requisito
  soloVisitantesOClientes?: boolean
}

const enlaces: Enlace[] = [
  { ruta: '/', texto: 'Inicio' },
  { ruta: '/catalogo', texto: 'Catálogo' },
  { ruta: '/armar', texto: 'Armar mi PC', soloVisitantesOClientes: true },
  { ruta: '/mis-ordenes', texto: 'Mis órdenes', requisito: 'cliente' },
  { ruta: '/taller', texto: 'Taller', requisito: 'taller' },
  { ruta: '/admin', texto: 'Administración', requisito: 'administrador' },
]

export const enlacesVisibles = (usuario: Usuario | null) =>
  enlaces.filter(({ requisito, soloVisitantesOClientes }) => {
    if (soloVisitantesOClientes) return !usuario || cumpleRequisito(usuario, 'cliente')
    return !requisito || cumpleRequisito(usuario, requisito)
  })
