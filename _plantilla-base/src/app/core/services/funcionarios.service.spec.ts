import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { FuncionariosService } from './funcionarios.service';
import { environment } from '../../../environments/environment';
import { PgFuncionarioUpdateDto } from '../models/funcionario.model';

describe('FuncionariosService', () => {
    let service: FuncionariosService;
    let httpMock: HttpTestingController;

    beforeEach(() => {
        TestBed.configureTestingModule({
            imports: [HttpClientTestingModule],
            providers: [FuncionariosService]
        });
        service = TestBed.inject(FuncionariosService);
        httpMock = TestBed.inject(HttpTestingController);
    });

    afterEach(() => {
        httpMock.verify();
    });

    it('debe consultar y normalizar las cuentas para la administración', (done) => {
        service.listar(false).subscribe((funcionarios) => {
            expect(funcionarios.length).toBe(1);
            expect(funcionarios[0].idFuncionario).toBe(10);
            expect(typeof funcionarios[0].idFuncionario).toBe('number');
            expect(funcionarios[0].rolId).toBe(3);
            done();
        });

        const req = httpMock.expectOne(`${environment.apiUrl}/Funcionarios?soloActivos=false`);
        expect(req.request.method).toBe('GET');
        req.flush([{ idFuncionario: '10', rolId: '3', benabled: true }]);
    });

    it('debe actualizar datos, rol y estado de una cuenta', (done) => {
        const updateDto: PgFuncionarioUpdateDto = {
            sprimerNombre: 'Juan Modificado',
            ssegundoNombre: null,
            sprimerApellido: 'Pérez',
            ssegundoApellido: null,
            rolId: 3,
            benabled: false
        };

        service.actualizar(10, updateDto).subscribe((actualizado) => {
            expect(actualizado.idFuncionario).toBe(10);
            expect(actualizado.benabled).toBe(false);
            done();
        });

        const req = httpMock.expectOne(`${environment.apiUrl}/Funcionarios/10`);
        expect(req.request.method).toBe('PUT');
        expect(req.request.body).toEqual(updateDto);
        req.flush({ idFuncionario: 10, ...updateDto });
    });

    it('debe solicitar el restablecimiento de contraseña', (done) => {
        service.restablecerContrasena(12).subscribe(() => done());

        const req = httpMock.expectOne(`${environment.apiUrl}/Funcionarios/12/restablecer-contrasena`);
        expect(req.request.method).toBe('POST');
        req.flush({ success: true });
    });

    it('debe solicitar la eliminación de las preguntas de seguridad del usuario', (done) => {
        service.restablecerPreguntasSeguridad(12).subscribe(() => done());

        const req = httpMock.expectOne(`${environment.apiUrl}/Funcionarios/12/restablecer-preguntas-seguridad`);
        expect(req.request.method).toBe('POST');
        req.flush({ success: true });
    });
});
