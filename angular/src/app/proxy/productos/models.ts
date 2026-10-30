import type { EntityDto } from '@abp/ng.core';

export interface CreateUpdateProductoDto {
  nombreEs?: string | null;
  nombreEn?: string | null;
  cantidad?: string | null;
  codigoBarras: string;
}

export interface ExternalProductDto {
  nombre?: string | null;
  nombreEs?: string | null;
  cantidad?: string | null;
  marca?: string | null;
  imagenUrl?: string | null;
}

export interface ProductoDto extends EntityDto<string> {
  nombreEs?: string | null;
  nombreEn?: string | null;
  cantidad?: string | null;
  codigoBarras?: string;
}

export interface SearchProductInputDto {
  codigoBarras: string;
}
