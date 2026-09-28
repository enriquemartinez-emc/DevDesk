import { Message, MessageContent, MessageResponse } from '@/components/ai-elements/message';
import { Tool, ToolContent, ToolHeader, ToolInput, ToolOutput } from '@/components/ai-elements/tool';
import type { ChatItem, ToolCallView } from './chatView';
import { toolLabel } from './toolLabels';

function ToolCall({ call }: { call: ToolCallView }) {
  const finished = call.result !== null;

  return (
    <Tool>
      <ToolHeader
        type="dynamic-tool"
        toolName={call.name}
        title={toolLabel(call.name, finished)}
        state={finished ? 'output-available' : 'input-available'}
      />
      <ToolContent>
        <ToolInput input={call.args} />
        {finished && <ToolOutput output={call.result} errorText={undefined} />}
      </ToolContent>
    </Tool>
  );
}

export function MessageItem({ item }: { item: ChatItem }) {
  return (
    <Message from={item.kind}>
      {item.kind === 'assistant' &&
        item.toolCalls.map((call) => <ToolCall key={call.id} call={call} />)}
      {item.text.length > 0 && (
        <MessageContent>
          {item.kind === 'assistant' ? <MessageResponse>{item.text}</MessageResponse> : item.text}
        </MessageContent>
      )}
    </Message>
  );
}
