import { zodResolver } from '@hookform/resolvers/zod'
import { useMutation } from '@tanstack/react-query'
import { useId, useState, type ChangeEvent, type ComponentProps } from 'react'
import { useForm, useWatch } from 'react-hook-form'
import {
  actualizarComponente,
  crearComponente,
  quitarImagenComponente,
  subirImagenComponente,
  type Componente,
} from '../api/componentes'
import { mensajeDeError } from '../api/errores'
import { Boton, Campo, estiloBotonSecundario } from '../componentes/Formulario'
import { Mensaje } from '../componentes/Mensaje'
import {
  aDatosComponente,
  esquemaComponente,
  problemaDeFoto,
  TIPOS_FOTO,
  valoresIniciales,
  type ValoresComponente,
} from './esquemaComponente'
import { CATEGORIAS, etiquetaCategoria } from './filtros'
import { FotoComponente } from './FotoComponente'

function Seleccion({
  etiqueta,
  error,
  children,
  ...props
}: ComponentProps<'select'> & { etiqueta: string; error?: string }) {
  const id = useId()
  const idError = `${id}-error`

  return (
    <div>
      <label htmlFor={id} className="block text-sm font-medium">
        {etiqueta}
      </label>
      <select
        id={id}
        aria-invalid={error ? true : undefined}
        aria-describedby={error ? idError : undefined}
        className={`mt-1.5 block h-11 w-full rounded-lg border bg-fondo px-3 text-[15px] text-texto outline-none focus:ring-2 disabled:cursor-not-allowed disabled:opacity-60 ${
          error
            ? 'border-error focus:ring-error/25'
            : 'border-borde-fuerte focus:border-acento focus:ring-acento/25'
        }`}
        {...props}
      >
        {children}
      </select>
      {error && (
        <p id={idError} className="mt-1.5 text-sm text-error">
          {error}
        </p>
      )}
    </div>
  )
}

// RF-DOC-01, RF-DOC-04
function FotoDelComponente({
  componente,
  onCambio,
}: {
  componente: Componente
  onCambio: (componente: Componente) => void
}) {
  const idArchivo = useId()
  const [problema, setProblema] = useState<string | null>(null)
  const [confirmarQuitar, setConfirmarQuitar] = useState(false)

  const subir = useMutation({
    mutationFn: (archivo: File) => subirImagenComponente(componente.id, archivo),
    onSuccess: onCambio,
  })
  const quitar = useMutation({
    mutationFn: () => quitarImagenComponente(componente.id),
    onSuccess: (actualizado) => {
      setConfirmarQuitar(false)
      onCambio(actualizado)
    },
  })

  const elegir = (evento: ChangeEvent<HTMLInputElement>) => {
    const archivo = evento.target.files?.[0]
    evento.target.value = ''
    if (!archivo) return

    const motivo = problemaDeFoto(archivo)
    setProblema(motivo)
    subir.reset()
    quitar.reset()
    if (!motivo) subir.mutate(archivo)
  }

  const error =
    problema ??
    (subir.error ? mensajeDeError(subir.error) : null) ??
    (quitar.error ? mensajeDeError(quitar.error) : null)

  return (
    <section aria-labelledby={`${idArchivo}-titulo`} className="space-y-3">
      <h3 id={`${idArchivo}-titulo`} className="text-sm font-semibold">
        Foto
      </h3>
      <FotoComponente componente={componente} className="aspect-[16/9] w-full max-w-xs" />
      <p className="text-sm text-texto-suave">
        {componente.imagenUrl
          ? 'Esta foto se ve en el catálogo.'
          : 'Sin foto: el catálogo muestra la ilustración de su categoría.'}{' '}
        JPG, PNG o WebP, hasta 2 MB.
      </p>
      {error && <Mensaje tipo="error">{error}</Mensaje>}
      {subir.isSuccess && !error && <Mensaje tipo="exito">Foto guardada.</Mensaje>}
      <div className="flex flex-wrap items-center gap-2">
        <input
          id={idArchivo}
          type="file"
          accept={TIPOS_FOTO.join(',')}
          onChange={elegir}
          disabled={subir.isPending}
          className="sr-only"
        />
        <label
          htmlFor={idArchivo}
          className={`${estiloBotonSecundario} cursor-pointer ${subir.isPending ? 'pointer-events-none opacity-60' : ''}`}
        >
          {subir.isPending ? 'Subiendo…' : componente.imagenUrl ? 'Cambiar foto' : 'Subir foto'}
        </label>
        {componente.imagenUrl &&
          (confirmarQuitar ? (
            <>
              <span className="text-sm text-texto-claro">¿Quitar la foto?</span>
              <Boton
                variante="secundario"
                cargando={quitar.isPending}
                onClick={() => quitar.mutate()}
              >
                Sí, quitarla
              </Boton>
              <Boton variante="secundario" onClick={() => setConfirmarQuitar(false)}>
                Cancelar
              </Boton>
            </>
          ) : (
            <Boton variante="secundario" onClick={() => setConfirmarQuitar(true)}>
              Quitar foto
            </Boton>
          ))}
      </div>
    </section>
  )
}

