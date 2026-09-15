import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { Button } from 'primeng/button';
import { InputText } from 'primeng/inputtext';
import { Message } from 'primeng/message';
import { Password } from 'primeng/password';
import { AuthService } from '../../../core/auth.service';

@Component({
  selector: 'app-login-page',
  imports: [ReactiveFormsModule, Button, InputText, Password, Message],
  templateUrl: './login.page.html',
  styleUrl: './login.page.scss',
})
export class LoginPage {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly fb = inject(FormBuilder);

  readonly error = signal<string | null>(null);
  readonly submitting = signal(false);
  readonly year = new Date().getFullYear();

  readonly form = this.fb.nonNullable.group({
    username: ['', Validators.required],
    password: ['', Validators.required],
  });

  submit(): void {
    this.error.set(null);
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      this.error.set('Enter username and password.');
      return;
    }
    if (this.submitting()) return;

    const { username, password } = this.form.getRawValue();
    this.submitting.set(true);
    try {
      this.auth.login(username, password).subscribe({
        next: (ok) => {
          this.submitting.set(false);
          if (!ok) {
            this.error.set('Invalid username or password.');
            return;
          }
          const raw = this.route.snapshot.queryParamMap.get('returnUrl') || '/home';
          const returnUrl = raw.startsWith('/') && !raw.startsWith('//') ? raw : '/home';
          void this.router.navigateByUrl(returnUrl);
        },
        error: () => {
          this.submitting.set(false);
          this.error.set('Could not reach the login service.');
        },
      });
    } catch {
      this.submitting.set(false);
      this.error.set('Could not start sign-in.');
    }
  }
}
