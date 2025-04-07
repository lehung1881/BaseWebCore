using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaseWebCore.Core.Services
{
    public interface IAuthService
    {
        Guid GetUserID();
    }
}
