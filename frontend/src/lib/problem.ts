import { z } from 'zod';

const problemSchema = z.object({
  title: z.string().nullish(),
  detail: z.string().nullish(),
  errors: z.record(z.string(), z.array(z.string())).nullish(),
});

// The generated client throws the whole response on a non-2xx status, with the parsed
// ProblemDetails body on `.error` - not an Error.
const thrownResponseSchema = z.object({ error: problemSchema });

export function problemMessage(error: unknown, fallback: string): string {
  const parsed = thrownResponseSchema.safeParse(error);
  if (parsed.success) {
    const { detail, title, errors } = parsed.data.error;
    const firstValidationError = errors ? Object.values(errors).flat()[0] : undefined;
    return detail ?? firstValidationError ?? title ?? fallback;
  }

  return error instanceof Error ? error.message : fallback;
}
