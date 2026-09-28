import { useState } from 'react';
import { Plus } from 'lucide-react';
import type { TicketResponse } from '@/api/devdesk-api';
import { Button } from '@/components/ui/button';
import { Card, CardAction, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { CreateTicketDialog } from './CreateTicketDialog';
import { TicketList } from './TicketList';

type TicketsPanelProps = {
  onAsk: (ticket: TicketResponse) => void;
};

export function TicketsPanel({ onAsk }: TicketsPanelProps) {
  const [creating, setCreating] = useState(false);

  return (
    <Card className="min-h-0 overflow-hidden">
      <CardHeader>
        <CardTitle>Tickets</CardTitle>
        <CardAction>
          <Button size="sm" onClick={() => setCreating(true)}>
            <Plus data-icon="inline-start" />
            New ticket
          </Button>
        </CardAction>
      </CardHeader>
      <CardContent className="min-h-0 overflow-y-auto">
        <TicketList onAsk={onAsk} />
      </CardContent>
      <CreateTicketDialog open={creating} onOpenChange={setCreating} />
    </Card>
  );
}
