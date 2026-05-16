export interface AgentEvent {
  type: 'token' | 'tool_call' | 'tool_result' | 'done' | 'error';
  content?: string | null;
  toolName?: string | null;
  error?: string | null;
}

export interface ChatMessage {
  role: 'user' | 'assistant';
  content: string;
  timestamp: Date;
  isStreaming?: boolean;
  files?: UploadedFile[];
}

export interface UploadedFile {
  fileName: string;
  blobUrl: string;
  contentType: string;
}

export interface SessionResponse {
  sessionId: string;
  agentId: string;
}
