import { mapEnumToOptions } from '@abp/ng.core';

export enum EstadoVencimiento {
  SinFecha = 0,
  Vigente = 1,
  ProximoAVencer = 2,
  Vencido = 3,
}

export const estadoVencimientoOptions = mapEnumToOptions(EstadoVencimiento);
