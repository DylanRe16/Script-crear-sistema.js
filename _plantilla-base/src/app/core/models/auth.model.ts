/**
 * Modelos y DTOs para Autenticación y Perfil
 * Alineados a los endpoints de autenticación y perfil.
 */
import { PgFuncionarioResponseDto } from './funcionario.model';

export interface LoginDto {
    snacionalidad?: string; // 'V', 'E' o 'J'
    ncedulaIdent: number;
    password: string;
}

export interface AuthResponseDto {
    success?: boolean;
    mensaje?: string;
    token?: string;
    tokenType?: string;
    expiresInSeconds?: number | string;
    funcionario?: PgFuncionarioResponseDto;
    /** Indica que el funcionario debe configurar sus preguntas de seguridad */
    requiereConfigurarPreguntasSeguridad?: boolean;
    /** Indica que el funcionario debe cambiar la contraseña temporal */
    requiereCambiarContrasena?: boolean;
    /** Cantidad de preguntas de seguridad configuradas al momento del login */
    totalPreguntasSeguridadConfiguradas?: number;
}

export interface ChangePasswordDto {
    funcionarioId?: number | string | null;
    currentPassword: string;
    newPassword: string;
    confirmNewPassword: string;
}

/** Usuario recordado en el navegador para autocompletar el login */
export interface UsuarioRecordado {
    snacionalidad: string;
    ncedulaIdent: number;
}

// =========================================================================
// Recuperación de contraseña mediante preguntas de seguridad
// =========================================================================

export interface SolicitarRecuperacionDto {
    snacionalidad: string;
    ncedulaIdent: number;
}

export interface PreguntaRecuperacionDto {
    idPreguntaSeg: number;
    sdescripcion: string;
}

export interface PreguntasRecuperacionResponseDto {
    tienePreguntasConfiguradas: boolean;
    totalPreguntas: number;
    preguntas: PreguntaRecuperacionDto[];
    mensaje: string;
}

export interface RespuestaRecuperacionItemDto {
    idPreguntaSeg: number;
    respuesta: string;
}

export interface ValidarRespuestasRecuperacionDto extends SolicitarRecuperacionDto {
    respuestas: RespuestaRecuperacionItemDto[];
}

export interface ValidacionRespuestasResponseDto {
    success: boolean;
    mensaje: string;
    tokenRecuperacion: string;
    expiraEnSegundos: number;
}

export interface RestablecerPasswordRecuperacionDto {
    tokenRecuperacion: string;
    newPassword: string;
    confirmNewPassword: string;
}

export interface RecuperacionPasswordResultDto {
    success: boolean;
    mensaje: string;
}

