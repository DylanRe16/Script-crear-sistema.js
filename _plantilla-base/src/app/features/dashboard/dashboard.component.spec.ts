import { ComponentFixture, TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { AuthService } from '@/core/auth/auth.service';
import { SgvtiDashboardComponent } from './dashboard.component';

describe('SgvtiDashboardComponent', () => {
    let fixture: ComponentFixture<SgvtiDashboardComponent>;
    let httpMock: HttpTestingController;

    beforeEach(() => {
        TestBed.configureTestingModule({
            imports: [HttpClientTestingModule, SgvtiDashboardComponent],
            providers: [
                {
                    provide: AuthService,
                    useValue: { currentUser: () => ({ nombreCompleto: 'Usuario de prueba' }) }
                }
            ]
        });

        fixture = TestBed.createComponent(SgvtiDashboardComponent);
        httpMock = TestBed.inject(HttpTestingController);
    });

    afterEach(() => {
        httpMock.verify();
    });

    it('muestra la bienvenida sin realizar peticiones HTTP al cargar', () => {
        fixture.detectChanges();

        expect(fixture.nativeElement.textContent).toContain('Usuario de prueba');
    });
});
