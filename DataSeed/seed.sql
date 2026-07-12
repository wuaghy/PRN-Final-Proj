-- 1. Dọn dẹp dữ liệu kiểm thử cũ nếu có để chạy lặp lại không bị trùng lắp hoặc lỗi khóa chính
DELETE FROM "Transactions" WHERE "Type" = 'Premium' AND "Amount" = 100000.0;
DELETE FROM "ChatMessages" WHERE "SessionId" = 'a0000000-0000-0000-0000-000000000001';
DELETE FROM "ChatSessions" WHERE "Id" = 'a0000000-0000-0000-0000-000000000001';
DELETE FROM "ContactMessages" WHERE "Content" LIKE 'Test Feedback%';
DELETE FROM "AppUsers" WHERE "Email" = 'test_student@gmail.com';
DELETE FROM "AppUsers" WHERE "Email" = 'test_premium_student@gmail.com';

-- 2. Thêm môn học mẫu nếu chưa có
INSERT INTO "Subjects" ("Code", "Name", "IsActive", "DepartmentId", "CreatedAt")
VALUES ('PRN222', 'Core Web Application Development', true, 1, NOW())
ON CONFLICT ("Code") DO NOTHING;

-- 3. Thêm người dùng mẫu (Student Free và Student Premium)
INSERT INTO "AppUsers" ("Email", "PasswordHash", "FirstName", "LastName", "Role", "IsActive", "Subscription", "DailyQueryCount", "LastQueryDate", "TodayChatCount", "LastActiveDate")
VALUES ('test_student@gmail.com', 'AQAAAAIAAYagAAAAENK5...', 'Học Sinh', 'Thường', 'Student', true, 0, 10, NOW(), 5, NOW());

INSERT INTO "AppUsers" ("Email", "PasswordHash", "FirstName", "LastName", "Role", "IsActive", "Subscription", "DailyQueryCount", "LastQueryDate", "TodayChatCount", "LastActiveDate")
VALUES ('test_premium_student@gmail.com', 'AQAAAAIAAYagAAAAENK5...', 'Học Sinh', 'Premium', 'Student', true, 1, 0, NOW(), 0, NOW());

-- 4. Thêm giao dịch VNPAY nâng cấp tài khoản Premium
INSERT INTO "Transactions" ("UserId", "Amount", "Type", "CreatedAt", "UsdVndRate")
VALUES (
    (SELECT "Id" FROM "AppUsers" WHERE "Email" = 'test_premium_student@gmail.com'),
    100000.0,
    'Premium',
    NOW() - INTERVAL '1 day',
    25000.0
);

-- 5. Tạo phiên hội thoại mẫu (ChatSession) liên kết với Môn học PRN222
INSERT INTO "ChatSessions" ("Id", "SubjectId", "UserId", "Title", "CreatedAt")
VALUES (
    'a0000000-0000-0000-0000-000000000001',
    (SELECT "Id" FROM "Subjects" WHERE "Code" = 'PRN222' LIMIT 1),
    (SELECT "Id" FROM "AppUsers" WHERE "Email" = 'test_premium_student@gmail.com'),
    'Test Refactor RAG Session 1',
    NOW() - INTERVAL '6 days'
);

-- 6. Thêm tin nhắn trao đổi trong 7 ngày gần nhất để vẽ biểu đồ và bảng xu hướng Token
-- Ngày 1 (6 ngày trước)
INSERT INTO "ChatMessages" ("SessionId", "Role", "Content", "Citations", "TokenIn", "TokenOut", "UsdRate", "Timestamp")
VALUES ('a0000000-0000-0000-0000-000000000001', 'user', 'Hỏi bài ngày 1', '[]', 0, 0, 0.0, NOW() - INTERVAL '6 days');
INSERT INTO "ChatMessages" ("SessionId", "Role", "Content", "Citations", "TokenIn", "TokenOut", "UsdRate", "Timestamp")
VALUES ('a0000000-0000-0000-0000-000000000001', 'assistant', 'Trả lời ngày 1', '[{"FileName":"GiaoTrinhCSharp.pdf","Page":5}]', 1000, 300, 25000.0, NOW() - INTERVAL '6 days');

-- Ngày 2 (5 ngày trước)
INSERT INTO "ChatMessages" ("SessionId", "Role", "Content", "Citations", "TokenIn", "TokenOut", "UsdRate", "Timestamp")
VALUES ('a0000000-0000-0000-0000-000000000001', 'user', 'Hỏi bài ngày 2', '[]', 0, 0, 0.0, NOW() - INTERVAL '5 days');
INSERT INTO "ChatMessages" ("SessionId", "Role", "Content", "Citations", "TokenIn", "TokenOut", "UsdRate", "Timestamp")
VALUES ('a0000000-0000-0000-0000-000000000001', 'assistant', 'Trả lời ngày 2', '[{"FileName":"GiaoTrinhCSharp.pdf","Page":12}]', 1500, 400, 25000.0, NOW() - INTERVAL '5 days');

