import { Component, inject } from '@angular/core';
import { RouterModule } from '@angular/router';
import { LayoutService } from './layout/service/layout.service';

@Component({
    selector: 'app-root',
    standalone: true,
    imports: [RouterModule],
    templateUrl: './app.component.html'
})
export class AppComponent {
    // Instancia el servicio de layout al arrancar para aplicar el tema
    // guardado (claro/oscuro) incluso antes de entrar al área autenticada.
    private readonly layoutService = inject(LayoutService);
}
