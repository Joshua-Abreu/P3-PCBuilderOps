## Qué cambia
Añade al README la sección “Cómo ejecutar el proyecto”, con pasos para clonar, restaurar dependencias, configurar SQL Server, aplicar migraciones y ejecutar la API. Actualiza `.gitignore` para excluir archivos de configuración sensibles, como `appsettings.Development.json`, `appsettings.*.local.json`, `appsettings.Production.json`, `secrets.json` y archivos `.env`.

## Por qué
Documenta cómo preparar y ejecutar el proyecto, y evita que configuraciones locales o con secretos se incluyan accidentalmente en Git.

## Cómo probarlo
Revisar la nueva sección del README y confirmar que `.gitignore` contiene los patrones de configuración sensible. Verificar, por ejemplo, con `git check-ignore -v appsettings.Development.json` y `git check-ignore -v .env.local`. El repositorio aún no contiene archivos `.sln` o `.csproj`, así que los comandos de .NET podrán ejecutarse cuando se agregue el proyecto.

## Qué NO incluye
No modifica código, la configuración de ejecución de la aplicación ni la base de datos; solo documenta la ejecución en el README y define exclusiones de archivos en `.gitignore`.
