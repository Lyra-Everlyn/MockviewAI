-- Bản sao migration đã chạy trên Supabase (project mockviewai), lấy từ supabase_migrations.schema_migrations.
-- Step 2 (Buổi 4): 1 dòng = trạng thái của 1 mục checklist
create table public.checklist_progress (
  item_key   text primary key check (char_length(item_key) between 1 and 50),
  is_done    boolean not null default false,
  updated_at timestamptz not null default now()
);

comment on table public.checklist_progress is 'MockViewAI: 1 dòng = trạng thái 1 mục checklist chuẩn bị phỏng vấn (chưa tách theo người dùng — Buổi 5).';

-- Tự cập nhật updated_at mỗi khi sửa dòng
create or replace function public.set_updated_at()
returns trigger language plpgsql set search_path = '' as $$
begin
  new.updated_at := now();
  return new;
end;
$$;

create trigger checklist_progress_set_updated_at
before update on public.checklist_progress
for each row execute function public.set_updated_at();

-- RLS luôn bật
alter table public.checklist_progress enable row level security;

-- Quyền tối thiểu cho trình duyệt: đọc, thêm, sửa — không xóa
revoke all on public.checklist_progress from anon, authenticated;
grant select, insert, update on public.checklist_progress to anon;

create policy "anon_read_checklist" on public.checklist_progress
  for select to anon using (true);
create policy "anon_insert_checklist" on public.checklist_progress
  for insert to anon with check (true);
create policy "anon_update_checklist" on public.checklist_progress
  for update to anon using (true) with check (true);
