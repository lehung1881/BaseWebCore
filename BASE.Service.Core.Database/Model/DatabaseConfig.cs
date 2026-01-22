using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BASE.Service.Core.Database.Model
{
    /// <summary>
    /// Model đại diện cho bảng database_config - Cấu hình kết nối database
    /// </summary>
    public class DatabaseConfig
    {
        /// <summary>
        /// ID định danh duy nhất của cấu hình database (Primary Key)
        /// </summary>
        [Key]
        public Guid DatabaseID { get; set; }

        /// <summary>
        /// ID của tenant (dùng cho hệ thống multi-tenant)
        /// </summary>
        public Guid? TenantID { get; set; }

        /// <summary>
        /// Mã ứng dụng sử dụng database này
        /// </summary>
        public string? AppCode { get; set; }

        /// <summary>
        /// Địa chỉ server database (hostname hoặc IP)
        /// </summary>
        public string? Server { get; set; }

        /// <summary>
        /// Cổng kết nối database (VD: 3306 cho MySQL, 1433 cho SQL Server)
        /// </summary>
        public int? Port { get; set; }

        /// <summary>
        /// Tên database cần kết nối
        /// </summary>
        public string? Database { get; set; }

        /// <summary>
        /// Tên người dùng để đăng nhập database
        /// </summary>
        public string? UserID { get; set; }

        /// <summary>
        /// Mật khẩu để đăng nhập database
        /// </summary>
        public string? Password { get; set; }

        /// <summary>
        /// Phiên bản database hiện tại
        /// </summary>
        public string? VersionDB { get; set; }

        /// <summary>
        /// Môi trường hiện tại của dữ liệu (VD: 'g2', 'prod', 'dev')
        /// </summary>
        public string Env { get; set; } = "g2";

        /// <summary>
        /// Trạng thái dữ liệu: 0 - Đang làm việc, 1 - Không làm việc
        /// </summary>
        public int Status { get; set; } = 0;

        /// <summary>
        /// Ngày tạo bản ghi cấu hình
        /// </summary>
        public DateTime? CreatedDate { get; set; } = DateTime.Now;
    }

}
