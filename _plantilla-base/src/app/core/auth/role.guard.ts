import { inject } from '@angular/core';
import { CanActivateFn, Router, ActivatedRouteSnapshot } from '@angular/router';
import { AuthService } from './auth.service';

/**
 * Guard de autenticación general: asegura que el usuario tenga una sesión activa con token.
 */
export const authGuard: CanActivateFn = () => {
    const authService = inject(AuthService);
    const router = inject(Router);

    if (!authService.isAuthenticated()) {
        router.navigate(['/app/login']);
        return false;
    }
    return true;
};

/**
 * Guard de control de acceso por rol: verifica que el rol del usuario esté en la lista permitida de la ruta.
 */
export const roleGuard: CanActivateFn = (route: ActivatedRouteSnapshot) => {
    const authService = inject(AuthService);
    const router = inject(Router);

    if (!authService.isAuthenticated()) {
        router.navigate(['/app/login']);
        return false;
    }

    const rolesPermitidos = route.data?.['roles'] as string[] | undefined;
    if (!rolesPermitidos || rolesPermitidos.length === 0) {
        return true;
    }

    const rolActual = normalizarRol(authService.userRole());
    if (rolesPermitidos.some((rol) => normalizarRol(rol) === rolActual)) {
        return true;
    }

    console.warn(`Acceso denegado para el rol "${authService.userRole()}". Roles permitidos:`, rolesPermitidos);
    router.navigate(['/app/dashboard']);
    return false;
};

function normalizarRol(rol: string | null | undefined): string {
    return (rol || '').trim().normalize('NFD').replace(/[\u0300-\u036f]/g, '').toLowerCase();
}
