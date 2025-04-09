using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BASE.Service.Core.Services
{
    /// <summary>
    /// Service dùng để xử lý các thông tin xác thực liên quan đến người dùng hiện tại.
    /// </summary>
    public class AuthService : IAuthService
    {
        /// <summary>
        /// Truy cập thông tin HTTP context hiện tại.
        /// </summary>
        protected readonly IHttpContextAccessor _httpContext;

        /// <summary>
        /// Khởi tạo một instance mới của <see cref="AuthService"/>.
        /// </summary>
        /// <param name="httpContext">IHttpContextAccessor để truy xuất thông tin từ HTTP context.</param>
        public AuthService(IHttpContextAccessor httpContext)
        {
            _httpContext = httpContext;
        }

        /// <summary>
        /// Lấy UserID từ header "X-UserID" trong HTTP request hiện tại.
        /// </summary>
        /// <returns>
        /// Trả về Guid đại diện cho ID của người dùng nếu tồn tại,
        /// ngược lại trả về Guid.Empty.
        /// </returns>
        public Guid GetUserID()
        {
            string userID = _httpContext.HttpContext?.Request?.Headers["X-UserID"];
            if (!string.IsNullOrEmpty(userID))
            {
                return Guid.Parse(userID);
            }
            return Guid.Empty;
        }
    }
}
