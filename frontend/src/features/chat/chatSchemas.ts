import { z } from 'zod';

export const MAX_MESSAGE_LENGTH = 2000;

export const chatInputSchema = z.object({
  text: z.string().trim().min(1, 'Type a message first.').max(MAX_MESSAGE_LENGTH),
});
