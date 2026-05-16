import { Component } from '@angular/core';
import { RouterOutlet, RouterLink, RouterLinkActive } from '@angular/router';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';

/**
 * App shell with top navigation.
 *
 * Azure AD auth is configured in app.config.ts + core/auth/msal.config.ts.
 * To enable: uncomment MsalGuard in app.routes.ts and restore MsalService
 * injection here for sign-in/sign-out buttons.
 */
@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet, RouterLink, RouterLinkActive, MatToolbarModule, MatButtonModule, MatIconModule],
  template: `
    <mat-toolbar color="primary">
      <a routerLink="/directory" class="toolbar-brand">
        <mat-icon>smart_toy</mat-icon>
        <span>Agent Directory</span>
      </a>
      <span class="toolbar-spacer"></span>
      <a mat-button routerLink="/directory" routerLinkActive="active-nav">
        <mat-icon>grid_view</mat-icon> Directory
      </a>
      <a mat-button routerLink="/admin" routerLinkActive="active-nav">
        <mat-icon>admin_panel_settings</mat-icon> Admin
      </a>
    </mat-toolbar>
    <main><router-outlet /></main>
  `,
  styles: [`
    mat-toolbar { position: sticky; top: 0; z-index: 1000; }
    .toolbar-brand {
      display: flex; align-items: center; gap: 8px;
      text-decoration: none; color: inherit;
      font-size: 1.2rem; font-weight: 600; margin-right: 24px;
    }
    .toolbar-spacer { flex: 1; }
    .active-nav { background: rgba(255,255,255,0.15); border-radius: 4px; }
    main { min-height: calc(100vh - 64px); }
  `],
})
export class AppComponent {}
