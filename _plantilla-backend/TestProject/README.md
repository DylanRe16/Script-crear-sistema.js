# API de autenticación

Plantilla reutilizable de API ASP.NET Core (.NET 10), con separación en capas, Entity Framework Core y PostgreSQL. El dominio incluye autenticación, perfil, cambio/recuperación de contraseña, preguntas de seguridad y administración de usuarios/roles.

## Requisitos

- .NET 10 SDK.
- PostgreSQL.
- `psql` para inicializar manualmente la base de datos.

## Base de datos

Desde la carpeta `TestProject`, ejecute el script contra una base PostgreSQL vacía:

```powershell
psql -d plantilla_db -f ..\init.sql
```

El script crea el esquema `"PlantillaBackend"`, las tablas, sus relaciones e índices, y los roles y preguntas iniciales. La aplicación no ejecuta `EnsureCreated`, migraciones ni DDL. La estructura de la base debe prepararse fuera de la aplicación con `init.sql`.

Configure la cadena de conexión, la clave JWT, `PathBase` y la contraseña temporal de restablecimiento mediante variables de entorno. Para desarrollo local, copie `.env.example` como `.env` y cambie los valores:

```powershell
Copy-Item .env.example .env
```

No agregue `.env` al control de versiones. En producción, use un almacén de secretos o variables de entorno administradas por el entorno de ejecución.

La aplicación busca `.env` en el directorio de trabajo actual. Ejecute los comandos desde `TestProject`:

```powershell
dotnet restore
dotnet build
dotnet run
```

El script generador quita las cadenas de conexión y la clave JWT de `appsettings*.json` del backend generado; el backend requiere configuración externa.

## Usuario semilla

Al iniciar, la aplicación comprueba si existe `V-12345678`. Si no existe, crea la cuenta vinculada al rol activo `Administrador` y hashea la contraseña `Admin123!` con `PasswordHasher<Funcionario>` de ASP.NET Core Identity.

Credenciales iniciales de desarrollo:

- Usuario: `V-12345678`
- Contraseña: `Admin123!`

Cámbielas antes de desplegar y no use esta cuenta semilla en producción.

## Compilar y ejecutar

El perfil HTTP usa el puerto `5136`. La ruta base predeterminada es `/PlantillaBackend/api`. El frontend utiliza `/api`; durante desarrollo, el proxy reescribe esa ruta al PathBase configurado.

## Endpoints principales

| Método | Ruta | Uso |
|---|---|---|
| POST | `/auth/register` | Registrar una cuenta |
| POST | `/auth/login` | Iniciar sesión con nacionalidad, cédula y contraseña |
| GET | `/profile/me` | Consultar el perfil autenticado |
| PUT | `/profile/change-password` | Cambiar la contraseña |
| POST | `/auth/recuperacion/preguntas` | Iniciar recuperación |
| POST | `/auth/recuperacion/validar` | Validar respuestas de seguridad |
| POST | `/auth/recuperacion/restablecer` | Restablecer contraseña |
| GET | `/preguntasseguridad` | Consultar el catálogo de preguntas |
| GET/POST | `/seguridad/mis-preguntas` | Consultar o configurar preguntas propias |
| POST | `/funcionarios/{id}/restablecer-contrasena` | Restablecer contraseña con el valor temporal del backend |
| POST | `/funcionarios/{id}/restablecer-preguntas-seguridad` | Eliminar las preguntas configuradas para obligar a registrarlas de nuevo |

Las rutas de administración requieren rol Administrador y no permiten operar sobre la propia cuenta. El inicio de sesión informa si debe cambiarse la contraseña temporal; su valor permanece configurado en el backend.

Ejemplo de login:

```json
{
  "snacionalidad": "V",
  "ncedulaIdent": 12345678,
  "password": "Admin123!"
}
```

El `PathBase` se puede cambiar con la configuración `PathBase`. OpenAPI, Scalar y Swagger están disponibles debajo de la ruta base.
