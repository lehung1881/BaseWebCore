using BASE.Service.Core.Model;
using BASE.Service.Core.Services;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data;
using System.Reflection;

namespace BASE.Service.Core.BL
{
    /// <summary>
    /// Base class cho Business Logic layer, cung cấp các chức năng CRUD cơ bản (MySQL + Dapper)
    /// </summary>
    /// <typeparam name="TModel">Model kế thừa từ BaseModel</typeparam>
    public partial class BaseBL
    {
        #region Fields & Constructor

        /// <summary>
        /// Cache column name theo Database + Table
        /// </summary>
        private static readonly Dictionary<string, List<string>> _columnCache = new();
        private static readonly object _columnLock = new();

        protected readonly CoreWebServiceCollection _serviceCollection;

        protected IAuthService _authService => _serviceCollection.AuthService();
        protected IMySQLService _mySQLService => _serviceCollection.MySQLService();

        protected Guid _databaseID = Guid.Parse("496f89b1-8f25-4c32-b6d6-d18b8bbb8ef8");

        protected BaseBL(CoreWebServiceCollection serviceCollection)
        {
            _serviceCollection = serviceCollection;
        }

        private Guid _userID = Guid.Empty;
        protected Guid UserID
        {
            get
            {
                if (_userID == Guid.Empty)
                {
                    _userID = _authService.GetUserID();
                }
                return _userID;
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Lấy connection theo databaseID (customer DB)
        /// </summary>
        protected virtual async Task<IDbConnection> GetConnectionAsync()
        {
            return await _mySQLService.GetDBConnectionAsync(_databaseID);
        }

        /// <summary>
        /// Lấy bản ghi theo ID
        /// </summary>
        public async Task<T> GetDataByID<T>(string id) where T : BaseModel
        {
            return await _mySQLService.GetDataByID<T>(_databaseID, id);
        }

        /// <summary>
        /// Lấy bản ghi theo ID
        /// </summary>
        public async Task<BaseModel> GetDataByID(Type modelType, string id, string columns = "*")
        {
            return await _mySQLService.GetDataByID(_databaseID, modelType, id);
        }

        /// <summary>
        /// Paging (chưa implement)
        /// </summary>
        public virtual PagingResponse GetPaging(
            int pageIndex,
            int pageSize,
            List<FilterCondition> filters,
            int? viewName,
            string sort = "")
        {
            return new PagingResponse();
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Lấy danh sách properties có thể map (loại bỏ NotMapped & không tồn tại trong DB)
        /// </summary>
        private static List<PropertyInfo> GetMappableProperties(Type modelType, HashSet<string> dbColumnSet)
        {
            return modelType
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.GetCustomAttribute<NotMappedAttribute>() == null)
                .Where(p => dbColumnSet.Contains(p.Name))
                .ToList();
        }

        /// <summary>
        /// Đảm bảo Primary Key được sinh tự động (nếu là Guid và chưa có giá trị)
        /// </summary>
        private static void EnsurePrimaryKey(BaseModel model, PropertyInfo pkProp)
        {
            if (pkProp.PropertyType != typeof(Guid))
                return;

            var currentValue = (Guid?)pkProp.GetValue(model);
            if (!currentValue.HasValue || currentValue == Guid.Empty)
            {
                pkProp.SetValue(model, Guid.NewGuid());
            }
        }

        /// <summary>
        /// Lấy danh sách properties cần update
        /// </summary>
        private static IEnumerable<PropertyInfo> GetUpdateProperties(
            List<PropertyInfo> allProps,
            List<string> updateColumns,
            string primaryKeyName)
        {
            // Loại bỏ PK
            var propsExcludePk = allProps
                .Where(p => !string.Equals(p.Name, primaryKeyName, StringComparison.OrdinalIgnoreCase));

            // Nếu có chỉ định UpdateColumns, chỉ update những cột đó
            if (updateColumns != null && updateColumns.Any())
            {
                var updateSet = new HashSet<string>(updateColumns, StringComparer.OrdinalIgnoreCase);
                return propsExcludePk.Where(p => updateSet.Contains(p.Name));
            }

            return propsExcludePk;
        }

        /// <summary>
        /// Build dictionary parameters cho Dapper
        /// </summary>
        private static Dictionary<string, object> BuildParameters(List<PropertyInfo> props, BaseModel model)
        {
            var parameters = new Dictionary<string, object>(props.Count, StringComparer.OrdinalIgnoreCase);

            foreach (var prop in props)
            {
                parameters[prop.Name] = prop.GetValue(model) ?? DBNull.Value;
            }

            return parameters;
        }

        #endregion
    }
}