import { useQuery } from '@tanstack/react-query'
import { descargarImagen, type Componente } from '../api/componentes'
import { IlustracionCategoria, MARCO_IMAGEN } from './IlustracionCategoria'

const leerComoDataUrl = (archivo: Blob) =>
  new Promise<string>((resolver, rechazar) => {
    const lector = new FileReader()
    lector.onload = () => resolver(lector.result as string)
    lector.onerror = () => rechazar(lector.error)
    lector.readAsDataURL(archivo)
  })

// FN-01, RF-NEG-06
export function FotoComponente({
  componente,
  className = 'aspect-[16/9]',
}: {
  componente: Pick<Componente, 'nombre' | 'categoria' | 'imagenUrl'>
  className?: string
}) {
  const { imagenUrl } = componente
  const consulta = useQuery({
    queryKey: ['imagen-componente', imagenUrl],
    queryFn: async ({ signal }) => leerComoDataUrl(await descargarImagen(imagenUrl!, signal)),
    enabled: imagenUrl !== null,
    staleTime: Infinity,
    retry: false,
  })

  if (imagenUrl === null || !consulta.data)
    return <IlustracionCategoria categoria={componente.categoria} className={className} />

  return (
    <div className={`${MARCO_IMAGEN} ${className}`}>
      <img
        src={consulta.data}
        alt={`Foto de ${componente.nombre}`}
        className="size-full object-contain"
      />
    </div>
  )
}
