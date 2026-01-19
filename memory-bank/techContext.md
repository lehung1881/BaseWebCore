# Tech Context: BaseWebCore

## Công nghệ sử dụng

### Nền tảng chính

-   **.NET 8.0**: Framework mới nhất của Microsoft với các cải tiến về hiệu suất và tính năng
-   **ASP.NET Core**: Framework web hiện đại, cross-platform
-   **C#**: Ngôn ngữ lập trình chính

### Cơ sở dữ liệu

-   **PostgreSQL**: Hỗ trợ chủ yếu qua Npgsql
-   **Entity Framework Core**: ORM framework để tương tác với cơ sở dữ liệu

### Xác thực & Bảo mật

-   **JWT (JSON Web Token)**: Cho xác thực và phân quyền
-   **Microsoft.AspNetCore.Authentication.JwtBearer**: Package hỗ trợ xác thực JWT

### Công cụ phát triển

-   **Visual Studio 2022**: IDE chính
-   **Git**: Quản lý mã nguồn

## Cấu hình phát triển

### Yêu cầu hệ thống

-   .NET SDK 8.0 trở lên
-   Visual Studio 2022 hoặc tương đương
-   PostgreSQL (hoặc cơ sở dữ liệu tương thích)

### Cấu trúc giải pháp

```
BaseWeb.sln
├── BASE.Service.Core/                # Core utilities
├── BASE.Service.Core.BL/             # Business logic
├── BASE.Service.Core.Model/          # Data models
├── BASE.Service.Core.Web/            # Web API layer
└── BASE.Service.Core.Database/       # Database access layer
```

### Môi trường

-   **Development**: Môi trường phát triển cục bộ
-   **Staging**: Môi trường kiểm thử
-   **Production**: Môi trường triển khai

## Ràng buộc kỹ thuật

1. **Tương thích**: Phải tương thích với các phiên bản .NET 8.0 trở lên

2. **Hiệu suất**:

    - Tối ưu cho các API có tải trung bình đến cao
    - Thời gian phản hồi của API cơ bản dưới 100ms

3. **Bảo mật**:

    - Sử dụng HTTPS cho tất cả các endpoint
    - Xác thực qua JWT
    - Mã hóa dữ liệu nhạy cảm

4. **Khả năng mở rộng**:
    - Thiết kế module hóa để dễ dàng thêm chức năng mới
    - Hỗ trợ microservices architecture

## Dependencies chính

Các package NuGet chính:

-   Microsoft.AspNetCore.Authentication.JwtBearer
-   Microsoft.EntityFrameworkCore
-   Npgsql.EntityFrameworkCore.PostgreSQL
-   Newtonsoft.Json (hoặc System.Text.Json)
-   Serilog (hoặc NLog) cho logging
