import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatChipsModule } from '@angular/material/chips';
import { MatIconModule } from '@angular/material/icon';
import { MatTooltipModule } from '@angular/material/tooltip';
import { AgentSummary } from '../../core/models/agent.model';

@Component({
  selector: 'app-agent-card',
  standalone: true,
  imports: [CommonModule, MatCardModule, MatButtonModule, MatChipsModule, MatIconModule, MatTooltipModule],
  template: `
    <mat-card class="agent-card" (click)="open.emit(agent)" tabindex="0" (keyup.enter)="open.emit(agent)">
      <mat-card-header>
        <div mat-card-avatar class="agent-avatar">
          @if (agent.iconUrl) {
            <img [src]="agent.iconUrl" [alt]="agent.name" />
          } @else {
            <mat-icon>smart_toy</mat-icon>
          }
        </div>
        <mat-card-title>{{ agent.name }}</mat-card-title>
        <mat-card-subtitle>
          <span class="protocol-badge" [class]="'protocol-' + protocolClass">{{ agent.protocolType }}</span>
          @if (agent.category) { <span class="category">{{ agent.category }}</span> }
        </mat-card-subtitle>
      </mat-card-header>

      <mat-card-content>
        <p class="description">{{ agent.shortDescription }}</p>

        @if (tags.length > 0) {
          <mat-chip-set aria-label="Agent tags">
            @for (tag of tags; track tag) {
              <mat-chip>{{ tag }}</mat-chip>
            }
          </mat-chip-set>
        }
      </mat-card-content>

      <mat-card-actions>
        <div class="capabilities">
          @if (agent.supportsMultimodal) {
            <mat-icon matTooltip="Supports images & files" class="cap-icon">image</mat-icon>
          }
          @if (agent.supportsStreaming) {
            <mat-icon matTooltip="Streaming responses" class="cap-icon">stream</mat-icon>
          }
        </div>
        <button mat-flat-button color="primary" (click)="$event.stopPropagation(); open.emit(agent)">
          Try it <mat-icon iconPositionEnd>arrow_forward</mat-icon>
        </button>
      </mat-card-actions>
    </mat-card>
  `,
  styles: [`
    .agent-card {
      cursor: pointer;
      transition: box-shadow 0.2s, transform 0.15s;
      height: 100%;
      display: flex;
      flex-direction: column;
    }
    .agent-card:hover {
      box-shadow: 0 8px 24px rgba(0,0,0,0.15);
      transform: translateY(-2px);
    }
    .agent-avatar {
      display: flex;
      align-items: center;
      justify-content: center;
      background: #f0f4ff;
      border-radius: 0;
      overflow: hidden;
    }
    .agent-avatar img { width: 100%; height: 100%; object-fit: contain; }
    .agent-avatar mat-icon { font-size: 28px; color: #5c6bc0; }
    .description {
      color: rgba(0,0,0,0.7);
      font-size: 0.9rem;
      line-height: 1.5;
      margin-bottom: 12px;
      display: -webkit-box;
      -webkit-line-clamp: 3;
      -webkit-box-orient: vertical;
      overflow: hidden;
    }
    .protocol-badge {
      display: inline-block;
      padding: 2px 8px;
      border-radius: 12px;
      font-size: 0.72rem;
      font-weight: 600;
      text-transform: uppercase;
      letter-spacing: 0.5px;
      background: #e8eaf6;
      color: #3949ab;
    }
    .protocol-openai { background: #e8f5e9; color: #2e7d32; }
    .protocol-a2a { background: #fff3e0; color: #e65100; }
    .protocol-mcp { background: #f3e5f5; color: #7b1fa2; }
    .protocol-agui { background: #e1f5fe; color: #0277bd; }
    .protocol-custom { background: #fce4ec; color: #c62828; }
    .category {
      margin-left: 8px;
      font-size: 0.8rem;
      color: rgba(0,0,0,0.5);
    }
    mat-card-actions {
      display: flex;
      align-items: center;
      justify-content: space-between;
      margin-top: auto;
    }
    .capabilities { display: flex; gap: 4px; }
    .cap-icon { font-size: 18px; color: rgba(0,0,0,0.4); cursor: help; }
  `],
})
export class AgentCardComponent {
  @Input({ required: true }) agent!: AgentSummary;
  @Output() open = new EventEmitter<AgentSummary>();

  get tags(): string[] {
    if (!this.agent.tags) return [];
    try { return JSON.parse(this.agent.tags); } catch { return this.agent.tags.split(',').map(t => t.trim()); }
  }

  get protocolClass(): string {
    const p = this.agent.protocolType.toLowerCase();
    if (p.includes('openai')) return 'openai';
    if (p === 'a2a') return 'a2a';
    if (p === 'mcp') return 'mcp';
    if (p === 'ag-ui') return 'agui';
    return 'custom';
  }
}
