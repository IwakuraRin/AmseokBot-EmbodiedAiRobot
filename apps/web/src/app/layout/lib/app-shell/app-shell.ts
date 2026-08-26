import { Component, inject } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatListModule } from '@angular/material/list';
import { MatToolbarModule } from '@angular/material/toolbar';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { SessionStore } from '../../../core/session/session';

@Component({
  selector: 'app-shell',
  imports: [
    MatButtonModule,
    MatListModule,
    MatToolbarModule,
    RouterLink,
    RouterLinkActive,
    RouterOutlet,
  ],
  templateUrl: './app-shell.html',
  styleUrl: './app-shell.scss',
})
export class AppShell {
  private readonly router = inject(Router);
  protected readonly sessionStore = inject(SessionStore);

  protected async logout(): Promise<void> {
    await this.sessionStore.logout();
    await this.router.navigateByUrl('/login');
  }
}
