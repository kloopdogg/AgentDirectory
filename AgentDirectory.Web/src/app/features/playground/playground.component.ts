import {
  Component, OnInit, OnDestroy, inject, signal, ViewChild, ElementRef, AfterViewChecked
} from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatChipsModule } from '@angular/material/chips';
import { MatExpansionModule } from '@angular/material/expansion';
import { MatTabsModule } from '@angular/material/tabs';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatDividerModule } from '@angular/material/divider';
import { Subscription } from 'rxjs';
import { AgentService } from '../../core/services/agent.service';
import { PlaygroundService } from '../../core/services/playground.service';
import { AgentDetail } from '../../core/models/agent.model';
import { ChatMessage, UploadedFile } from '../../core/models/playground.model';
import { FileDropComponent } from '../../shared/components/file-drop.component';

@Component({
  selector: 'app-playground',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    MatButtonModule,
    MatIconModule,
    MatProgressSpinnerModule,
    MatTooltipModule,
    MatChipsModule,
    MatExpansionModule,
    MatTabsModule,
    MatSnackBarModule,
    MatDividerModule,
    FileDropComponent,
  ],
  template: `
    @if (loading()) {
      <div class="loading-overlay"><mat-spinner /></div>
    } @else if (agent()) {
      <div class="playground-layout">

        <!-- ── Left panel: Agent details ──────────────────────────────── -->
        <aside class="agent-panel">
          <div class="agent-header">
            <div class="agent-icon">
              @if (agent()!.iconUrl) {
                <img [src]="agent()!.iconUrl" [alt]="agent()!.name" />
              } @else {
                <mat-icon>smart_toy</mat-icon>
              }
            </div>
            <div>
              <h2>{{ agent()!.name }}</h2>
              <span class="protocol-badge">{{ agent()!.protocolType }}</span>
              @if (agent()!.category) { <span class="category">{{ agent()!.category }}</span> }
            </div>
          </div>

          @if (agentTags.length > 0) {
            <mat-chip-set class="tags">
              @for (tag of agentTags; track tag) { <mat-chip>{{ tag }}</mat-chip> }
            </mat-chip-set>
          }

          <mat-divider />

          <div class="section-label">About this agent</div>
          <p class="agent-description">{{ agent()!.longDescription || agent()!.shortDescription }}</p>

          <div class="capabilities-row">
            @if (agent()!.supportsMultimodal) {
              <span class="cap-pill"><mat-icon>image</mat-icon> Multimodal</span>
            }
            @if (agent()!.supportsStreaming) {
              <span class="cap-pill"><mat-icon>stream</mat-icon> Streaming</span>
            }
          </div>

          <!-- How to integrate -->
          <mat-expansion-panel class="integration-panel">
            <mat-expansion-panel-header>
              <mat-panel-title><mat-icon>code</mat-icon> How to integrate</mat-panel-title>
            </mat-expansion-panel-header>

            <mat-tab-group>
              <mat-tab label="REST / OpenAI">
                <pre class="code-block">{{ integrationSnippets.rest }}</pre>
                <button mat-stroked-button (click)="copy(integrationSnippets.rest)">
                  <mat-icon>content_copy</mat-icon> Copy
                </button>
              </mat-tab>
              <mat-tab label="A2A">
                <pre class="code-block">{{ integrationSnippets.a2a }}</pre>
                <button mat-stroked-button (click)="copy(integrationSnippets.a2a)">
                  <mat-icon>content_copy</mat-icon> Copy
                </button>
              </mat-tab>
              <mat-tab label="MCP">
                <pre class="code-block">{{ integrationSnippets.mcp }}</pre>
                <button mat-stroked-button (click)="copy(integrationSnippets.mcp)">
                  <mat-icon>content_copy</mat-icon> Copy
                </button>
              </mat-tab>
            </mat-tab-group>

            @if (agent()!.usageInstructions) {
              <mat-divider />
              <div class="section-label" style="margin-top:12px">Usage notes</div>
              <p>{{ agent()!.usageInstructions }}</p>
            }
          </mat-expansion-panel>
        </aside>

        <!-- ── Right panel: Chat ────────────────────────────────────────── -->
        <section class="chat-panel">
          <div class="chat-toolbar">
            <span class="chat-title">Playground</span>
            <button mat-stroked-button (click)="newConversation()" [disabled]="isStreaming()">
              <mat-icon>add</mat-icon> New conversation
            </button>
          </div>

          <app-file-drop class="chat-drop-zone" (filesDropped)="onFilesDropped($event)">
            <div class="messages" #messageContainer>
              @if (messages().length === 0) {
                <div class="welcome-message">
                  <mat-icon>waving_hand</mat-icon>
                  <p>Start a conversation with <strong>{{ agent()!.name }}</strong></p>
                  <p class="sub">Type a message below or drop a file to get started.</p>
                </div>
              }
              @for (msg of messages(); track $index) {
                <div class="message" [class.user]="msg.role === 'user'" [class.assistant]="msg.role === 'assistant'">
                  <div class="message-avatar">
                    @if (msg.role === 'user') { <mat-icon>person</mat-icon> }
                    @else { <mat-icon>smart_toy</mat-icon> }
                  </div>
                  <div class="message-body">
                    @if (msg.files && msg.files.length > 0) {
                      <div class="file-previews">
                        @for (f of msg.files; track f.blobUrl) {
                          @if (f.contentType.startsWith('image/')) {
                            <img [src]="f.blobUrl" [alt]="f.fileName" class="file-preview-img" />
                          } @else {
                            <div class="file-chip"><mat-icon>attach_file</mat-icon>{{ f.fileName }}</div>
                          }
                        }
                      </div>
                    }
                    <div class="message-content" [innerHTML]="renderMarkdown(msg.content)"></div>
                    @if (msg.isStreaming) { <span class="cursor">▌</span> }
                    @if (msg.role === 'assistant' && !msg.isStreaming) {
                      <button mat-icon-button class="copy-btn" (click)="copy(msg.content)" matTooltip="Copy">
                        <mat-icon>content_copy</mat-icon>
                      </button>
                    }
                  </div>
                </div>
              }
            </div>
          </app-file-drop>

          <!-- Pending file attachments -->
          @if (pendingFiles().length > 0) {
            <div class="pending-files">
              @for (f of pendingFiles(); track f.blobUrl) {
                <div class="pending-chip">
                  <mat-icon>{{ f.contentType.startsWith('image/') ? 'image' : 'attach_file' }}</mat-icon>
                  {{ f.fileName }}
                  <button mat-icon-button (click)="removePendingFile(f)"><mat-icon>close</mat-icon></button>
                </div>
              }
            </div>
          }

          <!-- Input area -->
          <div class="chat-input-area">
            <textarea
              #inputArea
              class="chat-input"
              [(ngModel)]="userInput"
              placeholder="Message the agent… (Shift+Enter for new line)"
              rows="3"
              [disabled]="isStreaming()"
              (keydown.enter)="onEnterKey($any($event))"
              (paste)="onPaste($event)">
            </textarea>
            <div class="input-actions">
              <button mat-icon-button matTooltip="Attach file" (click)="fileInput.click()" [disabled]="!agent()!.supportsMultimodal">
                <mat-icon>attach_file</mat-icon>
              </button>
              <input #fileInput type="file" hidden multiple (change)="onFileInput($event)" />
              <button mat-fab extended color="primary" (click)="sendMessage()" [disabled]="!canSend()">
                @if (isStreaming()) { <mat-spinner diameter="20" /> }
                @else { <mat-icon>send</mat-icon> }
                Send
              </button>
            </div>
          </div>
        </section>
      </div>
    }
  `,
  styles: [`
    .loading-overlay {
      display: flex; align-items: center; justify-content: center; height: 60vh;
    }
    .playground-layout {
      display: flex;
      height: calc(100vh - 64px);
      overflow: hidden;
    }

    /* ── Left panel ── */
    .agent-panel {
      width: 340px;
      min-width: 280px;
      border-right: 1px solid rgba(0,0,0,0.12);
      overflow-y: auto;
      padding: 24px;
      display: flex;
      flex-direction: column;
      gap: 16px;
    }
    .agent-header {
      display: flex;
      gap: 12px;
      align-items: flex-start;
    }
    .agent-icon {
      width: 48px;
      height: 48px;
      border-radius: 0;
      background: #f0f4ff;
      display: flex;
      align-items: center;
      justify-content: center;
      flex-shrink: 0;
      overflow: hidden;
      img { width: 100%; height: 100%; object-fit: contain; }
      mat-icon { color: #5c6bc0; font-size: 28px; }
    }
    h2 { margin: 0 0 4px; font-size: 1.1rem; }
    .protocol-badge {
      display: inline-block;
      padding: 2px 8px;
      border-radius: 12px;
      font-size: 0.7rem;
      font-weight: 600;
      text-transform: uppercase;
      background: #e8eaf6;
      color: #3949ab;
    }
    .category { margin-left: 6px; font-size: 0.8rem; color: rgba(0,0,0,0.5); }
    .tags { flex-wrap: wrap; }
    .section-label { font-size: 0.75rem; font-weight: 600; text-transform: uppercase; color: rgba(0,0,0,0.4); letter-spacing: 0.5px; }
    .agent-description { font-size: 0.9rem; line-height: 1.6; color: rgba(0,0,0,0.7); margin: 0; }
    .capabilities-row { display: flex; gap: 8px; flex-wrap: wrap; }
    .cap-pill {
      display: flex; align-items: center; gap: 4px;
      padding: 4px 10px; border-radius: 16px;
      background: #f5f5f5; font-size: 0.8rem; color: rgba(0,0,0,0.6);
      mat-icon { font-size: 16px; width: 16px; height: 16px; }
    }
    .integration-panel { box-shadow: none !important; border: 1px solid rgba(0,0,0,0.12); border-radius: 8px !important; }
    .code-block {
      background: #1e1e1e; color: #d4d4d4; border-radius: 6px;
      padding: 12px; font-size: 0.78rem; white-space: pre-wrap;
      word-break: break-all; overflow-x: auto; margin: 8px 0;
    }

    /* ── Chat panel ── */
    .chat-panel {
      flex: 1;
      display: flex;
      flex-direction: column;
      overflow: hidden;
    }
    .chat-toolbar {
      display: flex;
      align-items: center;
      justify-content: space-between;
      padding: 12px 20px;
      border-bottom: 1px solid rgba(0,0,0,0.08);
    }
    .chat-title { font-size: 1rem; font-weight: 600; }
    .chat-drop-zone { flex: 1; overflow: hidden; display: flex; flex-direction: column; }
    .messages {
      flex: 1;
      overflow-y: auto;
      padding: 20px;
      display: flex;
      flex-direction: column;
      gap: 16px;
    }
    .welcome-message {
      display: flex; flex-direction: column; align-items: center;
      justify-content: center; height: 100%; color: rgba(0,0,0,0.4); gap: 8px;
      mat-icon { font-size: 48px; width: 48px; height: 48px; }
      p { margin: 0; font-size: 1rem; }
      .sub { font-size: 0.85rem; }
    }
    .message {
      display: flex;
      gap: 12px;
      align-items: flex-start;
      animation: slideIn 0.2s ease;
    }
    .message.user { flex-direction: row-reverse; }
    .message-avatar {
      width: 36px; height: 36px; border-radius: 50%;
      background: #e8eaf6; display: flex; align-items: center; justify-content: center;
      flex-shrink: 0;
      mat-icon { font-size: 20px; color: #5c6bc0; }
    }
    .message.user .message-avatar { background: #e3f2fd; mat-icon { color: #1565c0; } }
    .message-body {
      max-width: 72%;
      background: white;
      border: 1px solid rgba(0,0,0,0.1);
      border-radius: 12px;
      padding: 10px 14px;
      font-size: 0.9rem;
      line-height: 1.6;
      position: relative;
    }
    .message.user .message-body { background: #e3f2fd; border-color: #bbdefb; }
    .message-content ::ng-deep {
      p { margin: 0 0 8px; }
      p:last-child { margin-bottom: 0; }
      pre { background: #f5f5f5; padding: 8px; border-radius: 4px; overflow-x: auto; font-size: 0.82em; }
      code { background: #f5f5f5; padding: 1px 4px; border-radius: 3px; font-size: 0.88em; }
    }
    .cursor { animation: blink 0.8s step-end infinite; font-weight: bold; color: #3f51b5; }
    @keyframes blink { 0%, 100% { opacity: 1; } 50% { opacity: 0; } }
    @keyframes slideIn { from { opacity: 0; transform: translateY(8px); } to { opacity: 1; transform: none; } }
    .copy-btn {
      position: absolute; top: 4px; right: 4px;
      opacity: 0; transition: opacity 0.2s;
      width: 28px; height: 28px;
      mat-icon { font-size: 16px; }
    }
    .message-body:hover .copy-btn { opacity: 1; }
    .file-previews { display: flex; flex-wrap: wrap; gap: 8px; margin-bottom: 8px; }
    .file-preview-img { max-width: 200px; max-height: 150px; border-radius: 6px; object-fit: cover; }
    .file-chip {
      display: flex; align-items: center; gap: 4px;
      padding: 4px 10px; background: #f5f5f5; border-radius: 16px; font-size: 0.8rem;
    }
    .pending-files {
      display: flex; flex-wrap: wrap; gap: 8px; padding: 8px 20px;
      border-top: 1px solid rgba(0,0,0,0.08);
    }
    .pending-chip {
      display: flex; align-items: center; gap: 4px;
      padding: 4px 8px 4px 12px; background: #e8eaf6; border-radius: 20px; font-size: 0.82rem;
      mat-icon { font-size: 16px; }
    }

    /* ── Input ── */
    .chat-input-area {
      border-top: 1px solid rgba(0,0,0,0.12);
      padding: 12px 16px;
      display: flex;
      gap: 8px;
      align-items: flex-end;
    }
    .chat-input {
      flex: 1;
      border: 1px solid rgba(0,0,0,0.2);
      border-radius: 8px;
      padding: 10px 12px;
      font-family: inherit;
      font-size: 0.9rem;
      line-height: 1.5;
      resize: none;
      outline: none;
      transition: border-color 0.2s;
      &:focus { border-color: #3f51b5; }
    }
    .input-actions { display: flex; gap: 8px; align-items: center; }
  `],
})
export class PlaygroundComponent implements OnInit, OnDestroy, AfterViewChecked {
  @ViewChild('messageContainer') messageContainer!: ElementRef<HTMLDivElement>;
  @ViewChild('inputArea') inputArea!: ElementRef<HTMLTextAreaElement>;

