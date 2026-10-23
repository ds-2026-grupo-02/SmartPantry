import type { EntityDto } from '@abp/ng.core';

export interface DespensaDto extends EntityDto<string> {
  userId?: string;
  productoId?: string;
  nombreProducto?: string;
  cantidad?: number;
  unidad?: string;
  fechaVencimiento?: string | null;
  esConsumido?: boolean;
}

export interface UpdateDespensaDto {
  cantidad?: number;
  unidad?: string | null;
  fechaVencimiento?: string | null;
}
