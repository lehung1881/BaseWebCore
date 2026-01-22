using BASE.Service.Core.Services;
using Microsoft.Extensions.DependencyInjection;

namespace BASE.Service.Core.Database
{
    public static class StartupExtensiton
    {
        public static void UseDatabaseServices(this IServiceCollection service)
        {
            service.AddTransient<IMySQLService, MySQLService>();
        }
    }
}
