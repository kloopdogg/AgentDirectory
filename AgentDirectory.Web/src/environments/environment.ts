export const environment = {
  production: false,
  msalConfig: {
    clientId: 'YOUR_SPA_CLIENT_ID',
    tenantId: 'YOUR_TENANT_ID',
    // Scope exposed by the API app registration (e.g. api://YOUR_API_CLIENT_ID/access_as_user)
    apiScopes: ['api://YOUR_API_CLIENT_ID/access_as_user'],
  },
};
