import { createContext, useContext } from 'react'
import type { RespuestaLogin, Usuario } from '../api/auth'

export interface ValorSesion {
  usuario: Usuario | null
  cargando: boolean
  aviso: string | null
  debeCambiarPassword: boolean
  iniciarSesion: (datos: { correo: string; password: string }) => Promise<RespuestaLogin>
  cerrarSesion: () => Promise<void>
  terminarSesionConAviso: (aviso: string) => void
}

export const ContextoSesion = createContext<ValorSesion | null>(null)

export function useSesion(): ValorSesion {
  const valor = useContext(ContextoSesion)
  if (!valor) throw new Error('useSesion debe usarse dentro de ProveedorSesion.')
  return valor
}
