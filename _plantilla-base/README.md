# Frontend de autenticación

Aplicación Angular con PrimeNG y layout Sakai para autenticación, perfil, preguntas de seguridad y administración de usuarios y roles.

## Funcionalidades y rutas

- Login, registro, cierre de sesión y recuperación de contraseña.
- Perfil, cambio de contraseña y configuración obligatoria de preguntas de seguridad.
- Dashboard básico, sin widgets ni peticiones de dominios de negocio.
- Administración protegida de usuarios y roles. Un administrador no puede modificar su propia cuenta; puede solicitar el restablecimiento de contraseña o preguntas de otra cuenta.
- Rutas bajo `/app`: `/app/login`, `/app/dashboard`, `/app/profile`, `/app/change-password` y vistas administrativas.

El registro no es público: una cuenta Administrador crea usuarios desde **Usuarios y roles**.

## Requisitos y ejecución

Usa una versión de Node compatible con `package.json` y npm.

```powershell
npm ci
npm start
```

El servidor de desarrollo se inicia con `--proxy-config proxy.conf.json`.

## Proxy y API

Los servicios HTTP deben usar la base relativa `/api` definida en `src/environments/environment.ts`; no incluyas host, esquema ni PathBase en las rutas de los servicios.

El proxy local tiene la clave `/api`, un `target` de backend y reescribe el prefijo con `pathRewrite`. En la plantilla se usa `/PlantillaBackend/api`. Por ejemplo, `POST /api/auth/login` se reenvía como `POST /PlantillaBackend/api/auth/login`.

El generador actualiza el origen y PathBase del proxy según las respuestas del asistente. En producción, configura el proxy inverso del servidor web para enrutar las llamadas `/api` al PathBase real del backend.

## Verificación

```powershell
npm test -- --watch=false
npm run build
```

## Estructura relevante

- `src/app/core/`: guards, interceptors y servicios compartidos.
- `src/app/features/`: autenticación, dashboard, perfil, seguridad y administración.
- `src/environments/environment.ts`: configuración de API relativa.
- `proxy.conf.json`: proxy de desarrollo.

Para flujos de administración consulta [README-SESIONES.md](./README-SESIONES.md). La guía del backend, sus variables y endpoints se encuentra en `TestProject/README.md` dentro de la plantilla backend.
