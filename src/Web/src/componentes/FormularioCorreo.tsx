import { zodResolver } from '@hookform/resolvers/zod'
import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { Link } from 'react-router'
import type { RespuestaMensaje } from '../api/auth'
import { mensajeDeError } from '../api/errores'
import { Boton, Campo, TarjetaFormulario } from './Formulario'
import { Mensaje } from './Mensaje'
import { esquemaCorreo, type DatosCorreo } from '../sesion/esquemas'

export function FormularioCorreo({
  titulo,
  descripcion,
  textoBoton,
  enviarCorreo,
}: {
  titulo: string
  descripcion: string
  textoBoton: string
  enviarCorreo: (correo: string) => Promise<RespuestaMensaje>
}) {
  const [respuesta, setRespuesta] = useState<string | null>(null)
  const {
    register,
    handleSubmit,
    setError,
    formState: { errors, isSubmitting },
  } = useForm<DatosCorreo>({ resolver: zodResolver(esquemaCorreo) })

  const enviar = handleSubmit(async ({ correo }) => {
    try {
      setRespuesta((await enviarCorreo(correo)).mensaje)
    } catch (error) {
      setError('root', { message: mensajeDeError(error) })
    }
  })

  return (
    <TarjetaFormulario
      titulo={titulo}
      descripcion={descripcion}
      pie={
        <Link to="/login" className="font-medium text-acento-texto hover:underline">
          Volver a iniciar sesión
        </Link>
      }
    >
      {respuesta ? (
        <Mensaje tipo="exito">{respuesta}</Mensaje>
      ) : (
        <>
          {errors.root?.message && <Mensaje tipo="error">{errors.root.message}</Mensaje>}
          <form onSubmit={enviar} noValidate className="space-y-4">
            <Campo
              etiqueta="Correo"
              type="email"
              autoComplete="email"
              error={errors.correo?.message}
              {...register('correo')}
            />
            <Boton type="submit" cargando={isSubmitting} className="w-full">
              {textoBoton}
            </Boton>
          </form>
        </>
      )}
    </TarjetaFormulario>
  )
}
