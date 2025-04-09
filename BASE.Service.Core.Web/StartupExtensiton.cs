using BASE.Service.Core.BL;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BASE.Service.Core.Web
{
    public static class StartupExtensiton
    {
        public static void UseWebCoreServices(this IServiceCollection service, IConfiguration configuration)
        {
            service.UseBLServices(configuration);
        }
    }
}
