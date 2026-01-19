using BASE.Service.Core.Services;
using Microsoft.Extensions.DependencyInjection;

namespace BASE.Service.Core.BL
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

        /// <summary>
        /// Lấy đối tượng thao tác với MySQL
        /// </summary>
        private IMySQLService _mySQLService;
        public IMySQLService MySQLService()
        {
            if (_mySQLService == null)
            {
                _mySQLService = GetService<IMySQLService>();
            }
            return _mySQLService;
        }
    }
}
