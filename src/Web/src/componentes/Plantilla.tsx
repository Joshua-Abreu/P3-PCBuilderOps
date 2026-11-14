import { Outlet } from 'react-router'
import { Cabecera } from './Cabecera'
import { Pie } from './Pie'

export function Plantilla() {
  return (
    <div className="flex min-h-screen flex-col">
      <Cabecera />
      <main className="mx-auto w-full max-w-6xl flex-1 px-4 py-10">
        <Outlet />
      </main>
      <Pie />
    </div>
  )
}
