import { zodResolver } from '@hookform/resolvers/zod'
import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { Link, useLocation } from 'react-router'
import { mensajeDeError } from '../api/errores'
import { Boton, Campo, TarjetaFormulario } from '../componentes/Formulario'
import { Mensaje } from '../componentes/Mensaje'
import { useSesion } from '../sesion/contexto'
import { esquemaLogin, type DatosLogin } from '../sesion/esquemas'

export interface EstadoNavegacion {
  desde?: string
  aviso?: string
}

const MENSAJE_CUENTA_INACTIVA = 'La cuenta no está activa.'

// RF-CA-03, RF-CA-15, RF-CA-19
export function Login() {
  const { iniciarSesion, aviso } = useSesion()
  const estado = (useLocation().state ?? {}) as EstadoNavegacion
  const [cuentaInactiva, setCuentaInactiva] = useState(false)
  const {
    register,
    handleSubmit,
    setError,
    formState: { errors, isSubmitting },
  } = useForm<DatosLogin>({ resolver: zodResolver(esquemaLogin) })

  const enviar = handleSubmit(async (datos) => {
    setCuentaInactiva(false)
    try {
      await iniciarSesion(datos)
    } catch (error) {
      const mensaje = mensajeDeError(error)
      setCuentaInactiva(mensaje === MENSAJE_CUENTA_INACTIVA)
      setError('root', { message: mensaje })
    }
  })

  return (
    <TarjetaFormulario
      titulo="Iniciar sesión"
      descripcion="Entra para cotizar tu PC y seguir tus órdenes."
      pie={
        <>
          <p>
            ¿No tienes cuenta?{' '}
            <Link to="/registro" className="font-medium text-teal-700 hover:underline">
              Regístrate
            </Link>
          </p>
          <p>
            <Link to="/olvide-password" className="font-medium text-teal-700 hover:underline">
              Olvidé mi contraseña
            </Link>
          </p>
        </>
      }
    >
      {(estado.aviso ?? aviso) && <Mensaje tipo="exito">{estado.aviso ?? aviso}</Mensaje>}
      {errors.root?.message && (
        <Mensaje tipo="error">
          {errors.root.message}{' '}
          {cuentaInactiva && (
            <Link to="/reenviar-activacion" className="font-medium underline">
              Reenviar el correo de activación
            </Link>
          )}
        </Mensaje>
      )}
      <form onSubmit={enviar} noValidate className="space-y-4">
        <Campo
          etiqueta="Correo"
          type="email"
          autoComplete="email"
          error={errors.correo?.message}
          {...register('correo')}
        />
        <Campo
          etiqueta="Contraseña"
          type="password"
          autoComplete="current-password"
          error={errors.password?.message}
          {...register('password')}
        />
        <Boton type="submit" cargando={isSubmitting} className="w-full">
          Iniciar sesión
        </Boton>
      </form>
    </TarjetaFormulario>
  )
}
