# Kịch bản Kiểm thử Hệ thống (System Test Cases)

Tài liệu này cung cấp các kịch bản kiểm thử (Test Cases) chi tiết cho toàn bộ các tính năng phân quyền (RBAC), quản lý học liệu, nâng cấp Premium và đồng bộ thời gian thực SignalR trên hệ thống.

---

## 1. Đăng nhập & Xác thực (Authentication & Login)

### TC-AUTH-01: Đăng nhập thành công với các vai trò khác nhau

- **Mục tiêu**: Đảm bảo người dùng đăng nhập đúng tài khoản và được điều hướng về đúng trang tương ứng với vai trò.
- **Các bước thực hiện**:
    1. Truy cập trang đăng nhập `/Auth/Login`.
    2. Lần lượt nhập các tài khoản mẫu:
        - Admin: `admin@gmail.com` / Mật khẩu: `@Admin1`
        - HoD: `hod@gmail.com` / Mật khẩu: `@Hod1`
    3. Nhấp chọn nút **Đăng nhập**.
- **Kết quả mong đợi**:
    - Đăng nhập thành công mà không có lỗi.
    - Admin được điều hướng về `/Admin/Dashboard`.
    - HoD được điều hướng về `/Hod/Index`.
    - Lecturer được điều hướng về `/Document/Index`.
    - Student được điều hướng về trang chủ `/Index` hoặc trang chọn môn học.

---

## 2. Các Kịch bản cho Vai trò: Admin

### TC-ADMIN-01: Quản lý kích hoạt/khóa tài khoản người dùng

- **Mục tiêu**: Xác nhận Admin có thể vô hiệu hóa và kích hoạt lại tài khoản người dùng bất kỳ.
- **Các bước thực hiện**:
    1. Đăng nhập với vai trò Admin.
    2. Truy cập `/Admin/Index` (Quản lý User).
    3. Tìm tài khoản `test_student@gmail.com` trong danh sách.
    4. Nhấn công tắc khóa tài khoản (chuyển trạng thái sang `Inactive`).
    5. Mở một trình duyệt ẩn danh khác, thử đăng nhập bằng tài khoản `test_student@gmail.com`.
    6. Quay lại màn hình Admin, bật lại trạng thái tài khoản thành `Active`, thử đăng nhập lại ở trình duyệt ẩn danh.
- **Kết quả mong đợi**:
    - Khi bị khóa (`Inactive`), học sinh đăng nhập sẽ nhận thông báo lỗi tài khoản bị vô hiệu hóa.
    - Khi kích hoạt lại (`Active`), học sinh đăng nhập bình thường.

### TC-ADMIN-02: Quản lý danh mục môn học & phân khoa

- **Mục tiêu**: Admin có thể tạo mới và gán môn học vào đúng khoa.
- **Các bước thực hiện**:
    1. Đăng nhập với vai trò Admin.
    2. Truy cập `/Admin/Subjects` (Quản lý môn học).
    3. Nhấp **Thêm môn học mới**, điền thông tin: Mã môn `PRN211`, Tên môn `C# Desktop App`, chọn Khoa `CNTT`. Nhấn **Lưu**.
- **Kết quả mong đợi**: Môn học `PRN211` được lưu thành công vào cơ sở dữ liệu và hiển thị trong danh sách thuộc bộ môn CNTT.

---

## 3. Các Kịch bản cho Vai trò: Head of Department (HoD)

### TC-HOD-01: Phân công giảng viên phụ trách môn học

- **Mục tiêu**: HoD phân công giảng viên thuộc bộ môn của mình phụ trách môn học.
- **Các bước thực hiện**:
    1. Đăng nhập với vai trò HoD (`hod@gmail.com`).
    2. Truy cập trang `/Hod/Index`.
    3. Tại dòng môn học `PRN222`, chọn Giảng viên `Mẫu Giảng Viên` từ dropdown. Nhấn **Lưu**.
- **Kết quả mong đợi**: Hệ thống lưu thành công, hiển thị tên giảng viên đã gán và ghi nhận hành động phân công này vào hệ thống Audit Log.

### TC-HOD-02: Giám sát tài liệu trong bộ môn

- **Mục tiêu**: HoD có thể kiểm tra danh sách tài liệu và kết quả băm (chunks) nhưng không được xóa/sửa tên.
- **Các bước thực hiện**:
    1. Đăng nhập với vai trò HoD.
    2. Truy cập `/Document/Index`.
    3. Xác nhận **khung Upload tài liệu bị ẩn**.
    4. Xác nhận có nhãn chữ xanh `"Chế độ Giám sát & Quản lý Bộ môn"` hiển thị trên tiêu đề bảng tài liệu.
    5. Rê chuột vào tài liệu bất kỳ trong khoa:
        - Xác nhận nút **Bật/Tắt kích hoạt** (Toggle Active) **hiển thị**.
        - Xác nhận nút **Sửa tên hiển thị** (Rename) và **Xóa** (Delete) **bị ẩn hoàn toàn**.
    6. Nhấn vào nhãn trạng thái `✔️ Indexed` của tài liệu để mở Modal Chunks.
