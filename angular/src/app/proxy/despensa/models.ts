import type { EntityDto, PagedAndSortedResultRequestDto } from '@abp/ng.core';
import type { EstadoVencimiento } from './estado-vencimiento.enum';

export interface CreateDespensaDto {
  productoId: string;
  fechaVencimiento?: string | null;
}

export interface DespensaDto extends EntityDto<string> {
  usuarioId?: string;
  productoId?: string;
  nombreProductoEs?: string | null;
  nombreProductoEn?: string | null;
  cantidadProducto?: string | null;
  fechaVencimiento?: string | null;
  esConsumido?: boolean;
  estadoVencimiento?: EstadoVencimiento;
}

export interface GetDespensaListInput extends PagedAndSortedResultRequestDto {
  soloConsumidos?: boolean | null;
}

export interface GuardarNotaPrivadaDto {
  contenido: string;
}

export interface NotaPrivadaDto {
  id?: string;
  itemDespensaId?: string;
  contenido?: string;
}

export interface UpdateDespensaDto {
  fechaVencimiento?: string | null;
}
