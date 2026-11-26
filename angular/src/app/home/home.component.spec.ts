import { describe, beforeEach, it, expect, vi } from 'vitest';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { AuthService, LocalizationPipe } from '@abp/ng.core';
import { Pipe, PipeTransform } from '@angular/core';
import { HomeComponent } from './home.component';

@Pipe({
  name: 'abpLocalization',
  standalone: true,
})
class MockLocalizationPipe implements PipeTransform {
  transform(value: string): string {
    return value;
  }
}

describe('HomeComponent', () => {
  let component: HomeComponent;
  let fixture: ComponentFixture<HomeComponent>;
  let isAuthenticatedMock = false;

  const authServiceMock = {
    navigateToLogin: vi.fn(),
    get isAuthenticated() {
      return isAuthenticatedMock;
    },
  };

  beforeEach(async () => {
    isAuthenticatedMock = false;
    vi.clearAllMocks();

    await TestBed.configureTestingModule({
      imports: [HomeComponent],
      providers: [
        provideRouter([]),
        { provide: AuthService, useValue: authServiceMock },
      ],
    })
      .overrideComponent(HomeComponent, {
        remove: { imports: [LocalizationPipe] },
        add: { imports: [MockLocalizationPipe] },
      })
      .compileComponents();

    fixture = TestBed.createComponent(HomeComponent);
    component = fixture.componentInstance;
  });

  it('debería crearse correctamente', () => {
    fixture.detectChanges();
    expect(component).toBeTruthy();
  });

  it('debería mostrar todos los accesos directos cuando el usuario está autenticado', () => {
    isAuthenticatedMock = true;
    fixture.detectChanges();

    const text = fixture.nativeElement.textContent;
    expect(text).toContain('::Home:SearchTitle');
    expect(text).toContain('::Home:PantryTitle');
    expect(text).toContain('::Home:PreferencesTitle');
    expect(text).toContain('::Home:AlertsTitle');
  });

  it('debería ocultar los accesos privados cuando el usuario no inició sesión', () => {
    isAuthenticatedMock = false;
    fixture.detectChanges();

    const text = fixture.nativeElement.textContent;
    expect(text).not.toContain('::Home:PantryTitle');
    expect(text).not.toContain('::Home:PreferencesTitle');
    expect(text).not.toContain('::Home:AlertsTitle');
  });
});