// RF-NEG-01, RD-07
export function EditorComponente({
  componente,
  onGuardado,
  onCerrar,
}: {
  componente?: Componente
  onGuardado: (componente: Componente, cambio: 'creado' | 'editado' | 'foto') => void
  onCerrar: () => void
}) {
  const {
    register,
    handleSubmit,
    control,
    setError,
    formState: { errors, isSubmitting },
  } = useForm<ValoresComponente>({
    resolver: zodResolver(esquemaComponente),
    defaultValues: valoresIniciales(componente),
  })
  const categoria = useWatch({ control, name: 'categoria' })

  const enviar = handleSubmit(async (valores) => {
    try {
      const datos = aDatosComponente(valores)
      const guardado = componente
        ? await actualizarComponente(componente.id, datos)
        : await crearComponente(datos)
      onGuardado(guardado, componente ? 'editado' : 'creado')
    } catch (error) {
      setError('root', { message: mensajeDeError(error) })
    }
  })

  return (
    <section
      aria-label={componente ? `Editar ${componente.nombre}` : 'Nuevo componente'}
      className="space-y-6 rounded-xl border border-acento/40 bg-superficie p-5 sm:p-6"
    >
      <div className="flex flex-wrap items-start justify-between gap-3">
        <div>
          <h2 className="text-xl font-semibold">
            {componente ? 'Editar componente' : 'Nuevo componente'}
          </h2>
          {componente && (
            <p className="mt-1 text-sm text-texto-suave">
              La categoría no se puede cambiar; para otra categoría, retíralo y crea uno nuevo.
            </p>
          )}
        </div>
        <Boton variante="secundario" onClick={onCerrar}>
          Cerrar
        </Boton>
      </div>

      <form onSubmit={enviar} noValidate className="space-y-4">
        {errors.root?.message && <Mensaje tipo="error">{errors.root.message}</Mensaje>}

        <div className="grid gap-4 sm:grid-cols-2">
          <Campo etiqueta="Nombre" error={errors.nombre?.message} {...register('nombre')} />
          {componente ? (
            <div>
              <p className="text-sm font-medium">Categoría</p>
              <p className="mt-1.5 flex h-11 items-center rounded-lg border border-borde bg-superficie-2 px-3.5 text-[15px] text-texto-suave">
                {etiquetaCategoria(componente.categoria)}
              </p>
            </div>
          ) : (
            <Seleccion
              etiqueta="Categoría"
              error={errors.categoria?.message}
              {...register('categoria')}
            >
              <option value="">Elige una categoría</option>
              {CATEGORIAS.map(({ valor, etiqueta }) => (
                <option key={valor} value={valor}>
                  {etiqueta}
                </option>
              ))}
            </Seleccion>
          )}
          <Campo
            etiqueta="Fabricante"
            error={errors.fabricante?.message}
            {...register('fabricante')}
          />
          <Campo
            etiqueta="Socket o interfaz"
            placeholder="AM5, LGA1700, DDR5, PCIe 4.0…"
            error={errors.socketOInterfaz?.message}
            {...register('socketOInterfaz')}
          />
          <Campo
            etiqueta="Consumo (W)"
            inputMode="numeric"
            error={errors.consumoWatts?.message}
            {...register('consumoWatts')}
          />
          {categoria === 'Fuente' && (
            <Campo
              etiqueta="Potencia de la fuente (W)"
              inputMode="numeric"
              error={errors.potenciaWatts?.message}
              {...register('potenciaWatts')}
            />
          )}
          <Campo
            etiqueta="Precio (USD)"
            inputMode="decimal"
            placeholder="0.00"
            error={errors.precio?.message}
            {...register('precio')}
          />
          <Campo
            etiqueta="Existencias"
            inputMode="numeric"
            error={errors.stockDisponible?.message}
            {...register('stockDisponible')}
          />
          {categoria === 'PlacaBase' && (
            <>
              <Campo
                etiqueta="Tipo de memoria"
                placeholder="DDR4 o DDR5"
                error={errors.tipoMemoria?.message}
                {...register('tipoMemoria')}
              />
              <Campo
                etiqueta="Ranuras de memoria"
                inputMode="numeric"
                error={errors.ranurasMemoria?.message}
                {...register('ranurasMemoria')}
              />
            </>
          )}
        </div>

        <div className="flex flex-wrap gap-2">
          <Boton type="submit" cargando={isSubmitting}>
            {componente ? 'Guardar cambios' : 'Crear componente'}
          </Boton>
          <Boton type="button" variante="secundario" onClick={onCerrar}>
            Cancelar
          </Boton>
        </div>
      </form>

      {componente ? (
        <FotoDelComponente componente={componente} onCambio={(c) => onGuardado(c, 'foto')} />
      ) : (
        <p className="text-sm text-texto-suave">Podrás subir la foto después de crearlo.</p>
      )}
    </section>
  )
}
