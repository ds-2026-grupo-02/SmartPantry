import { Environment } from '@abp/ng.core';

const baseUrl = 'http://localhost:4200';

const oAuthConfig = {
  issuer: 'https://localhost:44374/',
  redirectUri: baseUrl,
  clientId: 'SmartPantry_App',
  responseType: 'code',
  scope: 'offline_access SmartPantry',
  requireHttps: true,
};

export const environment = {
  production: true,
  application: {
    baseUrl,
    name: 'SmartPantry',
  },
  oAuthConfig,
  apis: {
    default: {
      url: 'https://localhost:44374',
      rootNamespace: 'SmartPantry',
    },
    AbpAccountPublic: {
      url: oAuthConfig.issuer,
      rootNamespace: 'AbpAccountPublic',
    },
  },
  remoteEnv: {
    url: '/getEnvConfig',
    mergeStrategy: 'deepmerge'
  }
} as Environment;
