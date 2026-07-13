# Ma trận Phân quyền & Chức năng của các Actor trong Hệ thống

Tài liệu này tổng hợp toàn bộ các tính năng và giới hạn phân quyền đối với từng đối tượng người dùng (Actor) trong hệ thống RAG Chatbot.

---

## 1. Vai trò: Admin (Quản trị viên)
* **Quản lý Danh mục Môn học & Khoa**:
  * **Tạo mới, chỉnh sửa thông tin Môn học (Subject)** và chỉ định môn học thuộc Khoa/Bộ môn nào.
  * **Tạo mới, chỉnh sửa thông tin Khoa/Bộ môn (Department)** trong hệ thống.
* **Quản lý người dùng**:
  * Xem danh sách toàn bộ người dùng trong hệ thống.
  * Bật/Tắt trạng thái hoạt động (`IsActive`) của tài khoản người dùng để chặn đăng nhập hoặc mở khóa.
* **Quản lý cấu hình băm (Chunking Config)**:
  * Quản lý các tham số băm tài liệu (`ChunkSize`, `OverlapSize`, `ModelName`) áp dụng cho nền tảng.
* **Giám sát & Quản trị**:
  * Xem toàn bộ môn học và danh sách tất cả tài liệu của mọi môn học.
  * Xem danh sách các tài liệu bị báo cáo lỗi (Reported Documents).
  * Xem nhật ký kiểm toán (Audit Logs) ghi nhận các hành động phân công của Trưởng bộ môn (HoD).
  * Xem chi tiết kết quả băm tài liệu (Chunks) của mọi tài liệu trên hệ thống.
* **Giới hạn**: Không trực tiếp tham gia vào luồng tải lên tài liệu học tập của môn học.

---

## 2. Vai trò: Head of Department (HoD - Trưởng bộ môn)
* **Quản lý phân công chuyên môn**:
  * Quản lý danh sách môn học thuộc khoa/bộ môn của mình.
  * Phân công trực tiếp một Giảng viên (Lecturer) cùng bộ môn phụ trách một môn học cụ thể.
* **Giám sát học liệu khoa**:
  * Xem danh sách tất cả tài liệu được tải lên thuộc các môn học trong bộ môn của mình.
  * Xem chi tiết kết quả băm tài liệu (Chunks) của bất kỳ tài liệu nào thuộc bộ môn để giám sát chất lượng.
  * Bật/Tắt trạng thái hoạt động (Active/Inactive) của tất cả tài liệu trong bộ môn (ví dụ: tắt tài liệu lỗi thời hoặc nội dung không phù hợp).
* **Giới hạn**:
  * Không thể tải lên (Upload) tài liệu mới.
  * Không thể đổi tên hiển thị (Rename) hoặc Xóa (Delete) tài liệu của giảng viên khác tải lên.
  * Không thể phân công giảng viên thuộc khoa khác.

---

## 3. Vai trò: Lecturer (Giảng viên)
* **Quản lý học liệu cá nhân**:
  * Xem danh sách các môn học mình được Trưởng bộ môn phân công phụ trách.
  * Tải lên (Upload) tài liệu học tập (PDF, DOCX, TXT, XLSX, PPTX) cho các môn học được phân công.
  * Xem chi tiết kết quả băm tài liệu (Chunks) của các tài liệu do chính mình tải lên.
* **Thao tác tài liệu cá nhân**:
  * Đổi tên hiển thị (Rename) tài liệu cá nhân.
  * Bật/Tắt trạng thái hoạt động (Active/Inactive) của tài liệu cá nhân.
  * Xóa (Delete) tài liệu cá nhân khỏi hệ thống.
* **Giới hạn**:
  * Không được xem, chỉnh sửa hoặc thao tác trên tài liệu của giảng viên khác.
  * Không có quyền quản lý hay phân công giảng viên cho các môn học.

---

## 4. Vai trò: Student (Học sinh)
* **Tra cứu & Học tập**:
  * Xem danh sách môn học đang hoạt động trong hệ thống.
  * Trò chuyện, đặt câu hỏi cho AI Chatbot dựa trên dữ liệu tài liệu của môn học (RAG).
  * Xem trích dẫn nguồn (Citations) được AI phản hồi để đối chiếu thông tin.
* **Phân cấp gói dịch vụ (Subscription)**:
  * **Học sinh Free**:
    * Bị giới hạn số lượng lượt câu hỏi trong ngày.
    * **Không thể xem trực tiếp hoặc tải tài liệu gốc** để đọc (Chỉ được xem tên tài liệu và vị trí trích dẫn).
  * **Học sinh Premium**:
    * Không bị giới hạn số lượt câu hỏi.
    * Có quyền **xem/đọc trực tiếp nội dung tài liệu gốc** (PDF/DOCX) để tự học.
* **Giới hạn**:
  * Cấm tuyệt đối quyền xem kết quả băm nhỏ (Chunks) để tránh rò rỉ dữ liệu tài liệu gốc.
  * Không được phép can thiệp vào bất kỳ hành động quản trị, phân công, upload hay sửa đổi tài liệu nào.
