import { useState } from 'react';
import type { TicketResponse } from '@/api/devdesk-api';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { ChatPanel } from '@/features/chat/ChatPanel';
import { TicketList } from '@/features/tickets/TicketList';

function App() {
  const [draft, setDraft] = useState('');

  const askAboutTicket = (ticket: TicketResponse) => {
    setDraft(`What is the status of ticket ${ticket.id}?`);
  };

  return (
    <div className="mx-auto flex h-screen max-w-6xl flex-col gap-4 p-4">
      <header>
        <h1 className="text-xl font-semibold">DevDesk</h1>
        <p className="text-sm text-muted-foreground">Support assistant for tickets and runbooks</p>
      </header>

      <main className="grid min-h-0 flex-1 gap-4 lg:grid-cols-[1fr_360px]">
        <Card className="min-h-0">
          <CardContent className="h-full min-h-0">
            <ChatPanel draft={draft} onDraftChange={setDraft} />
          </CardContent>
        </Card>

        <Card className="min-h-0 overflow-hidden">
          <CardHeader>
            <CardTitle>Tickets</CardTitle>
          </CardHeader>
          <CardContent className="min-h-0 overflow-y-auto">
            <TicketList onAsk={askAboutTicket} />
          </CardContent>
        </Card>
      </main>
    </div>
  );
}

export default App;
