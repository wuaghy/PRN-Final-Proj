# Hướng dẫn cấu hình Chunking

## 1. Chunking dùng để làm gì?

Chunking là bước chia nội dung tài liệu dài thành các đoạn nhỏ hơn trước khi tạo embedding. Mỗi đoạn nhỏ được lưu thành một `DocumentChunk` và trở thành đơn vị để hệ thống tìm kiếm ngữ nghĩa.

Luồng tổng quát:

```text
PDF/DOCX
  → trích xuất văn bản
  → làm sạch và chia chunk
  → tạo embedding cho từng chunk
  → lưu DocumentChunk và vector
  → vector search chọn các chunk liên quan
  → đưa các chunk làm ngữ cảnh cho AI trả lời
```

Chunk quá dài thường chứa nhiều nội dung không liên quan; chunk quá ngắn dễ mất ngữ cảnh và tạo quá nhiều embedding. Cấu hình mặc định `400 token` và `50 từ overlap` là điểm khởi đầu phù hợp cho tài liệu học tập của hệ thống.

## 2. Cách sử dụng trang cấu hình

Trang **Cấu hình Chunking** chỉ dành cho Admin.

- Checkbox quyết định giới hạn có được áp dụng hay không.
- Khi sửa một ô số, giao diện tự bật checkbox của giới hạn đó.
- Giá trị của trường chưa bật vẫn được lưu nhưng không tham gia chia chunk.
- Bấm **Lưu Cấu hình** để ghi các giá trị vào bảng `AppSettings`.
- Cấu hình được áp dụng cho tài liệu bắt đầu xử lý sau khi lưu. Tài liệu đã `Indexed` không tự động được chia lại.
- Mỗi lần lưu thành công tạo Audit Log `UpdateChunkConfig`, gồm trạng thái bật/tắt và giá trị của từng trường.

Form chỉ hợp lệ khi có ít nhất một giới hạn cấp chunk được bật. Các giới hạn đang bật phải lớn hơn `0`; overlap được phép bằng `0` nhưng không được âm.

## 3. Ý nghĩa từng trường

### Số từ / Chunk

Giới hạn tổng số từ trong một chunk. Ví dụ giá trị `300` nghĩa là hệ thống đóng chunk trước khi việc thêm nội dung tiếp theo làm chunk vượt 300 từ.

### Số ký tự / Chunk

Giới hạn tổng số ký tự, bao gồm chữ, số, dấu câu, khoảng trắng và ký tự xuống dòng. Trường này hữu ích khi cần kiểm soát kích thước chuỗi đầu vào tương đối chặt.

### Số từ / Đoạn

Đây là bước tiền xử lý, không phải giới hạn trực tiếp của chunk. Một đoạn dài hơn giá trị này được cắt thành các đoạn con trước khi ghép chunk.

Ví dụ với giới hạn `150`:

```text
Đoạn 420 từ
  → đoạn con 150 từ
  → đoạn con 150 từ
  → đoạn con 120 từ
```

### Số đoạn / Chunk

Giới hạn số đoạn gốc hoặc đoạn con được ghép vào một chunk. Phần overlap không được tính là một đoạn.

### Token / Chunk

Giới hạn kích thước chunk theo token ước lượng. Custom chunker hiện dùng quy tắc:

```text
token ước lượng = làm tròn lên (số ký tự / 4)
```

Đây không phải tokenizer chính xác của Gemini. Nó là giới hạn ổn định để kiểm soát kích thước trước khi gửi nội dung đi embedding.

### Overlap (từ)

Số từ cuối chunk trước được lặp lại ở đầu chunk sau. Overlap giúp giữ ngữ cảnh tại điểm cắt.

```text
Chunk 1: A B C D E
Chunk 2: D E F G H
         └─┘ overlap
```

Overlap luôn tính theo từ. Nếu toàn bộ overlap làm chunk mới vượt một giới hạn đang bật, hệ thống tự giảm số từ overlap. Nội dung gốc không bị xóa; trong trường hợp chunk mới đã sát trần, overlap có thể giảm về `0`.

## 4. Khi bật nhiều giới hạn

Các giới hạn dùng quan hệ **AND**: chunk kết quả phải thỏa tất cả giới hạn đang bật. Giới hạn đạt trước quyết định điểm cắt.

Ví dụ:

```text
Số từ / Chunk:    300
Số ký tự / Chunk: 2000
Token / Chunk:    400
```

Nếu chunk mới đạt 400 token khi mới có 250 từ, hệ thống vẫn đóng chunk vì trần token đã đạt. Nếu 2.000 ký tự đạt trước, trần ký tự quyết định điểm cắt.

