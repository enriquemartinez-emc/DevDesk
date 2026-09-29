import type { Message } from '@ag-ui/client';

export type ChatItem = { kind: 'user' | 'assistant'; id: string; text: string };

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

export function toChatItems(messages: Message[]): ChatItem[] {
  return messages
    .filter((message) => message.role === 'user' || message.role === 'assistant')
    .map((message) => ({
      kind: message.role as 'user' | 'assistant',
      id: message.id,
      text: contentText(message.content),
    }))
    .filter((item) => item.text.length > 0);
}
