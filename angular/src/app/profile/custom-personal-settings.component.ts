import { Component, OnInit, inject, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { finalize } from 'rxjs/operators';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { LocalizationPipe } from '@abp/ng.core';
import { ToasterService } from '@abp/ng.theme.shared';
import { PerfilService } from '../proxy/perfil/perfil.service';
import { PerfilStateService } from './perfil-state.service';

@Component({
  selector: 'app-custom-personal-settings',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, LocalizationPipe],
  template: `
    <div class="card shadow-sm border-0 mb-4">
      <div class="card-header bg-white py-3 border-bottom">
        <h5 class="card-title mb-0 fw-bold text-primary">
          <i class="fas fa-user-circle me-2"></i>{{ '::Profile:Title' | abpLocalization }}
        </h5>
        <small style="color: #343a40;">{{ '::Profile:Subtitle' | abpLocalization }}</small>
      </div>
      <div class="card-body p-4">
        <!-- Sección Foto de Perfil (RF-02) -->
        <div
          class="d-flex flex-column flex-sm-row align-items-center gap-4 p-3 rounded-3 mb-4 border"
          style="background-color: #f8f9fa;"
        >
          <div class="avatar-wrapper position-relative">
            <img
              *ngIf="fotoPerfil; else defaultAvatar"
              [src]="fotoPerfil"
              alt="Foto de perfil"
              class="rounded-circle object-fit-cover shadow-sm border"
              width="100"
              height="100"
            />
            <ng-template #defaultAvatar>
              <div
                class="rounded-circle bg-primary-subtle text-primary d-flex align-items-center justify-content-center shadow-sm border"
                style="width: 100px; height: 100px;"
              >
                <i class="fas fa-user fs-1"></i>
              </div>
            </ng-template>
            <span
              *ngIf="isSavingPhoto"
              class="position-absolute top-50 start-50 translate-middle spinner-border text-primary"
              role="status"
            ></span>
          </div>

          <div class="text-center text-sm-start flex-grow-1">
            <h6 class="fw-bold mb-1" style="color: #212529;">{{ '::Profile:PhotoLabel' | abpLocalization }}</h6>
            <p class="small mb-2" style="color: #343a40;">{{ '::Profile:PhotoHint' | abpLocalization }}</p>
            <div class="d-flex flex-wrap gap-2 justify-content-center justify-content-sm-start">
              <label class="btn btn-sm btn-outline-primary mb-0" [class.disabled]="isSavingPhoto">
                <i class="fas fa-camera me-1"></i>{{ '::Profile:UploadPhoto' | abpLocalization }}
                <input
                  type="file"
                  (change)="onFileSelected($event)"
                  accept="image/png, image/jpeg, image/webp"
                  class="d-none"
                />
              </label>
              <button
                *ngIf="fotoPerfil"
                type="button"
                (click)="eliminarFoto()"
                class="btn btn-sm btn-outline-danger"
                [disabled]="isSavingPhoto"
              >
                <i class="fas fa-trash-alt me-1"></i>{{ '::Profile:RemovePhoto' | abpLocalization }}
              </button>
            </div>
          </div>
        </div>

        <!-- Formulario de Datos Personales -->
        <form [formGroup]="form" (ngSubmit)="onSubmit()">
          <div class="row g-3">
            <div class="col-md-6">
              <label class="form-label fw-semibold small">{{ '::Profile:Name' | abpLocalization }} *</label>
              <input type="text" formControlName="name" class="form-control" />
            </div>
            <div class="col-md-6">
              <label class="form-label fw-semibold small">{{ '::Profile:Surname' | abpLocalization }}</label>
              <input type="text" formControlName="surname" class="form-control" />
            </div>
            <div class="col-md-6">
              <label class="form-label fw-semibold small">{{ '::Profile:Email' | abpLocalization }} *</label>
              <input type="email" formControlName="email" class="form-control" />
            </div>
            <div class="col-md-6">
              <label class="form-label fw-semibold small">{{ '::Profile:Phone' | abpLocalization }}</label>
              <input type="tel" formControlName="phoneNumber" class="form-control" />
            </div>
          </div>

          <div class="mt-4 text-end">
            <button
              type="submit"
              class="btn btn-primary px-4"
              [disabled]="form.invalid || isSaving || isSavingPhoto"
            >
              <span *ngIf="isSaving" class="spinner-border spinner-border-sm me-1"></span>
              <i *ngIf="!isSaving" class="fas fa-save me-1"></i>{{ '::Profile:Save' | abpLocalization }}
            </button>
          </div>
        </form>
      </div>
    </div>
  `,
})
export class CustomPersonalSettingsComponent implements OnInit {
  private fb = inject(FormBuilder);
  private perfilService = inject(PerfilService);
  private perfilState = inject(PerfilStateService);
  private toaster = inject(ToasterService);
  private cdr = inject(ChangeDetectorRef);

