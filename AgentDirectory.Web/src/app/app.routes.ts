import { Routes } from '@angular/router';
// import { MsalGuard } from '@azure/msal-angular';
// Re-enable MsalGuard on routes when Azure AD is configured.

export const routes: Routes = [
  {
    path: '',
    redirectTo: 'directory',
    pathMatch: 'full',
  },
  {
    path: 'directory',
    loadComponent: () =>
      import('./features/directory/directory.component').then(m => m.DirectoryComponent),
    // canActivate: [MsalGuard],
  },
  {
    path: 'agent/:id',
    loadComponent: () =>
      import('./features/playground/playground.component').then(m => m.PlaygroundComponent),
    // canActivate: [MsalGuard],
  },
  {
    path: 'admin',
    loadComponent: () =>
      import('./features/admin/admin.component').then(m => m.AdminComponent),
    // canActivate: [MsalGuard],
  },
  {
    path: '**',
    redirectTo: 'directory',
  },
];
