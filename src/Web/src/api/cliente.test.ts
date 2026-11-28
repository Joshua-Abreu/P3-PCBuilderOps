import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { alCerrarSesion, guardarToken, leerToken } from '../sesion/almacenToken'
import { pedir, pedirArchivo } from './cliente'
import { ErrorApi, MENSAJE_SIN_CONEXION } from './errores'

const respuesta = (estado: number, cuerpo?: unknown) =>
  new Response(
    cuerpo === undefined ? null : typeof cuerpo === 'string' ? cuerpo : JSON.stringify(cuerpo),
    { status: estado },
  )

const fetchFalso = vi.fn<typeof fetch>()

const peticionEnviada = () => {
  const [url, opciones] = fetchFalso.mock.calls[0]
  return { url, opciones: opciones!, cabeceras: new Headers(opciones!.headers) }
}

const capturarError = async (promesa: Promise<unknown>): Promise<ErrorApi> => {
  try {
    await promesa
  } catch (error) {
    if (error instanceof ErrorApi) return error
    throw error
  }
  throw new Error('La petición debía fallar.')
}

beforeEach(() => {
  localStorage.clear()
  fetchFalso.mockReset()
  vi.stubGlobal('fetch', fetchFalso)
})

afterEach(() => {
  vi.unstubAllGlobals()
})

// RD-07, RD-08
describe('pedir', () => {
  it('antepone /api y devuelve el JSON de la respuesta', async () => {
    // Preparar
    fetchFalso.mockResolvedValue(respuesta(200, [{ id: 1 }]))

    // Actuar
    const datos = await pedir<{ id: number }[]>('/componentes')

    // Verificar
    expect(peticionEnviada().url).toBe('/api/componentes')
    expect(datos).toEqual([{ id: 1 }])
  })

  it('envía el token guardado en la cabecera Authorization', async () => {
    // Preparar
    guardarToken('token-de-prueba')
    fetchFalso.mockResolvedValue(respuesta(200, {}))

    // Actuar
    await pedir('/auth/yo')

    // Verificar
    expect(peticionEnviada().cabeceras.get('Authorization')).toBe('Bearer token-de-prueba')
  })

  it('sin sesión no envía la cabecera Authorization', async () => {
    // Preparar
    fetchFalso.mockResolvedValue(respuesta(200, {}))

    // Actuar
    await pedir('/componentes')

    // Verificar
    expect(peticionEnviada().cabeceras.has('Authorization')).toBe(false)
  })

  it('envía el cuerpo como JSON con su tipo de contenido', async () => {
    // Preparar
    fetchFalso.mockResolvedValue(respuesta(200, { token: 'x' }))

    // Actuar
    await pedir('/auth/login', {
      metodo: 'POST',
      cuerpo: { correo: 'a@b.com', password: 'Clave123' },
    })

    // Verificar
    const { opciones, cabeceras } = peticionEnviada()
    expect(opciones.method).toBe('POST')
    expect(cabeceras.get('Content-Type')).toBe('application/json')
    expect(opciones.body).toBe('{"correo":"a@b.com","password":"Clave123"}')
  })

  it('envía un formulario sin fijar el tipo de contenido', async () => {
    // Preparar
    const formulario = new FormData()
    formulario.append('archivo', new Blob(['hola']), 'nota.txt')
    fetchFalso.mockResolvedValue(respuesta(201, {}))

    // Actuar
    await pedir('/documentos', { metodo: 'POST', cuerpo: formulario })

    // Verificar
    const { opciones, cabeceras } = peticionEnviada()
    expect(opciones.body).toBe(formulario)
    expect(cabeceras.has('Content-Type')).toBe(false)
  })

  it('una respuesta sin cuerpo devuelve undefined', async () => {
    // Preparar
    fetchFalso.mockResolvedValue(respuesta(204))

    // Actuar
    const datos = await pedir('/algo', { metodo: 'DELETE' })

    // Verificar
    expect(datos).toBeUndefined()
  })

  it('un error con {"mensaje"} se convierte en ErrorApi con ese mensaje y el estado', async () => {
    // Preparar
    fetchFalso.mockResolvedValue(
      respuesta(409, { mensaje: 'La orden no puede pasar a Entregada.' }),
    )

    // Actuar
    const error = await capturarError(pedir('/taller/ordenes/1/entregar', { metodo: 'POST' }))

    // Verificar
    expect(error.estado).toBe(409)
    expect(error.message).toBe('La orden no puede pasar a Entregada.')
  })

  it('un error sin {"mensaje"} usa un mensaje genérico con el estado', async () => {
    // Preparar
    fetchFalso.mockResolvedValue(respuesta(500, '<html>Error</html>'))

    // Actuar
    const error = await capturarError(pedir('/componentes'))

    // Verificar
    expect(error.estado).toBe(500)
    expect(error.message).toBe('El servidor respondió con un error inesperado (500).')
  })

  it.each([502, 503, 504])(
    'un %i sin {"mensaje"}, como el del proxy con la API apagada, informa que no hay conexión',
    async (estado) => {
      // Preparar
      fetchFalso.mockResolvedValue(respuesta(estado))

      // Actuar
      const error = await capturarError(pedir('/componentes'))

      // Verificar
      expect(error.estado).toBe(estado)
      expect(error.message).toBe(MENSAJE_SIN_CONEXION)
    },
  )

  it('un 503 con {"mensaje"} de la API conserva su mensaje', async () => {
    // Preparar
    fetchFalso.mockResolvedValue(
      respuesta(503, { mensaje: 'El servicio de tasa de cambio no responde. Intenta más tarde.' }),
    )

    // Actuar
    const error = await capturarError(pedir('/ordenes/1/costo?moneda=DOP'))

    // Verificar
    expect(error.message).toBe('El servicio de tasa de cambio no responde. Intenta más tarde.')
  })

  it('si la API no responde, informa que no hay conexión', async () => {
    // Preparar
    fetchFalso.mockRejectedValue(new TypeError('Failed to fetch'))

    // Actuar
    const error = await capturarError(pedir('/componentes'))

    // Verificar
    expect(error.estado).toBe(0)
    expect(error.message).toBe(MENSAJE_SIN_CONEXION)
  })
})

