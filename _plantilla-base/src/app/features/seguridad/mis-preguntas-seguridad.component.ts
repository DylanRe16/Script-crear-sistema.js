import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import { forkJoin } from 'rxjs';
import { finalize } from 'rxjs/operators';
import { ButtonModule } from 'primeng/button';
import { DialogModule } from 'primeng/dialog';
import { InputTextModule } from 'primeng/inputtext';
import { TagModule } from 'primeng/tag';
import { MessageService } from 'primeng/api';
import { AuthService } from '@/core/auth/auth.service';
import { PreguntasSeguridadService } from '@/core/services/preguntas-seguridad.service';
import { getApiErrorMessage } from '@/core/http/error.util';
import { PgEstadoPreguntasSeguridadDto, PgFuncionarioPreguntaSeguridadResponseDto, PgPreguntaSeguridadResponseDto } from '@/core/models/pregunta-seguridad.model';
import { PageHeaderComponent } from '@/shared/components/page-header/page-header.component';

/**
 * Pantalla de configuración de preguntas de seguridad del funcionario autenticado.
 *
 * Reglas de negocio replicadas en el cliente:
 * - Mínimo 1 pregunta y máximo 6 (el máximo lo informa el backend).
 * - Sin preguntas duplicadas (la selección por ID lo garantiza).
 * - Cada respuesta debe tener entre 2 y 200 caracteres.
 * - El backend reemplaza el lote completo de forma atómica, por lo que al
 *   actualizar es necesario volver a registrar todas las respuestas.
 */
@Component({
    selector: 'app-sgvti-mis-preguntas-seguridad',
    standalone: true,
    imports: [CommonModule, FormsModule, RouterModule, ButtonModule, DialogModule, InputTextModule, TagModule, PageHeaderComponent],
    templateUrl: './mis-preguntas-seguridad.component.html'
})
export class SgvtiMisPreguntasSeguridadComponent implements OnInit {
    private readonly preguntasService = inject(PreguntasSeguridadService);
    readonly authService = inject(AuthService);
    private readonly messageService = inject(MessageService);
    private readonly router = inject(Router);

    readonly cargando = signal<boolean>(true);
    readonly guardando = signal<boolean>(false);
    readonly editando = signal<boolean>(false);
    readonly intentoGuardar = signal<boolean>(false);
    readonly mensajeError = signal<string | null>(null);
    readonly mostrarDialogoReinicio = signal<boolean>(false);

    readonly estado = signal<PgEstadoPreguntasSeguridadDto | null>(null);
    readonly misPreguntas = signal<PgFuncionarioPreguntaSeguridadResponseDto[]>([]);
    readonly catalogo = signal<PgPreguntaSeguridadResponseDto[]>([]);

    /** IDs de las preguntas seleccionadas, en el orden en que fueron elegidas */
    readonly seleccionadas = signal<number[]>([]);
    /** Respuestas capturadas por ID de pregunta */
    readonly respuestas = signal<Record<number, string | undefined>>({});

    readonly maximo = computed(() => this.estado()?.maximoPermitido ?? 6);
    readonly totalSeleccionadas = computed(() => this.seleccionadas().length);
    readonly configurado = computed(() => !!this.estado()?.tienePreguntasConfiguradas);
    readonly puedeAgregarMas = computed(() => this.totalSeleccionadas() < this.maximo());
    readonly sesionPendienteRenovar = computed(() => this.estado()?.tienePreguntasConfiguradas === true && this.authService.mustConfigureSecurityQuestions());
    readonly puedeVolverAlPanel = computed(() => !this.authService.mustConfigureSecurityQuestions());

    readonly puedeGuardar = computed(() => {
        const ids = this.seleccionadas();
        if (ids.length === 0) return false;
        return ids.every((id) => this.esRespuestaValida(this.respuestas()[id] ?? ''));
    });

    ngOnInit(): void {
        this.cargar();
    }

    cargar(): void {
        this.cargando.set(true);
        this.mensajeError.set(null);

        forkJoin({
            estado: this.preguntasService.obtenerEstado(),
            misPreguntas: this.preguntasService.obtenerMisPreguntas(),
            catalogo: this.preguntasService.listarCatalogo(true)
        })
            .pipe(finalize(() => this.cargando.set(false)))
            .subscribe({
                next: (resultado) => {
                    this.estado.set(resultado.estado);
                    this.misPreguntas.set(resultado.misPreguntas);
                    this.catalogo.set(resultado.catalogo);
                },
                error: (err) => {
                    this.mensajeError.set(getApiErrorMessage(err, 'No fue posible cargar su configuración de preguntas de seguridad.'));
                }
            });
    }

