import { Component, effect } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { MenuItem } from 'primeng/api';
import { AppMenuitem } from './app.menuitem';
import { AuthService } from '@/core/auth/auth.service';

@Component({
    selector: 'app-menu',
    standalone: true,
    imports: [CommonModule, AppMenuitem, RouterModule],
    templateUrl: './app.menu.html'
})
export class AppMenu {
    model: MenuItem[] = [];

    constructor(public authService: AuthService) {
        effect(() => {
            this.authService.currentUser();
            const requiereSeguridad = this.authService.mustConfigureSecurityQuestions();
            const requiereCambioContrasena = this.authService.mustChangePassword();
            const accionesCuenta: MenuItem[] = [
                ...(requiereSeguridad
                    ? [{ label: 'Preguntas de seguridad', icon: 'pi pi-fw pi-shield', routerLink: ['/app/seguridad'] }]
                    : requiereCambioContrasena
                      ? [{ label: 'Cambiar contraseña', icon: 'pi pi-fw pi-key', routerLink: ['/app/cambiar-contrasena'] }]
                      : [
                            { label: 'Mis datos', icon: 'pi pi-fw pi-id-card', routerLink: ['/app/perfil'] },
                            { label: 'Cambiar contraseña', icon: 'pi pi-fw pi-key', routerLink: ['/app/cambiar-contrasena'] },
                            { label: 'Preguntas de seguridad', icon: 'pi pi-fw pi-shield', routerLink: ['/app/seguridad'] }
                        ])
            ];

            this.model = [
                ...(!requiereSeguridad && !requiereCambioContrasena
                    ? [{ label: 'General', items: [{ label: 'Dashboard', icon: 'pi pi-fw pi-home', routerLink: ['/app/dashboard'] }] }]
                    : [])
            ];

            if (!requiereSeguridad && !requiereCambioContrasena && this.authService.isAdmin()) {
                this.model.push({
                    label: 'Administración',
                    items: [{ label: 'Usuarios y roles', icon: 'pi pi-fw pi-users', routerLink: ['/app/usuarios'] }]
                });
            }

            this.model.push({
                label: 'Cuenta',
                items: [
                    { label: 'Perfil', icon: 'pi pi-fw pi-user', items: accionesCuenta },
                    {
                        label: 'Cerrar sesión',
                        icon: 'pi pi-fw pi-sign-out',
                        command: () => this.authService.logout()
                    }
                ]
            });
        });
    }
}
