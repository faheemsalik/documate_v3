import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { Button } from 'primeng/button';
import { InputText } from 'primeng/inputtext';
import { Message } from 'primeng/message';
import { Password } from 'primeng/password';
import { AuthService } from '../../../core/auth.service';

@Component({
  selector: 'app-login-page',
  imports: [FormsModule, Button, InputText, Password, Message],
  templateUrl: './login.page.html',
  styleUrl: './login.page.scss',
})
export class LoginPage {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);

  username = 'ops-admin';
  password = '';
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);

  submit(): void {
    this.loading.set(true);
    this.error.set(null);
    this.auth.login(this.username.trim(), this.password).subscribe({
      next: (ok) => {
        if (!ok) {
          this.loading.set(false);
          this.error.set('Invalid admin credentials.');
          return;
        }
        this.auth.refreshMe().subscribe({
          next: () => {
            this.loading.set(false);
            const returnUrl = this.route.snapshot.queryParamMap.get('returnUrl') || '/dashboard';
            void this.router.navigateByUrl(returnUrl);
          },
          error: () => {
            this.loading.set(false);
            void this.router.navigateByUrl('/dashboard');
          },
        });
      },
    });
  }
}
