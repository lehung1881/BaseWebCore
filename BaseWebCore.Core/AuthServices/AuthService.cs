using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaseWebCore.Core.Services
{
    public class AuthService : IAuthService
    {
        protected readonly IHttpContextAccessor _httpContext;

        public AuthService(IHttpContextAccessor httpContext)
        {
            _httpContext = httpContext;
        }

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
