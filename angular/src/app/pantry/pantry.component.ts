import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { ListService, LocalizationModule } from '@abp/ng.core';
import { DespensaService } from '../proxy/pantry-items/despensa.service';
import { DespensaDto } from '../proxy/pantry-items/models';


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

  constructor(
    public readonly list: ListService,
    private despensaService: DespensaService,
    private fb: FormBuilder
  ) {
    this.buildForm();
  }

  ngOnInit(): void {
    this.loadItems();
  }

  buildForm() {
    this.form = this.fb.group({
      cantidad: [0, [Validators.min(0)]],
      unidad: [''],
      fechaVencimiento: [null],
    });
  }

  loadItems() {
    this.isLoading = true;
    this.hasError = false;

    const streamCreator = (query: any) => this.despensaService.getList(query);

    this.list.hookToQuery(streamCreator).subscribe({
      next: (response) => {
        this.items = response.items ?? [];
        this.totalCount = response.totalCount;
        this.isLoading = false;
      },
      error: () => {
        this.hasError = true;
        this.isLoading = false;
      },
    });
  }

  openEditModal(item: DespensaDto) {
    this.editingId = item.id ?? null;
    this.form.patchValue({
      cantidad: typeof item.cantidad === 'number' ? item.cantidad : 0,
      unidad: item.unidad ?? '',
      fechaVencimiento: item.fechaVencimiento
        ? item.fechaVencimiento.split('T')[0]
        : null,
    });
    this.isModalOpen = true;
  }

  closeModal() {
    this.isModalOpen = false;
    this.editingId = null;
    this.form.reset();
  }

  save() {
    if (this.form.invalid || !this.editingId) return;

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
