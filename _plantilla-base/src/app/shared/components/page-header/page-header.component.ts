import { ChangeDetectionStrategy, Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';

/**
 * Encabezado de página reutilizable.
 *
 * Estandariza el título, el ícono, la descripción y la zona de acciones
 * de todas las vistas del sistema, con soporte de modo claro/oscuro.
 */
@Component({
    selector: 'app-page-header',
    standalone: true,
    imports: [CommonModule],
    changeDetection: ChangeDetectionStrategy.OnPush,
    templateUrl: './page-header.component.html'
})
export class PageHeaderComponent {
    @Input() title = '';
    @Input() subtitle = '';
    @Input() icon = 'pi pi-file';
    @Input() eyebrow = '';
}
