using BASE.Service.Core.Enum;
using BASE.Service.Core.Model;
using BASE.Service.Core.Services;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data;
using System.Reflection;
using Dapper;

namespace BASE.Service.Core.BL
{
    /// <summary>
    /// Base class cho Business Logic layer, cung cấp các chức năng CRUD cơ bản
    /// </summary>
    /// <typeparam name="TModel">Model kế thừa từ BaseModelCore, đại diện cho một entity trong database</typeparam>
    public abstract class BaseBL<TModel> where TModel : BaseModel
    {
        #region Fields and constructor

        /// <summary>
        /// Cache column name theo tên bảng
        /// </summary>
        private static readonly Dictionary<string, List<string>> _columns = new Dictionary<string, List<string>>();
        private static readonly object objLock = new object();

        /// <summary>
        /// Collection chứa các services được inject
        /// </summary>
        private readonly CoreWebServiceCollection _serviceCollection;

        /// <summary>
        /// Service xử lý authentication/authorization
        /// </summary>
        protected IAuthService _authService { get => _serviceCollection.AuthService(); }

        /// <summary>
        /// Service tương tác với MySQLService database
        /// </summary>
        protected IMySQLService _mySQLService { get => _serviceCollection.MySQLService(); }

        /// <summary>
        /// ID định danh cho phiên làm việc với database
        /// </summary>
        protected Guid _databaseID = Guid.NewGuid();

        /// <summary>
        /// Phương thức khởi tạo
        /// </summary>
        public BaseBL(CoreWebServiceCollection serviceCollection)
        {
            _serviceCollection = serviceCollection;
        }

        /// <summary>
        /// Thông tin UserID
        /// </summary>
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

        #region Methods

        /// <summary>
        /// Lấy DB connection hiện tại (customer DB) theo _databaseID
        /// </summary>
        protected virtual IDbConnection GetDbConnection()
        {
            return _mySQLService.GetConnection(_databaseID);
        }

        /// <summary>
        /// Lấy bản ghi theo ID
        /// </summary>
        /// <param name="id">ID của bản ghi cần lấy</param>
        /// <returns>Entity tương ứng với ID. Null nếu không tìm thấy</returns>
        public virtual TModel GetByID(Guid id)
        {
            return _mySQLService.GetByID<TModel>(_databaseID, id);
        }

        /// <summary>
        /// Lưu dữ liệu (Insert/Update) theo ModelState
        /// </summary>
        /// <param name="model">Dữ liệu cần lưu</param>
        /// <returns>ServiceResponse</returns>
        public virtual ServiceResponse SaveData(BaseModel model)
        {
            ServiceResponse res = new ServiceResponse();
            IDbConnection cnn = null;
            IDbTransaction tran = null;
            try
            {
                //Kiểm tra check null
                if (model == null)
                {
                    res.OnError(ServiceResponseCode.InvalidData);
                    return res;
                }

                // Validate chung cho mọi trường hợp
                res = ValidateBeforeSaveData(model);
                if (!res.Success)
                {
                    return res;
                }

                // Xử lý trước khi lưu
                BeforeSaveData(model);

                cnn = GetDbConnection();
                if (cnn.State != ConnectionState.Open)
                {
                    cnn.Open();
                }
                tran = cnn.BeginTransaction();

                bool isSuccess = DoSaveData(model, cnn, tran);
                if (!isSuccess)
                {
                    tran.Rollback();
                    res.OnError(ServiceResponseCode.Exception, "SaveData failed");
                    return res;
                }

                tran.Commit();
                res.OnSuccess();

                // Xử lý sau khi lưu
                AfterSaveData(model, isSuccess);
            }
            catch (Exception ex)
            {
                if (tran != null)
                {
                    tran.Rollback();
                }
                res.OnError(ServiceResponseCode.Exception, ex.Message);
            }
            finally
            {
                if (cnn != null)
                {  
                    if (cnn.State != ConnectionState.Closed)
                    {
                        cnn.Close();
                    }
                    cnn.Dispose();
                }
            }
            return res;
        }

        /// <summary>
        /// Lấy danh sách bản ghi có phân trang
        /// </summary>
        /// <typeparam name="T">Kiểu dữ liệu của kết quả trả về</typeparam>
        /// <param name="pageIndex">Index của trang cần lấy (bắt đầu từ 0)</param>
        /// <param name="pageSize">Số bản ghi trên một trang</param>
        /// <param name="filters">Danh sách điều kiện lọc</param>
        /// <param name="viewName">Chế độ xem (tùy chọn)</param>
        /// <param name="sort">Chuỗi sắp xếp (format: "column_name ASC/DESC")</param>
        /// <returns>Đối tượng PagingResponse chứa danh sách kết quả và thông tin phân trang</returns>
        public PagingResponse GetPaging<T>(int pageIndex, int pageSize, List<FilterCondition> filters, int? viewName, string sort = "")
        {
            return new PagingResponse();
        }

        #endregion

        #region Sub methods

        /// <summary>
        /// Validate dữ liệu trước khi lưu (áp dụng chung cho Insert/Update)
        /// </summary>
        /// <param name="model">Dữ liệu cần validate</param>
        /// <returns>ServiceResponse</returns>
        public virtual ServiceResponse ValidateBeforeSaveData(BaseModel model)
        {
            var res = new ServiceResponse();
            return res;
        }

        /// <summary>
        /// Xử lý trước khi lưu dữ liệu (cho override)
        /// </summary>
        public virtual void BeforeSaveData(BaseModel model)
        {

        }

