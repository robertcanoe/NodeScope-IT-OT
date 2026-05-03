import { Component, inject } from '@angular/core';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatDividerModule } from '@angular/material/divider';
import { MatListModule } from '@angular/material/list';
import { MatSidenavModule } from '@angular/material/sidenav';
import { MatToolbarModule } from '@angular/material/toolbar';

import { AuthService } from '../auth.service';
import { AuthTokenStorage } from '../auth-token.storage';

/**
 * Primary shell scaffolding navigation rail + workspace chrome consumed by guarded feature slices.
 */
@Component({
  standalone: true,
  selector: 'app-main-layout',
  imports: [
    RouterOutlet,
    RouterLink,
    RouterLinkActive,
    MatSidenavModule,
    MatToolbarModule,
    MatListModule,
    MatButtonModule,
    MatDividerModule,
  ],
  templateUrl: './main-layout.component.html',
  styleUrl: './main-layout.component.scss',
})
export class MainLayoutComponent {
  private readonly authService = inject(AuthService);
  private readonly authStorage = inject(AuthTokenStorage);
  private readonly router = inject(Router);

  protected readonly currentUser = this.authStorage.currentUser;

  /** Clears SPA session primitives and reroutes strangers to credential capture. */
  logout(): void {
    this.authService.logout();
    void this.router.navigateByUrl('/login');
  }
}
