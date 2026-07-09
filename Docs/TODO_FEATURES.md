# ChatBotRagRazorPage — Tiến độ & TODO tính năng

Cập nhật: 2026-07-09. Thứ tự build: **C (xong) → A (đang làm) → B (chưa)**.

Tài liệu này theo dõi 3 mảng tính năng thêm vào đồ án PRN. Kiến trúc 3 lớp
(Business / DataAccess / PresentationRazorPage), Postgres+pgvector cổng 5433,
Gemini qua Semantic Kernel, EF Core code-first (app auto-migrate lúc startup).

---

## Tổng quan trạng thái

| Phần | Tính năng | Trạng thái |
|------|-----------|-----------|
| C | Gán giảng viên cho môn học (HoD) | ✅ Code xong — chưa chạy migrate |
| A | Admin cấu hình cách chia chunk | 🟡 Đang làm — ~70% |
| B | Trang Benchmark (token / chi phí / doanh thu / lợi nhuận) | ⬜ Chưa bắt đầu |
| — | Build + migrate toàn bộ | ⬜ |
| — | Soạn tài liệu mô tả tính năng (theo yêu cầu thầy) | ⬜ |

---

## PHẦN C — Gán giảng viên phụ trách môn (ĐÃ CODE XONG)

**Nghiệp vụ:** Trưởng bộ môn (HoD) gán 1 giảng viên phụ trách cho từng môn.
Quan hệ 1 GV → nhiều môn, 1 môn → đúng 1 GV. Mỗi lần đổi ghi vào AuditLog.
Lịch sử người upload tài liệu (`Document.UploaderId` + `UploadedAt`) độc lập
với GV nên đổi GV không mất lịch sử.

**Đã làm:**
- `Subject.LecturerId` (int? FK→AppUser, OnDelete=SetNull) + nav `Lecturer`.
- DbContext cấu hình quan hệ SetNull.
- `SubjectDto` thêm `LecturerId` + `LecturerName` (mặc định "Chưa gán");
  `MappingExtensions.ToDto` map tên GV.
- `ISubjectService.AssignLecturerAsync(subjectId, int? lecturerId)` (null = gỡ).
- Trang HoD (`Pages/Hod/Index`) thêm cột "Giảng viên phụ trách" + dropdown GV
  trong khoa + handler `OnPostAssignLecturer` (Forbid nếu môn/GV khác khoa) +
  ghi AuditLog (Action="AssignLecturer").
- Migration `20260709012209_AddSubjectLecturer` đã generate.

**Còn lại:**
- [ ] Chạy migrate (`dotnet ef database update`) — cần bật Docker Postgres 5433.
- [ ] Kiểm tra cột `LecturerId` thực sự có trong bảng `Subjects` trên DB.
- [ ] Xác nhận lại handler ghi AuditLog chạy đúng (đọc lại `Pages/Hod/Index.cshtml.cs`).

---

## PHẦN A — Admin cấu hình chunking (ĐANG LÀM ~70%)

**Nghiệp vụ (theo yêu cầu thầy):** Admin chỉnh cách chia tài liệu thành chunk
ngay lúc chạy, không build lại. Có **5 setting độc lập**, mỗi cái 1 toggle
bật/tắt + 1 ô số, cộng 1 ô overlap dùng chung:

1. **Số từ / chunk** — trần số từ trong một chunk.
2. **Số ký tự / chunk** — trần số ký tự trong một chunk.
3. **Số từ / đoạn** — trần cho ĐƠN VỊ ĐOẠN (tiền xử lý): đoạn nào dài hơn N từ
   bị cắt nhỏ TRƯỚC khi gom chunk. Không phải trần của chunk.
4. **Số đoạn / chunk** — trần số đoạn trong một chunk.
5. **Số token / chunk** — trần token (chính là `maxChunkSize` cũ, mặc định 400).

**Cơ chế 2 giai đoạn:**
- Giai đoạn 1 (tiền xử lý đoạn): nếu setting 3 bật, cắt nhỏ đoạn vượt N từ.
- Giai đoạn 2 (gom chunk): gom lần lượt các đoạn; trước khi thêm đoạn tiếp theo,
  nếu thêm vào sẽ vượt BẤT KỲ trần nào đang bật trong {từ, ký tự, đoạn, token}
  thì đóng chunk hiện tại, mở chunk mới (AND — chunk thỏa mọi trần đang bật).
- Tắt hết trừ token → chạy y hệt code cũ (đi qua đường Semantic Kernel).
- **Chỉ áp cho tài liệu upload MỚI.** Không re-chunk tài liệu cũ khi đổi config.

**Đã làm:**
- `AppSetting` entity (Id, Key, Value) + DbSet + unique index Key + repo
  (`IAppSettingRepository`/`AppSettingRepository`).
- `ChunkConfig` DTO (5 setting enable+value + overlap + cờ `IsTokenOnly`).
- `ISettingService`/`SettingService`: `GetChunkConfigAsync` (key thiếu →
  mặc định = hành vi cũ), `SaveChunkConfigAsync` (upsert từng key).
