import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { forkJoin } from 'rxjs';
import { finalize } from 'rxjs/operators';
import { TableModule } from 'primeng/table';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { IconFieldModule } from 'primeng/iconfield';
import { InputIconModule } from 'primeng/inputicon';
import { TagModule } from 'primeng/tag';
import { SelectModule } from 'primeng/select';
import { DialogModule } from 'primeng/dialog';
import { TooltipModule } from 'primeng/tooltip';
import { ConfirmationService, MessageService } from 'primeng/api';
import { FuncionariosService } from '@/core/services/funcionarios.service';
import { RolesService } from '@/core/services/roles.service';
import { PgFuncionarioResponseDto, PgFuncionarioUpdateDto } from '@/core/models/funcionario.model';
import { PgRolResponseDto } from '@/core/models/rol.model';
import { AuthService } from '@/core/auth/auth.service';
import { SesionFormComponent } from '../sesion-form/sesion-form.component';
import { getApiErrorMessage } from '@/core/http/error.util';
import { PageHeaderComponent } from '@/shared/components/page-header/page-header.component';

@Component({
    selector: 'app-sesiones-list',
    standalone: true,
    imports: [
        CommonModule,
        FormsModule,
        TableModule,
        ButtonModule,
        InputTextModule,
        IconFieldModule,
        InputIconModule,
        TagModule,
        SelectModule,
        DialogModule,
        TooltipModule,
        SesionFormComponent,
        PageHeaderComponent
    ],
    templateUrl: './sesiones-list.component.html'
})
export class SesionesListComponent implements OnInit {
    private readonly funcionariosService = inject(FuncionariosService);
    private readonly rolesService = inject(RolesService);
    private readonly authService = inject(AuthService);
    private readonly confirmationService = inject(ConfirmationService);
    private readonly messageService = inject(MessageService);

    readonly cargando = signal(false);
    readonly funcionarios = signal<PgFuncionarioResponseDto[]>([]);
    readonly roles = signal<PgRolResponseDto[]>([]);
    readonly filtroTexto = signal('');
    readonly filtroRolId = signal<number | null>(null);
    readonly filtroEstado = signal<'todos' | 'activos' | 'inactivos'>('todos');
    readonly modalVisible = signal(false);
    readonly modoFormulario = signal<'alta' | 'edicion'>('alta');
    readonly funcionarioSeleccionado = signal<PgFuncionarioResponseDto | null>(null);

    readonly funcionariosFiltrados = computed(() => {
        const texto = this.normalizarTexto(this.filtroTexto());
        const rolId = this.filtroRolId();
        const estado = this.filtroEstado();

        return this.funcionarios().filter((funcionario) => {
            const campos = [
                funcionario.cedulaCompleta,
                funcionario.snacionalidad,
                funcionario.ncedulaIdent,
                funcionario.nombreCompleto,
                funcionario.sprimerNombre,
                funcionario.ssegundoNombre,
                funcionario.sprimerApellido,
                funcionario.ssegundoApellido,
                funcionario.nombreRol
            ];
            const coincideTexto = !texto || campos.some((valor) => this.normalizarTexto(String(valor ?? '')).includes(texto));
            const coincideRol = rolId === null || Number(funcionario.rolId) === Number(rolId);
            const coincideEstado =
                estado === 'todos' ||
                (estado === 'activos' && funcionario.benabled) ||
                (estado === 'inactivos' && !funcionario.benabled);

            return coincideTexto && coincideRol && coincideEstado;
        });
    });

    readonly opcionesFiltroRol = computed(() => [
        { label: 'Todos los roles', value: null as number | null },
        ...this.roles().map((rol) => ({
            label: rol.sdescripcion || `Rol #${rol.idRol}`,
            value: Number(rol.idRol)
        }))
    ]);

    ngOnInit(): void {
        this.cargarDatos();
    }

    cargarDatos(): void {
        this.cargando.set(true);
        forkJoin({
            funcionarios: this.funcionariosService.listar(false),
            roles: this.rolesService.listar()
        })
            .pipe(finalize(() => this.cargando.set(false)))
            .subscribe({
                next: ({ funcionarios, roles }) => {
                    this.funcionarios.set(funcionarios);
                    this.roles.set(roles);
                },
                error: (error: unknown) => {
                    this.messageService.add({
                        severity: 'error',
                        summary: 'Error',
                        detail: getApiErrorMessage(error, 'No fue posible cargar las cuentas y roles.')
                    });
                }
            });
    }

    limpiarFiltros(): void {
        this.filtroTexto.set('');
        this.filtroRolId.set(null);
        this.filtroEstado.set('todos');
    }

    abrirAlta(): void {
        this.modoFormulario.set('alta');
        this.funcionarioSeleccionado.set(null);
        this.modalVisible.set(true);
    }

    abrirEdicion(funcionario: PgFuncionarioResponseDto): void {
        if (!this.validarCuentaAjena(funcionario)) return;
        this.modoFormulario.set('edicion');
        this.funcionarioSeleccionado.set({ ...funcionario });
        this.modalVisible.set(true);
    }

