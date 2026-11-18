import { Injectable, signal, computed } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { Observable, tap } from 'rxjs';
import { environment } from '../../../environments/environment';
import { AuthResponseDto, ChangePasswordDto, LoginDto, PreguntasRecuperacionResponseDto, RecuperacionPasswordResultDto, RestablecerPasswordRecuperacionDto, SolicitarRecuperacionDto, UsuarioRecordado, ValidacionRespuestasResponseDto, ValidarRespuestasRecuperacionDto } from '../models/auth.model';
import { PgFuncionarioResponseDto, RegisterFuncionarioDto } from '../models/funcionario.model';

const STORAGE_KEY_TOKEN = 'sgvti_token';
const STORAGE_KEY_SESION = 'sgvti_sesion_actual';
const STORAGE_KEY_PASSWORD_CHANGE_REQUIRED = 'sgvti_cambio_contrasena_requerido';
const STORAGE_KEY_SECURITY_QUESTIONS_REQUIRED = 'sgvti_preguntas_seguridad_requeridas';
const STORAGE_KEY_REMEMBERED_USER = 'sgvti_usuario_recordado';

/**
 * Estado de autenticación de la aplicación.
 * No contiene datos de demostración: la sesión solo existe si el backend
 * entregó un token válido.
 */
@Injectable({
    providedIn: 'root'
})
export class AuthService {
    private readonly baseUrl = environment.apiUrl;

    readonly token = signal<string | null>(this.cargarTokenInicial());
    readonly currentUser = signal<PgFuncionarioResponseDto | null>(this.cargarSesionInicial());
    readonly mustChangePassword = signal<boolean>(this.cargarCambioContrasenaRequerido());
    readonly mustConfigureSecurityQuestions = signal<boolean>(this.cargarPreguntasSeguridadRequeridas());
    readonly rememberedUser = signal<UsuarioRecordado | null>(this.cargarUsuarioRecordado());

    readonly isAuthenticated = computed(() => !!this.token() && !!this.currentUser());
    readonly userRole = computed(() => this.currentUser()?.nombreRol || null);
    readonly isAdmin = computed(() => this.normalizarRol(this.userRole()) === 'administrador');
    readonly idFuncionario = computed(() => this.currentUser()?.idFuncionario || null);

    constructor(
        private http: HttpClient,
        private router: Router
    ) {}

    /**
     * POST /Auth/register
     */
    register(dto: RegisterFuncionarioDto): Observable<AuthResponseDto> {
        return this.http.post<AuthResponseDto>(`${this.baseUrl}/Auth/register`, dto);
    }

    /**
     * POST /Auth/login
     */
    login(dto: LoginDto): Observable<AuthResponseDto> {
        return this.http.post<AuthResponseDto>(`${this.baseUrl}/Auth/login`, dto).pipe(
            tap(res => {
                if (res?.token) {
                    this.establecerSesion(res.token, res.funcionario || null);
                    this.establecerCambioContrasenaRequerido(!!res.requiereCambiarContrasena);
                    this.establecerPreguntasSeguridadRequeridas(!!res.requiereConfigurarPreguntasSeguridad);
                }
            })
        );
    }

    /**
     * GET /Profile/me
     */
    getProfileMe(): Observable<PgFuncionarioResponseDto> {
        return this.http.get<PgFuncionarioResponseDto>(`${this.baseUrl}/Profile/me`).pipe(
            tap(funcionario => {
                this.currentUser.set(funcionario);
                this.guardarSesion(funcionario);
            })
        );
    }

    /**
     * PUT /Profile/change-password
     */
    changePassword(dto: ChangePasswordDto): Observable<void> {
        return this.http.put<void>(`${this.baseUrl}/Profile/change-password`, dto).pipe(
            tap(() => this.establecerCambioContrasenaRequerido(false))
        );
    }

    /**
     * Marca que el funcionario debe configurar sus preguntas de seguridad.
     * Lo invoca el interceptor cuando el backend bloquea una operación
     * con el código PREGUNTAS_SEGURIDAD_REQUERIDAS.
     */
    marcarPreguntasSeguridadPendientes(): void {
        this.establecerPreguntasSeguridadRequeridas(true);
    }

    /**
     * Marca la configuración de preguntas de seguridad como completada.
     */
    marcarPreguntasSeguridadConfiguradas(): void {
        this.establecerPreguntasSeguridadRequeridas(false);
    }

    /**
     * Guarda la cédula del usuario en el navegador para autocompletar el login
     * cuando el usuario marca "Recordarme".
     */
    recordarUsuario(dto: LoginDto): void {
        const usuario: UsuarioRecordado = {
            snacionalidad: (dto.snacionalidad || 'V').toUpperCase(),
            ncedulaIdent: Number(dto.ncedulaIdent)
        };

        this.rememberedUser.set(usuario);

        if (this.tieneStorage()) {
            localStorage.setItem(STORAGE_KEY_REMEMBERED_USER, JSON.stringify(usuario));
        }
    }

    /**
     * Elimina la cédula recordada del navegador.
     */
    olvidarUsuario(): void {
        this.rememberedUser.set(null);

        if (this.tieneStorage()) {
            localStorage.removeItem(STORAGE_KEY_REMEMBERED_USER);
        }
    }