    estaSeleccionada(idPreguntaSeg: number): boolean {
        return this.seleccionadas().includes(idPreguntaSeg);
    }

    togglePregunta(idPreguntaSeg: number): void {
        if (this.estaSeleccionada(idPreguntaSeg)) {
            this.seleccionadas.update((ids) => ids.filter((id) => id !== idPreguntaSeg));
            this.respuestas.update((mapa) => {
                const copia = { ...mapa };
                delete copia[idPreguntaSeg];
                return copia;
            });
            return;
        }

        if (!this.puedeAgregarMas()) {
            this.messageService.add({
                severity: 'warn',
                summary: 'Límite alcanzado',
                detail: `Solo se permiten hasta ${this.maximo()} preguntas de seguridad.`
            });
            return;
        }

        this.seleccionadas.update((ids) => [...ids, idPreguntaSeg]);
    }

    setRespuesta(idPreguntaSeg: number, valor: string): void {
        this.respuestas.update((mapa) => ({ ...mapa, [idPreguntaSeg]: valor }));
    }

    respuestaInvalida(idPreguntaSeg: number): boolean {
        if (!this.intentoGuardar()) return false;
        return !this.esRespuestaValida(this.respuestas()[idPreguntaSeg] ?? '');
    }

    iniciarEdicion(): void {
        const idsConfigurados = this.misPreguntas()
            .map((pregunta) => Number(pregunta.idPreguntaSeg))
            .filter((id) => this.catalogo().some((pregunta) => Number(pregunta.idPreguntaSeg) === id));

        this.seleccionadas.set(idsConfigurados);
        this.respuestas.set({});
        this.intentoGuardar.set(false);
        this.editando.set(true);
    }

    cancelarEdicion(): void {
        this.limpiarSeleccion();
        this.editando.set(false);
    }

    guardar(): void {
        this.intentoGuardar.set(true);
        this.mensajeError.set(null);

        const ids = this.seleccionadas();
        if (ids.length === 0) {
            this.mensajeError.set('Debe seleccionar al menos una pregunta de seguridad.');
            return;
        }

        if (!this.puedeGuardar()) {
            this.mensajeError.set('Cada pregunta seleccionada requiere una respuesta de entre 2 y 200 caracteres.');
            return;
        }

        const requiereReinicio = this.authService.mustConfigureSecurityQuestions();
        const dto = {
            preguntas: ids.map((idPreguntaSeg) => ({
                idPreguntaSeg,
                respuesta: (this.respuestas()[idPreguntaSeg] ?? '').trim()
            }))
        };

        this.guardando.set(true);
        this.preguntasService
            .asignarMisPreguntas(dto)
            .pipe(finalize(() => this.guardando.set(false)))
            .subscribe({
                next: (estado) => {
                    this.estado.set(estado);
                    this.limpiarSeleccion();
                    this.editando.set(false);
                    this.refrescarMisPreguntas();

                    if (requiereReinicio) {
                        // El token actual conserva el claim `requiere_preguntas_seg=true`,
                        // por lo que el backend mantiene el bloqueo hasta renovar la sesión.
                        this.mostrarDialogoReinicio.set(true);
                        return;
                    }

                    this.authService.marcarPreguntasSeguridadConfiguradas();
                    this.messageService.add({
                        severity: 'success',
                        summary: 'Configuración guardada',
                        detail: estado.mensaje || 'Sus preguntas de seguridad fueron actualizadas correctamente.'
                    });
                },
                error: (err) => {
                    this.mensajeError.set(getApiErrorMessage(err, 'No fue posible guardar sus preguntas de seguridad.'));
                }
            });
    }

    reiniciarSesion(): void {
        this.authService.clearSession();
        this.router.navigate(['/app/login'], { queryParams: { seguridadConfigurada: 'true' } });
    }

    volverAlPanel(): void {
        this.router.navigate(['/app/dashboard']);
    }

    private refrescarMisPreguntas(): void {
        this.preguntasService.obtenerMisPreguntas().subscribe({
            next: (lista) => this.misPreguntas.set(lista),
            error: (err) => console.error('Error al refrescar las preguntas configuradas:', err)
        });
    }

    private limpiarSeleccion(): void {
        this.seleccionadas.set([]);
        this.respuestas.set({});
        this.intentoGuardar.set(false);
    }

    private esRespuestaValida(valor: string): boolean {
        const longitud = valor.trim().length;
        return longitud >= 2 && longitud <= 200;
    }
}
