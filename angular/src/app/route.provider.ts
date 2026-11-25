import { RoutesService, eLayoutType, ConfigStateService } from '@abp/ng.core';
import { inject, provideAppInitializer } from '@angular/core';

export const APP_ROUTE_PROVIDER = [
  provideAppInitializer(() => {
    configureRoutes();
  }),
];

function configureRoutes() {
  const routes = inject(RoutesService);
  const configState = inject(ConfigStateService);

  routes.add([
    {
      path: '/',
      name: '::Menu:Home',
      iconClass: 'fas fa-home',
      order: 1,
      layout: eLayoutType.application,
    },
    {
      path: '/search',
      name: '::Menu:Search',
      iconClass: 'fas fa-search',
      order: 2,
      layout: eLayoutType.application,
    },
    {
      path: '/pantry',
      name: '::Menu:Pantry',
      iconClass: 'fas fa-shopping-basket',
      order: 3,
      layout: eLayoutType.application,
    },
    {
      path: '/preferences',
      name: '::Menu:Preferences',
      iconClass: 'fas fa-sliders-h',
      order: 4,
      layout: eLayoutType.application,
    },
    {
      path: '/alerts',
      name: '::Menu:Alerts',
      iconClass: 'fas fa-bell',
      order: 5,
      layout: eLayoutType.application,
    },
  ]);

  configState.getOne$('currentUser').subscribe(currentUser => {
    const isAuth = !!currentUser?.isAuthenticated;
    routes.patch('::Menu:Pantry', { invisible: !isAuth });
    routes.patch('::Menu:Preferences', { invisible: !isAuth });
    routes.patch('::Menu:Alerts', { invisible: !isAuth });
    routes.refresh();
  });
}
