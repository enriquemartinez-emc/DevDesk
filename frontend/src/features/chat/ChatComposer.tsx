import { type KeyboardEvent, useState } from 'react';
import { SendHorizonal } from 'lucide-react';
import { Alert, AlertDescription } from '@/components/ui/alert';
import {
  InputGroup,
  InputGroupAddon,
  InputGroupButton,
  InputGroupTextarea,
} from '@/components/ui/input-group';
import { MAX_MESSAGE_LENGTH, chatInputSchema } from './chatSchemas';

type ChatComposerProps = {
  value: string;
  onChange: (value: string) => void;
  onSend: (text: string) => void;
  disabled: boolean;
};

export function ChatComposer({ value, onChange, onSend, disabled }: ChatComposerProps) {
  const [validationError, setValidationError] = useState<string | null>(null);

  const submit = () => {
    const parsed = chatInputSchema.safeParse({ text: value });
    if (!parsed.success) {
      setValidationError(parsed.error.issues[0]?.message ?? 'Invalid message.');
      return;
    }

    setValidationError(null);
    onSend(parsed.data.text);
    onChange('');
  };

  const handleKeyDown = (event: KeyboardEvent<HTMLTextAreaElement>) => {
    if (event.key === 'Enter' && !event.shiftKey) {
      event.preventDefault();
      if (!disabled) {
        submit();
      }
    }
  };

  return (
    <div className="space-y-2">
      <InputGroup>
        <InputGroupTextarea
          value={value}
          onChange={(event) => onChange(event.target.value)}
          onKeyDown={handleKeyDown}
          placeholder="Ask about a ticket or how to fix something…"
          maxLength={MAX_MESSAGE_LENGTH}
          aria-label="Message"
        />
        <InputGroupAddon align="block-end">
          <InputGroupButton
            variant="default"
            size="icon-sm"
            className="ml-auto"
            onClick={submit}
            disabled={disabled}
            aria-label="Send message"
          >
            <SendHorizonal />
          </InputGroupButton>
        </InputGroupAddon>
      </InputGroup>
      {validationError && (
        <Alert variant="destructive">
          <AlertDescription>{validationError}</AlertDescription>
        </Alert>
      )}
    </div>
  );
}
