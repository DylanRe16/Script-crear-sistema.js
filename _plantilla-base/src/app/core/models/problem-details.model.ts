/**
 * Representación estándar RFC 7807 de ProblemDetails devuelto por ASP.NET Core
 */
export interface ProblemDetails {
    type?: string | null;
    title?: string | null;
    status?: number | string | null;
    detail?: string | null;
    instance?: string | null;
    errors?: { [key: string]: string[] } | null;
}
