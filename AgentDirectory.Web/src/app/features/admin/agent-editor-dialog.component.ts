import { Component, Inject, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatInputModule } from '@angular/material/input';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatSelectModule } from '@angular/material/select';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatDialogModule, MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatDividerModule } from '@angular/material/divider';
import { MatIconModule } from '@angular/material/icon';
import { AgentService } from '../../core/services/agent.service';
import { AgentDetail, PROTOCOL_TYPES } from '../../core/models/agent.model';

@Component({
  selector: 'app-agent-editor-dialog',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    ReactiveFormsModule,
    MatButtonModule,
    MatInputModule,
    MatFormFieldModule,
    MatSelectModule,
    MatCheckboxModule,
    MatDialogModule,
    MatSnackBarModule,
    MatProgressSpinnerModule,
    MatDividerModule,
    MatIconModule,
  ],
  template: `
    <h2 mat-dialog-title>{{ isEdit ? 'Edit' : 'New' }} Agent</h2>

    <mat-dialog-content>
      <form [formGroup]="form" class="editor-form">
        <mat-form-field appearance="outline">
          <mat-label>Name *</mat-label>
          <input matInput formControlName="name" />
        </mat-form-field>

        <mat-form-field appearance="outline">
          <mat-label>Short description *</mat-label>
          <textarea matInput formControlName="shortDescription" rows="2"></textarea>
        </mat-form-field>

        <mat-form-field appearance="outline">
          <mat-label>Long description</mat-label>
          <textarea matInput formControlName="longDescription" rows="4"></textarea>
        </mat-form-field>

        <div class="two-col">
          <mat-form-field appearance="outline">
            <mat-label>Category</mat-label>
            <input matInput formControlName="category" />
          </mat-form-field>

          <div class="icon-field">
            <mat-form-field appearance="outline" style="flex:1">
              <mat-label>Icon URL</mat-label>
              <input matInput formControlName="iconUrl" />
            </mat-form-field>
            <div class="icon-upload-col">
              @if (form.value.iconUrl) {
                <img [src]="form.value.iconUrl" class="icon-preview" alt="icon preview" />
              }
              <button type="button" mat-stroked-button (click)="iconFileInput.click()" [disabled]="uploadingIcon()">
                @if (uploadingIcon()) { <mat-spinner diameter="16" style="display:inline-block;margin-right:6px" /> }
                <mat-icon>upload</mat-icon> Upload PNG
              </button>
              <input #iconFileInput type="file" accept="image/png" hidden (change)="onIconFileSelected($event)" />
            </div>
          </div>
        </div>

        <mat-form-field appearance="outline">
          <mat-label>Tags (comma-separated or JSON array)</mat-label>
          <input matInput formControlName="tags" placeholder='["tag1","tag2"] or tag1, tag2' />
        </mat-form-field>

        <mat-divider />
        <div class="section-label">Endpoint &amp; Protocol</div>

        <mat-form-field appearance="outline">
          <mat-label>Endpoint URL *</mat-label>
          <input matInput formControlName="endpointUrl" placeholder="https://..." />
        </mat-form-field>

        <div class="two-col">
          <mat-form-field appearance="outline">
            <mat-label>Protocol type *</mat-label>
            <mat-select formControlName="protocolType">
              @for (p of protocols; track p) { <mat-option [value]="p">{{ protocolLabel(p) }}</mat-option> }
            </mat-select>
          </mat-form-field>

          <mat-form-field appearance="outline">
            <mat-label>Auth type</mat-label>
            <mat-select formControlName="authType">
              <mat-option value="">None</mat-option>
              <mat-option value="ApiKey">API Key</mat-option>
              <mat-option value="BearerToken">Bearer Token</mat-option>
              <mat-option value="ManagedIdentity">Managed Identity</mat-option>
            </mat-select>
          </mat-form-field>
        </div>

        <mat-form-field appearance="outline">
          <mat-label>Key Vault secret name</mat-label>
          <input matInput formControlName="authSecretRef" placeholder="my-agent-api-key" />
          <mat-hint>The name of the secret in Azure Key Vault (never the value itself)</mat-hint>
        </mat-form-field>

        <mat-divider />
        <div class="section-label">Capabilities</div>

        <div class="checkboxes">
          <mat-checkbox formControlName="supportsStreaming">Supports streaming</mat-checkbox>
          <mat-checkbox formControlName="supportsMultimodal">Supports multimodal (images / files)</mat-checkbox>
          <mat-checkbox formControlName="isPublished">Published (visible in directory)</mat-checkbox>
        </div>

        <mat-form-field appearance="outline">
          <mat-label>Usage instructions</mat-label>
          <textarea matInput formControlName="usageInstructions" rows="4"
            placeholder="Describe how to use this agent, special instructions, example prompts...">
          </textarea>
        </mat-form-field>
      </form>
    </mat-dialog-content>

    <mat-dialog-actions align="end">
      <button mat-button mat-dialog-close>Cancel</button>
      <button mat-flat-button color="primary" (click)="save()" [disabled]="form.invalid || saving()">
        @if (saving()) { <mat-spinner diameter="16" style="display:inline-block;margin-right:6px" /> }
        {{ isEdit ? 'Save changes' : 'Create agent' }}
      </button>
    </mat-dialog-actions>
  `,
  styles: [`
    .editor-form { display: flex; flex-direction: column; gap: 12px; padding-top: 8px; }
    mat-form-field { width: 100%; }
    .two-col { display: grid; grid-template-columns: 1fr 1fr; gap: 12px; }
    .icon-field { display: flex; gap: 12px; align-items: flex-start; }
    .icon-upload-col { display: flex; flex-direction: column; align-items: center; gap: 6px; padding-top: 4px; min-width: 100px; }
    .icon-preview { width: 48px; height: 48px; object-fit: contain; border-radius: 6px; border: 1px solid rgba(0,0,0,0.12); }
    .section-label {
      font-size: 0.75rem; font-weight: 600; text-transform: uppercase;
      color: rgba(0,0,0,0.4); letter-spacing: 0.5px; margin: 8px 0 4px;
    }
    .checkboxes { display: flex; flex-direction: column; gap: 8px; padding: 4px 0; }
  `],
})
export class AgentEditorDialogComponent {
  private readonly agentService = inject(AgentService);
  private readonly snackBar = inject(MatSnackBar);
  private readonly dialogRef = inject(MatDialogRef<AgentEditorDialogComponent>);
  private readonly fb = inject(FormBuilder);

