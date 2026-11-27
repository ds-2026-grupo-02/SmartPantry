import { ComponentFixture, TestBed } from '@angular/core/testing';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule } from '@angular/forms';
import { LocalizationModule, LocalizationService, PagedResultDto } from '@abp/ng.core';
import {
  ConfirmationService,
  Confirmation,
  ToasterService,
  ThemeSharedModule,
  DEFAULT_VALIDATION_BLUEPRINTS,
} from '@abp/ng.theme.shared';
import { NgxValidateCoreModule } from '@ngx-validate/core';
import { of, throwError } from 'rxjs';
import { vi, describe, it, expect, beforeEach } from 'vitest';

import { PantryComponent } from './pantry.component';
import { DespensaService } from '../proxy/despensa/despensa.service';
import { DespensaDto } from '../proxy/despensa/models';
import { EstadoVencimiento } from '../proxy/despensa/estado-vencimiento.enum';
import { AdvertenciaVencimientoService } from '../proxy/advertencias/advertencia-vencimiento.service';
import { AdvertenciaVencimientoDto } from '../proxy/advertencias/models';

describe('PantryComponent', () => {
  let component: PantryComponent;
  let fixture: ComponentFixture<PantryComponent>;

  let mockDespensaService: {
    getList: ReturnType<typeof vi.fn>;
    update: ReturnType<typeof vi.fn>;
    delete: ReturnType<typeof vi.fn>;
    marcarConsumido: ReturnType<typeof vi.fn>;
    getNotaPrivada: ReturnType<typeof vi.fn>;
    guardarNotaPrivada: ReturnType<typeof vi.fn>;
    eliminarNotaPrivada: ReturnType<typeof vi.fn>;
  };

  let mockAdvertenciaService: {
    getActivas: ReturnType<typeof vi.fn>;
    marcarComoVista: ReturnType<typeof vi.fn>;
  };

  let mockConfirmationService: {
    warn: ReturnType<typeof vi.fn>;
  };

  let mockToasterService: {
    error: ReturnType<typeof vi.fn>;
  };

  let mockLocalizationService: {
    currentLang: string;
    instant: ReturnType<typeof vi.fn>;
  };

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
    {
      id: '22222222-2222-2222-2222-222222222222',
      usuarioId: 'user-1',
      productoId: 'prod-2',
      nombreProductoEs: 'Fideos',
      nombreProductoEn: 'Pasta',
      cantidadProducto: '500 g',
      fechaVencimiento: null,
      estadoVencimiento: EstadoVencimiento.SinFecha,
      esConsumido: false,
    },
  ];

  const sampleAlertas: AdvertenciaVencimientoDto[] = [
    {
      id: 'alert-1',
      itemDespensaId: '11111111-1111-1111-1111-111111111111',
      tipoAdvertencia: 'ProximoAVencer',
      estadoVencimiento: EstadoVencimiento.ProximoAVencer,
      fechaVencimiento: '2026-12-02',
    },
  ];

  beforeEach(async () => {
    mockDespensaService = {
      getList: vi.fn().mockReturnValue(
        of({
          items: sampleItems,
          totalCount: sampleItems.length,
        } as PagedResultDto<DespensaDto>)
      ),
      update: vi.fn().mockReturnValue(of({})),
      delete: vi.fn().mockReturnValue(of({})),
      marcarConsumido: vi.fn().mockReturnValue(of({})),
      getNotaPrivada: vi.fn().mockReturnValue(of({ contenido: 'Nota guardada' })),
      guardarNotaPrivada: vi.fn().mockReturnValue(of({})),
      eliminarNotaPrivada: vi.fn().mockReturnValue(of({})),
    };

    mockAdvertenciaService = {
      getActivas: vi.fn().mockReturnValue(of({ items: sampleAlertas })),
      marcarComoVista: vi.fn().mockReturnValue(of({})),
    };

    mockConfirmationService = {
      warn: vi.fn().mockReturnValue(of(Confirmation.Status.confirm)),
    };

    mockToasterService = {
      error: vi.fn(),
    };

    mockLocalizationService = {
      currentLang: 'es',
      instant: vi.fn((key: string) => key),
    };

    await TestBed.configureTestingModule({
      imports: [
        CommonModule,
        ReactiveFormsModule,
        LocalizationModule,
        ThemeSharedModule,
        NgxValidateCoreModule.forRoot({ blueprints: DEFAULT_VALIDATION_BLUEPRINTS }),
        PantryComponent,
      ],
      providers: [
        { provide: DespensaService, useValue: mockDespensaService },
        { provide: AdvertenciaVencimientoService, useValue: mockAdvertenciaService },
        { provide: ConfirmationService, useValue: mockConfirmationService },
        { provide: ToasterService, useValue: mockToasterService },
        { provide: LocalizationService, useValue: mockLocalizationService },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(PantryComponent);
    component = fixture.componentInstance;
  });

  it('debe crearse correctamente el componente', () => {
    expect(component).toBeTruthy();
  });

  // =========================================================================
  // 1. ESTADO DE CARGA (LOADING)
  // =========================================================================
  describe('Estado de Carga', () => {
    it('debe mostrar el spinner de carga mientras la solicitud está pendiente', () => {
      component.isLoading = true;
      component.hasError = false;
      component.totalCount = 0;
      component['cdr'].detectChanges();

      const element: HTMLElement = fixture.nativeElement;
      const spinner = element.querySelector('.spinner-border');
      expect(spinner).toBeTruthy();
      expect(element.querySelector('.alert-danger')).toBeFalsy();
    });
  });

  // =========================================================================
  // 2. ESTADO DE ERROR (ERROR STATE Y REINTENTAR)
  // =========================================================================
  describe('Estado de Error', () => {
    it('debe mostrar mensaje amigable de error y botón de reintentar cuando falla la llamada al servidor', () => {
      component.isLoading = false;
      component.hasError = true;
      component.totalCount = 0;
      component['cdr'].detectChanges();

      const element: HTMLElement = fixture.nativeElement;
      const alert = element.querySelector('.alert-danger');
      expect(alert).toBeTruthy();

      const retryBtn = alert?.querySelector('button');
      expect(retryBtn).toBeTruthy();
    });

    it('al hacer clic en el botón de reintentar debe invocar a loadItems, list.get() y loadAlertas()', () => {
      const listSpy = vi.spyOn(component.list, 'get');
      mockAdvertenciaService.getActivas.mockClear();

      component.loadItems();

      expect(component.hasError).toBe(false);
      expect(listSpy).toHaveBeenCalled();
      expect(mockAdvertenciaService.getActivas).toHaveBeenCalled();
    });
  });

  // =========================================================================
  // 3. ESTADO DE LISTA VACÍA
  // =========================================================================
  describe('Estado de Lista Vacía', () => {
    it('debe mostrar el mensaje de despensa vacía cuando no hay ítems registrados', () => {
      component.items = [];
      component.totalCount = 0;
      component.isLoading = false;
      component.hasError = false;
      fixture.detectChanges();

      const element: HTMLElement = fixture.nativeElement;
      const textMuted = element.querySelector('.text-muted');
      expect(textMuted).toBeTruthy();
      expect(element.querySelector('ngx-datatable')).toBeFalsy();
    });
  });

  // =========================================================================
  // 4. PESTAÑAS ACTIVOS E HISTORIAL DE CONSUMIDOS
  // =========================================================================
  describe('Pestañas Activos e Historial', () => {
    it('debe alternar soloConsumidos y refrescar la lista al cambiar de pestaña', () => {
      const listSpy = vi.spyOn(component.list, 'get');

      component.setSoloConsumidos(true);
      expect(component.soloConsumidos).toBe(true);
      expect(listSpy).toHaveBeenCalled();

      // Si se pasa el mismo valor no debe volver a pedir
      listSpy.mockClear();
      component.setSoloConsumidos(true);
      expect(listSpy).not.toHaveBeenCalled();
    });
  });

  // =========================================================================
  // 5. ESTADO CON RESULTADOS E IDIOMA
  // =========================================================================
  describe('Estado con Resultados', () => {
    it('debe renderizar la grilla de datos cuando hay ítems registrados', () => {
      component.items = sampleItems;
      component.totalCount = sampleItems.length;
      component.isLoading = false;
      component.hasError = false;
      fixture.detectChanges();

      const element: HTMLElement = fixture.nativeElement;
      const datatable = element.querySelector('ngx-datatable');
      expect(datatable).toBeTruthy();
      expect(component.items.length).toBe(2);
    });

    it('debe obtener el nombre según el idioma activo (es/en) con fallback', () => {
      const item: DespensaDto = {
        nombreProductoEs: 'Manzana',
        nombreProductoEn: 'Apple',
      };

      mockLocalizationService.currentLang = 'es';
      expect(component.getNombreProducto(item)).toBe('Manzana');

      mockLocalizationService.currentLang = 'en';
      expect(component.getNombreProducto(item)).toBe('Apple');

      mockLocalizationService.currentLang = 'es';
      expect(component.getNombreProducto({ nombreProductoEn: 'OnlyEnglish' })).toBe('OnlyEnglish');

      mockLocalizationService.currentLang = 'en';
      expect(component.getNombreProducto({ nombreProductoEs: 'SoloEspanol' })).toBe('SoloEspanol');
    });
  });

  // =========================================================================
  // 6. EDICIÓN: DATOS VÁLIDOS E INVÁLIDOS
  // =========================================================================
  describe('Edición de Ítem', () => {
    beforeEach(() => {
      component.openEditModal(sampleItems[0]);
    });

    it('debe abrir el modal con los valores precargados del ítem', () => {
      expect(component.isModalOpen).toBe(true);
      expect(component.editingId).toBe(sampleItems[0].id);
      expect(component.form.get('fechaVencimiento')?.value).toBe('2027-12-31');
    });

    it('NO debe llamar a despensaService.update cuando la fecha es inválida', () => {
      component.form.get('fechaVencimiento')?.setValue('2027-02-30');
      component.fechaInputInvalid = false;

      expect(component.form.get('fechaVencimiento')?.hasError('fechaInvalida')).toBe(true);
      expect(component.form.invalid).toBe(true);

      component.save();

      expect(mockDespensaService.update).not.toHaveBeenCalled();
      expect(component.isSaving).toBe(false);
    });

    it('NO debe llamar a update cuando fechaInputInvalid es true', () => {
      component.fechaInputInvalid = true;
      component.save();
      expect(mockDespensaService.update).not.toHaveBeenCalled();
    });

    it('debe llamar a despensaService.update, cerrar modal y refrescar lista y alertas cuando es válido', () => {
      component.form.get('fechaVencimiento')?.setValue('2028-05-15');
      const listSpy = vi.spyOn(component.list, 'get');
      mockAdvertenciaService.getActivas.mockClear();

      component.save();

      expect(mockDespensaService.update).toHaveBeenCalledWith(
        sampleItems[0].id,
        { fechaVencimiento: '2028-05-15' },
        { skipHandleError: true }
      );
      expect(component.isModalOpen).toBe(false);
      expect(listSpy).toHaveBeenCalled();
      expect(mockAdvertenciaService.getActivas).toHaveBeenCalled();
    });

    it('debe manejar error del servidor al guardar sin cerrar el modal y mostrando toaster', () => {
      mockDespensaService.update.mockReturnValue(throwError(() => new Error('Server error')));
      component.form.get('fechaVencimiento')?.setValue('2028-05-15');

      component.save();

      expect(component.isSaving).toBe(false);
      expect(mockToasterService.error).toHaveBeenCalled();
      expect(component.isModalOpen).toBe(true);
    });
  });

  // =========================================================================
  // 7. NOTA PRIVADA
  // =========================================================================
  describe('Nota Privada de Ítem', () => {
    it('debe abrir el modal, cargar la nota existente y guardarla recortando espacios', () => {
      component.openNotaModal(sampleItems[0]);

      expect(component.isNotaModalOpen).toBe(true);
      expect(component.notaItemId).toBe(sampleItems[0].id);
      expect(mockDespensaService.getNotaPrivada).toHaveBeenCalledWith(sampleItems[0].id, {
        skipHandleError: true,
      });
      expect(component.notaForm.get('contenido')?.value).toBe('Nota guardada');

      component.notaForm.get('contenido')?.setValue('  Nota actualizada  ');
      component.saveNota();

      expect(mockDespensaService.guardarNotaPrivada).toHaveBeenCalledWith(
        sampleItems[0].id,
        { contenido: 'Nota actualizada' },
        { skipHandleError: true }
      );
      expect(component.isNotaModalOpen).toBe(false);
    });

    it('NO debe guardar si el contenido de la nota está vacío', () => {
      component.openNotaModal(sampleItems[0]);
      component.notaForm.get('contenido')?.setValue('');

      expect(component.notaForm.invalid).toBe(true);
      component.saveNota();
      expect(mockDespensaService.guardarNotaPrivada).not.toHaveBeenCalled();
    });

    it('debe eliminar la nota privada y cerrar el modal', () => {
      component.openNotaModal(sampleItems[0]);
      component.deleteNota();

      expect(mockDespensaService.eliminarNotaPrivada).toHaveBeenCalledWith(sampleItems[0].id, {
        skipHandleError: true,
      });
      expect(component.isNotaModalOpen).toBe(false);
    });
  });

  // =========================================================================
  // 8. ALERTAS ACTIVAS
  // =========================================================================
  describe('Alertas Activas', () => {
    it('debe cargar alertas activas al iniciar', () => {
      component.ngOnInit();

      expect(mockAdvertenciaService.getActivas).toHaveBeenCalledWith({ skipHandleError: true });
      expect(component.alertasActivas).toEqual(sampleAlertas);
    });

    it('debe marcar alerta como vista y recargar alertas activas', () => {
      mockAdvertenciaService.getActivas.mockClear();
      component.marcarAlertaVista(sampleAlertas[0]);

      expect(mockAdvertenciaService.marcarComoVista).toHaveBeenCalledWith('alert-1', {
        skipHandleError: true,
      });
      expect(mockAdvertenciaService.getActivas).toHaveBeenCalled();
    });

    it('debe mostrar toaster de error si falla marcarAlertaVista', () => {
      mockAdvertenciaService.marcarComoVista.mockReturnValue(throwError(() => new Error('Error')));
      component.marcarAlertaVista(sampleAlertas[0]);

      expect(mockToasterService.error).toHaveBeenCalled();
    });
  });

  // =========================================================================
  // 9. CONFIRMACIÓN Y CANCELACIÓN DE ELIMINACIÓN
  // =========================================================================
  describe('Eliminación de Ítem', () => {
    const itemId = '11111111-1111-1111-1111-111111111111';

    it('debe invocar a delete, recargar la lista y alertas si el usuario CONFIRMA', () => {
      mockConfirmationService.warn.mockReturnValue(of(Confirmation.Status.confirm));
      const listSpy = vi.spyOn(component.list, 'get');
      mockAdvertenciaService.getActivas.mockClear();

      component.deleteItem(itemId);

      expect(mockConfirmationService.warn).toHaveBeenCalledWith('', '::Pantry:DeleteConfirmation');
      expect(mockDespensaService.delete).toHaveBeenCalledWith(itemId, { skipHandleError: true });
      expect(listSpy).toHaveBeenCalled();
      expect(mockAdvertenciaService.getActivas).toHaveBeenCalled();
    });

    it('NO debe invocar a delete si el usuario CANCELA', () => {
      mockConfirmationService.warn.mockReturnValue(of(Confirmation.Status.dismiss));

      component.deleteItem(itemId);

      expect(mockConfirmationService.warn).toHaveBeenCalledWith('', '::Pantry:DeleteConfirmation');
      expect(mockDespensaService.delete).not.toHaveBeenCalled();
    });
  });

  // =========================================================================
  // 10. CONFIRMACIÓN Y CANCELACIÓN DE CONSUMO
  // =========================================================================
  describe('Consumo de Ítem', () => {
    const itemId = '11111111-1111-1111-1111-111111111111';

    it('debe invocar a marcarConsumido, refrescar lista y alertas si el usuario CONFIRMA', () => {
      mockConfirmationService.warn.mockReturnValue(of(Confirmation.Status.confirm));
      const listSpy = vi.spyOn(component.list, 'get');
      mockAdvertenciaService.getActivas.mockClear();

      component.marcarConsumido(itemId);

      expect(mockConfirmationService.warn).toHaveBeenCalledWith('', '::Pantry:ConsumeConfirmation');
      expect(mockDespensaService.marcarConsumido).toHaveBeenCalledWith(itemId, {
        skipHandleError: true,
      });
      expect(listSpy).toHaveBeenCalled();
      expect(mockAdvertenciaService.getActivas).toHaveBeenCalled();
    });

    it('NO debe invocar a marcarConsumido si el usuario CANCELA', () => {
      mockConfirmationService.warn.mockReturnValue(of(Confirmation.Status.dismiss));

      component.marcarConsumido(itemId);

      expect(mockConfirmationService.warn).toHaveBeenCalledWith('', '::Pantry:ConsumeConfirmation');
      expect(mockDespensaService.marcarConsumido).not.toHaveBeenCalled();
    });
  });
});
