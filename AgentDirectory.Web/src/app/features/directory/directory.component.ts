import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatInputModule } from '@angular/material/input';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatSelectModule } from '@angular/material/select';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatChipsModule } from '@angular/material/chips';
import { AgentService } from '../../core/services/agent.service';
import { AgentSummary } from '../../core/models/agent.model';
import { AgentCardComponent } from './agent-card.component';

@Component({
  selector: 'app-directory',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    MatInputModule,
    MatFormFieldModule,
    MatIconModule,
    MatSelectModule,
    MatProgressSpinnerModule,
    MatChipsModule,
    AgentCardComponent,
  ],
  template: `
    <div class="directory-container">
      <div class="directory-header">
        <h1>Agent Directory</h1>
        <p>Discover and interact with AI agents built by our team.</p>
      </div>

      <div class="filters">
        <mat-form-field appearance="outline" class="search-field">
          <mat-label>Search agents</mat-label>
          <mat-icon matPrefix>search</mat-icon>
          <input matInput [(ngModel)]="searchQuery" placeholder="Name, description, or tag..." />
        </mat-form-field>

        <mat-form-field appearance="outline" class="category-filter">
          <mat-label>Category</mat-label>
          <mat-select [(ngModel)]="selectedCategory">
            <mat-option value="">All categories</mat-option>
            @for (cat of categories(); track cat) {
              <mat-option [value]="cat">{{ cat }}</mat-option>
            }
          </mat-select>
        </mat-form-field>
      </div>

      @if (loading()) {
        <div class="loading-state">
          <mat-spinner diameter="48"></mat-spinner>
          <p>Loading agents...</p>
        </div>
      } @else if (filteredAgents().length === 0) {
        <div class="empty-state">
          <mat-icon>search_off</mat-icon>
          <p>No agents found matching your search.</p>
        </div>
      } @else {
        <div class="agents-grid">
          @for (agent of filteredAgents(); track agent.id) {
            <app-agent-card [agent]="agent" (open)="navigateToPlayground($event)" />
          }
        </div>
      }
    </div>
  `,
  styles: [`
    .directory-container {
      max-width: 1400px;
      margin: 0 auto;
      padding: 32px 24px;
    }
    .directory-header {
      margin-bottom: 32px;
      h1 { font-size: 2rem; margin: 0 0 8px; }
      p { color: rgba(0,0,0,0.6); margin: 0; }
    }
    .filters {
      display: flex;
      gap: 16px;
      margin-bottom: 24px;
      flex-wrap: wrap;
    }
    .search-field { flex: 1; min-width: 260px; }
    .category-filter { width: 220px; }
    .agents-grid {
      display: grid;
      grid-template-columns: repeat(auto-fill, minmax(300px, 1fr));
      gap: 24px;
    }
    .loading-state, .empty-state {
      display: flex;
      flex-direction: column;
      align-items: center;
      padding: 80px 0;
      color: rgba(0,0,0,0.4);
      gap: 16px;
      mat-icon { font-size: 48px; width: 48px; height: 48px; }
    }
  `],
})
export class DirectoryComponent implements OnInit {
  private readonly agentService = inject(AgentService);
  private readonly router = inject(Router);

  agents = signal<AgentSummary[]>([]);
  loading = signal(true);
  searchQuery = '';
  selectedCategory = '';

  categories = computed(() =>
    [...new Set(this.agents().map(a => a.category).filter(Boolean) as string[])].sort()
  );

  filteredAgents = computed(() => {
    const q = this.searchQuery.toLowerCase();
    return this.agents().filter(a => {
      const matchesSearch = !q
        || a.name.toLowerCase().includes(q)
        || a.shortDescription.toLowerCase().includes(q)
        || (a.tags ?? '').toLowerCase().includes(q);
      const matchesCategory = !this.selectedCategory || a.category === this.selectedCategory;
      return matchesSearch && matchesCategory;
    });
  });

  ngOnInit() {
    this.agentService.getPublished().subscribe({
      next: agents => { this.agents.set(agents); this.loading.set(false); },
      error: () => this.loading.set(false),
    });
  }

  navigateToPlayground(agent: AgentSummary) {
    this.router.navigate(['/agent', agent.id]);
  }
}
