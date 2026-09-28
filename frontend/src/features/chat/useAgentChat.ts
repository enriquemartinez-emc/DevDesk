import { useState } from 'react';
import { useMutation } from '@tanstack/react-query';
import { HttpAgent, type Message } from '@ag-ui/client';

// DevDesk.Agent speaks AG-UI; the Vite dev server proxies /agui to it.
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

      await agent.runAgent(
        {},
        { onMessagesChanged: ({ messages: updated }) => setMessages([...updated]) },
      );
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
