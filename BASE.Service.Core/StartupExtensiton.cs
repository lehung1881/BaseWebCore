using BASE.Service.Core.Services;
using Microsoft.Extensions.DependencyInjection;
namespace BASE.Service.Core
{
    public static class StartupExtensiton
    {
        public static void UseCoreServices(this IServiceCollection service)
        {
            service.AddTransient<IAuthService, AuthService>();
        }
    }
}
