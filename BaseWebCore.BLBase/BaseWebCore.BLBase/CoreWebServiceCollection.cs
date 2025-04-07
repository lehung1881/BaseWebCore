using BaseWebCore.Core.DatabaseServices;
using BaseWebCore.Core.Services;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaseWebCore.BLBase
{
    public class CoreWebServiceCollection
    {
        protected IServiceProvider _serviceProvider;

        public CoreWebServiceCollection(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public T GetService<T>()
        {
            return _serviceProvider.GetRequiredService<T>();
        }

        private IAuthService _authService;
        public IAuthService AuthService()
        {
            if(_authService == null)
            {
                _authService = GetService<IAuthService>();
            }
            return _authService;
        }

        private IPostgresSQLService _postgresSQLService;
        public IPostgresSQLService PostgresSQLService()
        {
            if (_postgresSQLService == null)
            {
                _postgresSQLService = GetService<IPostgresSQLService>();
            }
            return _postgresSQLService;
        }
    }
}
