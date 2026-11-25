import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { AuthService, ConfigStateService, CurrentUserDto, LocalizationPipe } from '@abp/ng.core';
import { NgbDropdownModule } from '@ng-bootstrap/ng-bootstrap';
import { Observable } from 'rxjs';
import { PerfilService } from '../proxy/perfil/perfil.service';
import { PerfilStateService } from './perfil-state.service';

@Component({
  selector: 'app-custom-current-user',
  standalone: true,
  imports: [CommonModule, RouterModule, NgbDropdownModule, LocalizationPipe],
  template: `
    <ng-container *ngIf="currentUser$ | async as user">
      <div *ngIf="user.isAuthenticated" ngbDropdown class="dropdown">
        <button
          ngbDropdownToggle
          type="button"
          class="btn btn-link nav-link d-flex align-items-center gap-2 text-decoration-none text-dark py-1 px-2"
        >
          <img
            *ngIf="fotoPerfil$ | async as foto; else defaultIcon"
            [src]="foto"
            alt="Avatar"
            class="rounded-circle object-fit-cover border"
            width="30"
            height="30"
          />
          <ng-template #defaultIcon>
            <i class="fas fa-user-circle fs-4 text-secondary"></i>
          </ng-template>
          <span class="small fw-semibold">{{ user.userName }}</span>
        </button>

        <div ngbDropdownMenu class="dropdown-menu-end shadow-sm">
          <a ngbDropdownItem routerLink="/account/manage">
            <i class="fas fa-user-cog me-2"></i>{{ 'AbpAccount::MyAccount' | abpLocalization }}
          </a>
          <div class="dropdown-divider"></div>
          <button ngbDropdownItem type="button" (click)="logout()">
            <i class="fas fa-sign-out-alt me-2"></i>{{ 'AbpAccount::Logout' | abpLocalization }}
          </button>
        </div>
      </div>

      <button
        *ngIf="!user.isAuthenticated"
        type="button"
        class="btn btn-sm btn-primary"
        (click)="login()"
      >
        {{ 'AbpAccount::Login' | abpLocalization }}
      </button>
    </ng-container>
  `,
})
export class CustomCurrentUserComponent implements OnInit {
  private configState = inject(ConfigStateService);
  private authService = inject(AuthService);
  private perfilService = inject(PerfilService);
  private perfilState = inject(PerfilStateService);

  currentUser$: Observable<CurrentUserDto> = this.configState.getOne$('currentUser');
  fotoPerfil$ = this.perfilState.fotoPerfil$;

  ngOnInit(): void {
    if (this.configState.getOne('currentUser')?.isAuthenticated) {
      this.perfilService.get().subscribe({
        next: (perfil) => this.perfilState.setFotoPerfil(perfil.fotoPerfil || null),
      });
    }
  }

  login(): void {
    this.authService.navigateToLogin();
  }

  logout(): void {
    this.authService.logout().subscribe();
  }
}
