import { Injectable } from '@angular/core';
import { BehaviorSubject } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class PerfilStateService {
  private fotoPerfilSubject = new BehaviorSubject<string | null>(null);
  fotoPerfil$ = this.fotoPerfilSubject.asObservable();

  setFotoPerfil(foto: string | null): void {
    this.fotoPerfilSubject.next(foto);
  }
}
