import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { NavigationEnd, Router, RouterModule } from '@angular/router';
import { filter } from 'rxjs';
import { DialogModule } from 'primeng/dialog';
import { ButtonModule } from 'primeng/button';
import { AuthService } from '@/core/auth/auth.service';

/**
 * Aviso obligatorio de cambio de contraseña cuando la cuenta aún usa
 * la contraseña temporal asignada por el administrador.
 */
@Component({
    selector: 'app-password-change-modal',
    standalone: true,
    imports: [CommonModule, RouterModule, DialogModule, ButtonModule],
    changeDetection: ChangeDetectionStrategy.OnPush,
    templateUrl: './password-change-modal.component.html'
})
export class PasswordChangeModalComponent {
    private readonly authService = inject(AuthService);
    private readonly router = inject(Router);

    private readonly url = signal(this.router.url);

    readonly visible = computed(
        () =>
            this.authService.mustChangePassword() &&
            !this.authService.mustConfigureSecurityQuestions() &&
            !this.url().includes('cambiar-contrasena') &&
            !this.url().includes('seguridad')
    );

    constructor() {
        this.router.events.pipe(filter(event => event instanceof NavigationEnd)).subscribe(event => {
            this.url.set((event as NavigationEnd).urlAfterRedirects);
        });
    }

    irACambiarContrasena(): void {
        this.router.navigate(['/app/cambiar-contrasena']);
    }
}
