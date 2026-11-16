import { useQuery } from '@tanstack/react-query'
import { Link, useSearchParams } from 'react-router'
import { activar } from '../api/auth'
import { mensajeDeError } from '../api/errores'
import { Cargando } from '../componentes/Cargando'
import { TarjetaFormulario } from '../componentes/Formulario'
import { Mensaje } from '../componentes/Mensaje'

const enlaceReenvio = (
  <Link to="/reenviar-activacion" className="font-medium text-acento-texto hover:underline">
    Pedir un enlace nuevo
  </Link>
)

// RF-CA-16
export function Activar() {
  const token = useSearchParams()[0].get('token') ?? ''

  const activacion = useQuery({
    queryKey: ['activacion', token],
    queryFn: () => activar(token),
    enabled: token !== '',
    retry: false,
    staleTime: Infinity,
    gcTime: Infinity,
  })

  if (!token)
    return (
      <TarjetaFormulario titulo="Activar cuenta" pie={enlaceReenvio}>
        <Mensaje tipo="error">El enlace no tiene código de activación.</Mensaje>
      </TarjetaFormulario>
    )

  if (activacion.isPending)
    return (
      <TarjetaFormulario titulo="Activar cuenta">
        <Cargando texto="Activando tu cuenta…" />
      </TarjetaFormulario>
    )

  if (activacion.isError)
    return (
      <TarjetaFormulario titulo="No se pudo activar la cuenta" pie={enlaceReenvio}>
        <Mensaje tipo="error">{mensajeDeError(activacion.error)}</Mensaje>
      </TarjetaFormulario>
    )

  return (
    <TarjetaFormulario titulo="Cuenta activada">
      <Mensaje tipo="exito">Tu cuenta está lista. Ya puedes iniciar sesión.</Mensaje>
      <Link
        to="/login"
        className="inline-block rounded-lg bg-acento px-4 py-2 text-sm font-semibold text-sobre-acento hover:bg-acento-claro"
      >
        Iniciar sesión
      </Link>
    </TarjetaFormulario>
  )
}