-- Ngày 3 (4 ngày trước)
INSERT INTO "ChatMessages" ("SessionId", "Role", "Content", "Citations", "TokenIn", "TokenOut", "UsdRate", "Timestamp")
VALUES ('a0000000-0000-0000-0000-000000000001', 'user', 'Hỏi bài ngày 3', '[]', 0, 0, 0.0, NOW() - INTERVAL '4 days');
INSERT INTO "ChatMessages" ("SessionId", "Role", "Content", "Citations", "TokenIn", "TokenOut", "UsdRate", "Timestamp")
VALUES ('a0000000-0000-0000-0000-000000000001', 'assistant', 'Trả lời ngày 3', '[{"FileName":"Slide_RAG.pdf","Page":1}]', 2000, 500, 25000.0, NOW() - INTERVAL '4 days');

-- Ngày 4 (3 ngày trước)
INSERT INTO "ChatMessages" ("SessionId", "Role", "Content", "Citations", "TokenIn", "TokenOut", "UsdRate", "Timestamp")
VALUES ('a0000000-0000-0000-0000-000000000001', 'user', 'Hỏi bài ngày 4', '[]', 0, 0, 0.0, NOW() - INTERVAL '3 days');
INSERT INTO "ChatMessages" ("SessionId", "Role", "Content", "Citations", "TokenIn", "TokenOut", "UsdRate", "Timestamp")
VALUES ('a0000000-0000-0000-0000-000000000001', 'assistant', 'Trả lời ngày 4', '[{"FileName":"Slide_RAG.pdf","Page":4}]', 2500, 700, 25000.0, NOW() - INTERVAL '3 days');

-- Ngày 5 (2 ngày trước)
INSERT INTO "ChatMessages" ("SessionId", "Role", "Content", "Citations", "TokenIn", "TokenOut", "UsdRate", "Timestamp")
VALUES ('a0000000-0000-0000-0000-000000000001', 'user', 'Hỏi bài ngày 5', '[]', 0, 0, 0.0, NOW() - INTERVAL '2 days');
INSERT INTO "ChatMessages" ("SessionId", "Role", "Content", "Citations", "TokenIn", "TokenOut", "UsdRate", "Timestamp")
VALUES ('a0000000-0000-0000-0000-000000000001', 'assistant', 'Trả lời ngày 5', '[{"FileName":"GiaoTrinhCSharp.pdf","Page":25}]', 3000, 800, 25000.0, NOW() - INTERVAL '2 days');

-- Ngày 6 (1 ngày trước)
INSERT INTO "ChatMessages" ("SessionId", "Role", "Content", "Citations", "TokenIn", "TokenOut", "UsdRate", "Timestamp")
VALUES ('a0000000-0000-0000-0000-000000000001', 'user', 'Hỏi bài ngày 6', '[]', 0, 0, 0.0, NOW() - INTERVAL '1 day');
INSERT INTO "ChatMessages" ("SessionId", "Role", "Content", "Citations", "TokenIn", "TokenOut", "UsdRate", "Timestamp")
VALUES ('a0000000-0000-0000-0000-000000000001', 'assistant', 'Trả lời ngày 6', '[{"FileName":"GiaoTrinhCSharp.pdf","Page":26}]', 3500, 900, 25000.0, NOW() - INTERVAL '1 day');

-- Ngày 7 (Hôm nay)
INSERT INTO "ChatMessages" ("SessionId", "Role", "Content", "Citations", "TokenIn", "TokenOut", "UsdRate", "Timestamp")
VALUES ('a0000000-0000-0000-0000-000000000001', 'user', 'Hỏi bài ngày hôm nay', '[]', 0, 0, 0.0, NOW());
INSERT INTO "ChatMessages" ("SessionId", "Role", "Content", "Citations", "TokenIn", "TokenOut", "UsdRate", "Timestamp")
VALUES ('a0000000-0000-0000-0000-000000000001', 'assistant', 'Trả lời ngày hôm nay', '[{"FileName":"DocumentRef.pdf","Page":99}]', 4000, 1100, 25000.0, NOW());

-- 7. Thêm các ý kiến góp ý, báo cáo lỗi mẫu
INSERT INTO "ContactMessages" ("UserId", "Content", "Type", "Status", "CreatedAt")
VALUES (
    (SELECT "Id" FROM "AppUsers" WHERE "Email" = 'test_student@gmail.com'),
    'Test Feedback: Lỗi hiển thị PDF tài liệu môn C#',
    0,
    0,
    NOW()
);

INSERT INTO "ContactMessages" ("UserId", "Content", "Type", "Status", "CreatedAt")
VALUES (
    (SELECT "Id" FROM "AppUsers" WHERE "Email" = 'test_student@gmail.com'),
    'Test Feedback: Chatbot trả lời sai ngữ cảnh',
    1,
    0,
    NOW()
);

INSERT INTO "ContactMessages" ("UserId", "Content", "Type", "Status", "CreatedAt")
VALUES (
    (SELECT "Id" FROM "AppUsers" WHERE "Email" = 'test_student@gmail.com'),
    'Test Feedback: Góp ý thêm tính năng download slide',
    2,
    0,
    NOW()
);
