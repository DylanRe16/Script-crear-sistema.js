import { Component, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { PasswordModule } from 'primeng/password';
import { CheckboxModule } from 'primeng/checkbox';
import { SelectModule } from 'primeng/select';
import { CommonModule } from '@angular/common';
import { MessageService } from 'primeng/api';
import { ToastModule } from 'primeng/toast';
import { AuthService } from '@/core/auth/auth.service';
import { LoginDto, AuthResponseDto, UsuarioRecordado } from '@/core/models/auth.model';
import { getApiErrorMessage } from '@/core/http/error.util';
import { RecuperarContrasenaDialogComponent } from './recuperar-contrasena-dialog/recuperar-contrasena-dialog.component';

@Component({
    selector: 'app-sgvti-login',
    standalone: true,
    imports: [
        CommonModule,
        FormsModule,
        RouterModule,
        ButtonModule,
        InputTextModule,
        PasswordModule,
        CheckboxModule,
        SelectModule,
        ToastModule,
        RecuperarContrasenaDialogComponent
    ],
    templateUrl: './login.component.html'
})
export class SgvtiLoginComponent implements OnInit {
    readonly currentYear = new Date().getFullYear();
    snacionalidad: string = '';
    ncedulaIdentStr: string = '';
    password: string = '';
    checked: boolean = false;
    cargando: boolean = false;
    mensajeError: string | null = null;
    modalRecuperacionVisible: boolean = false;

    opcionesNacionalidad = [
        { label: 'V', value: 'V' },
        { label: 'E', value: 'E' },
        { label: 'J', value: 'J' }
    ];

    constructor(
        private router: Router,
        private route: ActivatedRoute,
        private authService: AuthService,
        private messageService: MessageService
    ) {}

    ngOnInit(): void {
        const usuarioRecordado = this.authService.rememberedUser();
        if (usuarioRecordado) {
            this.snacionalidad = usuarioRecordado.snacionalidad;
            this.ncedulaIdentStr = String(usuarioRecordado.ncedulaIdent);
            this.checked = true;
        }

        if (this.route.snapshot.queryParamMap.get('seguridadConfigurada') === 'true') {
            this.messageService.add({
                severity: 'success',
                summary: 'Configuración completada',
                detail: 'Sus preguntas de seguridad fueron registradas. Inicie sesión nuevamente para habilitar el acceso al sistema.'
            });
        }
    }

    abrirRecuperacion(): void {
        this.mensajeError = null;
        this.modalRecuperacionVisible = true;
    }

    onPasswordRestablecida(usuario: UsuarioRecordado): void {
        this.snacionalidad = usuario.snacionalidad;
        this.ncedulaIdentStr = String(usuario.ncedulaIdent);
        this.password = '';
    }

    onCedulaInput(event: Event): void {
        const input = event.target as HTMLInputElement;
        const limpio = input.value.replace(/\D/g, '').slice(0, 9);
        input.value = limpio;
        this.ncedulaIdentStr = limpio;
    }

    onLogin(): void {
        this.mensajeError = null;
        const cedulaNum = parseInt(this.ncedulaIdentStr.replace(/\D/g, ''), 10);

        if (!this.snacionalidad) {
            this.mensajeError = 'Debe seleccionar la nacionalidad del documento.';
            return;
        }

        if (!cedulaNum || isNaN(cedulaNum)) {
            this.mensajeError = 'Debe ingresar un número de documento válido (1 a 9 dígitos).';
            return;
        }

        if (!this.password) {
            this.mensajeError = 'Debe ingresar su contraseña.';
            return;
        }

        const dto: LoginDto = {
            snacionalidad: this.snacionalidad,
            ncedulaIdent: cedulaNum,
            password: this.password
        };

        this.cargando = true;
        this.authService.login(dto).subscribe({
            next: res => {
                this.cargando = false;
                if (this.checked) {
                    this.authService.recordarUsuario(dto);
                } else {
                    this.authService.olvidarUsuario();
                }
                this.navegarSegunRol(res);
            },
            error: err => {
                this.cargando = false;
                // Mostrar "detail" del ProblemDetails si existe
                this.mensajeError = getApiErrorMessage(err, 'No fue posible iniciar sesión. Verifique sus credenciales.');
            }
        });
    }

    private navegarSegunRol(res: AuthResponseDto): void {
        if (res.requiereConfigurarPreguntasSeguridad) {
            this.messageService.add({
                severity: 'warn',
                summary: 'Configuración de seguridad requerida',
                detail: res.mensaje || 'Debe configurar sus preguntas de seguridad para habilitar el acceso a los módulos del sistema.'
            });
            this.router.navigate(['/app/seguridad'], { queryParams: { requerido: 'true' } });
            return;
        }

        this.messageService.add({
            severity: 'success',
            summary: 'Bienvenido',
            detail: res.mensaje || 'Autenticación exitosa'
        });
        this.router.navigate(['/app/dashboard']);
    }
}
