import { Component, OnInit, ChangeDetectorRef, DestroyRef, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators, AbstractControl, ValidationErrors } from '@angular/forms';
import { ListService, LocalizationModule, LocalizationService } from '@abp/ng.core';
import { ConfirmationService, Confirmation, ToasterService, ThemeSharedModule } from '@abp/ng.theme.shared';
import { DespensaService } from '../proxy/despensa/despensa.service';
import { DespensaDto, GetDespensaListInput } from '../proxy/despensa/models';
import { EstadoVencimiento } from '../proxy/despensa/estado-vencimiento.enum';
import { AdvertenciaVencimientoService } from '../proxy/advertencias/advertencia-vencimiento.service';
import { AdvertenciaVencimientoDto } from '../proxy/advertencias/models';

export function fechaValidaValidator(control: AbstractControl): ValidationErrors | null {
  const value = control.value;
  if (!value) return null;

  if (typeof value === 'string') {
    const regex = /^\d{4}-\d{2}-\d{2}$/;
    if (!regex.test(value)) {
      return { fechaInvalida: true };
    }

    const [anioStr, mesStr, diaStr] = value.split('-');
    const anio = parseInt(anioStr, 10);
    const mes = parseInt(mesStr, 10);
    const dia = parseInt(diaStr, 10);

    const fecha = new Date(anio, mes - 1, dia);

    if (
      fecha.getFullYear() !== anio ||
      fecha.getMonth() !== mes - 1 ||
      fecha.getDate() !== dia
    ) {
      return { fechaInvalida: true };
    }
  } else if (value instanceof Date && isNaN(value.getTime())) {
    return { fechaInvalida: true };
  }

  return null;
}

@Component({
  selector: 'app-pantry',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, LocalizationModule, ThemeSharedModule],
  providers: [ListService],
  templateUrl: './pantry.component.html',
})
export class PantryComponent implements OnInit {
  readonly EstadoVencimiento = EstadoVencimiento;

  items: DespensaDto[] = [];
  totalCount = 0;
  soloConsumidos = false;

  // Estados de la UI
  isLoading = false;
  hasError = false;

  // Modal de Edición de Fecha
  isModalOpen = false;
  isSaving = false;
  editingId: string | null = null;
  form!: FormGroup;
  fechaInputInvalid = false;

  // Modal de Nota Privada
  isNotaModalOpen = false;
  isSavingNota = false;
  isLoadingNota = false;
  notaItemId: string | null = null;
  notaItemNombre: string | null = null;
  notaForm!: FormGroup;

  // Alertas activas
  alertasActivas: AdvertenciaVencimientoDto[] = [];
  isLoadingAlertas = false;

  private readonly destroyRef = inject(DestroyRef);

  constructor(
    public readonly list: ListService,
    private despensaService: DespensaService,
    private advertenciaService: AdvertenciaVencimientoService,
    private confirmation: ConfirmationService,
    private toaster: ToasterService,
    private localizationService: LocalizationService,
    private fb: FormBuilder,
    private cdr: ChangeDetectorRef
  ) {
    this.buildForms();
  }

  ngOnInit(): void {
    this.hookList();
    this.loadAlertas();
  }

  buildForms() {
    this.form = this.fb.group({
      fechaVencimiento: [null, [fechaValidaValidator]],
    });

    this.notaForm = this.fb.group({
      contenido: ['', [Validators.required, Validators.maxLength(500)]],
    });
  }

  getNombreProducto(item: { nombreProductoEs?: string | null; nombreProductoEn?: string | null }): string | null {
    const lang = (this.localizationService.currentLang || '').toLowerCase();
    if (lang.startsWith('en')) {
      return item.nombreProductoEn || item.nombreProductoEs || null;
    }
    return item.nombreProductoEs || item.nombreProductoEn || null;
  }

  getNombreProductoPorId(itemDespensaId?: string): string {
    if (!itemDespensaId) return '';
    const encontrado = this.items.find(x => x.id === itemDespensaId);
    return encontrado ? (this.getNombreProducto(encontrado) || '') : '';
  }

  onFechaInput(event: Event) {
    const input = event.target as HTMLInputElement;
    this.fechaInputInvalid = input.validity.badInput;
  }

  setSoloConsumidos(value: boolean) {
    if (this.soloConsumidos === value) return;
    this.soloConsumidos = value;
    this.list.get();
  }

