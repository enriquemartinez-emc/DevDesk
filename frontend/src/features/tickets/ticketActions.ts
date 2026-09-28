export type TicketAction = 'assign' | 'resolve' | 'close' | 'reopen';

export const ticketActionLabels: Record<TicketAction, string> = {
  assign: 'Assign to me',
  resolve: 'Resolve…',
  close: 'Close',
  reopen: 'Reopen',
};

// Mirrors the lifecycle in DevDesk.Server.Core (TicketRules) so the UI only offers legal moves.
// The server still enforces it and answers 409 for anything else.
const actionsByStatus: Record<string, TicketAction[]> = {
  open: ['assign'],
  in_progress: ['resolve'],
  resolved: ['close', 'reopen'],
  closed: ['reopen'],
};

export function availableActions(status: string): TicketAction[] {
  return actionsByStatus[status] ?? [];
}
