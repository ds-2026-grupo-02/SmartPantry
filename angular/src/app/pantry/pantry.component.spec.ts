import { ComponentFixture, TestBed } from '@angular/core/testing';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule } from '@angular/forms';
import { ListService, LocalizationModule, LocalizationService, PagedResultDto } from '@abp/ng.core';
import { ConfirmationService, Confirmation, ToasterService, ThemeSharedModule, DEFAULT_VALIDATION_BLUEPRINTS } from '@abp/ng.theme.shared';
import { NgxValidateCoreModule } from '@ngx-validate/core';
import { of, throwError } from 'rxjs';
import { vi, describe, it, expect, beforeEach } from 'vitest';

import { PantryComponent } from './pantry.component';
import { DespensaService } from '../proxy/pantry-items/despensa.service';
import { DespensaDto } from '../proxy/pantry-items/models';

describe('PantryComponent', () => {
  let component: PantryComponent;
  let fixture: ComponentFixture<PantryComponent>;

  let mockDespensaService: {
    getList: ReturnType<typeof vi.fn>;
    update: ReturnType<typeof vi.fn>;
    delete: ReturnType<typeof vi.fn>;
    marcarConsumido: ReturnType<typeof vi.fn>;
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
      userId: 'user-1',
      productoId: 'prod-1',
      nombreProductoEs: 'Arroz',
      nombreProductoEn: 'Rice',
      cantidadProducto: '1 kg',
      fechaVencimiento: '2027-12-31T00:00:00Z',
      esConsumido: false,
    },
    {
      id: '22222222-2222-2222-2222-222222222222',
      userId: 'user-1',
      productoId: 'prod-2',
      nombreProductoEs: 'Fideos',
      nombreProductoEn: 'Pasta',
      cantidadProducto: '500 g',
      fechaVencimiento: null,
      esConsumido: false,
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

      // No debe mostrar la grilla ni el mensaje de error mientras carga
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

    it('al hacer clic en el botón de reintentar debe invocar a loadItems y list.get()', () => {
      const listSpy = vi.spyOn(component.list, 'get');

      component.loadItems();

      expect(component.hasError).toBe(false);
      expect(listSpy).toHaveBeenCalled();
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
  // 4. ESTADO CON RESULTADOS
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

      // Español
      mockLocalizationService.currentLang = 'es';
      expect(component.getNombreProducto(item)).toBe('Manzana');

      // Inglés
      mockLocalizationService.currentLang = 'en';
      expect(component.getNombreProducto(item)).toBe('Apple');

      // Fallback a inglés si falta español
      mockLocalizationService.currentLang = 'es';
      expect(component.getNombreProducto({ nombreProductoEn: 'OnlyEnglish' })).toBe('OnlyEnglish');

      // Fallback a español si falta inglés
      mockLocalizationService.currentLang = 'en';
      expect(component.getNombreProducto({ nombreProductoEs: 'SoloEspanol' })).toBe('SoloEspanol');
    });
  });

  // =========================================================================
  // 5. EDICIÓN: DATOS VÁLIDOS E INVÁLIDOS
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

    it('NO debe llamar a despensaService.update cuando la fecha es inválida (ej. 30 de febrero)', () => {
      // Ingresar fecha no existente en el calendario
      component.form.get('fechaVencimiento')?.setValue('2027-02-30');
      component.fechaInputInvalid = false;

      // El validador custom fechaValidaValidator debe marcar el formulario inválido
      expect(component.form.get('fechaVencimiento')?.hasError('fechaInvalida')).toBe(true);
      expect(component.form.invalid).toBe(true);

      component.save();

      // No debe llamar a update en el servicio
      expect(mockDespensaService.update).not.toHaveBeenCalled();
      expect(component.isSaving).toBe(false);
    });

    it('NO debe llamar a update cuando fechaInputInvalid es true (input validity.badInput)', () => {
      component.fechaInputInvalid = true;

      component.save();

      expect(mockDespensaService.update).not.toHaveBeenCalled();
    });

    it('debe llamar a despensaService.update y cerrar modal cuando los datos son válidos', () => {
      component.form.get('fechaVencimiento')?.setValue('2028-05-15');
      const listSpy = vi.spyOn(component.list, 'get');

      component.save();

      expect(mockDespensaService.update).toHaveBeenCalledWith(
        sampleItems[0].id,
        { fechaVencimiento: '2028-05-15', cantidadProducto: null },
        { skipHandleError: true }
      );
      expect(component.isModalOpen).toBe(false);
      expect(listSpy).toHaveBeenCalled();
    });

    it('debe manejar error del servidor al guardar sin cerrar el modal y mostrando toaster', () => {
      mockDespensaService.update.mockReturnValue(throwError(() => new Error('Server error')));
      component.form.get('fechaVencimiento')?.setValue('2028-05-15');

      component.save();

      expect(component.isSaving).toBe(false);
      expect(mockToasterService.error).toHaveBeenCalled();
      expect(component.isModalOpen).toBe(true); // El modal no se cierra si falló
    });
  });

  // =========================================================================
  // 6. CONFIRMACIÓN Y CANCELACIÓN DE ELIMINACIÓN
  // =========================================================================
  describe('Eliminación de Ítem', () => {
    const itemId = '11111111-1111-1111-1111-111111111111';

    it('debe invocar a delete y recargar la lista si el usuario CONFIRMA en el diálogo', () => {
      mockConfirmationService.warn.mockReturnValue(of(Confirmation.Status.confirm));
      const listSpy = vi.spyOn(component.list, 'get');

      component.deleteItem(itemId);

      expect(mockConfirmationService.warn).toHaveBeenCalledWith('', '::Pantry:DeleteConfirmation');
      expect(mockDespensaService.delete).toHaveBeenCalledWith(itemId, { skipHandleError: true });
      expect(listSpy).toHaveBeenCalled();
    });

    it('NO debe invocar a delete si el usuario CANCELA o rechaza la confirmación', () => {
      mockConfirmationService.warn.mockReturnValue(of(Confirmation.Status.dismiss));

      component.deleteItem(itemId);

      expect(mockConfirmationService.warn).toHaveBeenCalledWith('', '::Pantry:DeleteConfirmation');
      expect(mockDespensaService.delete).not.toHaveBeenCalled();
    });
  });

  // =========================================================================
  // 7. CONFIRMACIÓN Y CANCELACIÓN DE CONSUMO
  // =========================================================================
  describe('Consumo de Ítem', () => {
    const itemId = '11111111-1111-1111-1111-111111111111';

    it('debe invocar a marcarConsumido si el usuario CONFIRMA', () => {
      mockConfirmationService.warn.mockReturnValue(of(Confirmation.Status.confirm));
      const listSpy = vi.spyOn(component.list, 'get');

      component.marcarConsumido(itemId);

      expect(mockConfirmationService.warn).toHaveBeenCalledWith('', '::Pantry:ConsumeConfirmation');
      expect(mockDespensaService.marcarConsumido).toHaveBeenCalledWith(itemId, { skipHandleError: true });
      expect(listSpy).toHaveBeenCalled();
    });

    it('NO debe invocar a marcarConsumido si el usuario CANCELA', () => {
      mockConfirmationService.warn.mockReturnValue(of(Confirmation.Status.dismiss));

      component.marcarConsumido(itemId);

      expect(mockConfirmationService.warn).toHaveBeenCalledWith('', '::Pantry:ConsumeConfirmation');
      expect(mockDespensaService.marcarConsumido).not.toHaveBeenCalled();
    });
  });
});