- **Kết quả mong đợi**: HoD xem được danh sách chunks bình thường, có thể bật/tắt kích hoạt tài liệu của khoa, nhưng không có quyền sửa đổi nội dung/xóa bỏ học liệu.

---

## 4. Các Kịch bản cho Vai trò: Lecturer

### TC-LECT-01: Tải lên học liệu môn học được phân công

- **Mục tiêu**: Giảng viên tải tài liệu mới lên các môn học mình dạy.
- **Các bước thực hiện**:
    1. Đăng nhập với vai trò Lecturer (`test_lecturer@gmail.com`).
    2. Truy cập `/Document/Index`.
    3. Xác nhận **khung Upload tài liệu hiển thị**.
    4. Trong dropdown môn học, chọn môn học `PRN222`.
    5. Chọn một file PDF học tập thực tế và nhấn **Upload tài liệu**.
- **Kết quả mong đợi**: File được tải lên thành công, trạng thái ban đầu hiển thị là `Processing` rồi chuyển sang `Indexed`. File vật lý được lưu trữ đúng thư mục quy định.

### TC-LECT-02: Sửa đổi và Xóa tài liệu cá nhân

- **Mục tiêu**: Giảng viên có quyền sửa tên hiển thị, bật/tắt kích hoạt và xóa tài liệu do chính mình tải lên.
- **Các bước thực hiện**:
    1. Đăng nhập với vai trò Lecturer, truy cập `/Document/Index`.
    2. Rê chuột vào tài liệu mình vừa upload:
        - Nhấn nút **Sửa tên hiển thị** (Bút chì), đổi tên thành `Slide Chuong 1 C#`, lưu lại.
        - Nhấn nút **Bật/Tắt kích hoạt** để kiểm tra trạng thái thay đổi.
        - Nhấn nút **Xóa** (Thùng rác) và xác nhận.
- **Kết quả mong đợi**:
    - Tên tài liệu cập nhật thành công trên giao diện.
    - Trạng thái hoạt động đổi bật/tắt tương ứng.
    - Sau khi xóa, tài liệu biến mất khỏi danh sách và file vật lý tương ứng được dọn dẹp khỏi ổ đĩa.

---

## 5. Các Kịch bản cho Vai trò: Student (Free & Premium)

### TC-STUD-01: Giới hạn lượt hỏi và chặn xem file đối với học sinh Free

- **Mục tiêu**: Học sinh Free chỉ được chat giới hạn và không xem được tài liệu gốc.
- **Các bước thực hiện**:
    1. Đăng nhập với tài khoản học sinh thường (`test_student@gmail.com`).
    2. Mở một phiên chat về môn `PRN222`. Thực hiện gửi 20 câu hỏi liên tục.
    3. Truy cập danh sách tài liệu môn học, nhấp vào tên tài liệu gốc để cố gắng đọc/tải xuống.
- **Kết quả mong đợi**:
    - Đến câu hỏi thứ 21, hệ thống hiển thị thông báo lỗi chặn chat và yêu cầu nâng cấp gói Premium.
    - Khi nhấp vào đọc tài liệu gốc, hệ thống chặn lại và hiển thị thông báo yêu cầu nâng cấp gói Premium.

### TC-STUD-02: Mở khóa giới hạn đối với học sinh Premium

- **Mục tiêu**: Học sinh Premium được chat không giới hạn và được xem tài liệu gốc.
- **Các bước thực hiện**:
    1. Đăng nhập với tài khoản học sinh Premium (`test_premium_student@gmail.com`).
    2. Tiến hành chat thử hơn 20 câu hỏi.
    3. Nhấp vào tên tài liệu gốc trong mục tài liệu tham khảo hoặc trong phần trích dẫn nguồn (citations) của AI.
- **Kết quả mong đợi**:
    - Chat bình thường không bị giới hạn lượt câu hỏi.
    - Mở và xem được trực tiếp nội dung tài liệu gốc một cách mượt mà.

---

## 6. Đồng bộ Thời gian thực (Real-time SignalR Sync)

### TC-SYNC-01: Đồng bộ khi HoD phân công Giảng viên

- **Mục tiêu**: Giao diện Giảng viên tự động cập nhật môn học khi HoD phân công mà không cần tải lại trang thủ công.
- **Các bước thực hiện**:
    1. Mở song song 2 trình duyệt:
        - Trình duyệt A: Đăng nhập HoD, truy cập `/Hod/Index`.
        - Trình duyệt B: Đăng nhập Giảng viên (`test_lecturer`), truy cập `/Document/Index`.
    2. Ở trình duyệt A, HoD tiến hành phân công giảng viên cho môn học `PRN222` rồi nhấn **Lưu**.
    3. Quan sát trình duyệt B của Giảng viên.
- **Kết quả mong đợi**: Trên trình duyệt B, danh sách môn học của Giảng viên tự động cập nhật thêm môn học `PRN222` và dropdown môn học trong thẻ Upload tự động xuất hiện môn `PRN222` mà không cần F5 tải lại trang.
