import { useMutation, useQueryClient } from '@tanstack/react-query';
import type { CreateTicketRequest } from '@/api/devdesk-api';
import { api } from '@/lib/api';
import { problemMessage } from '@/lib/problem';
import { CURRENT_AGENT_ID } from './demoIdentity';
import { ticketKeys } from './useTickets';

function useTicketMutation<TVariables>(mutationFn: (variables: TVariables) => Promise<unknown>) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn,
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ticketKeys.all }),
  });
}

export function useCreateTicket() {
  const create = useTicketMutation((body: CreateTicketRequest) => api.api.createTicket(body));

  return {
    create: (body: CreateTicketRequest, onSuccess?: () => void) =>
      create.mutate(body, { onSuccess }),
    isPending: create.isPending,
    errorMessage: create.error ? problemMessage(create.error, "Couldn't create the ticket.") : null,
    reset: create.reset,
  };
}

// One instance covers every lifecycle action so the list can disable its buttons and show a
// single error while any of them is running.
export function useTicketActions() {
  const assign = useTicketMutation((id: string) =>
    api.api.assignTicket({ id }, { agentId: CURRENT_AGENT_ID }),
  );
  const resolve = useTicketMutation(
    ({ id, resolutionNote }: { id: string; resolutionNote: string }) =>
      api.api.resolveTicket({ id }, { resolutionNote }),
  );
  const close = useTicketMutation((id: string) => api.api.closeTicket({ id }));
  const reopen = useTicketMutation((id: string) => api.api.reopenTicket({ id }));

  const mutations = [assign, resolve, close, reopen];
  const failed = mutations.find((mutation) => mutation.error);

  return {
    assign: (id: string) => assign.mutate(id),
    resolve: (id: string, resolutionNote: string, onSuccess?: () => void) =>
      resolve.mutate({ id, resolutionNote }, { onSuccess }),
    close: (id: string) => close.mutate(id),
    reopen: (id: string) => reopen.mutate(id),
    isPending: mutations.some((mutation) => mutation.isPending),
    errorMessage: failed ? problemMessage(failed.error, "Couldn't update the ticket.") : null,
    reset: () => mutations.forEach((mutation) => mutation.reset()),
  };
}
