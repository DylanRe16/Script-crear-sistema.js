import { Component, Input, Output, EventEmitter, OnInit, OnChanges, SimpleChanges, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule, AbstractControl, ValidationErrors } from '@angular/forms';
import { InputTextModule } from 'primeng/inputtext';
import { SelectModule } from 'primeng/select';
import { ButtonModule } from 'primeng/button';
import { MessageModule } from 'primeng/message';
import { MessageService } from 'primeng/api';
import { RolesService } from '@/core/services/roles.service';
import { FuncionariosService } from '@/core/services/funcionarios.service';
import { AuthService } from '@/core/auth/auth.service';
import { PgFuncionarioResponseDto, RegisterFuncionarioDto, PgFuncionarioUpdateDto } from '@/core/models/funcionario.model';
import { PgRolResponseDto } from '@/core/models/rol.model';
import { getApiErrorMessage } from '@/core/http/error.util';

function passwordMatchValidator(group: AbstractControl): ValidationErrors | null {
    const password = group.get('password')?.value;
    const confirmPassword = group.get('confirmPassword')?.value;
    return password && confirmPassword && password !== confirmPassword ? { passwordMismatch: true } : null;
}

@Component({
    selector: 'app-sesion-form',
    standalone: true,
    imports: [CommonModule, ReactiveFormsModule, InputTextModule, SelectModule, ButtonModule, MessageModule],
    templateUrl: './sesion-form.component.html'
})
export class SesionFormComponent implements OnInit, OnChanges {
    @Input() modo: 'alta' | 'edicion' = 'alta';
    @Input() funcionario?: PgFuncionarioResponseDto;

    @Output() guardado = new EventEmitter<PgFuncionarioResponseDto>();
    @Output() cancelar = new EventEmitter<void>();

    private readonly fb = inject(FormBuilder);
    private readonly authService = inject(AuthService);
    private readonly funcionariosService = inject(FuncionariosService);
    private readonly rolesService = inject(RolesService);
    private readonly messageService = inject(MessageService);

    form!: FormGroup;
    readonly guardando = signal(false);
    readonly roles = signal<PgRolResponseDto[]>([]);
    readonly mensajeErrorRoles = signal<string | null>(null);

    readonly nacionalidades = [
        { label: 'V', value: 'V' },
        { label: 'E', value: 'E' },
        { label: 'J', value: 'J' }
    ];

    ngOnInit(): void {
        this.inicializarFormulario();
        this.cargarRoles();
    }

    ngOnChanges(changes: SimpleChanges): void {
        if (this.form && (changes['funcionario'] || changes['modo'])) {
            this.aplicarDatos();
        }
    }

    filtrarSoloNumeros(event: Event): void {
        const input = event.target as HTMLInputElement;
        input.value = input.value.replace(/\D/g, '').slice(0, 9);
        this.form.patchValue({ ncedulaIdent: input.value });
    }

    guardar(): void {
        if (this.form.invalid) {
            this.form.markAllAsTouched();
            return;
        }

        this.guardando.set(true);
        const raw = this.form.getRawValue();

        if (this.modo === 'alta') {
            const dto: RegisterFuncionarioDto = {
                snacionalidad: String(raw.snacionalidad || 'V').toUpperCase(),
                ncedulaIdent: Number(raw.ncedulaIdent),
                sprimerNombre: String(raw.sprimerNombre).trim(),
                ssegundoNombre: raw.ssegundoNombre ? String(raw.ssegundoNombre).trim() : null,
                sprimerApellido: String(raw.sprimerApellido).trim(),
                ssegundoApellido: raw.ssegundoApellido ? String(raw.ssegundoApellido).trim() : null,
                password: raw.password,
                confirmPassword: raw.confirmPassword,
                rolId: Number(raw.rolId)
            };

            this.authService.register(dto).subscribe({
                next: (response) => {
                    this.guardando.set(false);
                    if (!response.funcionario) {
                        this.messageService.add({
                            severity: 'error',
                            summary: 'Respuesta incompleta',
                            detail: 'El backend no devolvió los datos de la cuenta creada.'
                        });
                        return;
                    }
                    this.messageService.add({
                        severity: 'success',
                        summary: 'Usuario creado',
                        detail: `Se creó la cuenta de ${response.funcionario.nombreCompleto || dto.sprimerNombre}.`
                    });
                    this.guardado.emit(response.funcionario);
                },
                error: (error: unknown) => {
                    this.guardando.set(false);
                    this.messageService.add({
                        severity: 'error',
                        summary: 'Error al crear usuario',
                        detail: getApiErrorMessage(error, 'No se pudo completar el registro.')
                    });
                }
            });
            return;
        }

        if (!this.funcionario?.idFuncionario) {
            this.guardando.set(false);
            return;
        }

        const dto: PgFuncionarioUpdateDto = {
            sprimerNombre: String(raw.sprimerNombre).trim(),
            ssegundoNombre: raw.ssegundoNombre ? String(raw.ssegundoNombre).trim() : null,
            sprimerApellido: String(raw.sprimerApellido).trim(),
            ssegundoApellido: raw.ssegundoApellido ? String(raw.ssegundoApellido).trim() : null,
            rolId: Number(raw.rolId),
            benabled: this.funcionario.benabled ?? true
        };

        this.funcionariosService.actualizar(this.funcionario.idFuncionario, dto).subscribe({
            next: (actualizado) => {
                this.guardando.set(false);
                this.messageService.add({
                    severity: 'success',
                    summary: 'Usuario actualizado',
                    detail: 'Los datos y el rol se guardaron correctamente.'
                });
                this.guardado.emit(actualizado);
            },
            error: (error: unknown) => {
                this.guardando.set(false);
                this.messageService.add({
                    severity: 'error',
                    summary: 'Error al actualizar',
                    detail: getApiErrorMessage(error, 'No se pudo actualizar la cuenta.')
                });
            }
        });
    }

