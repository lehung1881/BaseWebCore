using BASE.Service.Core.Enum;
using BASE.Service.Core.Model;
using BASE.Service.Core.Services;
using Dapper;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data;
using System.Reflection;

namespace BASE.Service.Core.BL
{
    /// <summary>
    /// Base class cho Business Logic layer, cung cấp các chức năng CRUD cơ bản (MySQL + Dapper)
    /// </summary>
    /// <typeparam name="TModel">Model kế thừa từ BaseModel</typeparam>
    public abstract class BaseBL
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

        protected Guid _databaseID = Guid.NewGuid();

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
        protected virtual IDbConnection GetDbConnection()
        {
            return _mySQLService.GetConnection(_databaseID);
        }

        /// <summary>
        /// Lấy bản ghi theo ID
        /// </summary>
        public virtual BaseModel GetByID(Guid id)
        {
            return _mySQLService.GetByID<BaseModel>(_databaseID, id);
        }

        /// <summary>
        /// Lưu dữ liệu (Insert / Update / Delete) theo ModelState
        /// </summary>
        public virtual ServiceResponse SaveData(BaseModel model)
        {
            var res = new ServiceResponse();
            IDbTransaction tran = null;
            IDbConnection cnn = null;
            try
            {
                if (model == null)
                {
                    res.OnError(ServiceResponseCode.InvalidData);
                    return res;
                }

                var validateResults = ValidateBeforeSaveData(model);
                if (validateResults != null && validateResults.Any())
                {
                    res.Success = false;
                    res.ValidateInfo = validateResults;
                    return res;
                }

                BeforeSaveData(model);

                cnn = GetDbConnection();
                if (cnn.State != ConnectionState.Open)
                    cnn.Open();

                tran = cnn.BeginTransaction();

                var success = DoSaveData(model, cnn, tran);
                if (!success)
                {
                    tran.Rollback();
                    res.OnError(ServiceResponseCode.Exception, "SaveData failed");
                    return res;
                }

                tran.Commit();
                res.OnSuccess();

                AfterSaveData(model, success);
            }
            catch (Exception ex)
            {
                tran?.Rollback();
                res.OnError(ServiceResponseCode.Exception, ex.Message);
            }
            finally
            {
                if (cnn != null)
                {
                    if (cnn.State != ConnectionState.Closed)
                        cnn.Close();
                    cnn.Dispose();
                }
            }

            return res;
        }

        /// <summary>
        /// Paging (chưa implement)
        /// </summary>
        public virtual PagingResponse GetPaging<T>(
            int pageIndex,
            int pageSize,
            List<FilterCondition> filters,
            int? viewName,
            string sort = "")
        {
            return new PagingResponse();
        }

        #endregion

        #region Hook Methods (Override)

        public virtual List<ValidateResult> ValidateBeforeSaveData(BaseModel model)
        {
            return new List<ValidateResult>();
        }

        public virtual void BeforeSaveData(BaseModel model)
        {
        }

        public virtual void AfterSaveData(BaseModel model, bool isSuccess)
        {
        }

        #endregion

        #region Core Save Logic (Optimized)

        /// <summary>
        /// Lấy danh sách column của bảng (cache theo database + table)
        /// </summary>
        protected virtual List<string> GetColumnByTableName(string tableName, IDbConnection cnn)
        {
            var cacheKey = $"{tableName}_{_databaseID.ToString()}";

            if (_columnCache.TryGetValue(cacheKey, out var cached))
                return cached;

            const string sql = "SELECT COLUMN_NAME FROM information_schema.COLUMNS WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = @TableName AND LENGTH(generation_expression) = 0 ORDER BY ORDINAL_POSITION";

            var columns = cnn.Query<string>(sql, new { TableName = tableName }).ToList();

            lock (_columnLock)
            {
                if (!_columnCache.ContainsKey(cacheKey))
                {
                    _columnCache[cacheKey] = columns;
                }
            }

            return columns;
        }

