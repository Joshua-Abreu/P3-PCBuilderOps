import { reenviarActivacion, solicitarRecuperacion } from '../api/auth'
import { FormularioCorreo } from '../componentes/FormularioCorreo'

// RF-CA-17
export function ReenviarActivacion() {
  return (
    <FormularioCorreo
      titulo="Reenviar activación"
      descripcion="Te enviaremos un enlace nuevo si tu cuenta está pendiente de activar. El anterior deja de servir."
      textoBoton="Reenviar enlace"
      enviarCorreo={reenviarActivacion}
    />
  )
}

// RF-CA-09
export function OlvidePassword() {
  return (
    <FormularioCorreo
      titulo="Recuperar contraseña"
      descripcion="Escribe tu correo y te enviaremos un enlace para crear una contraseña nueva."
      textoBoton="Enviar enlace"
      enviarCorreo={solicitarRecuperacion}
    />
  )
}
