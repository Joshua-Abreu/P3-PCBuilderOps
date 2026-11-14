import type { Usuario } from '../api/auth'

export type Requisito = 'sesion' | 'cliente' | 'taller' | 'administrador'

export const PERMISO_TALLER = 'OperarTaller'

export const esAdministrador = (usuario: Usuario) => usuario.rol === 'Administrador'

export const esTecnico = (usuario: Usuario) => usuario.permisos.includes(PERMISO_TALLER)

// RF-CA-06, RF-GP-05, RF-NEG-06
export function cumpleRequisito(usuario: Usuario | null, requisito: Requisito): boolean {
  if (!usuario) return false

  switch (requisito) {
    case 'sesion':
      return true
    case 'cliente':
      return usuario.rol === 'Estandar'
    case 'taller':
      return esAdministrador(usuario) || esTecnico(usuario)
    case 'administrador':
      return esAdministrador(usuario)
  }
}

export function describirRol(usuario: Usuario): string {
  if (esAdministrador(usuario)) return 'Administrador'
  return esTecnico(usuario) ? 'Cliente y técnico del taller' : 'Cliente'
}
