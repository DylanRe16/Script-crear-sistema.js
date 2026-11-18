import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { catchError, map } from 'rxjs/operators';
import { environment } from '../../../environments/environment';
import { PgRolResponseDto } from '../models/rol.model';

@Injectable({
    providedIn: 'root'
})
export class RolesService {
    private readonly baseUrl = environment.apiUrl;

    constructor(private http: HttpClient) {}

    /**
     * GET /Roles
     * Lista todos los roles institucionales definidos en el sistema
     */
    listar(): Observable<PgRolResponseDto[]> {
        return this.http.get<PgRolResponseDto[]>(`${this.baseUrl}/Roles`).pipe(
            map(roles => (roles || []).map(r => ({
                ...r,
                idRol: r.idRol !== undefined && r.idRol !== null ? Number(r.idRol) : undefined
            }))),
            catchError(err => {
                console.error('Error al listar roles:', err);
                return throwError(() => err);
            })
        );
    }
}

