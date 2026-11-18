import { HttpInterceptorFn, HttpErrorResponse } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';
import { AuthService } from '../auth/auth.service';

export const jwtInterceptor: HttpInterceptorFn = (req, next) => {
    const router = inject(Router);
    const authService = inject(AuthService);

    const token = authService.token();

    const authReq = token
        ? req.clone({
              setHeaders: { Authorization: `Bearer ${token}` }
          })
        : req;

    return next(authReq).pipe(
        catchError((error: HttpErrorResponse) => {
            if (error.status === 401) {
                // Mantiene sincronizados el storage y los signals de sesión.
                authService.clearSession();
                router.navigate(['/app/login'], { queryParams: { expired: 'true' } });
                return throwError(() => error);
            }

            // El middleware del backend bloquea los módulos operativos hasta que el
            // funcionario configure sus preguntas de seguridad. Se replica el estado
            // en el cliente y se redirige a la pantalla de configuración.
            if (error.status === 403 && esBloqueoPorPreguntasSeguridad(error)) {
                authService.marcarPreguntasSeguridadPendientes();

                if (!/^\/app\/seguridad(?:\/|\?|$)/.test(router.url)) {
                    router.navigate(['/app/seguridad'], { queryParams: { requerido: 'true' } });
                }
            }

            return throwError(() => error);
        })
    );
};

function esBloqueoPorPreguntasSeguridad(error: HttpErrorResponse): boolean {
    const cuerpo = error.error;
    if (!cuerpo || typeof cuerpo !== 'object') {
        return false;
    }

    return (
        cuerpo.codigo === 'PREGUNTAS_SEGURIDAD_REQUERIDAS' ||
        cuerpo.requiereConfigurarPreguntasSeguridad === true
    );
}
