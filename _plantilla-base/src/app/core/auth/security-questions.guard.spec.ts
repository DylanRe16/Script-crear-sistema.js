import { TestBed } from '@angular/core/testing';
import { ActivatedRouteSnapshot, Router, RouterStateSnapshot, UrlTree } from '@angular/router';
import { Observable, firstValueFrom, of } from 'rxjs';
import { AuthService } from './auth.service';
import { preguntasSeguridadGuard } from './security-questions.guard';
import { PreguntasSeguridadService } from '../services/preguntas-seguridad.service';

describe('preguntasSeguridadGuard', () => {
    const securityUrlTree = {} as UrlTree;
    const passwordUrlTree = {} as UrlTree;

    let authService: {
        isAuthenticated: jasmine.Spy;
        mustConfigureSecurityQuestions: jasmine.Spy;
        mustChangePassword: jasmine.Spy;
        marcarPreguntasSeguridadPendientes: jasmine.Spy;
        marcarPreguntasSeguridadConfiguradas: jasmine.Spy;
    };
    let preguntasService: { obtenerEstado: jasmine.Spy };
    let router: { createUrlTree: jasmine.Spy };

    beforeEach(() => {
        authService = {
            isAuthenticated: jasmine.createSpy().and.returnValue(true),
            mustConfigureSecurityQuestions: jasmine.createSpy().and.returnValue(false),
            mustChangePassword: jasmine.createSpy().and.returnValue(false),
            marcarPreguntasSeguridadPendientes: jasmine.createSpy(),
            marcarPreguntasSeguridadConfiguradas: jasmine.createSpy()
        };
        preguntasService = {
            obtenerEstado: jasmine.createSpy().and.returnValue(of({ tienePreguntasConfiguradas: false }))
        };
        router = {
            createUrlTree: jasmine.createSpy().and.returnValues(securityUrlTree, passwordUrlTree)
        };

        TestBed.configureTestingModule({
            providers: [
                { provide: AuthService, useValue: authService },
                { provide: PreguntasSeguridadService, useValue: preguntasService },
                { provide: Router, useValue: router }
            ]
        });
    });

    it('redirige a seguridad y marca pendientes si el backend indica que no hay preguntas', async () => {
        const result = TestBed.runInInjectionContext(() =>
            preguntasSeguridadGuard({} as ActivatedRouteSnapshot, { url: '/app/dashboard' } as RouterStateSnapshot)
        );

        await expectAsync(firstValueFrom(result as Observable<boolean | UrlTree>)).toBeResolvedTo(securityUrlTree);
        expect(authService.marcarPreguntasSeguridadPendientes).toHaveBeenCalled();
        expect(router.createUrlTree).toHaveBeenCalledWith(
            ['/app/seguridad'],
            { queryParams: { requerido: 'true' } }
        );
    });

    it('redirige al cambio obligatorio de contraseña después de validar preguntas configuradas', async () => {
        preguntasService.obtenerEstado.and.returnValue(of({ tienePreguntasConfiguradas: true }));
        authService.mustChangePassword.and.returnValue(true);

        const result = TestBed.runInInjectionContext(() =>
            preguntasSeguridadGuard({} as ActivatedRouteSnapshot, { url: '/app/dashboard' } as RouterStateSnapshot)
        );

        await expectAsync(firstValueFrom(result as Observable<boolean | UrlTree>)).toBeResolvedTo(passwordUrlTree);
        expect(authService.marcarPreguntasSeguridadConfiguradas).toHaveBeenCalled();
        expect(router.createUrlTree).toHaveBeenCalledWith(
            ['/app/cambiar-contrasena'],
            { queryParams: { requerido: 'true' } }
        );
    });
});
