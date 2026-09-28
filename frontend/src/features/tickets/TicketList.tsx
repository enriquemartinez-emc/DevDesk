import { MessageSquareText } from 'lucide-react';
import type { TicketResponse } from '@/api/devdesk-api';
import { Alert, AlertDescription } from '@/components/ui/alert';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Empty, EmptyDescription, EmptyHeader, EmptyTitle } from '@/components/ui/empty';
import {
  Item,
  ItemActions,
  ItemContent,
  ItemDescription,
  ItemFooter,
  ItemGroup,
  ItemTitle,
} from '@/components/ui/item';
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
      <ItemGroup aria-label="Loading tickets">
        {[0, 1, 2].map((i) => (
          <Skeleton key={i} className="h-20 w-full" />
        ))}
      </ItemGroup>
    );
  }

  if (isError) {
    return (
      <Alert variant="destructive">
        <AlertDescription>
          Couldn't load tickets.{' '}
          <Button variant="link" size="sm" onClick={() => void refetch()}>
            Try again
          </Button>
        </AlertDescription>
      </Alert>
    );
  }

  if (data.length === 0) {
    return (
      <Empty>
        <EmptyHeader>
          <EmptyTitle>No tickets yet</EmptyTitle>
          <EmptyDescription>Tickets created through the API will show up here.</EmptyDescription>
        </EmptyHeader>
      </Empty>
    );
  }

  return (
    <ItemGroup className="gap-3">
      {data.map((ticket) => (
        <Item key={ticket.id} variant="outline" size="sm">
          <ItemContent>
            <ItemTitle>{ticket.subject}</ItemTitle>
            <ItemDescription>{ticket.description}</ItemDescription>
          </ItemContent>
          <ItemActions>
            <Badge variant="secondary">{ticketStatusLabel(ticket.status)}</Badge>
          </ItemActions>
          <ItemFooter>
            <Button variant="ghost" size="sm" onClick={() => onAsk(ticket)}>
              <MessageSquareText data-icon="inline-start" />
              Ask about this ticket
            </Button>
          </ItemFooter>
        </Item>
      ))}
    </ItemGroup>
  );
}
