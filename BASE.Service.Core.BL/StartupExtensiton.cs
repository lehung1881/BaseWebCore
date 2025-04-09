using BASE.Service.Core.Database;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;

namespace BASE.Service.Core.BL
{
    public static class StartupExtensiton
    {
        public static void UseBLServices(this IServiceCollection service, IConfiguration configuration)
        {
            service.UseCoreServices();
            service.UseDatabaseServices();
        }
    }
}
