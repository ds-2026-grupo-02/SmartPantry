import type { PreferenciaDto, UpdatePreferenciaDto } from './models';
import { RestService, Rest } from '@abp/ng.core';
import { Injectable, inject } from '@angular/core';

@Injectable({
  providedIn: 'root',
})
export class PreferenciaService {
  private restService = inject(RestService);
  apiName = 'Default';

  get = (config?: Partial<Rest.Config>) =>
    this.restService.request<any, PreferenciaDto>(
      {
        method: 'GET',
        headers: { Accept: 'application/json' },
        url: '/api/app/preferencia',
      },
      { apiName: this.apiName, ...config }
    );

  update = (input: UpdatePreferenciaDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, PreferenciaDto>(
      {
        method: 'PUT',
        headers: { Accept: 'application/json' },
        url: '/api/app/preferencia',
        body: input,
      },
      { apiName: this.apiName, ...config }
    );
}
