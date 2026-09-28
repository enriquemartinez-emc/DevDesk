import { z } from 'zod';

export const MAX_MESSAGE_LENGTH = 2000;

export const chatInputSchema = z.object({
  text: z.string().trim().min(1, 'Type a message first.').max(MAX_MESSAGE_LENGTH),
});

// MCP text results reach the browser as a JSON string like {"text":"..."}.
const toolResultSchema = z.object({ text: z.string() });

export function parseToolResult(content: string): string {
  try {
    const parsed = toolResultSchema.safeParse(JSON.parse(content));
    return parsed.success ? parsed.data.text : content;
  } catch {
    return content;
  }
}

// Tool-call arguments arrive as a JSON string; fall back to the raw text if it isn't valid JSON.
export function parseToolArguments(json: string): unknown {
  try {
    return JSON.parse(json) as unknown;
  } catch {
    return json;
  }
}
