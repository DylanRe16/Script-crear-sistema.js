import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { catchError, map, of } from 'rxjs';
import { AuthService } from './auth.service';
import { PreguntasSeguridadService } from '../services/preguntas-seguridad.service';

export const preguntasSeguridadGuard: CanActivateFn = (_route, state) => {
    const authService = inject(AuthService);
    const router = inject(Router);
    const preguntasService = inject(PreguntasSeguridadService);

    if (!authService.isAuthenticated()) {
        return true;
    }

    const rutaActual = state.url.split(/[?#]/, 1)[0];
    const esRutaSeguridad = rutaActual === '/app/seguridad';
    const esRutaCambioContrasena = rutaActual === '/app/cambiar-contrasena';

    if (authService.mustConfigureSecurityQuestions()) {
        return esRutaSeguridad
            ? true
            : router.createUrlTree(['/app/seguridad'], { queryParams: { requerido: 'true' } });
    }

    return preguntasService.obtenerEstado().pipe(
        map((estado) => {
            if (!estado.tienePreguntasConfiguradas) {
                authService.marcarPreguntasSeguridadPendientes();
                return esRutaSeguridad
                    ? true
                    : router.createUrlTree(['/app/seguridad'], { queryParams: { requerido: 'true' } });
            }

            authService.marcarPreguntasSeguridadConfiguradas();
            if (authService.mustChangePassword() && !esRutaCambioContrasena) {
                return router.createUrlTree(['/app/cambiar-contrasena'], { queryParams: { requerido: 'true' } });
            }

            return true;
        }),
        catchError((error: unknown) => {
            console.error('No fue posible verificar los requisitos de la cuenta antes de navegar:', error);
            return of(
                esRutaSeguridad
                    ? true
                    : router.createUrlTree(['/app/seguridad'], { queryParams: { verificar: 'true' } })
            );
        })
    );
};
