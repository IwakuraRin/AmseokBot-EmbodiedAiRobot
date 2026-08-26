import { Component, inject, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { Router } from '@angular/router';
import { SessionStore } from '../../../../../core/session/session';

@Component({
  selector: 'app-setup-owner-page',
  imports: [MatButtonModule, MatCardModule, MatFormFieldModule, MatInputModule, ReactiveFormsModule],
  templateUrl: './setup-owner-page.html',
})
export class SetupOwnerPage {
  private readonly sessionStore = inject(SessionStore);
  private readonly router = inject(Router);

  protected readonly submitting = signal(false);
  protected readonly errorMessage = signal<string | null>(null);
  protected readonly form = new FormGroup({
    token: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    userName: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    displayName: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    password: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.minLength(12)],
    }),
  });

  protected async submit(): Promise<void> {
    if (this.form.invalid || this.submitting()) {
      this.form.markAllAsTouched();
      return;
    }

    this.submitting.set(true);
    this.errorMessage.set(null);
    try {
      const value = this.form.getRawValue();
      await this.sessionStore.createFirstOwner(value);
      await this.sessionStore.login(value.userName, value.password, false);
      await this.router.navigateByUrl('/dashboard');
    } catch {
      this.errorMessage.set('初始化失败。请确认令牌未过期，并检查用户名和密码要求。');
    } finally {
      this.submitting.set(false);
    }
  }
}
