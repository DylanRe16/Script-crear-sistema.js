import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { PageHeaderComponent } from '@/shared/components/page-header/page-header.component';
import { AuthService } from '@/core/auth/auth.service';

@Component({
    selector: 'app-sgvti-dashboard',
    standalone: true,
    imports: [CommonModule, PageHeaderComponent],
    templateUrl: './dashboard.component.html'
})
export class SgvtiDashboardComponent {
    constructor(public authService: AuthService) {}
}
