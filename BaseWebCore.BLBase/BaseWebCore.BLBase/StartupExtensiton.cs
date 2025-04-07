using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaseWebCore.BLBase
{
    public static class StartupExtensiton
    {
        public static void UseCoreBLServices(this IServiceCollection service)
        {
            service.AddTransient<CoreWebServiceCollection, CoreWebServiceCollection>();
        }
    }
}
