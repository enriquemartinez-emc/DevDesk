const ticketStatusLabels: Record<string, string> = {
  open: 'Open',
  in_progress: 'In progress',
  resolved: 'Resolved',
  closed: 'Closed',
};

export function ticketStatusLabel(status: string): string {
  return ticketStatusLabels[status] ?? status;
}

// Open/in-progress get an attention color (unclaimed or active work); resolved reads as done;
// closed is deliberately neutral so it doesn't compete with the still-actionable statuses.
const ticketStatusBadgeClassNames: Record<string, string> = {
  open: 'bg-sky-500/10 text-sky-700 dark:bg-sky-500/15 dark:text-sky-400',
  in_progress: 'bg-amber-500/10 text-amber-700 dark:bg-amber-500/15 dark:text-amber-400',
  resolved: 'bg-emerald-500/10 text-emerald-700 dark:bg-emerald-500/15 dark:text-emerald-400',
  closed: 'bg-muted text-muted-foreground',
};

export function ticketStatusBadgeClassName(status: string): string {
  return ticketStatusBadgeClassNames[status] ?? ticketStatusBadgeClassNames.closed;
}
