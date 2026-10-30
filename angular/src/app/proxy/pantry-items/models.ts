import type { EntityDto } from '@abp/ng.core';

export interface DespensaDto extends EntityDto<string> {
  userId?: string;
  productoId?: string;
  nombreProducto?: string;
  cantidad?: number | null;
  unidad?: string | null;
  fechaVencimiento?: string | null;
  esConsumido?: boolean;
}

export interface UpdateDespensaDto {
  cantidad?: number | null;
  unidad?: string | null;
  fechaVencimiento?: string | null;
}