  fotoPerfil: string | null = null;
  isSavingPhoto = false;
  isSaving = false;

  form: FormGroup = this.fb.group({
    name: ['', [Validators.required, Validators.maxLength(64)]],
    surname: ['', [Validators.maxLength(64)]],
    email: ['', [Validators.required, Validators.email, Validators.maxLength(256)]],
    phoneNumber: ['', [Validators.maxLength(32)]],
  });

  ngOnInit(): void {
    this.cargarPerfil();
  }

  cargarPerfil(): void {
    this.perfilService.get().subscribe({
      next: (perfil) => {
        this.fotoPerfil = perfil.fotoPerfil || null;
        this.perfilState.setFotoPerfil(this.fotoPerfil);
        this.form.patchValue({
          name: perfil.name,
          surname: perfil.surname,
          email: perfil.email,
          phoneNumber: perfil.phoneNumber,
        });
        this.cdr.detectChanges();
      },
      error: () => {
        this.toaster.error('No se pudo cargar la información del perfil.');
      },
    });
  }

  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (input.files && input.files[0]) {
      const file = input.files[0];
      if (!file.type.startsWith('image/')) {
        this.toaster.error('El archivo seleccionado debe ser una imagen (PNG, JPG o WEBP).');
        return;
      }
      if (file.size > 2 * 1024 * 1024) {
        this.toaster.error('La imagen no puede superar los 2MB de tamaño.');
        return;
      }

      const reader = new FileReader();
      reader.onload = () => {
        const base64 = reader.result as string;
        this.isSavingPhoto = true;
        this.perfilService
          .actualizarFoto({ fotoBase64: base64 })
          .pipe(
            finalize(() => {
              this.isSavingPhoto = false;
              this.cdr.detectChanges();
            })
          )
          .subscribe({
            next: () => {
              this.fotoPerfil = base64;
              this.perfilState.setFotoPerfil(base64);
              this.toaster.success('Foto de perfil actualizada correctamente.');
            },
            error: () => {
              this.toaster.error('Error al guardar la foto de perfil.');
            },
          });
      };
      reader.readAsDataURL(file);
    }
  }

  eliminarFoto(): void {
    this.isSavingPhoto = true;
    this.perfilService
      .eliminarFoto()
      .pipe(
        finalize(() => {
          this.isSavingPhoto = false;
          this.cdr.detectChanges();
        })
      )
      .subscribe({
        next: () => {
          this.fotoPerfil = null;
          this.perfilState.setFotoPerfil(null);
          this.toaster.success('Foto de perfil eliminada.');
        },
        error: () => {
          this.toaster.error('Error al eliminar la foto.');
        },
      });
  }

  onSubmit(): void {
    if (this.form.invalid) return;

    this.isSaving = true;
    this.perfilService
      .actualizarPerfil(this.form.value)
      .pipe(
        finalize(() => {
          this.isSaving = false;
          this.cdr.detectChanges();
        })
      )
      .subscribe({
        next: (perfil) => {
          this.form.patchValue({
            name: perfil.name,
            surname: perfil.surname,
            email: perfil.email,
            phoneNumber: perfil.phoneNumber,
          });
          this.toaster.success('Perfil actualizado correctamente.');
        },
        error: (err) => {
          const msg = err.error?.error?.message || 'Error al actualizar el perfil.';
          this.toaster.error(msg);
        },
      });
  }
}
