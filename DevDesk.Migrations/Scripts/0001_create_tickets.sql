-- Baseline: "if not exists" lets databases created before migrations existed adopt this script.
create table if not exists tickets (
    id uuid primary key,
    subject text not null,
    description text not null,
    requester_id uuid not null,
    created_at timestamptz not null,
    status text not null,
    assigned_agent uuid null,
    assigned_at timestamptz null,
    resolution_note text null,
    resolved_at timestamptz null,
    closed_at timestamptz null
);
