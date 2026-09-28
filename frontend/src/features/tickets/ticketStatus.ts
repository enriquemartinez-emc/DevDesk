export const ticketStatusLabels: Record<string, string> = {
  open: 'Open',
  in_progress: 'In progress',
  resolved: 'Resolved',
  closed: 'Closed',
};

export function ticketStatusLabel(status: string): string {
  return ticketStatusLabels[status] ?? status;
}