        /// <summary>
        /// Xử lý sau khi lưu dữ liệu (cho override)
        /// </summary>
        public virtual void AfterSaveData(BaseModel model, bool isSuccess)
        {

        }

        /// <summary>
        /// Lấy danh sách column của bảng trong DB (có cache)
        /// </summary>
        protected virtual List<string> GetColumnByTableName(string tableName, IDbConnection cnn)
        {
            var key = tableName;

            if (_columns.ContainsKey(key))
            {
                return _columns[key];
            }

            var param = new Dictionary<string, object>();
            param.Add("@TABLE_NAME", tableName);
            var sql = "SELECT COLUMN_NAME FROM information_schema.COLUMNS WHERE TABLE_NAME = @TABLE_NAME AND LENGTH(generation_expression)=0 AND TABLE_SCHEMA = database() ORDER BY ORDINAL_POSITION";

            var columns = cnn.Query<string>(sql, param).ToList();

            lock (objLock)
            {
                if (!_columns.ContainsKey(key))
                {
                    _columns.Add(key, columns);
                }
            }

            return columns;
        }

        /// <summary>
        /// Thực hiện lưu dữ liệu: tự build câu lệnh Insert/Update theo ModelState
        /// </summary>
        /// <remarks>
        /// - Dùng transaction truyền vào (không tự open/close connection)
        /// - Chỉ thao tác các field tồn tại trong DB (lọc theo information_schema)
        /// </remarks>
        protected virtual bool DoSaveData(BaseModel model, IDbConnection cnn, IDbTransaction tran)
        {
            if (model == null)
            {
                throw new Exception("Model is null");
            }

            var tableName = model.GetViewOrTableName();
            var dbColumns = GetColumnByTableName(tableName, cnn);

            var props = model.GetType()
                .GetProperties()
                .Where(p => !p.GetCustomAttributes(typeof(NotMappedAttribute), false).Any())
                .Where(p => dbColumns.Any(c => string.Equals(c, p.Name, StringComparison.OrdinalIgnoreCase)))
                .ToList();

            var primaryKeyName = model.GetPrimaykeyField();
            var pkProp = !string.IsNullOrEmpty(primaryKeyName) ? props.FirstOrDefault(p => p.Name == primaryKeyName) : null;

            // Nếu Insert và PK là Guid rỗng thì set mới
            if (model.ModelState == ModelState.Insert && pkProp != null && pkProp.PropertyType == typeof(Guid))
            {
                var val = pkProp.GetValue(model);
                if (val == null || (Guid)val == Guid.Empty)
                {
                    pkProp.SetValue(model, Guid.NewGuid());
                }
            }

            string sql;
            int affected;

            // Build Dictionary<string, object> để truyền tham số cho Dapper
            var parameters = new Dictionary<string, object>();
            foreach (var prop in props)
            {
                var value = prop.GetValue(model);
                parameters[prop.Name] = value;
            }

            if (model.ModelState == ModelState.Insert)
            {
                var columns = string.Join(", ", props.Select(p => p.Name));
                var values = string.Join(", ", props.Select(p => $"@{p.Name}"));
                sql = $"INSERT INTO {tableName} ({columns}) VALUES ({values})";
                affected = cnn.Execute(sql, parameters, tran);
            }
            else if (model.ModelState == ModelState.Update)
            {
                if (string.IsNullOrEmpty(primaryKeyName) || pkProp == null)
                {
                    throw new Exception("Primary key not found for update.");
                }

                // Nếu BaseModel (hoặc model con) có thuộc tính UpdateColumns (List<string>)
                // thì chỉ update các cột nằm trong danh sách đó.
                IEnumerable<PropertyInfo> propsForUpdate;
                var updateColumnsProp = model.GetType().GetProperty("UpdateColumns");
                var updateColumns = updateColumnsProp != null
                    ? updateColumnsProp.GetValue(model) as List<string>
                    : null;

                if (updateColumns != null && updateColumns.Any())
                {
                    var updateColumnSet = new HashSet<string>(updateColumns, StringComparer.OrdinalIgnoreCase);

                    propsForUpdate = props
                        .Where(p => !string.Equals(p.Name, primaryKeyName, StringComparison.OrdinalIgnoreCase))
                        .Where(p => updateColumnSet.Contains(p.Name))
                        .Where(p => dbColumns.Any(c => string.Equals(c, p.Name, StringComparison.OrdinalIgnoreCase)));
                }
                else
                {
                    // Mặc định: update tất cả cột (trừ khóa chính), vẫn lọc theo dbColumns
                    propsForUpdate = props
                        .Where(p => !string.Equals(p.Name, primaryKeyName, StringComparison.OrdinalIgnoreCase))
                        .Where(p => dbColumns.Any(c => string.Equals(c, p.Name, StringComparison.OrdinalIgnoreCase)));
                }

                var setClause = string.Join(", ", propsForUpdate.Select(p => $"{p.Name} = @{p.Name}"));

                if (string.IsNullOrWhiteSpace(setClause))
                {
                    // Không có cột nào để update -> coi như thành công
                    return true;
                }

                sql = $"UPDATE {tableName} SET {setClause} WHERE {primaryKeyName} = @{primaryKeyName}";
                affected = cnn.Execute(sql, parameters, tran);
            }
            else
            {
                throw new Exception("Unsupported ModelState for SaveData.");
            }

            return affected > 0;
        }
        #endregion
    }
}
