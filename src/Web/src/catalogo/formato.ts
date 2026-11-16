const dolares = new Intl.NumberFormat('es-DO', { style: 'currency', currency: 'USD' })

export const formatearPrecio = (valor: number) => dolares.format(valor)
