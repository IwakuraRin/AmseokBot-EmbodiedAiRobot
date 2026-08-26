import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatTableModule } from '@angular/material/table';
import { firstValueFrom } from 'rxjs';
import { PageHeader } from '../../../../../shared/ui/page-header';

interface WebUserSummary {
  id: string;
  userName: string;
  displayName: string;
  isEnabled: boolean;
  role: string;
}

@Component({
  selector: 'app-web-user-list-page',
  imports: [
    MatButtonModule,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatTableModule,
    PageHeader,
    ReactiveFormsModule,
  ],
  templateUrl: './web-user-list-page.html',
})
export class WebUserListPage implements OnInit {
  private readonly http = inject(HttpClient);

  protected readonly users = signal<WebUserSummary[]>([]);
  protected readonly loading = signal(false);
  protected readonly errorMessage = signal<string | null>(null);
  protected readonly roles = ['Owner', 'Operator', 'Viewer'];
  protected readonly displayedColumns = ['userName', 'displayName', 'role', 'status', 'actions'];
  protected readonly createForm = new FormGroup({
    userName: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    displayName: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    password: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.minLength(12)],
    }),
    role: new FormControl('Viewer', { nonNullable: true, validators: [Validators.required] }),
  });

  async ngOnInit(): Promise<void> {
    await this.loadUsers();
  }

  protected async createUser(): Promise<void> {
    if (this.createForm.invalid || this.loading()) {
      this.createForm.markAllAsTouched();
      return;
    }

    await this.runMutation(async () => {
      await firstValueFrom(this.http.post('/api/web-users/', this.createForm.getRawValue()));
      this.createForm.reset({ userName: '', displayName: '', password: '', role: 'Viewer' });
    });
  }

  protected async changeRole(user: WebUserSummary, role: string): Promise<void> {
    await this.updateUser(user, { role });
  }

  protected async toggleEnabled(user: WebUserSummary): Promise<void> {
    await this.updateUser(user, { isEnabled: !user.isEnabled });
  }

  protected async deleteUser(user: WebUserSummary): Promise<void> {
    if (!globalThis.confirm(`确定删除 Web 用户“${user.displayName}”吗？`)) {
      return;
    }

    await this.runMutation(async () => {
      await firstValueFrom(this.http.delete(`/api/web-users/${encodeURIComponent(user.id)}`));
    });
  }

  private async updateUser(
    user: WebUserSummary,
    changes: Partial<Pick<WebUserSummary, 'isEnabled' | 'role'>>,
  ): Promise<void> {
    await this.runMutation(async () => {
      await firstValueFrom(
        this.http.put(`/api/web-users/${encodeURIComponent(user.id)}`, {
          displayName: user.displayName,
          isEnabled: changes.isEnabled ?? user.isEnabled,
          role: changes.role ?? user.role,
        }),
      );
    });
  }

  private async runMutation(action: () => Promise<void>): Promise<void> {
    this.loading.set(true);
    this.errorMessage.set(null);
    try {
      await action();
      await this.loadUsers();
    } catch (error) {
      this.errorMessage.set(this.describeError(error));
    } finally {
      this.loading.set(false);
    }
  }

  private async loadUsers(): Promise<void> {
    this.users.set(await firstValueFrom(this.http.get<WebUserSummary[]>('/api/web-users/')));
  }

  private describeError(error: unknown): string {
    if (error instanceof HttpErrorResponse && Array.isArray(error.error?.errors)) {
      return error.error.errors.join(' ');
    }

    return '操作失败，请稍后重试。';
  }
}
