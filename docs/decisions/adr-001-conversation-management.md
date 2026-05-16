# ADR-001: Conversation Management for OpenAI Responses Adapter

**Status:** Accepted  
**Date:** 2026-04-26

## Context

The `OpenAI_Responses` protocol adapter communicates with agents published on Microsoft Foundry via the OpenAI Responses API (`Azure.AI.Extensions.OpenAI`). Multi-turn conversation state can be managed in two ways:

### Option A — Server-managed conversations (chosen)
Create a `ProjectConversation` via Foundry on the first turn of each session. Pass the server-assigned conversation ID to `GetProjectResponsesClientForAgent` on every subsequent turn. The server maintains history; only the new user message is sent per turn.

### Option B — Client-managed stateless
Send the full chat history as `InputItems` on every request. Set `Store = false` to explicitly opt out of server-side persistence since the server copy would be unreachable (no `previous_response_id` support on Agent Applications).

## Decision

**Option A** was chosen.

Benefits:
- History is maintained server-side — no risk of exceeding request size limits as conversations grow long.
- Turns are grouped under a single conversation ID in Foundry traces, enabling debugging across a full session.
- Simpler per-request payload (only the new message, not the full history each time).

## Constraints discovered during implementation

- Published Agent Applications **reject** a raw GUID (with or without hyphens) as `defaultConversationId`. Only a server-issued conversation ID from `CreateProjectConversationAsync()` is accepted.
- `previous_response_id` is also not supported at the Agent Application endpoint — the stateful chaining pattern from the standard Responses API does not apply here.
- Conversation creation is performed once per gateway session (lazy, on the first message). The ID is stored in `AgentSession.ConversationId` and emitted as a `conversation_id` event so the gateway can persist it without the adapter needing direct access to session state.

## Consequences

- `IAgentAdapter.StreamMessageAsync` gains a `string? conversationId` parameter. All adapters other than `OpenAIResponsesAdapter` receive and ignore it.
- `AgentSession` gains a `ConversationId` property alongside the existing `PreviousResponseId`.
- Client-side history (`AgentSession.History`) is still maintained by the gateway — it is used by all other adapters and retained as a fallback if the server-managed conversation path fails.
- If Foundry ever supports `previous_response_id` or `defaultConversationId` on Agent Applications, the adapter can be updated independently without changing other adapters or the gateway contract.
