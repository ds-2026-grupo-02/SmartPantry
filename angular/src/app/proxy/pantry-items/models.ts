import type { EntityDto } from '@abp/ng.core';

export interface DespensaDto extends EntityDto<string> {
  userId?: string;
  productoId?: string;
  nombreProductoEs?: string | null;
  nombreProductoEn?: string | null;
  cantidadProducto?: string | null;
  fechaVencimiento?: string | null;
  esConsumido?: boolean;
}

export interface CreateDespensaDto {
  productoId: string;
  fechaVencimiento?: string | null;
}

export interface UpdateDespensaDto {
  fechaVencimiento?: string | null;
}
