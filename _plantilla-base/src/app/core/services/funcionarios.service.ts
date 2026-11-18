import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { environment } from '../../../environments/environment';
import { PgFuncionarioResponseDto, PgFuncionarioUpdateDto } from '../models/funcionario.model';

@Injectable({
    providedIn: 'root'
})
export class FuncionariosService {
    private readonly baseUrl = environment.apiUrl;

    constructor(private http: HttpClient) {}

    listar(soloActivos = false): Observable<PgFuncionarioResponseDto[]> {
        const params = new HttpParams().set('soloActivos', soloActivos.toString());
        return this.http
            .get<PgFuncionarioResponseDto[]>(`${this.baseUrl}/Funcionarios`, { params })
            .pipe(map((lista) => (lista || []).map((funcionario) => this.normalizarFuncionario(funcionario))));
    }

    obtener(id: number): Observable<PgFuncionarioResponseDto> {
        return this.http
            .get<PgFuncionarioResponseDto>(`${this.baseUrl}/Funcionarios/${id}`)
            .pipe(map((funcionario) => this.normalizarFuncionario(funcionario)));
    }

    actualizar(id: number, dto: PgFuncionarioUpdateDto): Observable<PgFuncionarioResponseDto> {
        return this.http
            .put<PgFuncionarioResponseDto>(`${this.baseUrl}/Funcionarios/${id}`, dto)
            .pipe(map((funcionario) => this.normalizarFuncionario(funcionario)));
    }

    restablecerContrasena(id: number): Observable<void> {
        return this.http.post<void>(`${this.baseUrl}/Funcionarios/${id}/restablecer-contrasena`, null);
    }

    restablecerPreguntasSeguridad(id: number): Observable<void> {
        return this.http.post<void>(`${this.baseUrl}/Funcionarios/${id}/restablecer-preguntas-seguridad`, null);
    }

    private normalizarFuncionario(funcionario: PgFuncionarioResponseDto): PgFuncionarioResponseDto {
        return {
            ...funcionario,
            idFuncionario: Number(funcionario.idFuncionario),
            ncedulaIdent: Number(funcionario.ncedulaIdent),
            rolId: Number(funcionario.rolId)
        };
    }
}
