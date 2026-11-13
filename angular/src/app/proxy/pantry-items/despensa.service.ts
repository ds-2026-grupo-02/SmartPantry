import type { DespensaDto, UpdateDespensaDto } from './models';
import { RestService, Rest } from '@abp/ng.core';
import type { PagedAndSortedResultRequestDto, PagedResultDto } from '@abp/ng.core';
import { Injectable, inject } from '@angular/core';

@Injectable({
  providedIn: 'root',
})
export class DespensaService {
  private restService = inject(RestService);
  apiName = 'Default';
  

  delete = (id: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, void>({
      method: 'DELETE',
      url: `/api/app/despensa/${id}`,
    },
    { apiName: this.apiName,...config });
  

  getList = (input: PagedAndSortedResultRequestDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, PagedResultDto<DespensaDto>>({
      method: 'GET',
      headers: { Accept: 'application/json' },
      url: '/api/app/despensa',
      params: { sorting: input.sorting, skipCount: input.skipCount, maxResultCount: input.maxResultCount },
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