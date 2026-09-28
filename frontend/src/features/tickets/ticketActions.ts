export type TicketAction = 'assign' | 'resolve' | 'close' | 'reopen';

export const ticketActionLabels: Record<TicketAction, string> = {
  assign: 'Assign to me',
  resolve: 'Resolve…',
  close: 'Close',
  reopen: 'Reopen',
};

// Mirrors DevDesk.Server.Core's TicketRules so the UI only offers legal moves; the server still enforces it (409 otherwise).
const actionsByStatus: Record<string, TicketAction[]> = {
  open: ['assign'],
  in_progress: ['resolve'],
  resolved: ['close', 'reopen'],
  closed: ['reopen'],
};

export function availableActions(status: string): TicketAction[] {
  return actionsByStatus[status] ?? [];
}
