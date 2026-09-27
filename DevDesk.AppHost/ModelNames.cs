namespace DevDesk.AppHost;

public static class ModelNames
{
    // https://ollama.com/library/llama3.2 - local chat model for the support agent.
    public const string Chat = "llama3.2";

    // https://ollama.com/library/nomic-embed-text - local embedding model for pgvector doc_chunks.
    public const string Embedding = "nomic-embed-text";
}