// RF-CA-12, RF-CA-18
describe('sesión revocada', () => {
  it('un 401 con sesión borra el token y avisa a la aplicación', async () => {
    // Preparar
    guardarToken('token-revocado')
    const oyente = vi.fn()
    const dejarDeOir = alCerrarSesion(oyente)
    fetchFalso.mockResolvedValue(respuesta(401, { mensaje: 'Necesitas iniciar sesión.' }))

    // Actuar
    const error = await capturarError(pedir('/auth/yo'))

    // Verificar
    expect(error.estado).toBe(401)
    expect(leerToken()).toBeNull()
    expect(oyente).toHaveBeenCalledOnce()
    dejarDeOir()
  })

  it('un 401 sin sesión, como credenciales incorrectas, no avisa de cierre', async () => {
    // Preparar
    const oyente = vi.fn()
    const dejarDeOir = alCerrarSesion(oyente)
    fetchFalso.mockResolvedValue(respuesta(401, { mensaje: 'Credenciales incorrectas.' }))

    // Actuar
    const error = await capturarError(pedir('/auth/login', { metodo: 'POST', cuerpo: {} }))

    // Verificar
    expect(error.message).toBe('Credenciales incorrectas.')
    expect(oyente).not.toHaveBeenCalled()
    dejarDeOir()
  })
})

// RF-NEG-06
describe('pedirArchivo', () => {
  it('pide el archivo con el token y devuelve su contenido', async () => {
    // Preparar
    guardarToken('token-de-prueba')
    fetchFalso.mockResolvedValue(
      new Response('imagen', { status: 200, headers: { 'Content-Type': 'image/png' } }),
    )

    // Actuar
    const archivo = await pedirArchivo('/componentes/abc/imagen')

    // Verificar
    const { url, cabeceras } = peticionEnviada()
    expect(url).toBe('/api/componentes/abc/imagen')
    expect(cabeceras.get('Authorization')).toBe('Bearer token-de-prueba')
    expect(await archivo.text()).toBe('imagen')
  })

  it('un error de la API se convierte en ErrorApi y un 401 cierra la sesión', async () => {
    // Preparar
    guardarToken('token-revocado')
    fetchFalso.mockResolvedValue(respuesta(401, { mensaje: 'Necesitas iniciar sesión.' }))

    // Actuar
    const error = await capturarError(pedirArchivo('/componentes/abc/imagen'))

    // Verificar
    expect(error.estado).toBe(401)
    expect(error.message).toBe('Necesitas iniciar sesión.')
    expect(leerToken()).toBeNull()
  })
})