  readonly protocols = PROTOCOL_TYPES;
  readonly isEdit: boolean;
  readonly saving = signal(false);
  readonly uploadingIcon = signal(false);

  private readonly protocolLabels: Record<string, string> = {
    'OpenAI_Responses': 'OpenAI Responses',
    'OpenAI_Chat': 'OpenAI Chat',
    'A2A': 'A2A',
    'MCP': 'MCP',
    'AG-UI': 'AG-UI',
    'CustomREST': 'Custom REST',
  };

  protocolLabel(p: string): string {
    return this.protocolLabels[p] ?? p;
  }

  form!: ReturnType<typeof this.buildForm>;

  constructor(@Inject(MAT_DIALOG_DATA) readonly agent: AgentDetail | null) {
    this.isEdit = !!agent;
    this.form = this.buildForm(agent);
  }

  private buildForm(agent: AgentDetail | null) {
    return this.fb.group({
      name: [agent?.name ?? '', Validators.required],
      shortDescription: [agent?.shortDescription ?? '', Validators.required],
      longDescription: [agent?.longDescription ?? ''],
      endpointUrl: [agent?.endpointUrl ?? '', Validators.required],
      protocolType: [agent?.protocolType ?? 'OpenAI_Responses', Validators.required],
      authType: [agent?.authType ?? ''],
      authSecretRef: [''],  // never pre-fill for security
      supportsMultimodal: [agent?.supportsMultimodal ?? false],
      supportsStreaming: [agent?.supportsStreaming ?? true],
      tags: [agent?.tags ?? ''],
      category: [agent?.category ?? ''],
      iconUrl: [agent?.iconUrl ?? ''],
      usageInstructions: [agent?.usageInstructions ?? ''],
      isPublished: [agent?.isPublished ?? false],
    });
  }

  onIconFileSelected(event: Event) {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    if (!file) return;

    this.uploadingIcon.set(true);
    this.agentService.uploadIcon(file).subscribe({
      next: (result) => {
        this.form.patchValue({ iconUrl: result.url });
        this.uploadingIcon.set(false);
      },
      error: () => {
        this.snackBar.open('Icon upload failed.', 'OK', { duration: 4000 });
        this.uploadingIcon.set(false);
      },
    });
    // Reset so the same file can be re-selected if needed
    input.value = '';
  }

  save() {
    if (this.form.invalid) return;
    const v = this.form.value;

    const request = {
      name: v.name!,
      shortDescription: v.shortDescription!,
      longDescription: v.longDescription || null,
      endpointUrl: v.endpointUrl!,
      protocolType: v.protocolType!,
      authType: v.authType || null,
      authSecretRef: v.authSecretRef || null,
      supportsMultimodal: v.supportsMultimodal ?? false,
      supportsStreaming: v.supportsStreaming ?? true,
      tags: v.tags || null,
      category: v.category || null,
      iconUrl: v.iconUrl || null,
      usageInstructions: v.usageInstructions || null,
      isPublished: v.isPublished ?? false,
    };

    this.saving.set(true);
    const obs = this.isEdit
      ? this.agentService.update(this.agent!.id, request)
      : this.agentService.create(request);

    obs.subscribe({
      next: () => {
        this.snackBar.open(`Agent ${this.isEdit ? 'updated' : 'created'}.`, undefined, { duration: 3000 });
        this.dialogRef.close(true);
      },
      error: () => {
        this.snackBar.open('Failed to save agent.', 'OK', { duration: 4000 });
        this.saving.set(false);
      },
    });
  }
}
