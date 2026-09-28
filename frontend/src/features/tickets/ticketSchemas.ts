import { z } from 'zod';

const MAX_SUBJECT_LENGTH = 200;
const MAX_DESCRIPTION_LENGTH = 2000;
const MAX_RESOLUTION_NOTE_LENGTH = 2000;

export const createTicketFormSchema = z.object({
  subject: z
    .string()
    .trim()
    .min(1, 'Enter a subject.')
    .max(MAX_SUBJECT_LENGTH, `Keep the subject under ${MAX_SUBJECT_LENGTH} characters.`),
  description: z
    .string()
    .trim()
    .min(1, 'Describe the problem.')
    .max(
      MAX_DESCRIPTION_LENGTH,
      `Keep the description under ${MAX_DESCRIPTION_LENGTH} characters.`,
    ),
  requesterId: z.guid('Enter a valid requester ID (a GUID).'),
});

export const resolveTicketFormSchema = z.object({
  resolutionNote: z
    .string()
    .trim()
    .min(1, 'Describe how it was resolved.')
    .max(
      MAX_RESOLUTION_NOTE_LENGTH,
      `Keep the note under ${MAX_RESOLUTION_NOTE_LENGTH} characters.`,
    ),
});
