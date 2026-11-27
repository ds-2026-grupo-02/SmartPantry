import type { AdvertenciaVencimientoDto } from './models';
import { RestService, Rest } from '@abp/ng.core';
import type { ListResultDto } from '@abp/ng.core';
import { Injectable, inject } from '@angular/core';

@Injectable({
  providedIn: 'root',
})
export class AdvertenciaVencimientoService {
  private restService = inject(RestService);
  apiName = 'Default';
  

  getActivas = (config?: Partial<Rest.Config>) =>
    this.restService.request<any, ListResultDto<AdvertenciaVencimientoDto>>({
      method: 'GET',
      headers: { Accept: 'application/json' },
      url: '/api/app/advertencia-vencimiento/activas',
    },
    { apiName: this.apiName,...config });
  

  marcarComoVista = (id: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, void>({
      method: 'POST',
      url: `/api/app/advertencia-vencimiento/${id}/marcar-como-vista`,
    },
    { apiName: this.apiName,...config });
}