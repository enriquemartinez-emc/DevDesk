export const toolLabels: Record<string, { running: string; done: string }> = {
  lookup_ticket: { running: 'Looking up ticket…', done: 'Looked up ticket' },
  search_runbooks: { running: 'Searching runbooks…', done: 'Searched runbooks' },
};

export function toolLabel(name: string, finished: boolean): string {
  const labels = toolLabels[name];
  if (!labels) {
    return finished ? name : `${name}…`;
  }

  return finished ? labels.done : labels.running;
}
