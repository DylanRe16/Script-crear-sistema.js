# Administración de usuarios y roles

La vista administrativa reutiliza los componentes ubicados en `src/app/features/sesiones`. El nombre del directorio se conserva, pero la funcionalidad se limita a crear cuentas, consultar usuarios, cambiar datos/rol y activar o desactivar cuentas. No consulta salas ni administra sesiones operativas.

El alta está disponible únicamente a Administradores. El frontend protege la ruta y el backend vuelve a validar el rol; no se debe considerar la protección de rutas del navegador como autorización.

## Endpoints administrativos

Todos requieren un token Bearer de un Administrador:

- `GET /Roles`: roles activos que se pueden asignar.
- `GET /Funcionarios?soloActivos=false`: listado de cuentas.
- `GET /Funcionarios/{id}`: detalle de una cuenta.
- `PUT /Funcionarios/{id}`: actualiza nombre, apellidos, rol y estado.
- `POST /Auth/register`: crea una cuenta con el rol seleccionado.

El alta administrativa no inicia sesión como la cuenta recién creada ni devuelve un token para ella. El nuevo usuario debe iniciar sesión y completar la configuración requerida de preguntas de seguridad.

Los ejemplos para probar autorización y payloads están en `TestProject.http` dentro del backend plantilla. El PathBase del ejemplo puede ajustarse según la configuración del backend.
