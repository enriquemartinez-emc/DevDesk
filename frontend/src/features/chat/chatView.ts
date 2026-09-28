import type { Message } from '@ag-ui/client';
import { parseToolArguments, parseToolResult } from './chatSchemas';

export type ToolCallView = {
  id: string;
  name: string;
  args: unknown;
  result: string | null;
};

export type ChatItem =
  | { kind: 'user'; id: string; text: string }
  | { kind: 'assistant'; id: string; text: string; toolCalls: ToolCallView[] };

function contentText(content: unknown): string {
  if (typeof content === 'string') {
    return content;
  }

  if (Array.isArray(content)) {
    return content
      .map((part: { type?: string; text?: string }) =>
        part.type === 'text' ? (part.text ?? '') : '',
      )
      .join('');
  }

  return '';
}

// Turns the AG-UI message list into what the UI shows: tool results are folded into the
// assistant message that made the call, and empty assistant turns are dropped.
export function toChatItems(messages: Message[]): ChatItem[] {
  const toolResults = new Map<string, string>();
  for (const message of messages) {
    if (message.role === 'tool') {
      toolResults.set(message.toolCallId, parseToolResult(contentText(message.content)));
    }
  }

  const items: ChatItem[] = [];
  for (const message of messages) {
    if (message.role === 'user') {
      items.push({ kind: 'user', id: message.id, text: contentText(message.content) });
    } else if (message.role === 'assistant') {
      const text = contentText(message.content);
      const toolCalls = (message.toolCalls ?? []).map((call) => ({
        id: call.id,
        name: call.function.name,
        args: parseToolArguments(call.function.arguments),
        result: toolResults.get(call.id) ?? null,
      }));

      if (text.length > 0 || toolCalls.length > 0) {
        items.push({ kind: 'assistant', id: message.id, text, toolCalls });
      }
    }
  }

  return items;
}