`Số từ / Đoạn` chỉ cắt đoạn dài ở bước tiền xử lý và không thay thế yêu cầu phải bật ít nhất một giới hạn cấp chunk.

## 5. Luồng hoạt động trong hệ thống

1. `ChunkingConfigModel` đọc cấu hình thông qua `ISettingService` và hiển thị form cho Admin.
2. Khi lưu, PageModel kiểm tra dữ liệu rồi gọi `ISettingService.SaveChunkConfigAsync`.
3. `SettingService` upsert các key `Chunk.*` qua `IAppSettingRepository`; không truy cập `DbContext` trực tiếp.
4. `DocumentProcessingService` lấy một snapshot cấu hình trước khi chia nội dung tài liệu đang xử lý.
5. Tài liệu được trích xuất thành các trang. Hệ thống xử lý lại ranh giới câu giữa hai trang trước khi chunk từng trang.
6. `TextChunkingService` làm sạch văn bản và mask dấu chấm nằm giữa chữ số, ví dụ `43.000` thành dạng bảo vệ tạm thời.
7. Văn bản được tách theo xuống dòng. Nếu bật `Số từ / Đoạn`, các đoạn quá dài được cắt trước.
8. Đơn vị vẫn vượt trần từ, ký tự hoặc token được chia tiếp theo từ.
9. Các đoạn được ghép cho đến khi thêm đoạn tiếp theo sẽ vi phạm một giới hạn đang bật.
10. Hệ thống lấy tối đa số từ overlap từ cuối chunk vừa đóng, sau đó tự giảm overlap nếu cần để chunk kế tiếp vẫn hợp lệ.
11. Dấu chấm số được khôi phục, chunk rỗng bị loại và các header Markdown bị treo ở cuối được dọn dẹp.
12. `DocumentProcessingService` tạo embedding, lưu `DocumentChunk`, cập nhật tài liệu thành `Indexed` và phát thông báo realtime.

## 6. Ví dụ cấu hình

### Cấu hình mặc định

```text
Token / Chunk: bật, 400
Overlap: 50 từ
Các giới hạn khác: tắt
```

Phù hợp cho phần lớn PDF/DOCX học tập. Chunk có đủ ngữ cảnh mà không quá lớn.

### Tài liệu có đoạn trích xuất rất dài

```text
Số từ / Đoạn: bật, 150
Token / Chunk: bật, 400
Overlap: 50 từ
```

Giúp chia nhỏ các khối DOCX/PDF ít xuống dòng trước khi ghép chunk.

### Cần giới hạn kích thước chặt

```text
Số từ / Chunk: bật, 300
Số ký tự / Chunk: bật, 2000
Token / Chunk: bật, 400
Overlap: 30–50 từ
```

Chunk phải thỏa cả ba trần. Số lượng chunk có thể tăng đáng kể.

## 7. Xử lý sự cố

### Chunk quá ngắn hoặc số lượng chunk quá nhiều

- Kiểm tra có bật đồng thời quá nhiều giới hạn thấp hay không.
- Tăng giới hạn đạt trước, thường là token hoặc ký tự.
- Tắt giới hạn không cần thiết thay vì chỉ thay số.

### Chunk quá dài hoặc kết quả tìm kiếm thiếu chính xác

- Giảm `Token / Chunk` hoặc bật thêm `Số từ / Chunk`.
- Với văn bản ít xuống dòng, bật `Số từ / Đoạn`.

### Kết quả tìm kiếm bị trùng lặp nhiều

- Giảm overlap.
- Kiểm tra tài liệu có nội dung lặp sẵn hay không.

### Câu trả lời mất ngữ cảnh ở điểm cắt

- Tăng overlap vừa phải.
- Không đặt overlap quá lớn vì hệ thống vẫn phải giảm nó để tuân thủ các trần.

### Đã lưu cấu hình nhưng tài liệu cũ không thay đổi

Đây là hành vi mong đợi. Cấu hình không re-chunk tài liệu đã indexed. Muốn áp dụng cấu hình mới, tài liệu phải được đưa qua quy trình xử lý/index lại một cách chủ động.

## 8. Lưu ý kỹ thuật

- Chunk được tạo trong phạm vi từng trang sau bước xử lý ranh giới câu; overlap không nối xuyên qua hai lần gọi chunking riêng biệt.
- Một từ hoặc URL đơn lẻ dài hơn mọi giới hạn không thể chia theo khoảng trắng; hệ thống giữ nguyên theo cơ chế best-effort thay vì làm mất dữ liệu.
- Cấu hình runtime không yêu cầu build lại ứng dụng.
- Không chỉnh trực tiếp dữ liệu chunk trong Presentation; luồng luôn đi qua Business service và repository.
