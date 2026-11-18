import { ProblemDetails } from '../models/problem-details.model';

/**
 * Extrae el mensaje de error de una respuesta de la API (.NET / ProblemDetails RFC 7807)
 */
export function getApiErrorMessage(err: any, mensajePorDefecto: string = 'Ocurrió un error en el servidor'): string {
    if (!err) return mensajePorDefecto;

    const problem = err.error as ProblemDetails | string | undefined;
    if (problem) {
        if (typeof problem === 'string') {
            return problem;
        }
        if (problem.detail) {
            return problem.detail;
        }
        if (problem.title) {
            return problem.title;
        }
    }

    if (err.error?.mensaje) {
        return err.error.mensaje;
    }
    if (err.error?.message) {
        return err.error.message;
    }

    // Errores de validación ModelState de ASP.NET Core
    if (err.error?.errors && typeof err.error.errors === 'object') {
        const keys = Object.keys(err.error.errors);
        if (keys.length > 0 && Array.isArray(err.error.errors[keys[0]])) {
            return err.error.errors[keys[0]][0];
        }
    }

    if (err.status === 401) {
        return 'Sesión expirada o no autorizada. Por favor inicie sesión nuevamente.';
    }
    if (err.status === 403) {
        return 'No tiene los permisos requeridos para realizar esta acción.';
    }
    if (err.status === 404) {
        return 'El recurso solicitado no fue encontrado.';
    }
    if (err.status === 0) {
        return 'No se pudo conectar con el servidor. Verifique su conexión de red.';
    }

    return err.message || mensajePorDefecto;
}
