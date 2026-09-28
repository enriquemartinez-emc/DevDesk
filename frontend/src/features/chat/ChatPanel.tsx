import { useEffect, useMemo, useRef } from 'react';
import { RotateCcw } from 'lucide-react';
import { Button } from '@/components/ui/button';
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
  const endRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    endRef.current?.scrollIntoView({ behavior: 'smooth', block: 'end' });
  }, [items, isRunning]);

  return (
    <div className="flex h-full min-h-0 flex-col gap-3">
      <div className="flex items-center justify-between">
        <h2 className="text-sm font-medium">Assistant</h2>
        <Button variant="ghost" size="sm" onClick={reset} disabled={items.length === 0 && !isRunning}>
          <RotateCcw />
          New conversation
        </Button>
      </div>

      <div className="min-h-0 flex-1 space-y-4 overflow-y-auto pr-1" aria-live="polite">
        {items.length === 0 && !isRunning && (
          <p className="text-sm text-muted-foreground">
            Ask what's going on with a ticket, or how to fix a problem — for example "How do we reset a
            customer's 2FA?"
          </p>
        )}
        {items.map((item) => (
          <MessageItem key={item.id} item={item} />
        ))}
        {isRunning && <p className="text-xs text-muted-foreground">Thinking…</p>}
        {errorMessage && (
          <p role="alert" className="text-sm text-destructive">
            {errorMessage}
          </p>
        )}
        <div ref={endRef} />
      </div>

      <ChatComposer value={draft} onChange={onDraftChange} onSend={send} disabled={isRunning} />
    </div>
  );
}
