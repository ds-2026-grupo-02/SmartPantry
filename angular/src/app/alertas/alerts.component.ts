import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { LocalizationModule, LocalizationService } from '@abp/ng.core';
import { ToasterService } from '@abp/ng.theme.shared';
import { forkJoin, of } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { DespensaService } from '../proxy/despensa/despensa.service';
import { DespensaDto } from '../proxy/despensa/models';
import { EstadoVencimiento } from '../proxy/despensa/estado-vencimiento.enum';
import { AdvertenciaVencimientoService } from '../proxy/advertencias/advertencia-vencimiento.service';
import { AdvertenciaVencimientoDto } from '../proxy/advertencias/models';
import { PreferenciaService } from '../proxy/preferencias/preferencia.service';
import { PreferenciaDto } from '../proxy/preferencias/models';
import { ProductoService } from '../proxy/productos/producto.service';
import { ProductoDto } from '../proxy/productos/models';

export interface AlertaCompatibilidad {
  id: string;
  itemDespensaId: string;
  nombreProducto: string;
  tipo: 'alergeno' | 'ingrediente';
  coincidencia: string;
}

@Component({
  selector: 'app-alerts',
  standalone: true,
  imports: [CommonModule, LocalizationModule],
  templateUrl: './alerts.component.html',
})
export class AlertsComponent implements OnInit {
  readonly EstadoVencimiento = EstadoVencimiento;

  alertasActivas: AdvertenciaVencimientoDto[] = [];
  alertasCompatibilidad: AlertaCompatibilidad[] = [];
  isLoadingAlertas = false;
  hasErrorAlertas = false;

  private nombresItemsActivos = new Map<string, string>();
  private alertasCompatibilidadDescartadas = new Set<string>();

  constructor(
    private despensaService: DespensaService,
    private productoService: ProductoService,
    private advertenciaService: AdvertenciaVencimientoService,
    private preferenciaService: PreferenciaService,
    private toaster: ToasterService,
    private localizationService: LocalizationService,
    private cdr: ChangeDetectorRef
  ) { }

  ngOnInit(): void {
    this.loadAlertas();
  }

  getNombreProducto(item: { nombreProductoEs?: string | null; nombreProductoEn?: string | null }): string | null {
    const lang = (this.localizationService.currentLang || '').toLowerCase();
    if (lang.startsWith('en')) {
      return item.nombreProductoEn || item.nombreProductoEs || null;
    }
    return item.nombreProductoEs || item.nombreProductoEn || null;
  }

  getNombreProductoPorId(itemDespensaId?: string, fallbackTipo?: string): string {
    if (itemDespensaId) {
      const desdeActivos = this.nombresItemsActivos.get(itemDespensaId);
      if (desdeActivos) return desdeActivos;
    }

    if (fallbackTipo) {
      const keyMap: Record<string, string> = {
        VencimientoProximo: '::Pantry:Status:ExpiringSoon',
        ProximoAVencer: '::Pantry:Status:ExpiringSoon',
        Vencido: '::Pantry:Status:Expired',
        Vigente: '::Pantry:Status:Valid',
        SinFecha: '::Pantry:Status:NoDate',
      };
      const locKey = keyMap[fallbackTipo];
      if (locKey) {
        return this.localizationService.instant(locKey);
      }
    }

    return this.localizationService.instant('::Pantry:WithoutName');
  }

  registrarNombreItem(id: string, nombre: string) {
    if (id && nombre) {
      this.nombresItemsActivos.set(id, nombre);
    }
  }

  eliminarNombreItem(id: string) {
    this.nombresItemsActivos.delete(id);
  }

  private registrarNombresActivos(activos: DespensaDto[]) {
    for (const item of activos) {
      if (item.id) {
        const nombre = this.getNombreProducto(item);
        if (nombre) {
          this.nombresItemsActivos.set(item.id, nombre);
        }
      }
    }
  }

