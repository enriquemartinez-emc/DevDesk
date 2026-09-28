import { useState } from 'react';
import { useMutation } from '@tanstack/react-query';
import { HttpAgent, type Message } from '@ag-ui/client';

// DevDesk.Server speaks AG-UI at /agui; the Vite dev server proxies it there.
const AGENT_URL = '/agui';

function createAgent(): HttpAgent {
  return new HttpAgent({ url: AGENT_URL, threadId: crypto.randomUUID() });
}

export function useAgentChat() {
  const [agent, setAgent] = useState(createAgent);
  const [messages, setMessages] = useState<Message[]>([]);

  const run = useMutation({
    mutationFn: async (text: string) => {
      agent.addMessage({ id: crypto.randomUUID(), role: 'user', content: text });
      setMessages([...agent.messages]);

      let finished = false;
      await agent.runAgent(
        {},
        {
          onMessagesChanged: ({ messages: updated }) => setMessages([...updated]),
          onRunFinishedEvent: () => {
            finished = true;
          },
        },
      );

      // A dropped connection ends the stream with neither RUN_FINISHED nor RUN_ERROR; don't treat that as an empty successful answer.
      if (!finished) {
        throw new Error('The connection to the assistant closed before it finished. Try again.');
      }
    },
  });

  const reset = () => {
    agent.abortRun();
    run.reset();
    setMessages([]);
    setAgent(createAgent());
  };

  const errorMessage = run.error
    ? run.error instanceof Error
      ? run.error.message
      : 'The agent run failed.'
    : null;

  return {
    messages,
    isRunning: run.isPending,
    errorMessage,
    send: (text: string) => run.mutate(text),
    reset,
  };
}
