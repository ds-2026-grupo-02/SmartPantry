import { ChangeDetectionStrategy, Component } from '@angular/core';
import { LocalizationPipe } from '@abp/ng.core';

@Component({
  selector: 'abp-footer',
  standalone: true,
  imports: [LocalizationPipe],
  template: `
    <div class="lpx-footbar-container end-0">
      <div class="lpx-footbar flex-column flex-md-row justify-content-between align-items-center py-2 px-3 gap-2">
        <div class="smart-pantry-disclaimers text-center text-md-start small">
          <div class="disclaimer-item mb-1 mb-md-0">
            <i class="fas fa-database me-1 text-primary"></i>
            <span>{{ '::DataAttribution' | abpLocalization }}</span>
          </div>
          <div class="disclaimer-item">
            <i class="fas fa-info-circle me-1 text-warning"></i>
            <span>{{ '::MedicalDisclaimer' | abpLocalization }}</span>
          </div>
        </div>
        <div class="lpx-footbar-copyright text-center text-md-end small">
          <span>{{ currentYear }} © </span>
          <strong>SmartPantry</strong>
        </div>
      </div>
    </div>
  `,
  styles: [
    `
      .smart-pantry-disclaimers {
        font-size: 0.825rem;
        line-height: 1.4;
        opacity: 0.85;
      }
      .disclaimer-item {
        display: inline-block;
      }
      .disclaimer-item:not(:last-child)::after {
        content: ' • ';
        margin: 0 0.5rem;
        opacity: 0.6;
      }
      @media (max-width: 767.98px) {
        .disclaimer-item {
          display: block;
        }
        .disclaimer-item:not(:last-child)::after {
          display: none;
        }
      }
    `,
  ],
  changeDetection: ChangeDetectionStrategy.Eager,
})
export class FooterComponent {
  currentYear = new Date().getFullYear();
}
