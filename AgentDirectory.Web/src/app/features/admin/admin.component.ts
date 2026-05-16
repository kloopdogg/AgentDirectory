import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { MatTableModule } from '@angular/material/table';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatSelectModule } from '@angular/material/select';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatDialogModule, MatDialog } from '@angular/material/dialog';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { MatChipsModule } from '@angular/material/chips';
import { MatTooltipModule } from '@angular/material/tooltip';
import { AgentService } from '../../core/services/agent.service';
import { AgentDetail, AgentSummary, CreateAgentRequest, PROTOCOL_TYPES } from '../../core/models/agent.model';
import { AgentEditorDialogComponent } from './agent-editor-dialog.component';

@Component({
  selector: 'app-admin',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    ReactiveFormsModule,
    MatTableModule,
    MatButtonModule,
    MatIconModule,
    MatInputModule,
    MatFormFieldModule,
    MatSelectModule,
    MatCheckboxModule,
    MatDialogModule,
    MatSnackBarModule,
    MatProgressSpinnerModule,
    MatSlideToggleModule,
    MatChipsModule,
    MatTooltipModule,
  ],
  template: `
    <div class="admin-container">
      <div class="admin-header">
        <h1><mat-icon>admin_panel_settings</mat-icon> Agent Registry — Admin</h1>
        <button mat-flat-button color="primary" (click)="openEditor(null)">
          <mat-icon>add</mat-icon> New agent
        </button>
      </div>

      @if (loading()) {
        <div class="loading"><mat-spinner diameter="40" /></div>
      } @else {
        <table mat-table [dataSource]="agents()" class="agents-table">
          <ng-container matColumnDef="status">
            <th mat-header-cell *matHeaderCellDef>Status</th>
            <td mat-cell *matCellDef="let a">
              <mat-icon [class]="a.isPublished ? 'published' : 'draft'"
                        [matTooltip]="a.isPublished ? 'Published' : 'Draft'">
                {{ a.isPublished ? 'check_circle' : 'unpublished' }}
              </mat-icon>
            </td>
          </ng-container>

          <ng-container matColumnDef="name">
            <th mat-header-cell *matHeaderCellDef>Name</th>
            <td mat-cell *matCellDef="let a"><strong>{{ a.name }}</strong></td>
          </ng-container>

          <ng-container matColumnDef="protocol">
            <th mat-header-cell *matHeaderCellDef>Protocol</th>
            <td mat-cell *matCellDef="let a">
              <span class="proto-chip">{{ a.protocolType }}</span>
            </td>
          </ng-container>

          <ng-container matColumnDef="category">
            <th mat-header-cell *matHeaderCellDef>Category</th>
            <td mat-cell *matCellDef="let a">{{ a.category ?? '—' }}</td>
          </ng-container>

          <ng-container matColumnDef="capabilities">
            <th mat-header-cell *matHeaderCellDef>Capabilities</th>
            <td mat-cell *matCellDef="let a">
              @if (a.supportsMultimodal) { <mat-icon matTooltip="Multimodal">image</mat-icon> }
              @if (a.supportsStreaming) { <mat-icon matTooltip="Streaming">stream</mat-icon> }
            </td>
          </ng-container>

          <ng-container matColumnDef="actions">
            <th mat-header-cell *matHeaderCellDef></th>
            <td mat-cell *matCellDef="let a">
              <button mat-icon-button (click)="openEditor(a)" matTooltip="Edit">
                <mat-icon>edit</mat-icon>
              </button>
              <button mat-icon-button color="warn" (click)="deleteAgent(a)" matTooltip="Delete">
                <mat-icon>delete</mat-icon>
              </button>
            </td>
          </ng-container>

          <tr mat-header-row *matHeaderRowDef="columns"></tr>
          <tr mat-row *matRowDef="let row; columns: columns;"></tr>
        </table>

        @if (agents().length === 0) {
          <div class="empty-state">
            <mat-icon>inventory_2</mat-icon>
            <p>No agents yet. Click "New agent" to add one.</p>
          </div>
        }
      }
    </div>
  `,
  styles: [`
    .admin-container { max-width: 1200px; margin: 0 auto; padding: 32px 24px; }
    .admin-header {
      display: flex; align-items: center; justify-content: space-between; margin-bottom: 24px;
      h1 { display: flex; align-items: center; gap: 8px; margin: 0; font-size: 1.5rem; }
    }
    .agents-table { width: 100%; }
    .loading { display: flex; justify-content: center; padding: 60px; }
    .published { color: #2e7d32; }
    .draft { color: #9e9e9e; }
    .proto-chip {
      display: inline-block; padding: 2px 8px; border-radius: 12px;
      font-size: 0.72rem; font-weight: 600; background: #e8eaf6; color: #3949ab;
    }
    .empty-state {
      display: flex; flex-direction: column; align-items: center; padding: 60px; color: rgba(0,0,0,0.4);
      mat-icon { font-size: 48px; width: 48px; height: 48px; }
    }
    mat-icon { font-size: 18px; width: 18px; height: 18px; vertical-align: middle; }
  `],
})
export class AdminComponent implements OnInit {
  private readonly agentService = inject(AgentService);
  private readonly dialog = inject(MatDialog);
  private readonly snackBar = inject(MatSnackBar);

  agents = signal<AgentSummary[]>([]);
  loading = signal(true);
  columns = ['status', 'name', 'protocol', 'category', 'capabilities', 'actions'];

  ngOnInit() {
    this.loadAgents();
  }

  loadAgents() {
    this.loading.set(true);
    this.agentService.getAllAdmin().subscribe({
      next: agents => { this.agents.set(agents); this.loading.set(false); },
      error: () => this.loading.set(false),
    });
  }

  openEditor(agent: AgentSummary | null) {
    const agentDetail$ = agent
      ? this.agentService.getById(agent.id)
      : null;

    if (agentDetail$) {
      agentDetail$.subscribe(detail => this.openDialog(detail));
    } else {
      this.openDialog(null);
    }
  }

  private openDialog(agent: AgentDetail | null) {
    const ref = this.dialog.open(AgentEditorDialogComponent, {
      width: '680px',
      data: agent,
    });
    ref.afterClosed().subscribe(saved => {
      if (saved) this.loadAgents();
    });
  }

  deleteAgent(agent: AgentSummary) {
    if (!confirm(`Delete "${agent.name}"? This cannot be undone.`)) return;
    this.agentService.delete(agent.id).subscribe({
      next: () => {
        this.snackBar.open(`"${agent.name}" deleted.`, undefined, { duration: 3000 });
        this.loadAgents();
      },
    });
  }
}
