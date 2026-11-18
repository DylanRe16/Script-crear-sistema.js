import { Component } from '@angular/core';
import { RouterModule } from '@angular/router';
import { CommonModule } from '@angular/common';
import { TooltipModule } from 'primeng/tooltip';
import { LayoutService } from '../service/layout.service';
import { AuthService } from '@/core/auth/auth.service';

@Component({
    selector: 'app-topbar',
    standalone: true,
    imports: [RouterModule, CommonModule, TooltipModule],
    templateUrl: './app.topbar.html'
})
export class AppTopbar {
    constructor(
        public layoutService: LayoutService,
        public authService: AuthService
    ) { }

    toggleDarkMode() {
        this.layoutService.layoutConfig.update((state) => ({ ...state, darkTheme: !state.darkTheme }));
    }
}