        /// <summary>
        /// Thực hiện Insert / Update / Delete theo ModelState (MySQL) - Optimized version
        /// </summary>
        protected virtual bool DoSaveData(BaseModel model, IDbConnection cnn, IDbTransaction tran)
        {
            if (model == null)
                throw new ArgumentNullException(nameof(model));

            var tableName = model.GetViewOrTableName();
            var primaryKeyName = model.GetPrimaykeyField();

            if (string.IsNullOrEmpty(primaryKeyName))
                throw new InvalidOperationException($"Primary key not defined for table '{tableName}'");

            // Lấy danh sách cột từ DB (đã có cache)
            var dbColumns = GetColumnByTableName(tableName, cnn);
            var dbColumnSet = new HashSet<string>(dbColumns, StringComparer.OrdinalIgnoreCase);

            // Lấy properties có thể map
            var props = GetMappableProperties(model.GetType(), dbColumnSet);

            var pkProp = props.FirstOrDefault(p =>
                string.Equals(p.Name, primaryKeyName, StringComparison.OrdinalIgnoreCase));

            if (pkProp == null)
                throw new InvalidOperationException($"Primary key '{primaryKeyName}' not found in model properties");

            // Xử lý theo ModelState
            return model.ModelState switch
            {
                ModelState.Insert => ExecuteInsert(model, tableName, props, pkProp, cnn, tran),
                ModelState.Update => ExecuteUpdate(model, tableName, props, pkProp, primaryKeyName, cnn, tran),
                ModelState.Delete => ExecuteDelete(model, tableName, pkProp, primaryKeyName, cnn, tran),
                _ => throw new InvalidOperationException($"Unsupported ModelState: {model.ModelState}")
            };
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
        /// Thực thi câu lệnh INSERT
        /// </summary>
        private static bool ExecuteInsert(
            BaseModel model,
            string tableName,
            List<PropertyInfo> props,
            PropertyInfo pkProp,
            IDbConnection cnn,
            IDbTransaction tran)
        {
            // Tự động sinh Guid PK nếu cần
            EnsurePrimaryKey(model, pkProp);

            // Build SQL
            var columns = string.Join(", ", props.Select(p => $"`{p.Name}`"));
            var values = string.Join(", ", props.Select(p => $"@{p.Name}"));
            var sql = $"INSERT INTO `{tableName}` ({columns}) VALUES ({values})";

            // Execute
            var parameters = BuildParameters(props, model);
            var affected = cnn.Execute(sql, parameters, tran);

            return affected > 0;
        }

        /// <summary>
        /// Thực thi câu lệnh UPDATE
        /// </summary>
        private static bool ExecuteUpdate(
            BaseModel model,
            string tableName,
            List<PropertyInfo> props,
            PropertyInfo pkProp,
            string primaryKeyName,
            IDbConnection cnn,
            IDbTransaction tran)
        {
            // Lọc các cột cần update
            var updateProps = GetUpdateProperties(props, model.UpdateColumns, primaryKeyName);

            if (!updateProps.Any())
                return true; // Không có gì để update

            // Build SQL
            var setClause = string.Join(", ", updateProps.Select(p => $"`{p.Name}` = @{p.Name}"));
            var sql = $"UPDATE `{tableName}` SET {setClause} WHERE `{primaryKeyName}` = @{primaryKeyName}";

            // Execute
            var parameters = BuildParameters(props, model);
            var affected = cnn.Execute(sql, parameters, tran);

            return affected >= 0; // MySQL: affected = 0 vẫn OK
        }

        /// <summary>
        /// Thực thi câu lệnh DELETE (Hard Delete)
        /// </summary>
        private static bool ExecuteDelete(
            BaseModel model,
            string tableName,
            PropertyInfo pkProp,
            string primaryKeyName,
            IDbConnection cnn,
            IDbTransaction tran)
        {
            // Lấy giá trị Primary Key
            var pkValue = pkProp.GetValue(model);

            if (pkValue == null || (pkProp.PropertyType == typeof(Guid) && (Guid)pkValue == Guid.Empty))
            {
                throw new InvalidOperationException($"Primary key '{primaryKeyName}' must have a valid value for delete operation");
            }

            // Build SQL
            var sql = $"DELETE FROM `{tableName}` WHERE `{primaryKeyName}` = @{primaryKeyName}";

            // Build parameters
            var parameters = new Dictionary<string, object>(1, StringComparer.OrdinalIgnoreCase)
            {
                [primaryKeyName] = pkValue
            };

            // Execute
            var affected = cnn.Execute(sql, parameters, tran);

            return affected > 0;
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
