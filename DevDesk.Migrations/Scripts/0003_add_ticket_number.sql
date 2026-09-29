-- A short, human-friendly identifier for tickets (e.g. "#1042"), separate from the uuid primary key.
alter table tickets
    add column if not exists ticket_number bigint generated always as identity;