  hookList() {
    this.isLoading = true;
    this.hasError = false;

    this.list.isLoading$
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(loading => {
        this.isLoading = loading;
        this.cdr.detectChanges();
      });

    this.list.requestStatus$
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(status => {
        if (status === 'error') {
          this.hasError = true;
          this.isLoading = false;
          this.cdr.detectChanges();
        } else if (status === 'success') {
          this.hasError = false;
          this.cdr.detectChanges();
        }
      });

    const streamCreator = (query: GetDespensaListInput) =>
      this.despensaService.getList(
        { ...query, soloConsumidos: this.soloConsumidos },
        { skipHandleError: true }
      );

    this.list
      .hookToQuery(streamCreator)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (response) => {
          this.items = response.items ?? [];
          this.totalCount = response?.totalCount ?? 0;
          this.isLoading = false;
          this.cdr.detectChanges();
        },
        error: () => {
          this.hasError = true;
          this.isLoading = false;
          this.cdr.detectChanges();
        },
      });
  }

  loadItems() {
    this.hasError = false;
    this.list.get();
    this.loadAlertas();
  }

  loadAlertas() {
    this.isLoadingAlertas = true;
    this.advertenciaService.getActivas({ skipHandleError: true }).subscribe({
      next: (res) => {
        this.alertasActivas = res.items ?? [];
        this.isLoadingAlertas = false;
        this.cdr.detectChanges();
      },
      error: () => {
        this.isLoadingAlertas = false;
        this.cdr.detectChanges();
      },
    });
  }

  marcarAlertaVista(alerta: AdvertenciaVencimientoDto) {
    if (!alerta.id) return;
    this.advertenciaService.marcarComoVista(alerta.id, { skipHandleError: true }).subscribe({
      next: () => {
        this.loadAlertas();
      },
      error: () => {
        this.toaster.error('::Pantry:Error:ConnectionTitle');
      },
    });
  }

  // --- Modal Fecha de Vencimiento ---
  openEditModal(item: DespensaDto) {
    this.editingId = item.id ?? null;
    this.fechaInputInvalid = false;
    let fechaFormateada: string | null = null;
    if (item.fechaVencimiento) {
      const fechaStr = typeof item.fechaVencimiento === 'string'
        ? item.fechaVencimiento
        : new Date(item.fechaVencimiento).toISOString();
      fechaFormateada = fechaStr.split('T')[0];
    }

    this.form.patchValue({
      fechaVencimiento: fechaFormateada,
    });
    this.isModalOpen = true;
  }

  closeModal() {
    this.isModalOpen = false;
    this.editingId = null;
    this.isSaving = false;
    this.fechaInputInvalid = false;
    this.form.reset();
  }

  save() {
    if (this.form.invalid || !this.editingId || this.fechaInputInvalid) return;

    this.isSaving = true;
    const formValue = {
      fechaVencimiento: this.form.value.fechaVencimiento || null,
    };

    this.despensaService.update(this.editingId, formValue, { skipHandleError: true }).subscribe({
      next: () => {
        this.isSaving = false;
        this.closeModal();
        this.list.get();
        this.loadAlertas();
      },
      error: () => {
        this.isSaving = false;
        this.toaster.error('::Pantry:Error:SaveFailed', '::Pantry:Error:ConnectionTitle');
        this.cdr.detectChanges();
      },
    });
  }

  // --- Modal Nota Privada ---
  openNotaModal(item: DespensaDto) {
    if (!item.id) return;
    this.notaItemId = item.id;
    this.notaItemNombre = this.getNombreProducto(item);
    this.isLoadingNota = true;
    this.isNotaModalOpen = true;
    this.notaForm.reset({ contenido: '' });

    this.despensaService.getNotaPrivada(item.id, { skipHandleError: true }).subscribe({
      next: (nota) => {
        this.notaForm.patchValue({ contenido: nota?.contenido ?? '' });
        this.isLoadingNota = false;
        this.cdr.detectChanges();
      },
      error: () => {
        // Si aún no tiene nota creada, dejamos vacío para que pueda crearla
        this.isLoadingNota = false;
        this.cdr.detectChanges();
      },
    });
  }

  closeNotaModal() {
    this.isNotaModalOpen = false;
    this.notaItemId = null;
    this.notaItemNombre = null;
    this.isSavingNota = false;
    this.isLoadingNota = false;
    this.notaForm.reset({ contenido: '' });
  }

  saveNota() {
    if (this.notaForm.invalid || !this.notaItemId) return;
    this.isSavingNota = true;
    const contenido = (this.notaForm.value.contenido || '').trim();

    this.despensaService
      .guardarNotaPrivada(this.notaItemId, { contenido }, { skipHandleError: true })
      .subscribe({
        next: () => {
          this.isSavingNota = false;
          this.closeNotaModal();
        },
        error: () => {
          this.isSavingNota = false;
          this.toaster.error('::Pantry:Error:SaveFailed', '::Pantry:Error:ConnectionTitle');
          this.cdr.detectChanges();
        },
      });
  }

  deleteNota() {
    if (!this.notaItemId) return;
    this.isSavingNota = true;

    this.despensaService
      .eliminarNotaPrivada(this.notaItemId, { skipHandleError: true })
      .subscribe({
        next: () => {
          this.isSavingNota = false;
          this.closeNotaModal();
        },
        error: () => {
          this.isSavingNota = false;
          this.toaster.error('::Pantry:Error:DeleteFailed', '::Pantry:Error:ConnectionTitle');
          this.cdr.detectChanges();
        },
      });
  }

  // --- Consumir / Eliminar ---
  marcarConsumido(id: string) {
    this.confirmation
      .warn('', '::Pantry:ConsumeConfirmation')
      .subscribe((status: Confirmation.Status) => {
        if (status === Confirmation.Status.confirm) {
          this.despensaService.marcarConsumido(id, { skipHandleError: true }).subscribe({
            next: () => {
              this.list.get();
              this.loadAlertas();
            },
            error: () => {
              this.toaster.error('::Pantry:Error:ConsumeFailed', '::Pantry:Error:ConnectionTitle');
            },
          });
        }
      });
  }

  deleteItem(id: string) {
    this.confirmation
      .warn('', '::Pantry:DeleteConfirmation')
      .subscribe((status: Confirmation.Status) => {
        if (status === Confirmation.Status.confirm) {
          this.despensaService.delete(id, { skipHandleError: true }).subscribe({
            next: () => {
              this.list.get();
              this.loadAlertas();
            },
            error: () => {
              this.toaster.error('::Pantry:Error:DeleteFailed', '::Pantry:Error:ConnectionTitle');
            },
          });
        }
      });
  }
}
