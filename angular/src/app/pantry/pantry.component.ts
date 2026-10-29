import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { ListService, LocalizationModule } from '@abp/ng.core';
import { DespensaService } from '../proxy/pantry-items/despensa.service';
import { DespensaDto } from '../proxy/pantry-items/models';

export function fechaValidaValidator(control: AbstractControl): ValidationErrors | null {
  const value = control.value;
  if (!value) return null; // Si es opcional y está vacío, es válido

  if (typeof value === 'string') {
    // Formato esperado de input date: YYYY-MM-DD
    const regex = /^\d{4}-\d{2}-\d{2}$/;
    if (!regex.test(value)) {
      return { fechaInvalida: true };
    }

    const [anioStr, mesStr, diaStr] = value.split('-');
    const anio = parseInt(anioStr, 10);
    const mes = parseInt(mesStr, 10);
    const dia = parseInt(diaStr, 10);

    const fecha = new Date(anio, mes - 1, dia);

    // Verificar que coincida día, mes y año exactos (ej. 30 de febrero Date lo convierte a marzo)
    if (
      fecha.getFullYear() !== anio ||
      fecha.getMonth() !== mes - 1 ||
      fecha.getDate() !== dia
    ) {
      return { fechaInvalida: true };
    }
  } else if (value instanceof Date) {
    if (isNaN(value.getTime())) {
      return { fechaInvalida: true };
    }
  }

  return null;
}

@Component({
  selector: 'app-pantry',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, LocalizationModule],
  providers: [ListService],
  templateUrl: './pantry.component.html',
})
export class PantryComponent implements OnInit {
  items: DespensaDto[] = [];
  totalCount = 0;

  // Estados de la UI requeridos por la guía
  isLoading = false;
  hasError = false;

  // Modal de Edición
  isModalOpen = false;
  editingId: string | null = null;
  form!: FormGroup;
  cantidadInputInvalid = false;
  fechaInputInvalid = false;

  constructor(
    public readonly list: ListService,
    private despensaService: DespensaService,
    private fb: FormBuilder,
    private cdr: ChangeDetectorRef
  ) {
    this.buildForm();
  }

  ngOnInit(): void {
    this.hookList();
  }

  buildForm() {
    this.form = this.fb.group({
      cantidad: [1, [Validators.required, Validators.min(0), Validators.pattern(/^[0-9]+$/)]],
      unidad: ['', [Validators.maxLength(32)]],
      fechaVencimiento: [null, [fechaValidaValidator]],
    });
  }

  onCantidadInput(event: Event) {
    const input = event.target as HTMLInputElement;
    this.cantidadInputInvalid = input.validity.badInput;
  }

  onFechaInput(event: Event) {
    const input = event.target as HTMLInputElement;
    this.fechaInputInvalid = input.validity.badInput;
  }

  hookList() {
    this.isLoading = true;
    this.hasError = false;

    this.list.isLoading$.subscribe(loading => {
      this.isLoading = loading;
      this.cdr.detectChanges();
    });

    this.list.requestStatus$.subscribe(status => {
      if (status === 'error') {
        this.hasError = true;
        this.isLoading = false;
        this.cdr.detectChanges();
      } else if (status === 'success') {
        this.hasError = false;
        this.cdr.detectChanges();
      }
    });

    const streamCreator = (query: any) => this.despensaService.getList(query);

    this.list.hookToQuery(streamCreator).subscribe({
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
  }

  openEditModal(item: DespensaDto) {
    this.editingId = item.id ?? null;
    this.cantidadInputInvalid = false;
    this.fechaInputInvalid = false;
    let fechaFormateada = null;
    if (item.fechaVencimiento) {
      const fechaStr = typeof item.fechaVencimiento === 'string'
        ? item.fechaVencimiento
        : new Date(item.fechaVencimiento).toISOString();
      fechaFormateada = fechaStr.split('T')[0];
    }

    this.form.patchValue({
      cantidad: typeof item.cantidad === 'number' ? Math.floor(item.cantidad) : 0,
      unidad: item.unidad ?? '',
      fechaVencimiento: fechaFormateada,
    });
    this.isModalOpen = true;
  }

  closeModal() {
    this.isModalOpen = false;
    this.editingId = null;
    this.cantidadInputInvalid = false;
    this.fechaInputInvalid = false;
    this.form.reset();
  }

  save() {
    if (this.form.invalid || !this.editingId || this.cantidadInputInvalid || this.fechaInputInvalid) return;

    this.despensaService.update(this.editingId, this.form.value).subscribe(() => {
      this.closeModal();
      this.list.get();
    });
  }

  marcarConsumido(id: string) {
    this.despensaService.marcarConsumido(id).subscribe(() => {
      this.list.get();
    });
  }

  deleteItem(id: string) {
    if (confirm('¿Está seguro de eliminar este ítem de la despensa?')) {
      this.despensaService.delete(id).subscribe(() => {
        this.list.get();
      });
    }
  }
}
