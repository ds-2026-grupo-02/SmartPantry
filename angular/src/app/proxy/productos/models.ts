import type { EntityDto } from '@abp/ng.core';

export interface CreateUpdateProductoDto {
  nombre: string;
  codigoBarras: string;
}

export interface ExternalProductDto {
  nombre?: string | null;
  nombreEs?: string | null;
  marca?: string | null;
  imagenUrl?: string | null;
}

export interface ProductoDto extends EntityDto<string> {
  nombre?: string;
  codigoBarras?: string;
}

export interface SearchProductInputDto {
  codigoBarras: string;
}
