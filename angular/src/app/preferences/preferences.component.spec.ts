import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule } from '@angular/forms';
import { LocalizationModule, LocalizationService } from '@abp/ng.core';
import { ThemeSharedModule, ToasterService, DEFAULT_VALIDATION_BLUEPRINTS } from '@abp/ng.theme.shared';
import { NgxValidateCoreModule } from '@ngx-validate/core';
import { of, throwError } from 'rxjs';
import { vi, describe, it, expect, beforeEach } from 'vitest';

import { PreferencesComponent } from './preferences.component';
import { PreferenciaService } from '../proxy/preferencias/preferencia.service';
import { PreferenciaDto, UpdatePreferenciaDto } from '../proxy/preferencias/models';

describe('PreferencesComponent', () => {
  let component: PreferencesComponent;
  let fixture: ComponentFixture<PreferencesComponent>;

  let mockPreferenciaService: {
    get: ReturnType<typeof vi.fn>;
    update: ReturnType<typeof vi.fn>;
  };

  let mockToasterService: {
    success: ReturnType<typeof vi.fn>;
    error: ReturnType<typeof vi.fn>;
  };

  let mockLocalizationService: {
    currentLang: string;
    instant: ReturnType<typeof vi.fn>;
  };

  const initialPreferences: PreferenciaDto = {
    alergenos: ['Maní', 'Gluten'],
    ingredientes: ['Aceite de palma', 'Azúcar'],
    mostrarAlertas: true,
  };

  beforeEach(async () => {
    mockPreferenciaService = {
      get: vi.fn().mockReturnValue(of(initialPreferences)),
      update: vi.fn().mockReturnValue(of(initialPreferences)),
    };

    mockToasterService = {
      success: vi.fn(),
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
        PreferencesComponent,
      ],
      providers: [
        { provide: PreferenciaService, useValue: mockPreferenciaService },
        { provide: ToasterService, useValue: mockToasterService },
        { provide: LocalizationService, useValue: mockLocalizationService },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(PreferencesComponent);
    component = fixture.componentInstance;
  });

  it('debería crearse correctamente el componente', () => {
    expect(component).toBeTruthy();
  });

  it('debería cargar las preferencias iniciales y asignarlas al estado y al formulario', () => {
    fixture.detectChanges(); // Trigger ngOnInit

    expect(mockPreferenciaService.get).toHaveBeenCalled();
    expect(component.isLoading).toBe(false);
    expect(component.hasError).toBe(false);
    expect(component.alergenos).toEqual(['Maní', 'Gluten']);
    expect(component.ingredientes).toEqual(['Aceite de palma', 'Azúcar']);
    expect(component.form.get('mostrarAlertas')?.value).toBe(true);
  });

  it('debería manejar el estado de error al fallar la carga y permitir reintentar', () => {
    mockPreferenciaService.get.mockReturnValueOnce(
      throwError(() => new Error('Error al conectar con el servidor'))
    );

    fixture.detectChanges();

    expect(component.isLoading).toBe(false);
    expect(component.hasError).toBe(true);

    // Reintentar con éxito
    mockPreferenciaService.get.mockReturnValueOnce(of(initialPreferences));
    component.cargar();

    expect(component.isLoading).toBe(false);
    expect(component.hasError).toBe(false);
    expect(component.alergenos).toEqual(['Maní', 'Gluten']);
  });

  it('debería alternar el toggle de mostrarAlertas', () => {
    fixture.detectChanges();

    expect(component.form.get('mostrarAlertas')?.value).toBe(true);
    component.form.patchValue({ mostrarAlertas: false });
    expect(component.form.get('mostrarAlertas')?.value).toBe(false);
  });

  describe('Gestión de alérgenos', () => {
    beforeEach(() => {
      fixture.detectChanges();
    });

    it('debería agregar un alérgeno válido y resetear el input', () => {
      component.form.patchValue({ nuevoAlergeno: 'Lactosa' });
      component.agregarAlergeno();

      expect(component.alergenos).toContain('Lactosa');
      expect(component.alergenos.length).toBe(3);
      expect(component.form.get('nuevoAlergeno')?.value).toBe('');
      expect(component.alergenoError).toBeNull();
    });

    it('debería rechazar un alérgeno vacío o con solo espacios', () => {
      component.form.patchValue({ nuevoAlergeno: '   ' });
      component.agregarAlergeno();

      expect(component.alergenoError).toBe('::Preferences:Validation:Empty');
      expect(component.alergenos.length).toBe(2);
    });

    it('debería rechazar un alérgeno que supere 64 caracteres', () => {
      const longTerm = 'A'.repeat(65);
      component.form.patchValue({ nuevoAlergeno: longTerm });
      component.agregarAlergeno();

      expect(component.alergenoError).toBe('::Preferences:Validation:MaxLength');
      expect(component.alergenos.length).toBe(2);
    });

    it('debería rechazar un alérgeno duplicado case-insensitive y recortado', () => {
      component.form.patchValue({ nuevoAlergeno: '  maní  ' });
      component.agregarAlergeno();

      expect(component.alergenoError).toBe('::Preferences:Validation:Duplicate');
      expect(component.alergenos.length).toBe(2);
    });

    it('debería rechazar agregar cuando se alcanza el límite máximo de 50 elementos', () => {
      component.alergenos = Array.from({ length: 50 }, (_, i) => `Alergeno${i}`);
      component.form.patchValue({ nuevoAlergeno: 'Nuevo Extra' });
      component.agregarAlergeno();

      expect(component.alergenoError).toBe('::Preferences:Validation:MaxItems');
      expect(component.alergenos.length).toBe(50);
    });

    it('debería quitar un alérgeno por índice', () => {
      expect(component.alergenos).toEqual(['Maní', 'Gluten']);
      component.quitarAlergeno(0); // Quita Maní

      expect(component.alergenos).toEqual(['Gluten']);
    });
  });

  describe('Gestión de ingredientes', () => {
    beforeEach(() => {
      fixture.detectChanges();
    });

    it('debería agregar un ingrediente válido y resetear el input', () => {
      component.form.patchValue({ nuevoIngrediente: 'Sal' });
      component.agregarIngrediente();

      expect(component.ingredientes).toContain('Sal');
      expect(component.ingredientes.length).toBe(3);
      expect(component.form.get('nuevoIngrediente')?.value).toBe('');
      expect(component.ingredienteError).toBeNull();
    });

    it('debería rechazar un ingrediente vacío o con solo espacios', () => {
      component.form.patchValue({ nuevoIngrediente: '' });
      component.agregarIngrediente();

      expect(component.ingredienteError).toBe('::Preferences:Validation:Empty');
      expect(component.ingredientes.length).toBe(2);
    });

    it('debería rechazar un ingrediente que supere 64 caracteres', () => {
      const longTerm = 'B'.repeat(65);
      component.form.patchValue({ nuevoIngrediente: longTerm });
      component.agregarIngrediente();

      expect(component.ingredienteError).toBe('::Preferences:Validation:MaxLength');
      expect(component.ingredientes.length).toBe(2);
    });

    it('debería rechazar un ingrediente duplicado case-insensitive y recortado', () => {
      component.form.patchValue({ nuevoIngrediente: '  AZÚCAR  ' });
      component.agregarIngrediente();

      expect(component.ingredienteError).toBe('::Preferences:Validation:Duplicate');
      expect(component.ingredientes.length).toBe(2);
    });

    it('debería rechazar agregar cuando se alcanza el límite de 50 ingredientes', () => {
      component.ingredientes = Array.from({ length: 50 }, (_, i) => `Ingrediente${i}`);
      component.form.patchValue({ nuevoIngrediente: 'Nuevo Extra' });
      component.agregarIngrediente();

      expect(component.ingredienteError).toBe('::Preferences:Validation:MaxItems');
      expect(component.ingredientes.length).toBe(50);
    });

    it('debería quitar un ingrediente por índice', () => {
      expect(component.ingredientes).toEqual(['Aceite de palma', 'Azúcar']);
      component.quitarIngrediente(1); // Quita Azúcar

      expect(component.ingredientes).toEqual(['Aceite de palma']);
    });
  });

  describe('Guardado y preservación de datos', () => {
    beforeEach(() => {
      fixture.detectChanges();
    });

    it('debería guardar exitosamente llamando al servicio con el payload actualizado', () => {
      component.alergenos.push('Soja');
      component.form.patchValue({ mostrarAlertas: false });

      const updatedDto: PreferenciaDto = {
        alergenos: ['Maní', 'Gluten', 'Soja'],
        ingredientes: ['Aceite de palma', 'Azúcar'],
        mostrarAlertas: false,
      };
      mockPreferenciaService.update.mockReturnValueOnce(of(updatedDto));

      component.guardar();

      const expectedPayload: UpdatePreferenciaDto = {
        alergenos: ['Maní', 'Gluten', 'Soja'],
        ingredientes: ['Aceite de palma', 'Azúcar'],
        mostrarAlertas: false,
      };
      expect(mockPreferenciaService.update).toHaveBeenCalledWith(expectedPayload);
      expect(component.isSaving).toBe(false);
      expect(component.saveSuccess).toBe(true);
      expect(mockToasterService.success).toHaveBeenCalledWith('::Preferences:SavedSuccess');
    });

    it('debería preservar los datos ingresados cuando ocurre un error al guardar', () => {
      // Modificar datos
      component.alergenos.push('Crustáceos');
      component.ingredientes.push('Glutamato');
      component.form.patchValue({ mostrarAlertas: false });

      mockPreferenciaService.update.mockReturnValueOnce(
        throwError(() => ({
          error: {
            error: {
              code: 'SmartPantry:00003',
              message: 'Término duplicado',
            },
          },
        }))
      );

      component.guardar();

      // Verificar que terminó de guardar con error
      expect(component.isSaving).toBe(false);
      expect(component.saveSuccess).toBe(false);
      expect(component.errorMessage).toBe('Término duplicado');
      expect(mockToasterService.error).toHaveBeenCalledWith('Término duplicado');

      // CRITERIO CLAVE: Preservar los datos ingresados
      expect(component.alergenos).toContain('Crustáceos');
      expect(component.ingredientes).toContain('Glutamato');
      expect(component.form.get('mostrarAlertas')?.value).toBe(false);
    });

    it('debería mostrar mensaje de error genérico si el servidor no incluye código específico', () => {
      mockPreferenciaService.update.mockReturnValueOnce(
        throwError(() => new Error('Network failure'))
      );

      component.guardar();

      expect(component.isSaving).toBe(false);
      expect(component.errorMessage).toBe('::Preferences:SaveError');
      expect(mockToasterService.error).toHaveBeenCalledWith('::Preferences:SaveError');
    });
  });

  describe('Estados visuales en la plantilla', () => {
    it('debería renderizar el estado de carga mientras isLoading sea true', () => {
      fixture.detectChanges();
      component.isLoading = true;
      fixture.componentRef.injector.get(ChangeDetectorRef).detectChanges();

      const loadingEl = fixture.nativeElement.querySelector('[data-testid="loading-state"]');
      expect(loadingEl).toBeTruthy();
    });

    it('debería renderizar el estado vacío cuando las listas no tienen elementos', () => {
      fixture.detectChanges();
      component.isLoading = false;
      component.hasError = false;
      component.alergenos = [];
      component.ingredientes = [];
      fixture.detectChanges();
      component.isLoading = false;
      component.hasError = false;
      component.alergenos = [];
      component.ingredientes = [];
      fixture.componentRef.injector.get(ChangeDetectorRef).detectChanges();

      const emptyAllergens = fixture.nativeElement.querySelector('[data-testid="allergens-empty"]');
      const emptyIngredients = fixture.nativeElement.querySelector('[data-testid="ingredients-empty"]');

      expect(emptyAllergens).toBeTruthy();
      expect(emptyIngredients).toBeTruthy();
    });

    it('debería renderizar la lista de elementos y badges cuando hay datos', () => {
      fixture.detectChanges();

      const allergensList = fixture.nativeElement.querySelector('[data-testid="allergens-list"]');
      const ingredientsList = fixture.nativeElement.querySelector('[data-testid="ingredients-list"]');

      expect(allergensList).toBeTruthy();
      expect(ingredientsList).toBeTruthy();
    });
  });
});
