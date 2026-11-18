import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { PreguntasSeguridadService } from './preguntas-seguridad.service';
import { environment } from '../../../environments/environment';
import { PgEstadoPreguntasSeguridadDto, PgPreguntaSeguridadResponseDto } from '../models/pregunta-seguridad.model';

describe('PreguntasSeguridadService', () => {
    let service: PreguntasSeguridadService;
    let httpMock: HttpTestingController;

    beforeEach(() => {
        TestBed.configureTestingModule({
            imports: [HttpClientTestingModule],
            providers: [PreguntasSeguridadService]
        });
        service = TestBed.inject(PreguntasSeguridadService);
        httpMock = TestBed.inject(HttpTestingController);
    });

    afterEach(() => {
        httpMock.verify();
    });

    it('debe consultar y normalizar las preguntas disponibles', (done) => {
        const respuesta: PgPreguntaSeguridadResponseDto[] = [
            {
                idPreguntaSeg: 3,
                sdescripcion: '¿Cuál es el nombre de su primera mascota?',
                benabled: true,
                dfechaCreacion: '2026-11-11T00:00:00Z'
            }
        ];

        service.listarCatalogo().subscribe((lista) => {
            expect(lista.length).toBe(1);
            expect(lista[0].idPreguntaSeg).toBe(3);
            expect(service.catalogo()).toEqual(lista);
            done();
        });

        const req = httpMock.expectOne(`${environment.apiUrl}/PreguntasSeguridad?soloActivas=true`);
        expect(req.request.method).toBe('GET');
        req.flush(respuesta);
    });

    it('debe consultar el estado de configuración', (done) => {
        const estado: PgEstadoPreguntasSeguridadDto = {
            tienePreguntasConfiguradas: false,
            totalConfiguradas: 0,
            maximoPermitido: 6,
            requiereConfiguracion: true,
            mensaje: 'Debe configurar sus preguntas de seguridad.'
        };

        service.obtenerEstado().subscribe((resultado) => {
            expect(resultado.requiereConfiguracion).toBeTrue();
            expect(service.estado()).toEqual(estado);
            done();
        });

        const req = httpMock.expectOne(`${environment.apiUrl}/Seguridad/estado`);
        expect(req.request.method).toBe('GET');
        req.flush(estado);
    });

    it('debe asignar las preguntas del usuario', (done) => {
        const dto = { preguntas: [{ idPreguntaSeg: 5, respuesta: 'Caracas' }] };
        service.asignarMisPreguntas(dto).subscribe((estado) => {
            expect(estado.totalConfiguradas).toBe(1);
            done();
        });

        const req = httpMock.expectOne(`${environment.apiUrl}/Seguridad/mis-preguntas`);
        expect(req.request.method).toBe('POST');
        expect(req.request.body).toEqual(dto);
        req.flush({
            tienePreguntasConfiguradas: true,
            totalConfiguradas: 1,
            maximoPermitido: 6,
            requiereConfiguracion: false,
            mensaje: 'Preguntas configuradas.'
        });
    });
});
