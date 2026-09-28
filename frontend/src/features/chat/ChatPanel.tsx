import { useMemo } from 'react';
import { MessageSquare, RotateCcw } from 'lucide-react';
import {
  Conversation,
  ConversationContent,
  ConversationEmptyState,
  ConversationScrollButton,
} from '@/components/ai-elements/conversation';
import { Alert, AlertDescription } from '@/components/ui/alert';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Spinner } from '@/components/ui/spinner';
import { ChatComposer } from './ChatComposer';
import { MessageItem } from './MessageItem';
import { toChatItems } from './chatView';
import { useAgentChat } from './useAgentChat';

type ChatPanelProps = {
  draft: string;
  onDraftChange: (value: string) => void;
};

export function ChatPanel({ draft, onDraftChange }: ChatPanelProps) {
  const { messages, isRunning, errorMessage, send, reset } = useAgentChat();
  const items = useMemo(() => toChatItems(messages), [messages]);

  return (
    <div className="flex h-full min-h-0 flex-col gap-3">
      <div className="flex items-center justify-between">
        <h2 className="text-sm font-medium">Assistant</h2>
        <Button
          variant="ghost"
          size="sm"
          onClick={reset}
          disabled={items.length === 0 && !isRunning}
        >
          <RotateCcw />
          New conversation
        </Button>
      </div>

      <Conversation>
        <ConversationContent>
          {items.length === 0 && !isRunning ? (
            <ConversationEmptyState
              icon={<MessageSquare className="size-6" />}
              title="Ask DevDesk"
              description={`Ask about a ticket's status, or how to fix a problem, for example "How do we reset a customer's 2FA?"`}
            />
          ) : (
            items.map((item) => <MessageItem key={item.id} item={item} />)
          )}
          {isRunning && (
            <Badge variant="secondary">
              <Spinner data-icon="inline-start" />
              Thinking…
            </Badge>
          )}
        </ConversationContent>
        <ConversationScrollButton />
      </Conversation>

      {errorMessage && (
        <Alert variant="destructive">
          <AlertDescription>{errorMessage}</AlertDescription>
        </Alert>
      )}

      <ChatComposer value={draft} onChange={onDraftChange} onSend={send} disabled={isRunning} />
    </div>
  );
}