  private readonly route = inject(ActivatedRoute);
  private readonly agentService = inject(AgentService);
  private readonly playgroundService = inject(PlaygroundService);
  private readonly snackBar = inject(MatSnackBar);

  agent = signal<AgentDetail | null>(null);
  loading = signal(true);
  messages = signal<ChatMessage[]>([]);
  pendingFiles = signal<UploadedFile[]>([]);
  isStreaming = signal(false);
  userInput = '';

  private sessionId: string | null = null;
  private streamSub: Subscription | null = null;
  private shouldScroll = false;

  get agentTags(): string[] {
    const tags = this.agent()?.tags;
    if (!tags) return [];
    try { return JSON.parse(tags); } catch { return tags.split(',').map(t => t.trim()); }
  }

  get integrationSnippets() {
    const a = this.agent();
    const endpoint = a?.endpointUrl ?? 'https://your-agent-endpoint';
    return {
      rest: `curl -X POST "${endpoint}" \\
  -H "Authorization: Bearer YOUR_TOKEN" \\
  -H "Content-Type: application/json" \\
  -d '{"messages": [{"role":"user","content":"Hello!"}]}'`,
      a2a: `// Agent card: GET ${endpoint}/.well-known/agent-card.json
// Start session: POST ${endpoint}/sessions
// Send message: POST ${endpoint}/sessions/{id}/messages
// Stream: GET ${endpoint}/sessions/{id}/stream`,
      mcp: `// In your MCP host config (e.g. claude_desktop_config.json):
{
  "mcpServers": {
    "${a?.name ?? 'agent'}": {
      "url": "${endpoint}/mcp",
      "transport": "http-sse"
    }
  }
}`,
    };
  }

