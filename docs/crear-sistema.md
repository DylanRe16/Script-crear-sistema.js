# Generador `crear-sistema.js`

El generador copia la plantilla Angular y, opcionalmente, la API .NET, aplica el nombre y la configuración específicos del sistema, y valida la compilación. No sobrescribe una salida existente.

## Requisitos

- Node.js instalado y `npm` disponible en PATH.
- Angular CLI y dependencias se resuelven desde el `package-lock.json` de la plantilla.
- Para generar el backend: .NET SDK 10 disponible con el comando `dotnet`.
- PostgreSQL y `psql` son necesarios para inicializar y ejecutar la aplicación; el generador no crea la base de datos ni ejecuta `init.sql`.

## Ejecución

Desde la raíz:

```powershell
node .\crear-sistema.js
```

El nombre debe ser un slug de letras minúsculas, números y guiones, debe comenzar con letra y tiene un máximo de 54 caracteres. El generador lo utiliza para el nombre del proyecto y la carpeta de frontend, y lo muestra con espacios y mayúsculas iniciales en la interfaz (por ejemplo, `sistema-ventas` se muestra como **Sistema Ventas**). La carpeta de backend se llama `<nombre>-api`.

Preguntas del asistente:

1. Nombre del sistema.
2. Color primario de Sakai (azul por defecto; también verde, esmeralda, turquesa, cian, índigo, violeta, morado, rosa, rojo o naranja).
3. Si se debe generar el backend.
4. Origen del backend para el proxy, por defecto `http://localhost:5136`.
5. Si se genera backend: esquema/base de datos, host, puerto, usuario, contraseña y PathBase.
6. Si solo se genera frontend: PathBase del backend existente.

La contraseña de PostgreSQL se enmascara en terminales interactivas. Si se ejecuta sin una terminal interactiva, la entrada estándar no puede ocultarse. La contraseña nunca se escribe en la salida de consola; se guarda en el `.env` local generado.

## Salida y configuración

Por defecto, crea:

```text
sistemas/
  <nombre>/
  <nombre>-api/        (solo si se solicita backend)
```

En el frontend:

- Actualiza el nombre del paquete y el identificador de proyecto Angular.
- Reemplaza el nombre visible «Portal» por el nombre del sistema y aplica el color Sakai elegido a PrimeNG y los estilos globales.
- Actualiza las claves `localStorage` que usan el prefijo `sgvti_`.
- Configura `proxy.conf.json` o `proxy.config.json` con la regla `/api`, `target`, `secure: false`, `changeOrigin: true`, `pathRewrite` y `logLevel: debug`.
- Asegura que `npm start` use `--proxy-config`.
- Ejecuta `npm ci` (o `npm install` si la plantilla no tiene lockfile) y `npm run build`.

En el backend:

- Copia la plantilla excluyendo secretos `.env`, bases de datos y directorios de compilación/publicación.
- Reemplaza el esquema de ejemplo, actualiza `AssemblyName`/`RootNamespace` y renombra el `.csproj`.
- Configura `PathBase` en los archivos `appsettings*.json`.
- Elimina las cadenas de conexión y la clave JWT de `appsettings*.json` para no propagar secretos de desarrollo.
- Crea `TestProject/.env` desde `.env.example`, con cadena de conexión, `PathBase`, clave JWT aleatoria y contraseña temporal aleatoria para restablecimientos.
- Ejecuta `dotnet restore` y `dotnet build`.

Si una instalación, restauración o compilación falla, el generador informa el error, termina con código de salida distinto de cero y no muestra un mensaje de éxito.

### PathBase y proxy

Para un sistema llamado `ventas` con PathBase `/VentasBackend/api`:

```json
{
  "/api": {
    "target": "http://localhost:5136",
    "secure": false,
    "changeOrigin": true,
    "pathRewrite": {
      "^/api": "/VentasBackend/api"
    },
    "logLevel": "debug"
  }
}
```

El frontend envía `POST /api/auth/login`; el proxy lo reescribe a `POST /VentasBackend/api/auth/login`. El backend debe tener el mismo `PathBase`. En la plantilla, el PathBase predeterminado es `/<esquema>/api`.

### Inicialización y ejecución del backend

Después de generar el sistema, crea la base de datos PostgreSQL y ejecuta `init.sql` desde la carpeta backend que imprime el generador. El script no ejecuta DDL automáticamente. Después, ejecuta `dotnet run` desde la carpeta `TestProject` para que la aplicación pueda leer el `.env` local:

```powershell
Set-Location .\sistemas\<nombre>-api\TestProject
dotnet run --project .\<nombre>.Api.csproj
```

La cuenta semilla de desarrollo usa `V-12345678` y `Admin123!`; cámbiala antes de desplegar. La contraseña temporal de restablecimiento se genera por sistema y solo queda en `TestProject/.env`.

### Variables para rutas personalizadas

Se pueden definir antes de ejecutar el script:

| Variable | Predeterminado | Uso |
|---|---|---|
| `PLANTILLA_FRONT` | `.\_plantilla-base` | Ruta absoluta o relativa a la plantilla Angular |
| `PLANTILLA_BACK` | `.\_plantilla-backend` | Ruta a la plantilla .NET |
| `SISTEMAS_DESTINO` | `.\sistemas` | Directorio donde se crean las salidas |

## Pruebas

Pruebas unitarias del generador:

```powershell
node --test .\crear-sistema.test.js
```

Una ejecución real realiza además los builds del frontend y, si aplica, del backend. Para probarla sin tocar las plantillas, apunta `SISTEMAS_DESTINO` a un directorio temporal que no exista.

## Solución de problemas

| Mensaje/síntoma | Revisión |
|---|---|
| No se puede leer JSON | Verifica que `package.json`, `angular.json` y el archivo de proxy sean JSON válidos |
| No encuentra `.csproj` o encuentra varios | La plantilla debe contener exactamente un proyecto .NET |
| Error al conectar con PostgreSQL | Comprueba host, puerto, base de datos, usuario, contraseña y que ya ejecutaste `init.sql` |
| El backend no carga variables | Inícialo con el directorio de trabajo en `TestProject`, donde vive el `.env` |
| Login devuelve 404 | Confirma que el proxy apunta al host y PathBase usados por el backend y que ambos usan `/api` |
| Falla `npm ci` | Confirma que `package.json` y `package-lock.json` de la plantilla están sincronizados |
| Falla `dotnet restore/build` | Comprueba SDK, red/feeds NuGet y errores reportados por MSBuild |
