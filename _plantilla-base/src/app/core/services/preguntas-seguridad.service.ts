import { Injectable, signal } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { catchError, map, tap } from 'rxjs/operators';
import { environment } from '../../../environments/environment';
import {
    PgAsignarPreguntasSeguridadDto,
    PgEstadoPreguntasSeguridadDto,
    PgFuncionarioPreguntaSeguridadResponseDto,
    PgPreguntaSeguridadResponseDto
} from '../models/pregunta-seguridad.model';

@Injectable({
    providedIn: 'root'
})
export class PreguntasSeguridadService {
    private readonly baseUrl = environment.apiUrl;

    readonly catalogo = signal<PgPreguntaSeguridadResponseDto[]>([]);
    readonly misPreguntas = signal<PgFuncionarioPreguntaSeguridadResponseDto[]>([]);
    readonly estado = signal<PgEstadoPreguntasSeguridadDto | null>(null);

    constructor(private http: HttpClient) {}

    listarCatalogo(soloActivas = true): Observable<PgPreguntaSeguridadResponseDto[]> {
        const params = new HttpParams().set('soloActivas', soloActivas.toString());
        return this.http.get<PgPreguntaSeguridadResponseDto[]>(`${this.baseUrl}/PreguntasSeguridad`, { params }).pipe(
            map((lista) => (lista || []).map((pregunta) => this.normalizar(pregunta))),
            tap((lista) => this.catalogo.set(lista)),
            catchError((error) => {
                console.error('Error al listar el catálogo de preguntas de seguridad:', error);
                return throwError(() => error);
            })
        );
    }

    obtenerEstado(): Observable<PgEstadoPreguntasSeguridadDto> {
        return this.http.get<PgEstadoPreguntasSeguridadDto>(`${this.baseUrl}/Seguridad/estado`).pipe(
            tap((estado) => this.estado.set(estado)),
            catchError((error) => {
                console.error('Error al consultar el estado de preguntas de seguridad:', error);
                return throwError(() => error);
            })
        );
    }

    obtenerMisPreguntas(): Observable<PgFuncionarioPreguntaSeguridadResponseDto[]> {
        return this.http.get<PgFuncionarioPreguntaSeguridadResponseDto[]>(`${this.baseUrl}/Seguridad/mis-preguntas`).pipe(
            map((lista) => lista || []),
            tap((lista) => this.misPreguntas.set(lista)),
            catchError((error) => {
                console.error('Error al listar las preguntas de seguridad del usuario:', error);
                return throwError(() => error);
            })
        );
    }

    asignarMisPreguntas(dto: PgAsignarPreguntasSeguridadDto): Observable<PgEstadoPreguntasSeguridadDto> {
        return this.http
            .post<PgEstadoPreguntasSeguridadDto>(`${this.baseUrl}/Seguridad/mis-preguntas`, dto)
            .pipe(tap((estado) => this.estado.set(estado)));
    }

    limpiarEstado(): void {
        this.catalogo.set([]);
        this.misPreguntas.set([]);
        this.estado.set(null);
    }

    private normalizar(pregunta: PgPreguntaSeguridadResponseDto): PgPreguntaSeguridadResponseDto {
        return { ...pregunta, idPreguntaSeg: Number(pregunta.idPreguntaSeg) };
    }
}
