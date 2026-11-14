const CLAVE = 'pcbuilderops.token'

type Oyente = () => void

const oyentes = new Set<Oyente>()
let tokenSinAlmacen: string | null = null

export function leerToken(): string | null {
  try {
    return localStorage.getItem(CLAVE)
  } catch {
    return tokenSinAlmacen
  }
}

export function guardarToken(token: string): void {
  try {
    localStorage.setItem(CLAVE, token)
  } catch {
    tokenSinAlmacen = token
  }
}

// RF-CA-12, RF-CA-18
export function cerrarSesionLocal(): void {
  tokenSinAlmacen = null
  try {
    localStorage.removeItem(CLAVE)
  } catch {
    tokenSinAlmacen = null
  }
  oyentes.forEach((oyente) => oyente())
}

export function alCerrarSesion(oyente: Oyente): () => void {
  oyentes.add(oyente)
  return () => {
    oyentes.delete(oyente)
  }
}
