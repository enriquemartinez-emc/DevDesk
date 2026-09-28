import { type FormEvent, useState } from 'react';
import type { TicketResponse } from '@/api/devdesk-api';
import { Alert, AlertDescription } from '@/components/ui/alert';
import { Button } from '@/components/ui/button';
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog';
import { Field, FieldError, FieldLabel } from '@/components/ui/field';
import { Spinner } from '@/components/ui/spinner';
import { Textarea } from '@/components/ui/textarea';
import { resolveTicketFormSchema } from './ticketSchemas';

type ResolveTicketDialogProps = {
  ticket: TicketResponse | null;
  onResolve: (ticketId: string, resolutionNote: string) => void;
  onClose: () => void;
  isPending: boolean;
  errorMessage: string | null;
};

function ResolveTicketForm({
  ticket,
  onResolve,
  onClose,
  isPending,
  errorMessage,
}: Omit<ResolveTicketDialogProps, 'ticket'> & { ticket: TicketResponse }) {
  const [resolutionNote, setResolutionNote] = useState('');
  const [validationError, setValidationError] = useState<string | null>(null);

  const submit = (event: FormEvent) => {
    event.preventDefault();
    const parsed = resolveTicketFormSchema.safeParse({ resolutionNote });
    if (!parsed.success) {
      setValidationError(parsed.error.issues[0]?.message ?? 'Invalid note.');
      return;
    }

    setValidationError(null);
    onResolve(ticket.id, parsed.data.resolutionNote);
  };

  return (
    <form onSubmit={submit} className="grid gap-4">
      <DialogHeader>
        <DialogTitle>Resolve ticket</DialogTitle>
        <DialogDescription>{ticket.subject}</DialogDescription>
      </DialogHeader>

      <Field data-invalid={Boolean(validationError)}>
        <FieldLabel htmlFor="resolution-note">Resolution</FieldLabel>
        <Textarea
          id="resolution-note"
          value={resolutionNote}
          onChange={(event) => setResolutionNote(event.target.value)}
          aria-invalid={Boolean(validationError)}
          autoFocus
        />
        {validationError && <FieldError>{validationError}</FieldError>}
      </Field>

      {errorMessage && (
        <Alert variant="destructive">
          <AlertDescription>{errorMessage}</AlertDescription>
        </Alert>
      )}

      <DialogFooter>
        <Button type="button" variant="outline" onClick={onClose}>
          Cancel
        </Button>
        <Button type="submit" disabled={isPending}>
          {isPending && <Spinner data-icon="inline-start" />}
          Resolve
        </Button>
      </DialogFooter>
    </form>
  );
}

export function ResolveTicketDialog({ ticket, ...rest }: ResolveTicketDialogProps) {
  return (
    <Dialog open={ticket !== null} onOpenChange={(open) => !open && rest.onClose()}>
      <DialogContent>
        {ticket && <ResolveTicketForm key={ticket.id} ticket={ticket} {...rest} />}
      </DialogContent>
    </Dialog>
  );
}
