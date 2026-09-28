import { MessageSquareText } from 'lucide-react';
import type { TicketResponse } from '@/api/devdesk-api';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Skeleton } from '@/components/ui/skeleton';
import { ticketStatusLabel } from './ticketStatus';
import { useTickets } from './useTickets';

type TicketListProps = {
  onAsk: (ticket: TicketResponse) => void;
};

export function TicketList({ onAsk }: TicketListProps) {
  const { data, isPending, isError, refetch } = useTickets();

  if (isPending) {
    return (
      <div className="space-y-3" aria-label="Loading tickets">
        {[0, 1, 2].map((i) => (
          <Skeleton key={i} className="h-16 w-full" />
        ))}
      </div>
    );
  }

  if (isError) {
    return (
      <div className="space-y-2 text-sm">
        <p className="text-destructive">Couldn't load tickets.</p>
        <Button variant="outline" size="sm" onClick={() => void refetch()}>
          Try again
        </Button>
      </div>
    );
  }

  if (data.length === 0) {
    return <p className="text-sm text-muted-foreground">No tickets yet.</p>;
  }

  return (
    <ul className="space-y-3">
      {data.map((ticket) => (
        <li key={ticket.id} className="rounded-lg border p-3">
          <div className="flex items-start justify-between gap-2">
            <p className="text-sm font-medium leading-tight">{ticket.subject}</p>
            <Badge variant="secondary">{ticketStatusLabel(ticket.status)}</Badge>
          </div>
          <p className="mt-1 line-clamp-2 text-xs text-muted-foreground">{ticket.description}</p>
          <Button variant="ghost" size="sm" className="mt-2 -ml-2" onClick={() => onAsk(ticket)}>
            <MessageSquareText />
            Ask about this ticket
          </Button>
        </li>
      ))}
    </ul>
  );
}
