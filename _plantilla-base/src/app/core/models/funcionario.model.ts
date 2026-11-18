/**
 * DTOs de autenticación y administración de cuentas.
 */

export interface PgFuncionarioResponseDto {
    idFuncionario?: number;
    snacionalidad?: string;
    ncedulaIdent?: number;
    cedulaCompleta?: string;
    sprimerNombre?: string;
    ssegundoNombre?: string | null;
    sprimerApellido?: string;
    ssegundoApellido?: string | null;
    nombreCompleto?: string;
    rolId?: number;
    nombreRol?: string;
    benabled?: boolean;
    dfechaCreacion?: string;
}

export interface RegisterFuncionarioDto {
    snacionalidad: string;
    ncedulaIdent: number;
    sprimerNombre: string;
    ssegundoNombre?: string | null;
    sprimerApellido: string;
    ssegundoApellido?: string | null;
    password: string;
    confirmPassword: string;
    rolId: number;
}

export interface PgFuncionarioUpdateDto {
    sprimerNombre: string;
    ssegundoNombre?: string | null;
    sprimerApellido: string;
    ssegundoApellido?: string | null;
    rolId: number;
    benabled: boolean;
}
