import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AbstractControl, FormBuilder, ReactiveFormsModule, ValidationErrors, Validators } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { PasswordModule } from 'primeng/password';
import { finalize } from 'rxjs';
import { AuthService } from '@/core/auth/auth.service';
import { getApiErrorMessage } from '@/core/http/error.util';
import { PageHeaderComponent } from '@/shared/components/page-header/page-header.component';

function passwordsMatch(control: AbstractControl): ValidationErrors | null {
    const newPassword = control.get('newPassword')?.value;
    const confirmNewPassword = control.get('confirmNewPassword')?.value;

    return newPassword && confirmNewPassword && newPassword !== confirmNewPassword
        ? { passwordsMismatch: true }
        : null;
}

@Component({
    selector: 'app-sgvti-cambiar-contrasena',
    standalone: true,
    imports: [CommonModule, ReactiveFormsModule, ButtonModule, PasswordModule, PageHeaderComponent],
    templateUrl: './cambiar-contrasena.component.html'
})
export class SgvtiCambiarContrasenaComponent {
    private readonly formBuilder = inject(FormBuilder);
    private readonly authService = inject(AuthService);
    private readonly passwordPattern = /^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).{6,}$/;

    readonly passwordForm = this.formBuilder.nonNullable.group(
        {
            currentPassword: ['', Validators.required],
            newPassword: ['', [Validators.required, Validators.minLength(6), Validators.maxLength(100), Validators.pattern(this.passwordPattern)]],
            confirmNewPassword: ['', Validators.required]
        },
        { validators: passwordsMatch }
    );

    cargando = false;
    mensajeError: string | null = null;
    mensajeExito: string | null = null;

    cambiarContrasena(): void {
        this.mensajeError = null;
        this.mensajeExito = null;

        if (this.passwordForm.invalid) {
            this.passwordForm.markAllAsTouched();
            return;
        }

        this.cargando = true;
        this.authService
            .changePassword(this.passwordForm.getRawValue())
            .pipe(finalize(() => (this.cargando = false)))
            .subscribe({
                next: () => {
                    this.mensajeExito = 'Su contraseña se actualizó correctamente.';
                    this.passwordForm.reset();
                },
                error: (error: unknown) => {
                    this.mensajeError = getApiErrorMessage(error, 'No fue posible cambiar la contraseña.');
                }
            });
    }
}