    /**
     * POST /Auth/recuperacion/preguntas
     * Obtiene las preguntas de seguridad configuradas para iniciar la recuperación.
     */
    obtenerPreguntasRecuperacion(dto: SolicitarRecuperacionDto): Observable<PreguntasRecuperacionResponseDto> {
        return this.http.post<PreguntasRecuperacionResponseDto>(`${this.baseUrl}/Auth/recuperacion/preguntas`, dto);
    }

    /**
     * POST /Auth/recuperacion/validar
     * Valida las respuestas de las 2 preguntas aleatorias y devuelve un token temporal.
     */
    validarRespuestasRecuperacion(dto: ValidarRespuestasRecuperacionDto): Observable<ValidacionRespuestasResponseDto> {
        return this.http.post<ValidacionRespuestasResponseDto>(`${this.baseUrl}/Auth/recuperacion/validar`, dto);
    }

    /**
     * POST /Auth/recuperacion/restablecer
     * Restablece la contraseña usando el token emitido tras validar las respuestas.
     */
    restablecerPasswordRecuperacion(dto: RestablecerPasswordRecuperacionDto): Observable<RecuperacionPasswordResultDto> {
        return this.http.post<RecuperacionPasswordResultDto>(`${this.baseUrl}/Auth/recuperacion/restablecer`, dto);
    }

    establecerSesion(token: string, funcionario: PgFuncionarioResponseDto | null): void {
        this.token.set(token);
        this.currentUser.set(funcionario);

        if (this.tieneStorage()) {
            localStorage.setItem(STORAGE_KEY_TOKEN, token);
            if (funcionario) {
                localStorage.setItem(STORAGE_KEY_SESION, JSON.stringify(funcionario));
            } else {
                localStorage.removeItem(STORAGE_KEY_SESION);
            }
        }
    }

    /**
     * Limpia el estado en memoria y en localStorage sin navegar.
     * Útil para el interceptor cuando el backend responde 401.
     */
    clearSession(): void {
        this.token.set(null);
        this.currentUser.set(null);
        this.establecerCambioContrasenaRequerido(false);
        this.establecerPreguntasSeguridadRequeridas(false);

        if (this.tieneStorage()) {
            localStorage.removeItem(STORAGE_KEY_TOKEN);
            localStorage.removeItem(STORAGE_KEY_SESION);
        }
    }

    logout(): void {
        this.clearSession();
        this.router.navigate(['/app/login']);
    }

    private cargarTokenInicial(): string | null {
        return this.tieneStorage() ? localStorage.getItem(STORAGE_KEY_TOKEN) : null;
    }

    private cargarSesionInicial(): PgFuncionarioResponseDto | null {
        if (!this.tieneStorage()) return null;

        const raw = localStorage.getItem(STORAGE_KEY_SESION);
        if (!raw) return null;

        try {
            return JSON.parse(raw) as PgFuncionarioResponseDto;
        } catch {
            localStorage.removeItem(STORAGE_KEY_SESION);
            return null;
        }
    }

    private cargarCambioContrasenaRequerido(): boolean {
        return this.tieneStorage()
            ? localStorage.getItem(STORAGE_KEY_PASSWORD_CHANGE_REQUIRED) === 'true'
            : false;
    }

    private establecerCambioContrasenaRequerido(requerido: boolean): void {
        this.mustChangePassword.set(requerido);

        if (!this.tieneStorage()) return;

        if (requerido) {
            localStorage.setItem(STORAGE_KEY_PASSWORD_CHANGE_REQUIRED, 'true');
        } else {
            localStorage.removeItem(STORAGE_KEY_PASSWORD_CHANGE_REQUIRED);
        }
    }

    private cargarPreguntasSeguridadRequeridas(): boolean {
        return this.tieneStorage()
            ? localStorage.getItem(STORAGE_KEY_SECURITY_QUESTIONS_REQUIRED) === 'true'
            : false;
    }

    private establecerPreguntasSeguridadRequeridas(requerido: boolean): void {
        this.mustConfigureSecurityQuestions.set(requerido);

        if (!this.tieneStorage()) return;

        if (requerido) {
            localStorage.setItem(STORAGE_KEY_SECURITY_QUESTIONS_REQUIRED, 'true');
        } else {
            localStorage.removeItem(STORAGE_KEY_SECURITY_QUESTIONS_REQUIRED);
        }
    }

    private cargarUsuarioRecordado(): UsuarioRecordado | null {
        if (!this.tieneStorage()) return null;

        const raw = localStorage.getItem(STORAGE_KEY_REMEMBERED_USER);
        if (!raw) return null;

        try {
            const usuario = JSON.parse(raw) as UsuarioRecordado;
            if (!usuario?.ncedulaIdent) return null;
            return {
                snacionalidad: usuario.snacionalidad || 'V',
                ncedulaIdent: Number(usuario.ncedulaIdent)
            };
        } catch {
            localStorage.removeItem(STORAGE_KEY_REMEMBERED_USER);
            return null;
        }
    }

    private guardarSesion(funcionario: PgFuncionarioResponseDto): void {
        if (this.tieneStorage()) {
            localStorage.setItem(STORAGE_KEY_SESION, JSON.stringify(funcionario));
        }
    }

    private tieneStorage(): boolean {
        return typeof window !== 'undefined' && !!window.localStorage;
    }

    private normalizarRol(rol: string | null | undefined): string {
        return (rol || '')
            .trim()
            .normalize('NFD')
            .replace(/[\u0300-\u036f]/g, '')
            .toLowerCase();
    }
}
