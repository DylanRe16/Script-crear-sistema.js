import { Routes } from '@angular/router';
import { AppLayout } from './layout/component/app.layout';
import { authGuard, roleGuard } from './core/auth/role.guard';
import { preguntasSeguridadGuard } from './core/auth/security-questions.guard';

export const appRoutes: Routes = [
    { path: '', redirectTo: 'app/login', pathMatch: 'full' },
    {
        path: 'app/login',
        loadComponent: () => import('./features/auth/login.component').then(m => m.SgvtiLoginComponent)
    },
    {
        path: 'app',
        component: AppLayout,
        canActivate: [authGuard, preguntasSeguridadGuard],
        canActivateChild: [authGuard, preguntasSeguridadGuard],
        children: [
            {
                path: 'dashboard',
                loadComponent: () =>
                    import('./features/dashboard/dashboard.component').then(m => m.SgvtiDashboardComponent)
            },
            {
                path: 'cambiar-contrasena',
                loadComponent: () =>
                    import('./features/auth/cambiar-contrasena.component').then(m => m.SgvtiCambiarContrasenaComponent)
            },
            {
                path: 'perfil',
                loadComponent: () =>
                    import('./features/auth/perfil.component').then(m => m.SgvtiPerfilComponent)
            },
            {
                path: 'seguridad',
                loadComponent: () =>
                    import('./features/seguridad/mis-preguntas-seguridad.component').then(m => m.SgvtiMisPreguntasSeguridadComponent)
            },
            {
                path: 'usuarios',
                loadComponent: () =>
                    import('./features/sesiones/sesiones-list/sesiones-list.component').then(m => m.SesionesListComponent),
                canActivate: [roleGuard],
                data: { roles: ['Administrador'] }
            },
            { path: '', redirectTo: 'dashboard', pathMatch: 'full' }
        ]
    },
    {
        path: '**',
        loadComponent: () => import('./features/notfound/notfound').then(m => m.Notfound)
    }
];
