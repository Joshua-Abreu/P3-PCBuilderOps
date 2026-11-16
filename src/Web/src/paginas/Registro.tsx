import { zodResolver } from '@hookform/resolvers/zod'
import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { Link } from 'react-router'
import { registrar } from '../api/auth'
import { mensajeDeError } from '../api/errores'
import { Boton, Campo, TarjetaFormulario } from '../componentes/Formulario'
import { Mensaje } from '../componentes/Mensaje'
import { VentajasTaller } from '../componentes/VentajasTaller'
import { esquemaRegistro, type DatosRegistro } from '../sesion/esquemas'

const enlaceLogin = (
  <p>
    ¿Ya tienes cuenta?{' '}
    <Link to="/login" className="font-medium text-acento-texto hover:underline">
      Inicia sesión
    </Link>
  </p>
)

// RF-CA-01, RF-CA-14, RF-CA-15
export function Registro() {
  const [correoRegistrado, setCorreoRegistrado] = useState<string | null>(null)
  const {
    register,
    handleSubmit,
    setError,
    formState: { errors, isSubmitting },
  } = useForm<DatosRegistro>({ resolver: zodResolver(esquemaRegistro) })

  const enviar = handleSubmit(async ({ nombre, correo, password }) => {
    try {
      await registrar({ nombre, correo, password })
      setCorreoRegistrado(correo)
    } catch (error) {
      setError('root', { message: mensajeDeError(error) })
    }
  })

  if (correoRegistrado)
    return (
      <TarjetaFormulario titulo="Revisa tu correo" pie={enlaceLogin}>
        <Mensaje tipo="exito">
          Te enviamos un enlace de activación a <strong>{correoRegistrado}</strong>. Ábrelo para
          activar tu cuenta; vence en 24 horas.
        </Mensaje>
        <p className="text-sm text-texto-suave">
          ¿No llegó?{' '}
          <Link to="/reenviar-activacion" className="font-medium text-acento-texto hover:underline">
            Reenviar el correo de activación
          </Link>
        </p>
      </TarjetaFormulario>
    )

  return (
    <TarjetaFormulario
      titulo="Crear cuenta"
      descripcion="Regístrate para armar tu PC y seguir tus órdenes."
      lateral={
        <VentajasTaller
          titulo="Crea tu cuenta en PC Builder Ops"
          texto="Con tu cuenta ves el catálogo, cotizas tu armado y sigues tu orden hasta la entrega."
        />
      }
      pie={enlaceLogin}
    >
      {errors.root?.message && <Mensaje tipo="error">{errors.root.message}</Mensaje>}
      <form onSubmit={enviar} noValidate className="space-y-4">
        <Campo
          etiqueta="Nombre"
          autoComplete="name"
          error={errors.nombre?.message}
          {...register('nombre')}
        />
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
          autoComplete="new-password"
          error={errors.password?.message}
          {...register('password')}
        />
        <p className="-mt-2 text-xs text-texto-suave">
          Mínimo 8 caracteres, con al menos una letra y un número.
        </p>
        <Campo
          etiqueta="Confirmar contraseña"
          type="password"
          autoComplete="new-password"
          error={errors.confirmacion?.message}
          {...register('confirmacion')}
        />
        <Boton type="submit" cargando={isSubmitting} className="w-full">
          Crear cuenta
        </Boton>
      </form>
    </TarjetaFormulario>
  )
}