    confirmarToggleEstado(funcionario: PgFuncionarioResponseDto): void {
        if (!this.validarCuentaAjena(funcionario)) return;
        if (!funcionario.idFuncionario) return;

        const nuevoEstado = !funcionario.benabled;
        const nombre = funcionario.nombreCompleto || funcionario.cedulaCompleta || 'la cuenta';
        this.confirmationService.confirm({
            message: `¿Deseas ${nuevoEstado ? 'activar' : 'desactivar'} ${nombre}?`,
            header: 'Confirmar cambio de estado',
            icon: 'pi pi-exclamation-triangle',
            acceptLabel: nuevoEstado ? 'Activar' : 'Desactivar',
            rejectLabel: 'Cancelar',
            accept: () => this.actualizarEstado(funcionario, nuevoEstado)
        });
    }

    confirmarRestablecerContrasena(funcionario: PgFuncionarioResponseDto): void {
        if (!this.validarCuentaAjena(funcionario)) return;
        const id = funcionario.idFuncionario;
        if (!id) return;

        const nombre = funcionario.nombreCompleto || funcionario.cedulaCompleta || 'la cuenta';
        this.confirmationService.confirm({
            message: `Se restablecerá la contraseña de ${nombre} a la contraseña temporal configurada en el backend. El usuario deberá cambiarla al iniciar sesión.`,
            header: 'Restablecer contraseña',
            icon: 'pi pi-exclamation-triangle',
            acceptLabel: 'Restablecer',
            rejectLabel: 'Cancelar',
            accept: () => {
                this.funcionariosService.restablecerContrasena(id).subscribe({
                    next: () => this.messageService.add({
                        severity: 'success',
                        summary: 'Contraseña restablecida',
                        detail: 'La cuenta deberá cambiar la contraseña temporal al iniciar sesión.'
                    }),
                    error: (error: unknown) => this.messageService.add({
                        severity: 'error',
                        summary: 'Error',
                        detail: getApiErrorMessage(error, 'No fue posible restablecer la contraseña.')
                    })
                });
            }
        });
    }

    confirmarRestablecerPreguntas(funcionario: PgFuncionarioResponseDto): void {
        if (!this.validarCuentaAjena(funcionario)) return;
        const id = funcionario.idFuncionario;
        if (!id) return;

        const nombre = funcionario.nombreCompleto || funcionario.cedulaCompleta || 'la cuenta';
        this.confirmationService.confirm({
            message: `Se eliminarán las preguntas de seguridad configuradas para ${nombre}. Deberá registrarlas nuevamente antes de acceder a otras secciones.`,
            header: 'Restablecer preguntas de seguridad',
            icon: 'pi pi-exclamation-triangle',
            acceptLabel: 'Restablecer',
            rejectLabel: 'Cancelar',
            accept: () => {
                this.funcionariosService.restablecerPreguntasSeguridad(id).subscribe({
                    next: () => this.messageService.add({
                        severity: 'success',
                        summary: 'Preguntas restablecidas',
                        detail: 'El usuario deberá configurar nuevamente sus preguntas de seguridad.'
                    }),
                    error: (error: unknown) => this.messageService.add({
                        severity: 'error',
                        summary: 'Error',
                        detail: getApiErrorMessage(error, 'No fue posible restablecer las preguntas de seguridad.')
                    })
                });
            }
        });
    }

    esUsuarioActual(funcionario: PgFuncionarioResponseDto): boolean {
        const idActual = this.authService.idFuncionario();
        return idActual !== null && Number(funcionario.idFuncionario) === Number(idActual);
    }

    getRolSeverity(nombreRol?: string): 'danger' | 'info' | 'secondary' {
        return nombreRol?.trim().toLowerCase() === 'administrador' ? 'danger' : nombreRol ? 'info' : 'secondary';
    }

    alGuardar(): void {
        this.modalVisible.set(false);
        this.cargarDatos();
    }

    private actualizarEstado(funcionario: PgFuncionarioResponseDto, benabled: boolean): void {
        const id = funcionario.idFuncionario;
        if (!id) return;

        const dto: PgFuncionarioUpdateDto = {
            sprimerNombre: funcionario.sprimerNombre || '',
            ssegundoNombre: funcionario.ssegundoNombre ?? null,
            sprimerApellido: funcionario.sprimerApellido || '',
            ssegundoApellido: funcionario.ssegundoApellido ?? null,
            rolId: Number(funcionario.rolId),
            benabled
        };

        this.funcionariosService.actualizar(id, dto).subscribe({
            next: () => {
                this.messageService.add({
                    severity: 'success',
                    summary: 'Estado actualizado',
                    detail: `La cuenta fue ${benabled ? 'activada' : 'desactivada'}.`
                });
                this.cargarDatos();
            },
            error: (error: unknown) => {
                this.messageService.add({
                    severity: 'error',
                    summary: 'Error',
                    detail: getApiErrorMessage(error, 'No fue posible actualizar el estado de la cuenta.')
                });
            }
        });
    }

    private validarCuentaAjena(funcionario: PgFuncionarioResponseDto): boolean {
        if (!funcionario.idFuncionario) return false;
        if (!this.esUsuarioActual(funcionario)) return true;

        this.messageService.add({
            severity: 'warn',
            summary: 'Acción no permitida',
            detail: 'No puedes modificar tu propia cuenta desde la administración.'
        });
        return false;
    }

    private normalizarTexto(valor: string): string {
        return valor.trim().normalize('NFD').replace(/[\u0300-\u036f]/g, '').toLocaleLowerCase();
    }
}
