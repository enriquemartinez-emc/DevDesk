import { type FormEvent, useState } from 'react';
import { z } from 'zod';
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
import { Field, FieldDescription, FieldError, FieldGroup, FieldLabel } from '@/components/ui/field';
import { Input } from '@/components/ui/input';
import { Spinner } from '@/components/ui/spinner';
import { Textarea } from '@/components/ui/textarea';
import { DEMO_REQUESTER_ID } from './demoIdentity';
import { createTicketFormSchema } from './ticketSchemas';
import { useCreateTicket } from './useTicketActions';

type CreateTicketDialogProps = {
  open: boolean;
  onOpenChange: (open: boolean) => void;
};

// The form lives inside DialogContent, which unmounts when closed, so it starts fresh each time.
function CreateTicketForm({ onDone }: { onDone: () => void }) {
  const { create, isPending, errorMessage } = useCreateTicket();
  const [values, setValues] = useState({
    subject: '',
    description: '',
    requesterId: DEMO_REQUESTER_ID,
  });
  const [fieldErrors, setFieldErrors] = useState<Partial<Record<keyof typeof values, string[]>>>(
    {},
  );

  const submit = (event: FormEvent) => {
    event.preventDefault();
    const parsed = createTicketFormSchema.safeParse(values);
    if (!parsed.success) {
      setFieldErrors(z.flattenError(parsed.error).fieldErrors);
      return;
    }

    setFieldErrors({});
    create(parsed.data, onDone);
  };

  return (
    <form onSubmit={submit} className="grid gap-4">
      <DialogHeader>
        <DialogTitle>New ticket</DialogTitle>
        <DialogDescription>Describe the problem the customer reported.</DialogDescription>
      </DialogHeader>

      <FieldGroup>
        <Field data-invalid={Boolean(fieldErrors.subject)}>
          <FieldLabel htmlFor="ticket-subject">Subject</FieldLabel>
          <Input
            id="ticket-subject"
            value={values.subject}
            onChange={(event) => setValues({ ...values, subject: event.target.value })}
            aria-invalid={Boolean(fieldErrors.subject)}
            autoFocus
          />
          {fieldErrors.subject && <FieldError>{fieldErrors.subject[0]}</FieldError>}
        </Field>

        <Field data-invalid={Boolean(fieldErrors.description)}>
          <FieldLabel htmlFor="ticket-description">Description</FieldLabel>
          <Textarea
            id="ticket-description"
            value={values.description}
            onChange={(event) => setValues({ ...values, description: event.target.value })}
            aria-invalid={Boolean(fieldErrors.description)}
          />
          {fieldErrors.description && <FieldError>{fieldErrors.description[0]}</FieldError>}
        </Field>

        <Field data-invalid={Boolean(fieldErrors.requesterId)}>
          <FieldLabel htmlFor="ticket-requester">Requester ID</FieldLabel>
          <Input
            id="ticket-requester"
            value={values.requesterId}
            onChange={(event) => setValues({ ...values, requesterId: event.target.value })}
            aria-invalid={Boolean(fieldErrors.requesterId)}
          />
          <FieldDescription>
            Prefilled with a demo requester until the app has customers and sign-in.
          </FieldDescription>
          {fieldErrors.requesterId && <FieldError>{fieldErrors.requesterId[0]}</FieldError>}
        </Field>
      </FieldGroup>

      {errorMessage && (
        <Alert variant="destructive">
          <AlertDescription>{errorMessage}</AlertDescription>
        </Alert>
      )}

      <DialogFooter>
        <Button type="button" variant="outline" onClick={onDone}>
          Cancel
        </Button>
        <Button type="submit" disabled={isPending}>
          {isPending && <Spinner data-icon="inline-start" />}
          Create ticket
        </Button>
      </DialogFooter>
    </form>
  );
}

export function CreateTicketDialog({ open, onOpenChange }: CreateTicketDialogProps) {
  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent>
        <CreateTicketForm onDone={() => onOpenChange(false)} />
      </DialogContent>
    </Dialog>
  );
}
