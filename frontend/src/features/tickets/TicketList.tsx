import { useState } from 'react';
import { MessageSquareText } from 'lucide-react';
import type { TicketResponse } from '@/api/devdesk-api';
import { Alert, AlertDescription } from '@/components/ui/alert';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Empty, EmptyDescription, EmptyHeader, EmptyTitle } from '@/components/ui/empty';
import {
  Item,
  ItemContent,
  ItemDescription,
  ItemFooter,
  ItemGroup,
  ItemHeader,
  ItemTitle,
} from '@/components/ui/item';
import { Skeleton } from '@/components/ui/skeleton';
import { ResolveTicketDialog } from './ResolveTicketDialog';
import { type TicketAction, availableActions, ticketActionLabels } from './ticketActions';
import { ticketStatusBadgeClassName, ticketStatusLabel } from './ticketStatus';
import { useTicketActions } from './useTicketActions';
import { useTickets } from './useTickets';

type TicketListProps = {
  onAsk: (ticket: TicketResponse) => void;
};

export function TicketList({ onAsk }: TicketListProps) {
  const { data, isPending, isError, refetch } = useTickets();
  const actions = useTicketActions();
  const [resolving, setResolving] = useState<TicketResponse | null>(null);

  const runAction = (action: TicketAction, ticket: TicketResponse) => {
    actions.reset();
    switch (action) {
      case 'assign':
        actions.assign(ticket.id);
        break;
      case 'resolve':
        setResolving(ticket);
        break;
      case 'close':
        actions.close(ticket.id);
        break;
      case 'reopen':
        actions.reopen(ticket.id);
        break;
    }
  };

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
          <EmptyDescription>Use “New ticket” to create the first one.</EmptyDescription>
        </EmptyHeader>
      </Empty>
    );
  }

  return (
    <>
      {!resolving && actions.errorMessage && (
        <Alert variant="destructive">
          <AlertDescription>{actions.errorMessage}</AlertDescription>
        </Alert>
      )}

      <ItemGroup className="gap-3">
        {data.map((ticket) => (
          <Item key={ticket.id} variant="outline" size="sm">
            <ItemHeader>
              <span className="text-sm text-muted-foreground">#{ticket.number}</span>
              <Badge className={ticketStatusBadgeClassName(ticket.status)}>
                {ticketStatusLabel(ticket.status)}
              </Badge>
            </ItemHeader>
            <ItemContent>
              <ItemTitle>{ticket.subject}</ItemTitle>
              <ItemDescription>{ticket.description}</ItemDescription>
            </ItemContent>
            <ItemFooter>
              <Button variant="ghost" size="sm" onClick={() => onAsk(ticket)}>
                <MessageSquareText data-icon="inline-start" />
                Ask about this ticket
              </Button>
              {availableActions(ticket.status).map((action) => (
                <Button
                  key={action}
                  variant="outline"
                  size="sm"
                  disabled={actions.isPending}
                  onClick={() => runAction(action, ticket)}
                >
                  {ticketActionLabels[action]}
                </Button>
              ))}
            </ItemFooter>
          </Item>
        ))}
      </ItemGroup>

      <ResolveTicketDialog
        ticket={resolving}
        onResolve={(id, note) => actions.resolve(id, note, () => setResolving(null))}
        onClose={() => {
          actions.reset();
          setResolving(null);
        }}
        isPending={actions.isPending}
        errorMessage={actions.errorMessage}
      />
    </>
  );
}
