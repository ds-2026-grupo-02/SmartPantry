import { ComponentFixture, TestBed } from '@angular/core/testing';
import { CommonModule } from '@angular/common';
import { LocalizationModule, LocalizationService, PagedResultDto } from '@abp/ng.core';
import { ToasterService, ThemeSharedModule } from '@abp/ng.theme.shared';
import { of, throwError } from 'rxjs';
import { vi, describe, it, expect, beforeEach } from 'vitest';

import { AlertsComponent } from './alerts.component';
import { DespensaService } from '../proxy/despensa/despensa.service';
import { DespensaDto } from '../proxy/despensa/models';
import { EstadoVencimiento } from '../proxy/despensa/estado-vencimiento.enum';
import { AdvertenciaVencimientoService } from '../proxy/advertencias/advertencia-vencimiento.service';
import { AdvertenciaVencimientoDto } from '../proxy/advertencias/models';
import { PreferenciaService } from '../proxy/preferencias/preferencia.service';
import { PreferenciaDto } from '../proxy/preferencias/models';
import { ProductoService } from '../proxy/productos/producto.service';

describe('AlertsComponent', () => {
  let component: AlertsComponent;
  let fixture: ComponentFixture<AlertsComponent>;

  let mockDespensaService: { getList: ReturnType<typeof vi.fn> };
  let mockProductoService: { get: ReturnType<typeof vi.fn> };
  let mockAdvertenciaService: {
    getActivas: ReturnType<typeof vi.fn>;
    marcarComoVista: ReturnType<typeof vi.fn>;
  };
  let mockPreferenciaService: { get: ReturnType<typeof vi.fn> };
  let mockToasterService: { error: ReturnType<typeof vi.fn> };
  let mockLocalizationService: { currentLang: string; instant: ReturnType<typeof vi.fn> };

  const sampleItems: DespensaDto[] = [
    {
      id: '11111111-1111-1111-1111-111111111111',
      usuarioId: 'user-1',
      productoId: 'prod-1',
      nombreProductoEs: 'Arroz',
      nombreProductoEn: 'Rice',
      cantidadProducto: '1 kg',
      fechaVencimiento: '2027-12-31T00:00:00Z',
      estadoVencimiento: EstadoVencimiento.Vigente,
      esConsumido: false,
    },
  ];

  const sampleAlertas: AdvertenciaVencimientoDto[] = [
    {
      id: 'alert-1',
      itemDespensaId: '11111111-1111-1111-1111-111111111111',
      tipoAdvertencia: 'VencimientoProximo',
      estadoVencimiento: EstadoVencimiento.ProximoAVencer,
      fechaVencimiento: '2026-12-02',
    },
  ];

  const samplePreferencias: PreferenciaDto = {
    alergenos: ['Arroz'],
    ingredientes: [],
    mostrarAlertas: true,
  };

  beforeEach(async () => {
    mockDespensaService = {
      getList: vi.fn().mockReturnValue(
        of({
          items: sampleItems,
          totalCount: sampleItems.length,
        } as PagedResultDto<DespensaDto>)
      ),
    };

    mockProductoService = {
      get: vi.fn().mockReturnValue(
        of({
          id: 'prod-1',
          alergenos: ['Arroz'],
          ingredientes: [],
        })
      ),
    };

    mockAdvertenciaService = {
      getActivas: vi.fn().mockReturnValue(of({ items: sampleAlertas })),
      marcarComoVista: vi.fn().mockReturnValue(of({})),
    };

    mockPreferenciaService = {
      get: vi.fn().mockReturnValue(of(samplePreferencias)),
    };

    mockToasterService = {
      error: vi.fn(),
    };

    mockLocalizationService = {
      currentLang: 'es',
      instant: vi.fn((key: string) => key),
    };

    await TestBed.configureTestingModule({
      imports: [CommonModule, LocalizationModule, ThemeSharedModule, AlertsComponent],
      providers: [
        { provide: DespensaService, useValue: mockDespensaService },
        { provide: ProductoService, useValue: mockProductoService },
        { provide: AdvertenciaVencimientoService, useValue: mockAdvertenciaService },
        { provide: PreferenciaService, useValue: mockPreferenciaService },
        { provide: ToasterService, useValue: mockToasterService },
        { provide: LocalizationService, useValue: mockLocalizationService },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(AlertsComponent);
    component = fixture.componentInstance;
  });

  it('debe crearse correctamente', () => {
    expect(component).toBeTruthy();
  });

  it('debe cargar alertas activas y compatibilidad al iniciar', () => {
    component.ngOnInit();

    expect(mockAdvertenciaService.getActivas).toHaveBeenCalledWith({ skipHandleError: true });
    expect(component.alertasActivas).toEqual(sampleAlertas);
    expect(component.alertasCompatibilidad.length).toBe(1);
    expect(component.alertasCompatibilidad[0].coincidencia).toBe('Arroz');
  });

  it('no debe generar alertas de compatibilidad cuando mostrarAlertas es false', () => {
    mockPreferenciaService.get.mockReturnValue(
      of({
        alergenos: ['Arroz'],
        ingredientes: ['Gluten'],
        mostrarAlertas: false,
      })
    );

    component.loadAlertas();

    expect(component.alertasCompatibilidad).toEqual([]);
  });

  it('debe detectar compatibilidad cuando alergenos e ingredientes del producto llegan como string separado por comas', () => {
    mockDespensaService.getList.mockReturnValue(
      of({
        items: [
          {
            id: '44444444-4444-4444-4444-444444444444',
            usuarioId: 'user-1',
            productoId: 'prod-mix',
            nombreProductoEs: 'Snack',
            nombreProductoEn: 'Snack',
            cantidadProducto: '200 g',
            fechaVencimiento: null,
            estadoVencimiento: EstadoVencimiento.SinFecha,
            esConsumido: false,
          },
        ],
        totalCount: 1,
      } as PagedResultDto<DespensaDto>)
    );

    mockProductoService.get.mockReturnValue(
      of({
        id: 'prod-mix',
        alergenos: 'Maní, Gluten',
        ingredientes: 'Leche, Azúcar',
      })
    );

    mockPreferenciaService.get.mockReturnValue(
      of({
        alergenos: ['Maní'],
        ingredientes: ['Leche'],
        mostrarAlertas: true,
      })
    );

    component.loadAlertas();

    expect(component.alertasCompatibilidad).toContainEqual({
      id: 'compat-alergeno-44444444-4444-4444-4444-444444444444-maní',
      itemDespensaId: '44444444-4444-4444-4444-444444444444',
      nombreProducto: 'Snack',
      tipo: 'alergeno',
      coincidencia: 'Maní',
    });

    expect(component.alertasCompatibilidad).toContainEqual({
      id: 'compat-ingrediente-44444444-4444-4444-4444-444444444444-leche',
      itemDespensaId: '44444444-4444-4444-4444-444444444444',
      nombreProducto: 'Snack',
      tipo: 'ingrediente',
      coincidencia: 'Leche',
    });
  });

  it('debe descartar una alerta de compatibilidad de la lista', () => {
    const alertaADescartar = {
      id: 'compat-1',
      itemDespensaId: 'item-1',
      nombreProducto: 'Arroz',
      tipo: 'alergeno' as const,
      coincidencia: 'Arroz',
    };

    component.alertasCompatibilidad = [
      alertaADescartar,
      {
        id: 'compat-2',
        itemDespensaId: 'item-2',
        nombreProducto: 'Pan',
        tipo: 'ingrediente' as const,
        coincidencia: 'Gluten',
      },
    ];

    component.descartarAlertaCompatibilidad(alertaADescartar);

    expect(component.alertasCompatibilidad.map((a) => a.id)).toEqual(['compat-2']);
  });

  it('debe activar hasErrorAlertas y mostrar toaster cuando falla loadAlertas', () => {
    mockAdvertenciaService.getActivas.mockReturnValue(throwError(() => new Error('Error')));
    component.loadAlertas();

    expect(component.hasErrorAlertas).toBe(true);
    expect(mockToasterService.error).toHaveBeenCalledWith(
      '::Pantry:Error:LoadFailed',
      '::Pantry:Error:ConnectionTitle'
    );
  });

  it('debe marcar alerta como vista y recargar', () => {
    mockAdvertenciaService.getActivas.mockClear();
    component.marcarAlertaVista(sampleAlertas[0]);

    expect(mockAdvertenciaService.marcarComoVista).toHaveBeenCalledWith('alert-1', {
      skipHandleError: true,
    });
    expect(mockAdvertenciaService.getActivas).toHaveBeenCalled();
  });

  it('debe mostrar error en toaster si falla marcarAlertaVista', () => {
    mockAdvertenciaService.marcarComoVista.mockReturnValue(throwError(() => new Error('Error')));
    mockAdvertenciaService.getActivas.mockClear();

    component.marcarAlertaVista(sampleAlertas[0]);

    expect(mockToasterService.error).toHaveBeenCalled();
    expect(mockAdvertenciaService.getActivas).not.toHaveBeenCalled();
  });

  it('debe resolver el nombre del ítem en alertas aunque no esté en la página actual', () => {
    (component as unknown as { nombresItemsActivos: Map<string, string> }).nombresItemsActivos =
      new Map<string, string>([['11111111-1111-1111-1111-111111111111', 'Arroz']]);

    expect(
      component.getNombreProductoPorId('11111111-1111-1111-1111-111111111111', 'VencimientoProximo')
    ).toBe('Arroz');
    expect(
      component.getNombreProductoPorId('id-inexistente', 'VencimientoProximo')
    ).toBe('::Pantry:Status:ExpiringSoon');
  });
});
