# Plantilla de autenticación

Repositorio para generar sistemas con frontend Angular y backend ASP.NET Core/.NET, autenticación, perfil y administración básica de cuentas.

## Contenido

| Ruta | Descripción |
|---|---|
| [`crear-sistema.js`](./crear-sistema.js) | Generador interactivo de proyectos |
| [`crear-sistema.test.js`](./crear-sistema.test.js) | Pruebas del generador |
| [`docs/crear-sistema.md`](./docs/crear-sistema.md) | Requisitos, parámetros, salidas y solución de problemas |
| [`_plantilla-base/`](./_plantilla-base/) | Aplicación Angular/PrimeNG basada en Sakai |
| [`_plantilla-backend/`](./_plantilla-backend/) | API .NET y base PostgreSQL |
| [`_plantilla-backend/TestProject/README.md`](./_plantilla-backend/TestProject/README.md) | Configuración, ejecución y endpoints del backend |
| [`_plantilla-base/README-SESIONES.md`](./_plantilla-base/README-SESIONES.md) | Administración de usuarios y roles |

## Requisitos

- Node.js y npm compatibles con la versión indicada en `_plantilla-base/package.json`.
- .NET SDK 10 para generar y compilar el backend.
- PostgreSQL y `psql` para inicializar la base de datos.

## Generar un sistema

Desde la raíz del repositorio:

```powershell
node .\crear-sistema.js
```

El generador pregunta por el nombre, backend, host y PathBase; si se genera backend, también pregunta por PostgreSQL. Crea el resultado bajo `sistemas/`, configura `/api` en el proxy, genera una clave JWT y una contraseña temporal de restablecimiento, restaura/compila el backend e instala/compila el frontend.

Lee primero la [documentación del generador](./docs/crear-sistema.md), especialmente las notas sobre `.env` y credenciales.

## Desarrollo de las plantillas

Frontend:

```powershell
Set-Location .\_plantilla-base
npm ci
npm start
```

Backend: sigue los pasos y variables descritos en [`_plantilla-backend/TestProject/README.md`](./_plantilla-backend/TestProject/README.md).

Pruebas del generador:

```powershell
node --test .\crear-sistema.test.js
```

## Flujo de API

Los clientes Angular usan rutas relativas `/api/...`. El servidor de desarrollo reescribe `/api` al PathBase configurado para el backend; por ejemplo, `/api/auth/login` se envía a `/ventas/api/auth/login`. En despliegue, configura el proxy inverso y el PathBase del backend de forma equivalente.

## Seguridad y archivos locales

- No guardes contraseñas ni claves reales en archivos rastreados.
- El generador no copia `.env`, bases de datos locales ni salidas de compilación de las plantillas.
- El `.env` creado dentro del backend generado es local e ignorado por Git; conserva `.env.example` para documentar las variables necesarias.
- Cambia las credenciales de desarrollo antes de cualquier despliegue.