  canSend = () => (!!this.userInput.trim() || this.pendingFiles().length > 0) && !this.isStreaming();

  ngOnInit() {
    const id = this.route.snapshot.paramMap.get('id')!;
    this.agentService.getById(id).subscribe({
      next: agent => {
        this.agent.set(agent);
        this.loading.set(false);
        this.initSession(id);
      },
      error: () => this.loading.set(false),
    });
  }

  ngAfterViewChecked() {
    if (this.shouldScroll) {
      this.scrollToBottom();
      this.shouldScroll = false;
    }
  }

  ngOnDestroy() {
    this.streamSub?.unsubscribe();
    if (this.sessionId) this.playgroundService.deleteSession(this.sessionId).subscribe();
  }

  private initSession(agentId: string) {
    this.playgroundService.createSession(agentId).subscribe({
      next: res => this.sessionId = res.sessionId,
    });
  }

  newConversation() {
    if (this.sessionId) {
      this.playgroundService.deleteSession(this.sessionId).subscribe();
    }
    const agentId = this.route.snapshot.paramMap.get('id')!;
    this.messages.set([]);
    this.pendingFiles.set([]);
    this.userInput = '';
    this.streamSub?.unsubscribe();
    this.initSession(agentId);
  }

  sendMessage() {
    if (!this.canSend() || !this.sessionId) return;

    const text = this.userInput.trim();
    const files = [...this.pendingFiles()];
    this.userInput = '';
    this.pendingFiles.set([]);

    // Add user message
    this.messages.update(msgs => [...msgs, {
      role: 'user',
      content: text,
      timestamp: new Date(),
      files,
    }]);

    // Add empty assistant message that will be filled in via streaming
    this.messages.update(msgs => [...msgs, {
      role: 'assistant',
      content: '',
      timestamp: new Date(),
      isStreaming: true,
    }]);

    this.isStreaming.set(true);
    this.shouldScroll = true;

    this.streamSub = this.playgroundService.streamMessage(this.sessionId, text).subscribe({
      next: event => {
        if (event.type === 'token' && event.content) {
          this.messages.update(msgs => {
            const updated = [...msgs];
            const last = updated[updated.length - 1];
            if (last.role === 'assistant') {
              updated[updated.length - 1] = { ...last, content: last.content + event.content };
            }
            return updated;
          });
          this.shouldScroll = true;
        } else if (event.type === 'tool_call') {
          // Show tool call indicator
          this.messages.update(msgs => {
            const updated = [...msgs];
            const last = updated[updated.length - 1];
            if (last.role === 'assistant') {
              updated[updated.length - 1] = {
                ...last,
                content: last.content + `\n\n🔧 *Using tool: ${event.toolName}...*\n\n`
              };
            }
            return updated;
          });
        } else if (event.type === 'done' || event.type === 'error') {
          this.messages.update(msgs => {
            const updated = [...msgs];
            const last = updated[updated.length - 1];
            if (last.role === 'assistant') {
              updated[updated.length - 1] = { ...last, isStreaming: false };
            }
            return updated;
          });
          this.isStreaming.set(false);
        }
      },
      error: () => {
        this.isStreaming.set(false);
        this.messages.update(msgs => {
          const updated = [...msgs];
          const last = updated[updated.length - 1];
          if (last.role === 'assistant') {
            updated[updated.length - 1] = { ...last, content: 'An error occurred. Please try again.', isStreaming: false };
          }
          return updated;
        });
      },
    });
  }

