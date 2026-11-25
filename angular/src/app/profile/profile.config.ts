import { provideAppInitializer, inject } from '@angular/core';
import { ReplaceableComponentsService } from '@abp/ng.core';
import { eAccountComponents } from '@abp/ng.account';
import { eThemeLeptonXComponents } from '@abp/ng.theme.lepton-x';
import { CustomPersonalSettingsComponent } from './custom-personal-settings.component';
import { CustomCurrentUserComponent } from './custom-current-user.component';

function initAccountProfile() {
  const replaceableComponents = inject(ReplaceableComponentsService);

  replaceableComponents.add({
    key: eAccountComponents.PersonalSettings,
    component: CustomPersonalSettingsComponent,
  });

  replaceableComponents.add({
    key: eThemeLeptonXComponents.CurrentUser,
    component: CustomCurrentUserComponent,
  });
}

export const PROFILE_PROVIDER = [
  provideAppInitializer(() => {
    initAccountProfile();
  }),
];
