import { Check, Loader2 } from 'lucide-react';
import { Badge } from '@/components/ui/badge';
import { cn } from '@/lib/utils';
import type { ChatItem, ToolCallView } from './chatView';
import { toolLabel } from './toolLabels';

function ToolCallChip({ call }: { call: ToolCallView }) {
  const finished = call.result !== null;

  return (
    <Badge variant="outline" className="gap-1.5 font-normal">
      {finished ? <Check className="size-3" /> : <Loader2 className="size-3 animate-spin" />}
      {toolLabel(call.name, finished)}
    </Badge>
  );
}

export function MessageItem({ item }: { item: ChatItem }) {
  const isUser = item.kind === 'user';

  return (
    <div className={cn('flex flex-col gap-2', isUser ? 'items-end' : 'items-start')}>
      {item.kind === 'assistant' && item.toolCalls.length > 0 && (
        <div className="flex flex-wrap gap-1.5">
          {item.toolCalls.map((call) => (
            <ToolCallChip key={call.id} call={call} />
          ))}
        </div>
      )}
      {item.text.length > 0 && (
        <div
          className={cn(
            'max-w-[85%] whitespace-pre-wrap rounded-2xl px-4 py-2 text-sm',
            isUser ? 'bg-primary text-primary-foreground' : 'bg-muted',
          )}
        >
          {item.text}
        </div>
      )}
    </div>
  );
}
