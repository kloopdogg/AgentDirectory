export interface AgentSummary {
  id: string;
  name: string;
  shortDescription: string;
  protocolType: string;
  supportsMultimodal: boolean;
  supportsStreaming: boolean;
  tags: string | null;
  category: string | null;
  iconUrl: string | null;
  isPublished: boolean;
}

export interface AgentDetail extends AgentSummary {
  longDescription: string | null;
  endpointUrl: string;
  authType: string | null;
  usageInstructions: string | null;
  createdAt: string;
  updatedAt: string;
  createdBy: string | null;
}

export interface CreateAgentRequest {
  name: string;
  shortDescription: string;
  longDescription: string | null;
  endpointUrl: string;
  protocolType: string;
  authType: string | null;
  authSecretRef: string | null;
  supportsMultimodal: boolean;
  supportsStreaming: boolean;
  tags: string | null;
  category: string | null;
  iconUrl: string | null;
  usageInstructions: string | null;
  isPublished: boolean;
}

export type UpdateAgentRequest = CreateAgentRequest;

export const PROTOCOL_TYPES = [
  'OpenAI_Responses',
] as const;

export type ProtocolType = typeof PROTOCOL_TYPES[number];
