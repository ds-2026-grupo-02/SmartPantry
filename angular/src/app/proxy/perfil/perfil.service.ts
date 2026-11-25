import type { ActualizarFotoDto, ActualizarPerfilDto, PerfilDto } from './models';
import { RestService, Rest } from '@abp/ng.core';
import { Injectable, inject } from '@angular/core';

@Injectable({
  providedIn: 'root',
})
export class PerfilService {
  private restService = inject(RestService);
  apiName = 'Default';
  

  actualizarFoto = (input: ActualizarFotoDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, string>({
      method: 'POST',
      headers: { Accept: 'application/json' },
      url: '/api/app/perfil/actualizar-foto',
      body: input,
    },
    { apiName: this.apiName,...config });
  

  actualizarPerfil = (input: ActualizarPerfilDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, PerfilDto>({
      method: 'POST',
      headers: { Accept: 'application/json' },
      url: '/api/app/perfil/actualizar-perfil',
      body: input,
    },
    { apiName: this.apiName,...config });
  

  eliminarFoto = (config?: Partial<Rest.Config>) =>
    this.restService.request<any, void>({
      method: 'POST',
      url: '/api/app/perfil/eliminar-foto',
    },
    { apiName: this.apiName,...config });
  

  get = (config?: Partial<Rest.Config>) =>
    this.restService.request<any, PerfilDto>({
      method: 'GET',
      headers: { Accept: 'application/json' },
      url: '/api/app/perfil',
    },
    { apiName: this.apiName,...config });
}