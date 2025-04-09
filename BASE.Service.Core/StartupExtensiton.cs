using BASE.Service.Core.Services;
using BASE.Service.Core.Services.Interfaces;
using BaseWebCore.Core.DatabaseServices;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaseWebCore.Core
{
    public static class StartupExtensiton
    {
        public static void UseCoreServices(this IServiceCollection service)
        {
            service.AddTransient<IAuthService, AuthService>();
            service.AddTransient<IPostgresSQLService, PostgresSQLService>();
        }
    }
}
