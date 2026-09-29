import { Message, MessageContent, MessageResponse } from '@/components/ai-elements/message';
import type { ChatItem } from './chatView';

export function MessageItem({ item }: { item: ChatItem }) {
  return (
    <Message from={item.kind}>
      <MessageContent>
        {item.kind === 'assistant' ? <MessageResponse>{item.text}</MessageResponse> : item.text}
      </MessageContent>
    </Message>
  );
}
