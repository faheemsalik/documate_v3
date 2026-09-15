import { Component, computed, inject, signal } from '@angular/core';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { ToggleSwitch } from 'primeng/toggleswitch';
import { Button } from 'primeng/button';
import { ThemeService } from '../../core/theme.service';
import { AuthService } from '../../core/auth.service';
import { NAV_GROUPS, type NavGroup } from './nav.config';

@Component({
  selector: 'app-shell',
  imports: [RouterOutlet, RouterLink, RouterLinkActive, ToggleSwitch, Button, FormsModule],
  templateUrl: './app-shell.component.html',
  styleUrl: './app-shell.component.scss',
})
export class AppShellComponent {
  private readonly theme = inject(ThemeService);
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  readonly brand = 'DOCUMATE ADMIN';
  readonly groups = NAV_GROUPS;
  readonly userId = computed(() => this.auth.current()?.userId ?? 'ops-admin');

  private readonly openState = signal<Record<string, boolean | undefined>>({});
  readonly darkMode = computed(() => this.theme.mode() === 'dark');

  isExpanded(group: NavGroup): boolean {
    const override = this.openState()[group.label];
    if (override !== undefined) return override;
    return !!group.defaultOpen;
  }

  toggleGroup(group: NavGroup): void {
    this.openState.update((s) => ({ ...s, [group.label]: !this.isExpanded(group) }));
  }

  onDarkToggle(checked: boolean): void {
    this.theme.setMode(checked ? 'dark' : 'light');
  }

  logout(): void {
    this.auth.logout();
    void this.router.navigateByUrl('/login');
  }
}
