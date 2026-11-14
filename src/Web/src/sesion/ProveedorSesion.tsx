import { useQuery, useQueryClient } from '@tanstack/react-query'
import { useCallback, useEffect, useMemo, useState, type ReactNode } from 'react'
import {
  cerrarSesion as cerrarSesionApi,
  iniciarSesion as iniciarSesionApi,
  obtenerUsuarioActual,
} from '../api/auth'
import {
  alCerrarSesion,
  CLAVE_TOKEN,
  cerrarSesionLocal,
  guardarToken,
  leerToken,
} from './almacenToken'
import { ContextoSesion, type ValorSesion } from './contexto'

const CLAVE_USUARIO = ['sesion', 'usuario']

// RF-CA-03, RF-CA-07, RF-CA-12, RF-CA-18
export function ProveedorSesion({ children }: { children: ReactNode }) {
  const clienteConsultas = useQueryClient()
  const [hayToken, setHayToken] = useState(() => leerToken() !== null)
  const [aviso, setAviso] = useState<string | null>(null)
  const [debeCambiarPassword, setDebeCambiarPassword] = useState(false)

  const consulta = useQuery({
    queryKey: CLAVE_USUARIO,
    queryFn: obtenerUsuarioActual,
    enabled: hayToken,
    staleTime: 5 * 60 * 1000,
    retry: false,
  })

  useEffect(
    () =>
      alCerrarSesion(() => {
        setHayToken(false)
        setDebeCambiarPassword(false)
        clienteConsultas.clear()
      }),
    [clienteConsultas],
  )

  useEffect(() => {
    const alCambiarOtraPestana = (evento: StorageEvent) => {
      if (evento.key !== CLAVE_TOKEN) return
      clienteConsultas.clear()
      setHayToken(leerToken() !== null)
    }
    window.addEventListener('storage', alCambiarOtraPestana)
    return () => window.removeEventListener('storage', alCambiarOtraPestana)
  }, [clienteConsultas])

  const iniciarSesion = useCallback<ValorSesion['iniciarSesion']>(
    async (datos) => {
      const respuesta = await iniciarSesionApi(datos)
      guardarToken(respuesta.token)
      await clienteConsultas.fetchQuery({ queryKey: CLAVE_USUARIO, queryFn: obtenerUsuarioActual })
      setAviso(null)
      setDebeCambiarPassword(respuesta.debeCambiarPassword)
      setHayToken(true)
      return respuesta
    },
    [clienteConsultas],
  )

  const cerrarSesion = useCallback(async () => {
    await cerrarSesionApi().catch(() => undefined)
    cerrarSesionLocal()
  }, [])

  // RF-CA-12, RF-CA-22
  const terminarSesionConAviso = useCallback((texto: string) => {
    setAviso(texto)
    cerrarSesionLocal()
  }, [])

  const valor = useMemo<ValorSesion>(
    () => ({
      usuario: hayToken ? (consulta.data ?? null) : null,
      cargando: hayToken && consulta.isPending,
      aviso,
      debeCambiarPassword,
      iniciarSesion,
      cerrarSesion,
      terminarSesionConAviso,
    }),
    [
      hayToken,
      consulta.data,
      consulta.isPending,
      aviso,
      debeCambiarPassword,
      iniciarSesion,
      cerrarSesion,
      terminarSesionConAviso,
    ],
  )

  return <ContextoSesion.Provider value={valor}>{children}</ContextoSesion.Provider>
}