  loadAlertas() {
    this.isLoadingAlertas = true;
    this.hasErrorAlertas = false;

    forkJoin({
      vencimientos: this.advertenciaService.getActivas({ skipHandleError: true }),
      preferencias: this.preferenciaService.get({ skipHandleError: true }).pipe(
        catchError(() => of(null as PreferenciaDto | null))
      ),
      activos: this.despensaService
        .getList({ soloConsumidos: false, maxResultCount: 1000 }, { skipHandleError: true })
        .pipe(catchError(() => of({ items: [] as DespensaDto[], totalCount: 0 }))),
    }).subscribe({
      next: ({ vencimientos, preferencias, activos }) => {
        const itemsActivos = activos.items ?? [];
        this.registrarNombresActivos(itemsActivos);
        this.alertasActivas = vencimientos.items ?? [];

        const productoIds = Array.from(
          new Set(itemsActivos.map(i => i.productoId).filter((id): id is string => Boolean(id)))
        );

        if (productoIds.length === 0) {
          this.alertasCompatibilidad = this.calcularAlertasCompatibilidad(
            preferencias,
            itemsActivos,
            new Map<string, ProductoDto>()
          );
          this.isLoadingAlertas = false;
          this.cdr.detectChanges();
          return;
        }

        const productosCalls = productoIds.reduce<Record<string, ReturnType<ProductoService['get']>>>(
          (acc, id) => {
            acc[id] = this.productoService
              .get(id, { skipHandleError: true })
              .pipe(catchError(() => of(null as unknown as ProductoDto)));
            return acc;
          },
          {}
        );

        forkJoin(productosCalls).subscribe({
          next: (productosMapObj) => {
            const productosMap = new Map<string, ProductoDto>();
            for (const [id, prod] of Object.entries(productosMapObj)) {
              if (prod) productosMap.set(id, prod);
            }
            this.alertasCompatibilidad = this.calcularAlertasCompatibilidad(
              preferencias,
              itemsActivos,
              productosMap
            );
            this.isLoadingAlertas = false;
            this.cdr.detectChanges();
          },
          error: () => {
            this.alertasCompatibilidad = [];
            this.isLoadingAlertas = false;
            this.cdr.detectChanges();
          },
        });
      },
      error: () => {
        this.isLoadingAlertas = false;
        this.hasErrorAlertas = true;
        this.toaster.error('::Pantry:Error:LoadFailed', '::Pantry:Error:ConnectionTitle');
        this.cdr.detectChanges();
      },
    });
  }

  private textoDesdeCampo(valor: unknown): string {
    if (Array.isArray(valor)) {
      return valor.map(v => String(v ?? '')).join(' ').toLowerCase();
    }
    if (typeof valor === 'string') {
      return valor.toLowerCase();
    }
    return '';
  }

  private calcularAlertasCompatibilidad(
    preferencia: PreferenciaDto | null,
    itemsActivos: DespensaDto[],
    productosMap: Map<string, ProductoDto>
  ): AlertaCompatibilidad[] {
    if (!preferencia || preferencia.mostrarAlertas === false) {
      return [];
    }

    const alergenos = (preferencia.alergenos ?? []).map(a => a.trim()).filter(Boolean);
    const ingredientes = (preferencia.ingredientes ?? []).map(i => i.trim()).filter(Boolean);

    if (alergenos.length === 0 && ingredientes.length === 0) {
      return [];
    }

    const resultado: AlertaCompatibilidad[] = [];

    for (const item of itemsActivos) {
      if (!item.id) continue;
      const nombre = this.getNombreProducto(item) || this.localizationService.instant('::Pantry:WithoutName');
      const producto = item.productoId ? productosMap.get(item.productoId) : undefined;

      const alergenosProducto = this.textoDesdeCampo(producto?.alergenos);
      const ingredientesProducto = this.textoDesdeCampo(producto?.ingredientes);
      const nombresProducto = [item.nombreProductoEs ?? '', item.nombreProductoEn ?? '']
        .join(' ')
        .toLowerCase();

      const textoAlergenos = `${alergenosProducto} ${ingredientesProducto} ${nombresProducto}`;
      const textoIngredientes = `${ingredientesProducto} ${nombresProducto}`;

      for (const alergeno of alergenos) {
        const idAlerta = `compat-alergeno-${item.id}-${alergeno.toLowerCase()}`;
        if (this.alertasCompatibilidadDescartadas.has(idAlerta)) continue;

        if (textoAlergenos.includes(alergeno.toLowerCase())) {
          resultado.push({
            id: idAlerta,
            itemDespensaId: item.id,
            nombreProducto: nombre,
            tipo: 'alergeno',
            coincidencia: alergeno,
          });
        }
      }

      for (const ingrediente of ingredientes) {
        const idAlerta = `compat-ingrediente-${item.id}-${ingrediente.toLowerCase()}`;
        if (this.alertasCompatibilidadDescartadas.has(idAlerta)) continue;

        if (textoIngredientes.includes(ingrediente.toLowerCase())) {
          resultado.push({
            id: idAlerta,
            itemDespensaId: item.id,
            nombreProducto: nombre,
            tipo: 'ingrediente',
            coincidencia: ingrediente,
          });
        }
      }
    }

    return resultado;
  }

  descartarAlertaCompatibilidad(alerta: AlertaCompatibilidad) {
    this.alertasCompatibilidadDescartadas.add(alerta.id);
    this.alertasCompatibilidad = this.alertasCompatibilidad.filter(x => x.id !== alerta.id);
    this.cdr.detectChanges();
  }

  marcarAlertaVista(alerta: AdvertenciaVencimientoDto) {
    if (!alerta.id) return;
    this.advertenciaService.marcarComoVista(alerta.id, { skipHandleError: true }).subscribe({
      next: () => {
        this.loadAlertas();
      },
      error: () => {
        this.toaster.error('::Pantry:Error:SaveFailed', '::Pantry:Error:ConnectionTitle');
      },
    });
  }
}
