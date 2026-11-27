import type { CreateDespensaDto, DespensaDto, GetDespensaListInput, GuardarNotaPrivadaDto, NotaPrivadaDto, UpdateDespensaDto } from './models';
import { RestService, Rest } from '@abp/ng.core';
import type { PagedResultDto } from '@abp/ng.core';
import { Injectable, inject } from '@angular/core';

@Injectable({
  providedIn: 'root',
})
export class DespensaService {
  private restService = inject(RestService);
  apiName = 'Default';
  

  create = (input: CreateDespensaDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, DespensaDto>({
      method: 'POST',
      headers: { Accept: 'application/json' },
      url: '/api/app/despensa',
      body: input,
    },
    { apiName: this.apiName,...config });
  

  delete = (id: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, void>({
      method: 'DELETE',
      url: `/api/app/despensa/${id}`,
    },
    { apiName: this.apiName,...config });
  

  eliminarNotaPrivada = (itemId: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, void>({
      method: 'POST',
      url: `/api/app/despensa/eliminar-nota-privada/${itemId}`,
    },
    { apiName: this.apiName,...config });
  

  getList = (input: GetDespensaListInput, config?: Partial<Rest.Config>) =>
    this.restService.request<any, PagedResultDto<DespensaDto>>({
      method: 'GET',
      headers: { Accept: 'application/json' },
      url: '/api/app/despensa',
      params: { soloConsumidos: input.soloConsumidos, sorting: input.sorting, skipCount: input.skipCount, maxResultCount: input.maxResultCount },
    },
    { apiName: this.apiName,...config });
  

  getNotaPrivada = (itemId: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, NotaPrivadaDto>({
      method: 'GET',
      headers: { Accept: 'application/json' },
      url: `/api/app/despensa/nota-privada/${itemId}`,
    },
    { apiName: this.apiName,...config });
  

  guardarNotaPrivada = (itemId: string, input: GuardarNotaPrivadaDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, NotaPrivadaDto>({
      method: 'POST',
      headers: { Accept: 'application/json' },
      url: `/api/app/despensa/guardar-nota-privada/${itemId}`,
      body: input,
    },
    { apiName: this.apiName,...config });
  

  marcarConsumido = (id: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, void>({
      method: 'POST',
      url: `/api/app/despensa/${id}/marcar-consumido`,
    },
    { apiName: this.apiName,...config });
  

  update = (id: string, input: UpdateDespensaDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, DespensaDto>({
      method: 'PUT',
      headers: { Accept: 'application/json' },
      url: `/api/app/despensa/${id}`,
      body: input,
    },
    { apiName: this.apiName,...config });
}