    onCancelar(): void {
        this.cancelar.emit();
    }

    private inicializarFormulario(): void {
        this.form = this.fb.group({
            snacionalidad: ['V', [Validators.required, Validators.pattern(/^[VEJvej]$/)]],
            ncedulaIdent: ['', [Validators.required, Validators.pattern(/^\d{1,9}$/)]],
            sprimerNombre: ['', [Validators.required, Validators.minLength(2), Validators.maxLength(100)]],
            ssegundoNombre: ['', [Validators.maxLength(100)]],
            sprimerApellido: ['', [Validators.required, Validators.minLength(2), Validators.maxLength(100)]],
            ssegundoApellido: ['', [Validators.maxLength(100)]],
            password: [''],
            confirmPassword: [''],
            rolId: [null, [Validators.required]]
        }, { validators: this.modo === 'alta' ? [passwordMatchValidator] : [] });

        this.configurarValidacionesModo();
        this.aplicarDatos();
    }

    private configurarValidacionesModo(): void {
        const password = this.form.get('password');
        const confirmPassword = this.form.get('confirmPassword');
        const cedula = this.form.get('ncedulaIdent');
        const nacionalidad = this.form.get('snacionalidad');

        if (this.modo === 'alta') {
            password?.setValidators([Validators.required, Validators.minLength(6), Validators.pattern(/^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).{6,}$/)]);
            confirmPassword?.setValidators([Validators.required]);
            cedula?.enable();
            nacionalidad?.enable();
        } else {
            password?.clearValidators();
            password?.setValue('');
            confirmPassword?.clearValidators();
            confirmPassword?.setValue('');
            cedula?.disable();
            nacionalidad?.disable();
        }

        password?.updateValueAndValidity();
        confirmPassword?.updateValueAndValidity();
    }

    private cargarRoles(): void {
        this.mensajeErrorRoles.set(null);
        this.rolesService.listar().subscribe({
            next: (roles) => this.roles.set(roles),
            error: (error: unknown) => {
                const message = getApiErrorMessage(error, 'No fue posible cargar los roles disponibles.');
                this.mensajeErrorRoles.set(message);
                this.messageService.add({ severity: 'error', summary: 'Error', detail: message });
            }
        });
    }

    private aplicarDatos(): void {
        if (!this.form) return;
        this.configurarValidacionesModo();

        if (this.modo === 'edicion' && this.funcionario) {
            this.form.patchValue({
                snacionalidad: this.funcionario.snacionalidad || 'V',
                ncedulaIdent: this.funcionario.ncedulaIdent ? String(this.funcionario.ncedulaIdent) : '',
                sprimerNombre: this.funcionario.sprimerNombre || '',
                ssegundoNombre: this.funcionario.ssegundoNombre || '',
                sprimerApellido: this.funcionario.sprimerApellido || '',
                ssegundoApellido: this.funcionario.ssegundoApellido || '',
                rolId: this.funcionario.rolId ? Number(this.funcionario.rolId) : null
            });
            return;
        }

        this.form.reset({
            snacionalidad: 'V',
            ncedulaIdent: '',
            sprimerNombre: '',
            ssegundoNombre: '',
            sprimerApellido: '',
            ssegundoApellido: '',
            password: '',
            confirmPassword: '',
            rolId: null
        });
    }
}