- Đăng ký DI repo + service.
- `ITextChunkingService.ChunkTextAsync(text, ChunkConfig)` — đổi chữ ký.
- `TextChunkingService` viết lại: fast-path token-only giữ nguyên SK cũ;
  đường mới hand-roll logic AND 2 giai đoạn (pre-split đoạn + gom chunk + overlap).

**Còn lại:**
- [ ] **`DocumentProcessingService`**: inject `ISettingService`, gọi
  `GetChunkConfigAsync()` MỘT lần trước `Parallel.ForEachAsync`, truyền config
  vào `ChunkTextAsync(page.Text, config)`. (Hiện còn gọi `ChunkTextAsync(page.Text)`
  kiểu cũ → build SẼ FAIL cho tới khi sửa chỗ này.)
- [ ] Trang `Pages/Admin/ChunkingConfig.cshtml(.cs)`: 5 nhóm toggle+số + overlap,
  OnGet đọc `GetChunkConfigAsync`, OnPost lưu + ghi AuditLog. `[Authorize(Roles="Admin")]`.
- [ ] Thêm nav link trong `Pages/Shared/_Layout.cshtml` (cạnh "QL Môn học").
- [ ] Migration `AddAppSetting`.
- [ ] Test nhanh logic chunk (1 self-check: cắt theo từng trần bật riêng lẻ + AND).

---

## PHẦN B — Trang Benchmark / Metrics (CHƯA BẮT ĐẦU)

**Nghiệp vụ:** Trang riêng `/Admin/Benchmarks` thống kê:
- Token in/out (đọc từ GeminiMetadata — SK đã phơi sẵn, chưa lưu).
- Chi phí = token × đơn giá Gemini, hiện cả **USD và VND**.
- Doanh thu: lấy từ bảng **Transaction thật** (CHƯA CÓ — phải tạo mới; luồng
  VnPay hiện chỉ set `Subscription="Premium"`, giá hardcode 100k, không lưu gì).
- Lợi nhuận = doanh thu − chi phí AI.
- Tỷ giá USD→VND: Admin nhập, lưu `AppSetting` (đã có sẵn bảng từ phần A).
- **Snapshot tỷ giá** vào từng bản ghi (Transaction + ChatMessage) để thống kê
  lịch sử không đổi khi tỷ giá hiện tại thay đổi.

**Cần làm:**
- [ ] Entity `Transaction` (Id, UserId, Amount, Type, CreatedAt, UsdVndRate) + DbSet + repo.
- [ ] Ghi Transaction ở handler VnPay return (`Pages/Wallet/Index.cshtml.cs`).
- [ ] Thêm cột `ChatMessage.TokenIn` / `TokenOut` / `UsdRate` (snapshot).
- [ ] Sửa `AiService` (đường streaming `GetChatStreamingResponseAsync` hiện chỉ
  trả `string`, bỏ metadata) để lộ token usage ra ngoài (out-param/callback/return object).
- [ ] `ChatHub` lưu token vào ChatMessage assistant khi lưu tin nhắn.
- [ ] Đơn giá Gemini + tỷ giá đọc từ AppSetting.
- [ ] Trang `Pages/Admin/Benchmarks.cshtml(.cs)`: token, chi phí (USD+VND),
  doanh thu, lợi nhuận + ô nhập tỷ giá.
- [ ] Migration `AddTransactionAndTokenColumns`.

---

## Việc chung cuối cùng

- [ ] Build toàn solution PASS (`dotnet build RagChatbot.slnx -c Debug`).
- [ ] Tạo + chạy tất cả migration còn thiếu trên DB (Docker Postgres 5433).
      Lệnh EF: `... --project RagChatbot.DataAccess --startup-project RagChatbot.PresentationRazorPage`.
- [ ] Verify cột/bảng mới thực sự landed:
      `docker exec rag_postgres_db psql -U postgres -d RagChatbotDb -c '\d "AppSettings"'`.
- [ ] Soạn tài liệu mô tả tính năng A/B/C (yêu cầu của thầy) sau khi code xong.

---

## Ghi chú / cạm bẫy

- App auto-migrate lúc startup qua `Database.Migrate()` → thêm entity/cột PHẢI
  có migration, không thể chỉ thêm property C#.
- `dotnet ef migrations add` LUÔN cảnh báo "may result in loss of data" — báo
  động giả do seed data có timestamp động (`DateTime.UtcNow`). Mở file migration
  kiểm tra `Up()` chỉ có AddColumn/CreateIndex + vài dòng `UpdateData` timestamp,
  không có `DropColumn`/`DropTable` dữ liệu thật → an toàn.
- Vai trò giảng viên = chuỗi **"Lecturer"**.
- Config đọc từ `.env` ở gốc repo (DotNetEnv), không có appsettings.json.
  Connection string dùng **Port=5433** (docker map 5433→5432).
