import type { EntityDto } from '@abp/ng.core';
import type { EstadoVencimiento } from '../despensa/estado-vencimiento.enum';

export interface AdvertenciaVencimientoDto extends EntityDto<string> {
  usuarioId?: string;
  itemDespensaId?: string;
  tipoAdvertencia?: string;
  esActiva?: boolean;
  fechaVencimiento?: string | null;
  estadoVencimiento?: EstadoVencimiento;
}
