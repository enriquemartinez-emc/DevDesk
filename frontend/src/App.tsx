import { useState } from 'react';
import type { TicketResponse } from '@/api/devdesk-api';
import { AppSidebar } from '@/components/AppSidebar';
import {
  Breadcrumb,
  BreadcrumbItem,
  BreadcrumbList,
  BreadcrumbPage,
} from '@/components/ui/breadcrumb';
import { Card, CardContent } from '@/components/ui/card';
import { Separator } from '@/components/ui/separator';
import { SidebarInset, SidebarProvider, SidebarTrigger } from '@/components/ui/sidebar';
import { ChatPanel } from '@/features/chat/ChatPanel';
import { TicketsPanel } from '@/features/tickets/TicketsPanel';

function App() {
  const [draft, setDraft] = useState('');

  const askAboutTicket = (ticket: TicketResponse) => {
    setDraft(`What is the status of ticket #${ticket.number}?`);
  };

  return (
    <SidebarProvider className="h-svh">
      <AppSidebar />
      <SidebarInset className="min-h-0">
        <header className="flex h-14 shrink-0 items-center gap-2 border-b px-4">
          <SidebarTrigger />
          <Separator orientation="vertical" className="h-4" />
          <Breadcrumb>
            <BreadcrumbList>
              <BreadcrumbItem>
                <BreadcrumbPage>Tickets</BreadcrumbPage>
              </BreadcrumbItem>
            </BreadcrumbList>
          </Breadcrumb>
        </header>

        <main className="grid min-h-0 flex-1 gap-4 p-4 lg:grid-cols-[1fr_360px]">
          <Card className="min-h-0">
            <CardContent className="h-full min-h-0">
              <ChatPanel draft={draft} onDraftChange={setDraft} />
            </CardContent>
          </Card>

          <TicketsPanel onAsk={askAboutTicket} />
        </main>
      </SidebarInset>
    </SidebarProvider>
  );
}

export default App;
