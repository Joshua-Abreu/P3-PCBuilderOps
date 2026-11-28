import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useRef, useState } from 'react'
import { Link } from 'react-router'
import { cambiarEstadoComponente, listarComponentes, type Componente } from '../api/componentes'
import { mensajeDeError } from '../api/errores'
import { EditorComponente } from '../catalogo/EditorComponente'
import { filtrarAdministracion } from '../catalogo/administracion'
import { CATEGORIAS, etiquetaCategoria } from '../catalogo/filtros'
import { FotoComponente } from '../catalogo/FotoComponente'
import { formatearPrecio } from '../catalogo/formato'
import { EtiquetaRetirado } from '../catalogo/TarjetaComponente'
import { Cargando } from '../componentes/Cargando'
import { Boton, estiloEnlace } from '../componentes/Formulario'
import { Mensaje } from '../componentes/Mensaje'

const estiloCampo =
  'h-11 rounded-lg border border-borde-fuerte bg-fondo px-3 text-[15px] font-normal text-texto outline-none placeholder:text-texto-tenue focus:border-acento focus:ring-2 focus:ring-acento/25'

const estiloEtiqueta = 'flex flex-col gap-1.5 text-sm font-medium text-texto-suave'

type Edicion = { tipo: 'nuevo' } | { tipo: 'editar'; id: string } | null

function FilaComponente({
  componente,
  cambiando,
  onEditar,
  onCambiarEstado,
}: {
  componente: Componente
  cambiando: boolean
  onEditar: () => void
  onCambiarEstado: () => void
}) {
  return (
    <li className="grid gap-4 rounded-xl border border-borde bg-superficie p-4 sm:grid-cols-[8rem_minmax(0,1fr)] sm:items-center xl:grid-cols-[8rem_minmax(0,1fr)_18rem_auto]">
      <FotoComponente componente={componente} className="aspect-[16/9] w-full" />

      <div className="min-w-0">
        <p className="text-xs font-semibold tracking-wider text-acento-texto uppercase">
          {etiquetaCategoria(componente.categoria)}
        </p>
        <p className="mt-0.5 font-semibold text-balance">{componente.nombre}</p>
        <p className="mt-0.5 text-sm text-texto-suave">
          {componente.fabricante} · <span className="font-mono">{componente.socketOInterfaz}</span>
        </p>
      </div>

      <dl className="grid grid-cols-3 gap-3 text-sm sm:col-span-2 xl:col-span-1">
        <div>
          <dt className="text-xs text-texto-suave">Precio</dt>
          <dd className="mt-0.5 font-mono font-medium">{formatearPrecio(componente.precio)}</dd>
        </div>
        <div>
          <dt className="text-xs text-texto-suave">Existencias</dt>
          <dd className="mt-0.5 font-mono font-medium">{componente.stockDisponible}</dd>
        </div>
        <div>
          <dt className="text-xs text-texto-suave">Estado</dt>
          <dd className="mt-0.5">
            {componente.estaActivo ? (
              <span className="font-medium text-exito">Activo</span>
            ) : (
              <EtiquetaRetirado />
            )}
          </dd>
        </div>
      </dl>

      <div className="flex gap-2 sm:col-span-2 xl:col-span-1">
        <Boton variante="secundario" onClick={onEditar} className="flex-1 xl:flex-none">
          Editar
        </Boton>
        <Boton
          variante="secundario"
          cargando={cambiando}
          onClick={onCambiarEstado}
          className="flex-1 xl:flex-none"
        >
          {componente.estaActivo ? 'Retirar' : 'Reactivar'}
        </Boton>
      </div>
    </li>
  )
}

