import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { AgentDetail, AgentSummary, CreateAgentRequest, UpdateAgentRequest } from '../models/agent.model';

@Injectable({ providedIn: 'root' })
export class AgentService {
  private readonly http = inject(HttpClient);
  private readonly base = '/api/agents';

  getPublished(): Observable<AgentSummary[]> {
    return this.http.get<AgentSummary[]>(this.base);
  }

  getById(id: string): Observable<AgentDetail> {
    return this.http.get<AgentDetail>(`${this.base}/${id}`);
  }

  // ── Admin ──────────────────────────────────────────────────────────────────

  getAllAdmin(): Observable<AgentSummary[]> {
    return this.http.get<AgentSummary[]>(`${this.base}/admin/all`);
  }

  create(request: CreateAgentRequest): Observable<AgentDetail> {
    return this.http.post<AgentDetail>(this.base, request);
  }

  update(id: string, request: UpdateAgentRequest): Observable<AgentDetail> {
    return this.http.put<AgentDetail>(`${this.base}/${id}`, request);
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${this.base}/${id}`);
  }

  uploadIcon(file: File): Observable<{ url: string }> {
    const form = new FormData();
    form.append('file', file, file.name);
    return this.http.post<{ url: string }>(`${this.base}/icon`, form);
  }
}
