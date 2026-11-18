import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { finalize } from 'rxjs';
import { ButtonModule } from 'primeng/button';
import { RouterModule } from '@angular/router';
import { AuthService } from '@/core/auth/auth.service';
import { PgFuncionarioResponseDto } from '@/core/models/funcionario.model';
import { getApiErrorMessage } from '@/core/http/error.util';
import { PageHeaderComponent } from '@/shared/components/page-header/page-header.component';

@Component({
    selector: 'app-sgvti-perfil',
    standalone: true,
    imports: [CommonModule, RouterModule, ButtonModule, PageHeaderComponent],
    templateUrl: './perfil.component.html'
})
export class SgvtiPerfilComponent implements OnInit {
    readonly perfil = signal<PgFuncionarioResponseDto | null>(null);
    readonly cargando = signal(false);
    readonly mensajeError = signal<string | null>(null);

    constructor(private authService: AuthService) {}

    ngOnInit(): void {
        this.cargarPerfil();
    }

    cargarPerfil(): void {
        this.cargando.set(true);
        this.mensajeError.set(null);
        this.authService
            .getProfileMe()
            .pipe(finalize(() => this.cargando.set(false)))
            .subscribe({
                next: (perfil) => this.perfil.set(perfil),
                error: (error: unknown) => this.mensajeError.set(getApiErrorMessage(error, 'No fue posible cargar el perfil.'))
            });
    }

    nombres(usuario: PgFuncionarioResponseDto): string {
        return [usuario.sprimerNombre, usuario.ssegundoNombre]
            .filter((nombre): nombre is string => !!nombre?.trim())
            .join(' ') || '—';
    }

    apellidos(usuario: PgFuncionarioResponseDto): string {
        return [usuario.sprimerApellido, usuario.ssegundoApellido]
            .filter((apellido): apellido is string => !!apellido?.trim())
            .join(' ') || '—';
    }

    iniciales(usuario: PgFuncionarioResponseDto): string {
        return [this.nombres(usuario), this.apellidos(usuario)]
            .filter((valor) => valor !== '—')
            .map((valor) => valor.charAt(0))
            .join('')
            .slice(0, 2)
            .toUpperCase() || 'U';
    }
}