  onEnterKey(event: KeyboardEvent) {
    if (!event.shiftKey) {
      event.preventDefault();
      this.sendMessage();
    }
  }

  onPaste(event: ClipboardEvent) {
    const items = event.clipboardData?.items;
    if (!items || !this.agent()?.supportsMultimodal) return;
    for (const item of Array.from(items)) {
      if (item.type.startsWith('image/')) {
        event.preventDefault();
        const file = item.getAsFile();
        if (file) this.uploadFiles([file]);
      }
    }
  }

  onFilesDropped(files: File[]) {
    if (this.agent()?.supportsMultimodal) this.uploadFiles(files);
    else this.snackBar.open('This agent does not support file uploads.', 'OK', { duration: 3000 });
  }

  onFileInput(event: Event) {
    const files = Array.from((event.target as HTMLInputElement).files ?? []);
    if (files.length > 0) this.uploadFiles(files);
  }

  removePendingFile(file: UploadedFile) {
    this.pendingFiles.update(files => files.filter(f => f.blobUrl !== file.blobUrl));
  }

  private uploadFiles(files: File[]) {
    if (!this.sessionId) return;
    for (const file of files) {
      this.playgroundService.uploadFile(this.sessionId, file).subscribe({
        next: uploaded => this.pendingFiles.update(f => [...f, uploaded]),
        error: () => this.snackBar.open(`Failed to upload ${file.name}`, 'OK', { duration: 3000 }),
      });
    }
  }

  renderMarkdown(text: string): string {
    // Basic markdown → HTML (bold, italic, code, line breaks)
    return text
      .replace(/&/g, '&amp;')
      .replace(/</g, '&lt;')
      .replace(/>/g, '&gt;')
      .replace(/```([\s\S]*?)```/g, '<pre><code>$1</code></pre>')
      .replace(/`([^`]+)`/g, '<code>$1</code>')
      .replace(/\*\*(.+?)\*\*/g, '<strong>$1</strong>')
      .replace(/\*(.+?)\*/g, '<em>$1</em>')
      .replace(/\n/g, '<br>');
  }

  copy(text: string) {
    navigator.clipboard.writeText(text).then(() =>
      this.snackBar.open('Copied!', undefined, { duration: 1500 })
    );
  }

  private scrollToBottom() {
    const el = this.messageContainer?.nativeElement;
    if (el) el.scrollTop = el.scrollHeight;
  }
}
