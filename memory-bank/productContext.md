# Product Context: BaseWebCore

## Vấn đề cần giải quyết

BaseWebCore giải quyết vấn đề lặp lại công việc khi xây dựng các dự án web API mới. Thay vì phải thiết lập lại các thành phần cơ bản cho mỗi dự án, BaseWebCore cung cấp một khung làm việc đã được cấu hình sẵn với các chức năng tiêu chuẩn.

## Người dùng mục tiêu

-   Lập trình viên phát triển backend
-   Đội ngũ phát triển cần triển khai nhanh chóng các API
-   Tổ chức muốn duy trì tính nhất quán giữa các dự án web

## Cách hoạt động

BaseWebCore được tổ chức thành các module chuyên biệt:

1. **BASE.Service.Core**: Chứa các dịch vụ cốt lõi và các tiện ích chung
2. **BASE.Service.Core.BL**: Chứa logic nghiệp vụ
3. **BASE.Service.Core.Model**: Định nghĩa các mô hình dữ liệu
4. **BASE.Service.Core.Web**: Cung cấp cấu hình web và API endpoints
5. **BASE.Service.Core.Database**: Xử lý tương tác với cơ sở dữ liệu

## Mục tiêu trải nghiệm người dùng

-   **Đơn giản trong sử dụng**: Dễ dàng mở rộng và tích hợp vào dự án mới
-   **Nhất quán**: Cung cấp cơ chế xử lý lỗi và cấu trúc phản hồi API nhất quán
-   **Bảo mật**: Tích hợp sẵn xác thực JWT và các biện pháp bảo mật tiêu chuẩn
-   **Mạnh mẽ**: Xử lý hiệu quả các tác vụ phức tạp và tải cao

## Hiệu quả kỳ vọng

-   Giảm thời gian phát triển dự án mới từ 30-50%
-   Tăng tính nhất quán giữa các dự án khác nhau
-   Giảm thiểu lỗi phổ biến thông qua việc tái sử dụng mã đã được kiểm thử
