import type { EntityDto } from '@abp/ng.core';

export interface CreateUpdateProductoDto {
  nombreEs?: string | null;
  nombreEn?: string | null;
  cantidad?: string | null;
  codigoBarras: string;
  marca?: string | null;
  imagenUrl?: string | null;
  categorias?: string | null;
  ingredientes?: string | null;
  alergenos?: string | null;
  nutriScore?: string | null;
  nova?: number | null;
}

export interface ExternalProductDto {
  nombre?: string | null;
  nombreEs?: string | null;
  cantidad?: string | null;
  marca?: string | null;
  imagenUrl?: string | null;
  codigoBarras?: string | null;
  categorias?: string | null;
  ingredientes?: string | null;
  alergenos?: string | null;
  nutriScore?: string | null;
  nova?: number | null;
}

export interface ProductoDto extends EntityDto<string> {
  nombreEs?: string | null;
  nombreEn?: string | null;
  cantidad?: string | null;
  codigoBarras?: string;
  marca?: string | null;
  imagenUrl?: string | null;
  categorias?: string | null;
  ingredientes?: string | null;
  alergenos?: string | null;
  nutriScore?: string | null;
  nova?: number | null;
}

export interface SearchProductInputDto {
  codigoBarras: string;
}
