# Plantilla backend de autenticación

API ASP.NET Core/.NET con persistencia PostgreSQL, organizada en capas de dominio, casos de uso, persistencia y Web API. El proyecto de ejecución está en [`TestProject/`](./TestProject/).

## Componentes

- Autenticación: registro, inicio/cierre de sesión, JWT y recuperación.
- Perfil: datos del usuario y cambio de contraseña.
- Preguntas de seguridad.
- Administración protegida de usuarios y roles.
- PostgreSQL y script de inicialización `init.sql`.
- PathBase de ejemplo `/PlantillaBackend/api`.

## Inicio rápido

1. Instala .NET SDK 10, PostgreSQL y `psql`.
2. Crea una base de datos y ejecuta `init.sql`.
3. Configura `.env` en `TestProject` con los valores de `.env.example`.
4. Desde `TestProject`, ejecuta:

```powershell
dotnet restore
dotnet run
```

La guía de variables, la cuenta semilla, ejecución, endpoints y API HTTP está en [`TestProject/README.md`](./TestProject/README.md). El generador completa el PathBase, el esquema y el `.env` de cada sistema; no copies credenciales reales al repositorio.
