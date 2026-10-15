namespace Core.Auth.Services;

public interface IInputValidator
{
    void ValidarNombre(string nombre);
    void ValidarCorreo(string correo);
    void ValidarPassword(string password);
}
