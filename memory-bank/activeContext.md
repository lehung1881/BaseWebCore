# Active Context: BaseWebCore

## Trọng tâm công việc hiện tại

Hiện tại, dự án BaseWebCore đang trong giai đoạn phát triển ban đầu. Mục tiêu chính là hoàn thiện cấu trúc cơ bản và các tính năng cốt lõi của framework.

## Thay đổi gần đây

-   Khởi tạo cấu trúc dự án với 5 module chính
-   Thiết lập cấu hình cơ bản cho Web API
-   Triển khai xác thực JWT
-   Cấu hình cơ sở hạ tầng cho việc xử lý request và response
-   Thêm `MySQLService` (Dapper + MySqlConnector) với cơ chế lấy connection string từ master DB (`ConnectionStrings:MasterMySql`)

## Các quyết định đang được xem xét

1. **Cơ chế logging**: Cần quyết định và triển khai giải pháp logging nhất quán
2. **Cấu trúc repository**: Hoàn thiện mẫu repository để truy cập dữ liệu
3. **Cơ chế cache**: Xem xét giải pháp caching để tối ưu hiệu suất
4. **Xử lý lỗi**: Xây dựng cơ chế xử lý lỗi toàn diện và nhất quán

## Các bước tiếp theo

Các nhiệm vụ ưu tiên hiện tại bao gồm:

1. **Hoàn thiện lớp dịch vụ cốt lõi**:

    - Xác thực và phân quyền
    - Xử lý lỗi
    - Logging

2. **Xây dựng các mẫu dịch vụ**:

    - CRUD cơ bản
    - Validation
    - Xử lý transaction

3. **Tài liệu hóa**:

    - Tạo tài liệu API
    - Hướng dẫn sử dụng
    - Mẫu và best practices

4. **Kiểm thử**:
    - Unit tests cho các thành phần chính
    - Integration tests
    - Stress tests
