import { ChangeDetectorRef, Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { LocalizationModule, LocalizationService } from '@abp/ng.core';
import { ThemeSharedModule, ToasterService } from '@abp/ng.theme.shared';
import { PreferenciaService } from '../proxy/preferencias/preferencia.service';
import { PreferenciaDto, UpdatePreferenciaDto } from '../proxy/preferencias/models';

@Component({
  selector: 'app-preferences',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, LocalizationModule, ThemeSharedModule],
  templateUrl: './preferences.component.html',
  styleUrls: ['./preferences.component.scss'],
})
export class PreferencesComponent implements OnInit {
  private fb = inject(FormBuilder);
  private preferenciaService = inject(PreferenciaService);
  private toaster = inject(ToasterService);
  private localizationService = inject(LocalizationService);
  private cdr = inject(ChangeDetectorRef);

  form!: FormGroup;

  alergenos: string[] = [];
  ingredientes: string[] = [];

  isLoading = false;
  isSaving = false;
  hasError = false;
  saveSuccess = false;
  errorMessage: string | null = null;

  alergenoError: string | null = null;
  ingredienteError: string | null = null;

  ngOnInit(): void {
    this.buildForm();
    this.cargar();
  }

  buildForm(): void {
    this.form = this.fb.group({
      mostrarAlertas: [true],
      nuevoAlergeno: ['', [Validators.maxLength(64)]],
      nuevoIngrediente: ['', [Validators.maxLength(64)]],
    });
  }

  cargar(): void {
    this.isLoading = true;
    this.hasError = false;
    this.errorMessage = null;

    this.preferenciaService.get().subscribe({
      next: (dto: PreferenciaDto) => {
        this.isLoading = false;
        this.alergenos = [...(dto?.alergenos || [])];
        this.ingredientes = [...(dto?.ingredientes || [])];
        this.form.patchValue({
          mostrarAlertas: dto?.mostrarAlertas ?? true,
        });
        this.cdr.markForCheck();
      },
      error: () => {
        this.isLoading = false;
        this.hasError = true;
        this.cdr.markForCheck();
      },
    });
  }

  agregarAlergeno(): void {
    this.alergenoError = null;
    const rawValue = this.form.get('nuevoAlergeno')?.value ?? '';
    const term = typeof rawValue === 'string' ? rawValue.trim() : '';

    if (!term) {
      this.alergenoError = '::Preferences:Validation:Empty';
      return;
    }

    if (term.length > 64) {
      this.alergenoError = '::Preferences:Validation:MaxLength';
      return;
    }

    const yaExiste = this.alergenos.some(
      a => a.trim().toLowerCase() === term.toLowerCase()
    );
    if (yaExiste) {
      this.alergenoError = '::Preferences:Validation:Duplicate';
      return;
    }

    if (this.alergenos.length >= 50) {
      this.alergenoError = '::Preferences:Validation:MaxItems';
      return;
    }

    this.alergenos.push(term);
    this.form.get('nuevoAlergeno')?.reset('');
    this.saveSuccess = false;
  }

  quitarAlergeno(index: number): void {
    if (index >= 0 && index < this.alergenos.length) {
      this.alergenos.splice(index, 1);
      this.saveSuccess = false;
    }
  }

  agregarIngrediente(): void {
    this.ingredienteError = null;
    const rawValue = this.form.get('nuevoIngrediente')?.value ?? '';
    const term = typeof rawValue === 'string' ? rawValue.trim() : '';

    if (!term) {
      this.ingredienteError = '::Preferences:Validation:Empty';
      return;
    }

    if (term.length > 64) {
      this.ingredienteError = '::Preferences:Validation:MaxLength';
      return;
    }

    const yaExiste = this.ingredientes.some(
      i => i.trim().toLowerCase() === term.toLowerCase()
    );
    if (yaExiste) {
      this.ingredienteError = '::Preferences:Validation:Duplicate';
      return;
    }

    if (this.ingredientes.length >= 50) {
      this.ingredienteError = '::Preferences:Validation:MaxItems';
      return;
    }

    this.ingredientes.push(term);
    this.form.get('nuevoIngrediente')?.reset('');
    this.saveSuccess = false;
  }

  quitarIngrediente(index: number): void {
    if (index >= 0 && index < this.ingredientes.length) {
      this.ingredientes.splice(index, 1);
      this.saveSuccess = false;
    }
  }

  guardar(): void {
    this.isSaving = true;
    this.saveSuccess = false;
    this.errorMessage = null;

    const input: UpdatePreferenciaDto = {
      alergenos: [...this.alergenos],
      ingredientes: [...this.ingredientes],
      mostrarAlertas: !!this.form.get('mostrarAlertas')?.value,
    };

    this.preferenciaService.update(input).subscribe({
      next: (dto: PreferenciaDto) => {
        this.isSaving = false;
        this.saveSuccess = true;
        this.alergenos = [...(dto?.alergenos || [])];
        this.ingredientes = [...(dto?.ingredientes || [])];
        this.form.patchValue({
          mostrarAlertas: dto?.mostrarAlertas ?? input.mostrarAlertas,
        });
        this.toaster.success('::Preferences:SavedSuccess');
        this.cdr.markForCheck();
      },
      error: (err: any) => {
        this.isSaving = false;
        const serverMessage = err?.error?.error?.message;
        const mensaje = serverMessage || '::Preferences:SaveError';

        this.errorMessage = mensaje;
        this.toaster.error(mensaje);
        this.cdr.markForCheck();
      },
    });
  }
}
