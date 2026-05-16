import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { AgentEvent, SessionResponse, UploadedFile } from '../models/playground.model';

@Injectable({ providedIn: 'root' })
export class PlaygroundService {
  private readonly http = inject(HttpClient);
  private readonly base = '/api/playground';

  createSession(agentId: string): Observable<SessionResponse> {
    return this.http.post<SessionResponse>(`${this.base}/sessions`, { agentId });
  }

  deleteSession(sessionId: string): Observable<void> {
    return this.http.delete<void>(`${this.base}/sessions/${sessionId}`);
  }

  /**
   * Opens an SSE stream for the given session + message.
   * Returns an Observable that emits AgentEvent objects as they arrive.
   */
  streamMessage(sessionId: string, message: string): Observable<AgentEvent> {
    return new Observable(subscriber => {
      const params = new URLSearchParams({ message });
      const url = `${this.base}/sessions/${sessionId}/stream?${params}`;
      const eventSource = new EventSource(url);

      eventSource.onmessage = (event) => {
        try {
          const data: AgentEvent = JSON.parse(event.data);
          subscriber.next(data);
          if (data.type === 'done' || data.type === 'error') {
            eventSource.close();
            subscriber.complete();
          }
        } catch {
          // ignore malformed events
        }
      };

      eventSource.onerror = () => {
        eventSource.close();
        subscriber.error(new Error('SSE connection error'));
      };

      // Cleanup when unsubscribed
      return () => eventSource.close();
    });
  }

  uploadFile(sessionId: string, file: File): Observable<UploadedFile> {
    const formData = new FormData();
    formData.append('file', file);
    return this.http.post<UploadedFile>(`${this.base}/sessions/${sessionId}/upload`, formData);
  }
}
