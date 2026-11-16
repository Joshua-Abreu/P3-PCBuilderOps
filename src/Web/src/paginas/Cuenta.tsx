import { zodResolver } from '@hookform/resolvers/zod'
import { useForm } from 'react-hook-form'
import { useNavigate } from 'react-router'
import { cambiarPassword } from '../api/auth'
import { mensajeDeError } from '../api/errores'
import { Boton, Campo } from '../componentes/Formulario'
import { Mensaje } from '../componentes/Mensaje'
import { describirRol } from '../sesion/acceso'
import { useSesion } from '../sesion/contexto'
import { esquemaCambiarPassword, type DatosCambiarPassword } from '../sesion/esquemas'

// RF-CA-07, RF-CA-18, RF-CA-22
export function Cuenta() {
  const { usuario, cerrarSesion, terminarSesionConAviso, debeCambiarPassword } = useSesion()
  const navegar = useNavigate()
  const {
    register,
    handleSubmit,
    setError,
    formState: { errors, isSubmitting },
  } = useForm<DatosCambiarPassword>({ resolver: zodResolver(esquemaCambiarPassword) })

  const enviar = handleSubmit(async ({ passwordActual, nuevaPassword }) => {
    try {
      await cambiarPassword({ passwordActual, nuevaPassword })
      terminarSesionConAviso(
        'Contraseña actualizada. Por seguridad se cerraron tus sesiones; inicia sesión con la nueva contraseña.',
      )
    } catch (error) {
      setError('root', { message: mensajeDeError(error) })
    }
  })

  const salir = async () => {
    await navegar('/', { replace: true })
    await cerrarSesion()
  }

  if (!usuario) return null

  return (
    <div className="mx-auto max-w-2xl space-y-6">
      <h1 className="text-3xl font-semibold tracking-tight">Mi cuenta</h1>
      {debeCambiarPassword && (
        <Mensaje tipo="info">Debes cambiar tu contraseña antes de continuar.</Mensaje>
      )}

      <section className="rounded-xl border border-borde bg-superficie p-6">
        <h2 className="text-lg font-semibold">Tus datos</h2>
        <dl className="mt-4 grid gap-4 text-sm sm:grid-cols-2">
          <div>
            <dt className="text-texto-suave">Nombre</dt>
            <dd className="mt-1 font-medium">{usuario.nombre}</dd>
          </div>
          <div>
            <dt className="text-texto-suave">Correo</dt>
            <dd className="mt-1 font-medium break-all">{usuario.correo}</dd>
          </div>
          <div>
            <dt className="text-texto-suave">Tipo de cuenta</dt>
            <dd className="mt-1 font-medium">{describirRol(usuario)}</dd>
          </div>
        </dl>
        <Boton variante="secundario" className="mt-6" onClick={salir}>
          Cerrar sesión
        </Boton>
      </section>

      <section className="rounded-xl border border-borde bg-superficie p-6">
        <h2 className="text-lg font-semibold">Cambiar contraseña</h2>
        <p className="mt-1 text-sm text-texto-suave">
          Al cambiarla se cerrarán todas tus sesiones, incluida esta.
        </p>
        <form onSubmit={enviar} noValidate className="mt-4 max-w-sm space-y-4">
          {errors.root?.message && <Mensaje tipo="error">{errors.root.message}</Mensaje>}
          <Campo
            etiqueta="Contraseña actual"
            type="password"
            autoComplete="current-password"
            error={errors.passwordActual?.message}
            {...register('passwordActual')}
          />
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
          <Boton type="submit" cargando={isSubmitting}>
            Cambiar contraseña
          </Boton>
        </form>
      </section>
    </div>
  )
}
