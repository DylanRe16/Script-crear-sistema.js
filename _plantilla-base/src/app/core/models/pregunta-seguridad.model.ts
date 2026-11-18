/**
 * DTOs de configuración de preguntas de seguridad.
 */

/** Pregunta del catálogo maestro de preguntas de seguridad */
export interface PgPreguntaSeguridadResponseDto {
    idPreguntaSeg: number;
    sdescripcion: string;
    benabled: boolean;
    dfechaCreacion: string;
}

/** Elemento individual para asignar una respuesta a una pregunta de seguridad */
export interface PgAsignarPreguntaItemDto {
    idPreguntaSeg: number;
    respuesta: string;
}

/** Lote de preguntas de seguridad del funcionario (máximo 6) */
export interface PgAsignarPreguntasSeguridadDto {
    preguntas: PgAsignarPreguntaItemDto[];
}

/**
 * Pregunta configurada por el funcionario.
 * Por seguridad el backend nunca expone la respuesta ni su hash.
 */
export interface PgFuncionarioPreguntaSeguridadResponseDto {
    idFpseguridad: number;
    idPreguntaSeg: number;
    sdescripcionPregunta: string;
    dfechaReg?: string | null;
}

/** Estado de cumplimiento de preguntas de seguridad del funcionario autenticado */
export interface PgEstadoPreguntasSeguridadDto {
    tienePreguntasConfiguradas: boolean;
    totalConfiguradas: number;
    maximoPermitido: number;
    requiereConfiguracion: boolean;
    mensaje: string;
}

