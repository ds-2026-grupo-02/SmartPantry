import { describe, beforeEach, it, expect, vi } from 'vitest';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { LocalizationPipe } from '@abp/ng.core';
import { Pipe, PipeTransform } from '@angular/core';
import { FooterComponent } from './footer.component';

// Mock que devuelve el texto esperado según la clave de localización
@Pipe({
  name: 'abpLocalization',
  standalone: true,
})
class MockLocalizationPipe implements PipeTransform {
  private translations: Record<string, string> = {
    '::DataAttribution': 'Datos proporcionados por Open Food Facts',
    '::MedicalDisclaimer': 'Esta aplicación no ofrece diagnóstico ni consejo médico',
  };

  transform(value: string): string {
    return this.translations[value] ?? value;
  }
}

describe('FooterComponent', () => {
  let component: FooterComponent;
  let fixture: ComponentFixture<FooterComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [FooterComponent],
    })
      .overrideComponent(FooterComponent, {
        remove: { imports: [LocalizationPipe] },
        add: { imports: [MockLocalizationPipe] },
      })
      .compileComponents();

    fixture = TestBed.createComponent(FooterComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('debería crearse correctamente', () => {
    expect(component).toBeTruthy();
  });

  it('debería mostrar la atribución a Open Food Facts', () => {
    const text = fixture.nativeElement.textContent;
    expect(text).toContain('Open Food Facts');
  });

  it('debería mostrar el aviso de que no ofrece diagnóstico ni consejo médico', () => {
    const text = fixture.nativeElement.textContent;
    expect(text).toContain('diagnóstico ni consejo médico');
  });
});
