import { type FormEvent, type KeyboardEvent, useState } from 'react';
import { SendHorizonal } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Textarea } from '@/components/ui/textarea';
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

  const handleSubmit = (event: FormEvent) => {
    event.preventDefault();
    submit();
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
    <form onSubmit={handleSubmit} className="space-y-1">
      <div className="flex items-end gap-2">
        <Textarea
          value={value}
          onChange={(event) => onChange(event.target.value)}
          onKeyDown={handleKeyDown}
          placeholder="Ask about a ticket or how to fix something…"
          rows={2}
          maxLength={MAX_MESSAGE_LENGTH}
          aria-label="Message"
          className="min-h-0 resize-none"
        />
        <Button type="submit" size="icon" disabled={disabled} aria-label="Send message">
          <SendHorizonal />
        </Button>
      </div>
      {validationError && <p className="text-xs text-destructive">{validationError}</p>}
    </form>
  );
}
