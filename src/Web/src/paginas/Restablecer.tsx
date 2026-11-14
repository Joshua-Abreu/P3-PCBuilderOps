import { zodResolver } from '@hookform/resolvers/zod'
import { useQuery } from '@tanstack/react-query'
import { useForm } from 'react-hook-form'
import { Link, useNavigate, useSearchParams } from 'react-router'
import { restablecerPassword, validarCodigoRecuperacion } from '../api/auth'
import { mensajeDeError } from '../api/errores'
import { Cargando } from '../componentes/Cargando'
import { Boton, Campo, TarjetaFormulario } from '../componentes/Formulario'
import { Mensaje } from '../componentes/Mensaje'
import { cerrarSesionLocal } from '../sesion/almacenToken'
import { esquemaRestablecer, type DatosRestablecer } from '../sesion/esquemas'

const enlaceNuevo = (
  <Link to="/olvide-password" className="font-medium text-teal-700 hover:underline">
    Pedir un enlace nuevo
  </Link>
)

// RF-CA-10, RF-CA-11, RF-CA-12
export function Restablecer() {
  const token = useSearchParams()[0].get('token') ?? ''
  const navegar = useNavigate()

  const validacion = useQuery({
    queryKey: ['recuperacion', token],
    queryFn: () => validarCodigoRecuperacion(token),
    enabled: token !== '',
    retry: false,
    staleTime: Infinity,
  })

  const {
    register,
    handleSubmit,
    setError,
    formState: { errors, isSubmitting },
  } = useForm<DatosRestablecer>({ resolver: zodResolver(esquemaRestablecer) })

  const enviar = handleSubmit(async ({ nuevaPassword }) => {
    try {
      await restablecerPassword({ token, nuevaPassword })
      cerrarSesionLocal()
      navegar('/login', {
        replace: true,
        state: { aviso: 'Contraseña restablecida. Inicia sesión con la nueva contraseña.' },
      })
    } catch (error) {
      setError('root', { message: mensajeDeError(error) })
    }
  })

  if (!token)
    return (
      <TarjetaFormulario titulo="Restablecer contraseña" pie={enlaceNuevo}>
        <Mensaje tipo="error">El enlace no tiene código de recuperación.</Mensaje>
      </TarjetaFormulario>
    )

  if (validacion.isPending)
    return (
      <TarjetaFormulario titulo="Restablecer contraseña">
        <Cargando texto="Revisando el enlace…" />
      </TarjetaFormulario>
    )

  if (validacion.isError)
    return (
      <TarjetaFormulario titulo="El enlace no sirve" pie={enlaceNuevo}>
        <Mensaje tipo="error">{mensajeDeError(validacion.error)}</Mensaje>
      </TarjetaFormulario>
    )

  return (
    <TarjetaFormulario
      titulo="Crea tu nueva contraseña"
      descripcion="Al guardarla se cerrarán todas tus sesiones abiertas."
    >
      {errors.root?.message && <Mensaje tipo="error">{errors.root.message}</Mensaje>}
      <form onSubmit={enviar} noValidate className="space-y-4">
        <Campo
          etiqueta="Nueva contraseña"
          type="password"
          autoComplete="new-password"
          error={errors.nuevaPassword?.message}
          {...register('nuevaPassword')}
        />
        <Campo
          etiqueta="Confirmar contraseña"
          type="password"
          autoComplete="new-password"
          error={errors.confirmacion?.message}
          {...register('confirmacion')}
        />
        <Boton type="submit" cargando={isSubmitting} className="w-full">
          Guardar contraseña
        </Boton>
      </form>
    </TarjetaFormulario>
  )
}
