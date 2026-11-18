import { Component, EventEmitter, Input, OnChanges, Output, SimpleChanges, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { finalize } from 'rxjs/operators';
import { ButtonModule } from 'primeng/button';
import { DialogModule } from 'primeng/dialog';
import { InputTextModule } from 'primeng/inputtext';
import { PasswordModule } from 'primeng/password';
import { SelectModule } from 'primeng/select';
import { AuthService } from '@/core/auth/auth.service';
import { getApiErrorMessage } from '@/core/http/error.util';
import { PreguntaRecuperacionDto, UsuarioRecordado } from '@/core/models/auth.model';

/**
 * Diálogo de recuperación de contraseña:
 * 1. Identificación por cédula.
 * 2. Dos preguntas de seguridad aleatorias (se regeneran si falla).
 * 3. Definición de la nueva contraseña, solo tras validar las respuestas.
 * 4. Confirmación del restablecimiento.
 */
@Component({
    selector: 'app-recuperar-contrasena-dialog',
    standalone: true,
    imports: [CommonModule, FormsModule, ButtonModule, DialogModule, InputTextModule, PasswordModule, SelectModule],
    templateUrl: './recuperar-contrasena-dialog.component.html'
})
export class RecuperarContrasenaDialogComponent implements OnChanges {
    @Input() visible = false;
    @Output() visibleChange = new EventEmitter<boolean>();
    @Output() restablecida = new EventEmitter<UsuarioRecordado>();

    private readonly authService = inject(AuthService);
    private readonly passwordPattern = /^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).{6,}$/;

    readonly paso = signal<1 | 2 | 3 | 4>(1);
    readonly cargando = signal<boolean>(false);
    readonly intentoEnvio = signal<boolean>(false);
    readonly mensajeError = signal<string | null>(null);
    readonly tokenRecuperacion = signal<string | null>(null);

    // Paso 1: identificación
    snacionalidad = '';
    ncedulaIdentStr = '';

    readonly opcionesNacionalidad = [
        { label: 'V', value: 'V' },
        { label: 'E', value: 'E' },
        { label: 'J', value: 'J' }
    ];

    // Paso 2: preguntas de seguridad aleatorias
    readonly preguntas = signal<PreguntaRecuperacionDto[]>([]);
    readonly respuestas = signal<Record<number, string | undefined>>({});

    // Paso 3: nueva contraseña
    newPassword = '';
    confirmNewPassword = '';

    readonly totalPreguntas = computed(() => this.preguntas().length);

    ngOnChanges(changes: SimpleChanges): void {
        if (changes['visible']?.currentValue === true) {
            this.reiniciar();
        }
    }

    onVisibleChange(visible: boolean): void {
        this.visible = visible;
        this.visibleChange.emit(visible);
    }

    onCedulaInput(event: Event): void {
        const input = event.target as HTMLInputElement;
        const limpio = input.value.replace(/\D/g, '').slice(0, 9);
        input.value = limpio;
        this.ncedulaIdentStr = limpio;
    }

    buscarPreguntas(): void {
        this.mensajeError.set(null);

        const cedula = this.cedulaNumerica();
        if (!this.snacionalidad) {
            this.mensajeError.set('Debe seleccionar la nacionalidad del documento.');
            return;
        }
        if (!cedula) {
            this.mensajeError.set('Debe ingresar un número de documento válido (1 a 9 dígitos).');
            return;
        }

        this.cargarPreguntas(false);
    }

    setRespuesta(idPreguntaSeg: number, valor: string): void {
        this.respuestas.update(mapa => ({ ...mapa, [idPreguntaSeg]: valor }));
    }

    respuestaInvalida(idPreguntaSeg: number): boolean {
        if (!this.intentoEnvio()) return false;
        return !this.esRespuestaValida(this.respuestas()[idPreguntaSeg] ?? '');
    }

    get respuestasCompletas(): boolean {
        return (
            this.preguntas().length > 0 &&
            this.preguntas().every(pregunta => this.esRespuestaValida(this.respuestas()[pregunta.idPreguntaSeg] ?? ''))
        );
    }

    get passwordValida(): boolean {
        return this.passwordPattern.test(this.newPassword) && this.newPassword.length <= 100;
    }

    get confirmacionValida(): boolean {
        return !!this.confirmNewPassword && this.newPassword === this.confirmNewPassword;
    }

    get puedeRestablecer(): boolean {
        return !!this.tokenRecuperacion() && this.passwordValida && this.confirmacionValida;
    }

    /**
     * Valida las respuestas del paso 2. Si son incorrectas se solicita un nuevo
     * conjunto aleatorio de preguntas para reintentar.
     */
    validarRespuestas(): void {
        this.intentoEnvio.set(true);
        this.mensajeError.set(null);

        if (!this.respuestasCompletas) {
            this.mensajeError.set('Debe responder las preguntas de seguridad para continuar.');
            return;
        }

        const cedula = this.cedulaNumerica();
        if (!cedula) return;

        const dto = {
            snacionalidad: this.snacionalidad,
            ncedulaIdent: cedula,
            respuestas: this.preguntas().map(pregunta => ({
                idPreguntaSeg: pregunta.idPreguntaSeg,
                respuesta: (this.respuestas()[pregunta.idPreguntaSeg] ?? '').trim()
            }))
        };

        this.cargando.set(true);
        this.authService
            .validarRespuestasRecuperacion(dto)
            .subscribe({
                next: resultado => {
                    this.cargando.set(false);

                    if (!resultado.success || !resultado.tokenRecuperacion) {
                        this.mensajeError.set(resultado.mensaje || 'No fue posible validar sus respuestas de seguridad.');
                        this.cargarPreguntas(true);
                        return;
                    }

                    this.tokenRecuperacion.set(resultado.tokenRecuperacion);
                    this.intentoEnvio.set(false);
                    this.mensajeError.set(null);
                    this.paso.set(3);
                },
                error: err => {
                    this.cargando.set(false);
                    this.mensajeError.set(getApiErrorMessage(err, 'No fue posible validar sus respuestas de seguridad.'));
                    // Respuestas incorrectas: se solicitan 2 preguntas nuevas al azar.
                    this.cargarPreguntas(true);
                }
            });
    }

    restablecerPassword(): void {
        this.intentoEnvio.set(true);
        this.mensajeError.set(null);

        const token = this.tokenRecuperacion();
        if (!token) {
            this.volverAlPaso1();
            this.mensajeError.set('La validación de seguridad no es válida. Vuelva a responder las preguntas.');
            return;
        }

        if (!this.puedeRestablecer) {
            this.mensajeError.set('Verifique la nueva contraseña y su confirmación.');
            return;
        }

        const dto = {
            tokenRecuperacion: token,
            newPassword: this.newPassword,
            confirmNewPassword: this.confirmNewPassword
        };

        this.cargando.set(true);
        this.authService
            .restablecerPasswordRecuperacion(dto)
            .pipe(finalize(() => this.cargando.set(false)))
            .subscribe({
                next: resultado => {
                    if (!resultado.success) {
                        this.mensajeError.set(resultado.mensaje || 'No fue posible restablecer la contraseña.');
                        return;
                    }

                    this.paso.set(4);
                    this.restablecida.emit({
                        snacionalidad: this.snacionalidad,
                        ncedulaIdent: this.cedulaNumerica()
                    });
                },
                error: err => {
                    this.mensajeError.set(getApiErrorMessage(err, 'No fue posible restablecer la contraseña.'));
                }
            });
    }

    volverPasoAnterior(): void {
        if (this.paso() === 2) {
            this.volverAlPaso1();
            return;
        }

        // Desde el paso de contraseña se regresa a las preguntas y se descarta
        // el token para exigir una nueva validación.
        this.mensajeError.set(null);
        this.intentoEnvio.set(false);
        this.tokenRecuperacion.set(null);
        this.newPassword = '';
        this.confirmNewPassword = '';
        this.paso.set(2);
    }

    cerrar(): void {
        this.visible = false;
        this.visibleChange.emit(false);
    }

    private cargarPreguntas(esReintento: boolean): void {
        const cedula = this.cedulaNumerica();
        if (!cedula) return;

        this.cargando.set(true);
        this.authService
            .obtenerPreguntasRecuperacion({ snacionalidad: this.snacionalidad, ncedulaIdent: cedula })
            .pipe(finalize(() => this.cargando.set(false)))
            .subscribe({
                next: respuesta => {
                    if (!respuesta.tienePreguntasConfiguradas || respuesta.preguntas.length === 0) {
                        this.mensajeError.set(respuesta.mensaje || 'La cuenta no posee preguntas de seguridad configuradas. Contacte al administrador.');
                        return;
                    }

                    this.preguntas.set(respuesta.preguntas);
                    this.respuestas.set({});
                    this.tokenRecuperacion.set(null);
                    this.newPassword = '';
                    this.confirmNewPassword = '';
                    this.intentoEnvio.set(false);
                    this.paso.set(2);

                    if (!esReintento) {
                        this.mensajeError.set(null);
                    }
                },
                error: err => {
                    // Si falla la recarga tras un intento incorrecto, se conserva
                    // el mensaje original del error de validación.
                    if (!esReintento) {
                        this.mensajeError.set(getApiErrorMessage(err, 'No fue posible obtener sus preguntas de seguridad.'));
                    }
                }
            });
    }

    private volverAlPaso1(): void {
        this.mensajeError.set(null);
        this.intentoEnvio.set(false);
        this.tokenRecuperacion.set(null);
        this.respuestas.set({});
        this.newPassword = '';
        this.confirmNewPassword = '';
        this.preguntas.set([]);
        this.paso.set(1);
    }

    private reiniciar(): void {
        this.paso.set(1);
        this.cargando.set(false);
        this.intentoEnvio.set(false);
        this.mensajeError.set(null);
        this.tokenRecuperacion.set(null);
        this.snacionalidad = '';
        this.ncedulaIdentStr = '';
        this.preguntas.set([]);
        this.respuestas.set({});
        this.newPassword = '';
        this.confirmNewPassword = '';
    }

    private cedulaNumerica(): number {
        const cedula = parseInt(this.ncedulaIdentStr.replace(/\D/g, ''), 10);
        return !cedula || isNaN(cedula) ? 0 : cedula;
    }

    private esRespuestaValida(valor: string): boolean {
        const longitud = valor.trim().length;
        return longitud >= 2 && longitud <= 200;
    }
}
