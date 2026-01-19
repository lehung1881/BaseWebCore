# System Patterns: BaseWebCore

## Kiến trúc hệ thống

BaseWebCore sử dụng kiến trúc đa tầng (multi-tier) với các lớp được phân chia rõ ràng:

```
┌─────────────────────────┐
│  Web/API Layer          │
│  (BASE.Service.Core.Web)│
└───────────┬─────────────┘
            │
┌───────────▼─────────────┐
│  Business Logic Layer   │
│  (BASE.Service.Core.BL) │
└───────────┬─────────────┘
            │
┌───────────▼─────────────┐
│  Data Access Layer      │
│(BASE.Service.Core.Database)│
└───────────┬─────────────┘
            │
┌───────────▼─────────────┐
│  Models Layer           │
│(BASE.Service.Core.Model)│
└─────────────────────────┘
```

## Mẫu thiết kế

### 1. Dependency Injection

-   Sử dụng DI container tích hợp trong ASP.NET Core
-   Đăng ký dịch vụ trong phương thức ConfigureServices
-   Tiêm các dependency thông qua constructor

### 2. Repository Pattern

-   Trừu tượng hóa lớp truy cập dữ liệu
-   Cung cấp interface chung cho các thao tác CRUD
-   Tách biệt logic nghiệp vụ với logic truy cập dữ liệu

### 3. Service Pattern

-   Các logic nghiệp vụ được đóng gói trong các service class
-   Tách biệt logic nghiệp vụ khỏi controller
-   Dễ dàng unit test

### 4. JWT Authentication

-   Sử dụng JWT cho xác thực và ủy quyền
-   Cấu hình trong BaseStartupServices
-   Bảo vệ API endpoints

## Quyết định kỹ thuật chính

1. **ASP.NET Core 8.0**: Sử dụng công nghệ mới nhất để tận dụng các tính năng hiện đại

2. **Cấu trúc module hóa**: Chia dự án thành các module riêng biệt giúp dễ bảo trì

3. **Chuẩn hóa lỗi**: Định dạng phản hồi lỗi nhất quán

4. **Cấu hình tập trung**: Sử dụng các tệp cấu hình được chia sẻ

5. **Bảo mật tích hợp**: JWT và các biện pháp bảo mật khác được cấu hình sẵn

## Mối quan hệ giữa các thành phần

-   **Web Layer** gọi đến **Business Logic Layer** để xử lý yêu cầu
-   **Business Logic Layer** gọi đến **Data Access Layer** để thao tác dữ liệu
-   **Data Access Layer** sử dụng **Models** để đại diện cho dữ liệu
-   **Core Layer** cung cấp các tiện ích chung cho tất cả các lớp khác
