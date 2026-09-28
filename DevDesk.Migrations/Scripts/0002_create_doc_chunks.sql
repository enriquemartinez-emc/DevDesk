-- Baseline: "if not exists" lets databases created before migrations existed adopt this script.
-- 768 dimensions: what nomic-embed-text produces.
create extension if not exists vector;

create table if not exists doc_chunks (
    id uuid primary key,
    source_path text not null,
    chunk_index int not null,
    content text not null,
    embedding vector(768) not null
);
