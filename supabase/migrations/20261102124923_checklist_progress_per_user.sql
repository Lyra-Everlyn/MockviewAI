-- Bản sao migration đã chạy trên Supabase (project mockviewai), lấy từ supabase_migrations.schema_migrations.
-- Buổi 5 · Nhịp 2: mỗi dòng tiến độ thuộc về một người dùng.

-- 1) Xóa 4 dòng test của Buổi 4 (dữ liệu thử, học viên đã đồng ý)
delete from public.checklist_progress;

-- 2) Thêm chủ sở hữu và đổi khóa chính: mỗi người có bộ mục riêng
alter table public.checklist_progress drop constraint checklist_progress_pkey;
alter table public.checklist_progress
  add column user_id uuid not null default auth.uid()
  references auth.users(id) on delete cascade;
alter table public.checklist_progress add primary key (user_id, item_key);

comment on table public.checklist_progress is 'MockViewAI: 1 dòng = trạng thái 1 mục checklist của 1 người dùng (RLS: auth.uid() = user_id).';

-- 3) Bỏ luật Buổi 4 (khách chưa đăng nhập được đọc/ghi chung)
drop policy if exists "anon_read_checklist"   on public.checklist_progress;
drop policy if exists "anon_insert_checklist" on public.checklist_progress;
drop policy if exists "anon_update_checklist" on public.checklist_progress;
revoke all on public.checklist_progress from anon, authenticated;

-- 4) Luật mới: chỉ người đã đăng nhập, chỉ dòng của chính mình. Không xóa.
alter table public.checklist_progress enable row level security;
grant select, insert, update on public.checklist_progress to authenticated;

create policy "own_rows_select" on public.checklist_progress
  for select to authenticated using ((select auth.uid()) = user_id);
create policy "own_rows_insert" on public.checklist_progress
  for insert to authenticated with check ((select auth.uid()) = user_id);
create policy "own_rows_update" on public.checklist_progress
  for update to authenticated
  using ((select auth.uid()) = user_id)
  with check ((select auth.uid()) = user_id);
