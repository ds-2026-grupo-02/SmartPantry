import { provideAppInitializer, inject } from '@angular/core';
import { ReplaceableComponentsService } from '@abp/ng.core';
import { eAccountComponents } from '@abp/ng.account';
import { CustomPersonalSettingsComponent } from './custom-personal-settings.component';

function initAccountProfile() {
  const replaceableComponents = inject(ReplaceableComponentsService);
  replaceableComponents.add({
    key: eAccountComponents.PersonalSettings,
    component: CustomPersonalSettingsComponent,
  });
}

export const PROFILE_PROVIDER = [
  provideAppInitializer(() => {
    initAccountProfile();
  }),
];
