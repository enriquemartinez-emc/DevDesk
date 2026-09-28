import { useQuery } from '@tanstack/react-query';
import { api } from '@/lib/api';

export const ticketKeys = {
  all: ['tickets'] as const,
  list: () => [...ticketKeys.all, 'list'] as const,
};

export function useTickets() {
  return useQuery({
    queryKey: ticketKeys.list(),
    queryFn: async () => (await api.api.listTickets()).data,
    refetchInterval: 15_000,
  });
}
