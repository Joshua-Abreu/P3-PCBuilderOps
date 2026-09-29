namespace Core.Auth.Services;

public interface IInputValidator
{
    void ValidarCorreo(string correo);
    void ValidarPassword(string password);
}