// RF-NEG-01, RD-06
export function AdministrarCatalogo() {
  const clienteConsultas = useQueryClient()
  const consulta = useQuery({ queryKey: ['componentes'], queryFn: listarComponentes })
  const [edicion, setEdicion] = useState<Edicion>(null)
  const [aviso, setAviso] = useState<string | null>(null)
  const [texto, setTexto] = useState('')
  const [categoria, setCategoria] = useState('')
  const [mostrarRetirados, setMostrarRetirados] = useState(true)
  const editorRef = useRef<HTMLDivElement>(null)

  const reemplazar = (actualizado: Componente) => {
    clienteConsultas.setQueryData<Componente[]>(['componentes'], (lista) => {
      if (!lista) return lista
      const existe = lista.some((c) => c.id === actualizado.id)
      return existe
        ? lista.map((c) => (c.id === actualizado.id ? actualizado : c))
        : [...lista, actualizado]
    })
    void clienteConsultas.invalidateQueries({ queryKey: ['componentes'] })
  }

  const cambioEstado = useMutation({
    mutationFn: (componente: Componente) =>
      cambiarEstadoComponente(componente.id, !componente.estaActivo),
    onSuccess: (actualizado) => {
      reemplazar(actualizado)
      setAviso(
        actualizado.estaActivo
          ? `${actualizado.nombre} vuelve a estar disponible en el catálogo.`
          : `${actualizado.nombre} quedó retirado: los clientes ya no lo ven ni pueden cotizarlo.`,
      )
    },
  })

  const abrir = (nueva: Edicion) => {
    setEdicion(nueva)
    setAviso(null)
    cambioEstado.reset()
    requestAnimationFrame(() => editorRef.current?.scrollIntoView({ block: 'start' }))
  }

  if (consulta.isPending) return <Cargando texto="Cargando el catálogo…" />

  if (consulta.isError)
    return (
      <div className="max-w-xl space-y-4">
        <h1 className="text-3xl font-bold tracking-tight">Administrar catálogo</h1>
        <Mensaje tipo="error">{mensajeDeError(consulta.error)}</Mensaje>
        <Boton variante="secundario" onClick={() => consulta.refetch()}>
          Reintentar
        </Boton>
      </div>
    )

  const retirados = consulta.data.filter((c) => !c.estaActivo).length
  const visibles = filtrarAdministracion(consulta.data, { texto, categoria, mostrarRetirados })
  const enEdicion =
    edicion?.tipo === 'editar' ? consulta.data.find((c) => c.id === edicion.id) : undefined

  return (
    <div className="space-y-6">
      <div className="flex flex-wrap items-end justify-between gap-4">
        <div>
          <h1 className="text-3xl font-bold tracking-tight">Administrar catálogo</h1>
          <p className="mt-1.5 text-[15px] text-texto-suave">
            {consulta.data.length} componentes · {retirados}{' '}
            {retirados === 1 ? 'retirado' : 'retirados'} ·{' '}
            <Link to="/catalogo" className={estiloEnlace}>
              Ver como cliente
            </Link>
          </p>
        </div>
        <Boton onClick={() => abrir({ tipo: 'nuevo' })}>Nuevo componente</Boton>
      </div>

      {aviso && <Mensaje tipo="exito">{aviso}</Mensaje>}
      {cambioEstado.error && <Mensaje tipo="error">{mensajeDeError(cambioEstado.error)}</Mensaje>}

      <div ref={editorRef} className="scroll-mt-6">
        {edicion && (edicion.tipo === 'nuevo' || enEdicion) && (
          <EditorComponente
            key={edicion.tipo === 'nuevo' ? 'nuevo' : edicion.id}
            componente={enEdicion}
            onCerrar={() => setEdicion(null)}
            onGuardado={(guardado, cambio) => {
              reemplazar(guardado)
              if (cambio === 'creado') {
                setEdicion({ tipo: 'editar', id: guardado.id })
                setAviso(`${guardado.nombre} se creó. Ya puedes subirle una foto.`)
              } else if (cambio === 'editado') {
                setEdicion(null)
                setAviso(`Se guardaron los cambios de ${guardado.nombre}.`)
              }
            }}
          />
        )}
      </div>

      <div className="grid gap-3 rounded-xl border border-borde bg-superficie p-5 sm:grid-cols-[1fr_1fr_auto] sm:items-end">
        <label className={estiloEtiqueta}>
          Buscar
          <input
            type="search"
            value={texto}
            placeholder="Nombre o fabricante"
            onChange={(evento) => setTexto(evento.target.value)}
            className={estiloCampo}
          />
        </label>
        <label className={estiloEtiqueta}>
          Categoría
          <select
            value={categoria}
            onChange={(evento) => setCategoria(evento.target.value)}
            className={estiloCampo}
          >
            <option value="">Todas</option>
            {CATEGORIAS.map(({ valor, etiqueta }) => (
              <option key={valor} value={valor}>
                {etiqueta}
              </option>
            ))}
          </select>
        </label>
        <label className="flex h-11 items-center gap-2 text-sm text-texto-claro">
          <input
            type="checkbox"
            checked={mostrarRetirados}
            onChange={(evento) => setMostrarRetirados(evento.target.checked)}
            className="size-[18px] accent-acento"
          />
          Mostrar retirados
        </label>
      </div>

      {visibles.length === 0 ? (
        <div className="rounded-xl border border-dashed border-borde-fuerte bg-superficie p-10 text-center">
          <p className="font-medium">Ningún componente coincide con la búsqueda.</p>
        </div>
      ) : (
        <ul className="space-y-3">
          {visibles.map((componente) => (
            <FilaComponente
              key={componente.id}
              componente={componente}
              cambiando={cambioEstado.isPending && cambioEstado.variables?.id === componente.id}
              onEditar={() => abrir({ tipo: 'editar', id: componente.id })}
              onCambiarEstado={() => {
                setAviso(null)
                cambioEstado.mutate(componente)
              }}
            />
          ))}
        </ul>
      )}
    </div>
  )
}
