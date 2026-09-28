import { z } from 'zod';

export const createTicketFormSchema = z.object({
  subject: z
    .string()
    .trim()
    .min(1, 'Enter a subject.')
    .max(200, 'Keep the subject under 200 characters.'),
  description: z
    .string()
    .trim()
    .min(1, 'Describe the problem.')
    .max(2000, 'Keep the description under 2000 characters.'),
  requesterId: z.guid('Enter a valid requester ID (a GUID).'),
});

export const resolveTicketFormSchema = z.object({
  resolutionNote: z
    .string()
    .trim()
    .min(1, 'Describe how it was resolved.')
    .max(2000, 'Keep the note under 2000 characters.'),
});
