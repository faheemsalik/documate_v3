import { NgClass } from '@angular/common';
import { Component, computed, inject, signal } from '@angular/core';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { ToggleSwitch } from 'primeng/toggleswitch';
import { Button } from 'primeng/button';
import { FormsModule } from '@angular/forms';
import { ThemeService } from '../../core/theme.service';
import { AppContextService } from '../../core/app-context.service';
import { AuthService } from '../../core/auth.service';
import { NAV_GROUPS, NAV_HELP, NAV_PINNED, type NavGroup } from './nav.config';

@Component({
  selector: 'app-shell',
  imports: [RouterOutlet, RouterLink, RouterLinkActive, NgClass, ToggleSwitch, Button, FormsModule],
  templateUrl: './app-shell.component.html',
  styleUrl: './app-shell.component.scss',
})
export class AppShellComponent {
  private readonly theme = inject(ThemeService);
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  readonly ctx = inject(AppContextService);

  readonly brand = 'Documate';
  readonly pinned = NAV_PINNED;
  readonly groups = NAV_GROUPS;
  readonly help = NAV_HELP;

  /** Explicit open state; undefined = use defaultOpen / auto. */
  private readonly openState = signal<Record<string, boolean | undefined>>({});

  readonly darkMode = computed(() => this.theme.mode() === 'dark');

  isExpanded(group: NavGroup): boolean {
    const override = this.openState()[group.label];
    if (override !== undefined) return override;
    return !!group.defaultOpen;
  }

  toggleGroup(group: NavGroup): void {
    const next = !this.isExpanded(group);
    this.openState.update((s) => ({ ...s, [group.label]: next }));
  }

  onDarkToggle(checked: boolean): void {
    this.theme.setMode(checked ? 'dark' : 'light');
  }

  logout(): void {
    this.auth.logout();
    void this.router.navigateByUrl('/login');
  }
}
