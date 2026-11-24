# Ví dụ: request và response có conversation

Ví dụ này gọi trực tiếp REST API của Foundry project để xem JSON gửi đi và nhận về khi dùng **conversation**.

## Chạy

Từ thư mục gốc repo, đăng nhập `az login` rồi chạy:

```powershell
dotnet run --project src/Examples.ConversationStorage
```

Chương trình in ra mã conversation và câu trả lời, đồng thời ghi toàn bộ request/response vào [conversation-sample-output.md](conversation-sample-output.md).

## Luồng

1. `POST openai/conversations` tạo một conversation. Server trả về `id` dạng `conv_...`.
2. `POST openai/responses` với `conversation: <id>` gửi câu thứ nhất. Response được ghi vào conversation.
3. `POST openai/responses` cùng `conversation: <id>` gửi câu thứ hai, không lặp lại thông tin cũ. Model vẫn trả lời đúng vì server đã nạp lịch sử của conversation.
4. `GET openai/conversations/<id>/items` liệt kê các item đã lưu.

## Các trường quan trọng

| Trường | Ý nghĩa |
|---|---|
| `conversation` (request) | ID conversation mà response này thuộc về. Thiếu trường này thì response không được gắn vào hội thoại nào. |
| `input` (request) | Tin nhắn của người dùng cho lượt này. |
| `instructions` (request) | System prompt. Chỉ cần gửi ở lượt đầu, hoặc khi muốn đổi. |
| `id` (response) | ID của response, dạng `resp_...`. |
| `output` (response) | Các item do model tạo, gồm message và tool call nếu có. |
| `usage` (response) | Số token đầu vào và đầu ra của lượt này. |
| `data` (items) | Danh sách item đã lưu trong conversation, gồm cả tin nhắn người dùng và trợ lý. |

## Lưu ý

- Ví dụ này chỉ dùng REST, không dùng Agent Framework, để JSON hiển thị nguyên dạng.
- Conversation trên Basic setup được giữ 60 ngày theo tài liệu FAQ, sau đó tự xóa.
- `conversation-sample-output.md` chứa ID thật của tài nguyên trong project của bạn. Nếu chia sẻ, hãy cân nhắc trước.
