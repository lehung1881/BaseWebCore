using Dapper;
using Npgsql;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using BaseWebCore.Common.Model;
using BaseWebCore.DLBase.PostgresSQL;
using BaseWebCore.Common.Utils;
using BaseWebCore.Common.Constant;
using BaseWebCore.Common.Enum;

namespace BaseWebCore.DLBase
{
    public abstract partial class DLBase<TModel> where TModel : BaseModel
    {
        #region Constructor and init 
        protected IPostgresServices _postgresServices;

        protected string cnnString = string.Empty;

        public DLBase()
        {
            cnnString = "User ID=postgres;Password=Lehung@181;Host=localhost;Port=5432;Database=db_employee;Pooling=true;";
            InitDL();
        }

        /// <summary>
        /// Init đối tượng thao tác với PostgresSQL
        /// </summary>
        protected virtual void InitDL()
        {
            _postgresServices = InitPostgresServices();
        }

        /// <summary>
        /// Khởi tạo lớp xử lý với DB
        /// </summary>
        /// <returns></returns>
        protected virtual IPostgresServices InitPostgresServices()
        {
            return new PostgresServices();
        }
        #endregion

        #region Methods base bussiness
        /// <summary>
        /// Lấy dữ liệu 1 bảng theo id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public TModel GetByID(Guid id)
        {
            var cnn = GetConnection();
            TModel result;
            try
            {
                OpenConnection(cnn);
                string tableName = ModelHelper.GetTableName<TModel>();
                string primaryKey = ModelHelper.GetPrimaryKeyFiled(typeof(TModel));
                result = GetByListID<TModel>(new List<Guid> { id }, tableName, primaryKey)[0];
            }
            finally
            {
                CloseConnection(cnn);
            }
            return result;
        }

        /// <summary>
        /// Lấy dữ liệu 1 bảng theo list id
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="ids"></param>
        /// <param name="tableName"></param>
        /// <param name="primaryKey"></param>
        /// <returns></returns>
        public List<T> GetByListID<T>(List<Guid> ids, string tableName, string primaryKey)
        {
            var script = new DBScriptHelper();
            script.script = $"select * from {tableName} where {primaryKey} = any(:ids);";
            script.AppendParam("ids", ids);
            var result = Query<T>(CommandType.Text, script.script, script.param);
            return result.ToList();
        }

        /// <summary>
        /// Thêm mới 1 bản ghi
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public bool InsertByState(TModel model)
        {
            return InsertData(model);
        }

        /// <summary>
        /// Xóa 1 bản ghi
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public bool Delete(TModel model)
        {
            var script = GenerateScriptDelete(model);
            return Execute(CommandType.Text, script.script, script.param);
        }

        /// <summary>
        /// Lấy phân trang
        /// </summary>
        /// <typeparam name="TModel"></typeparam>
        /// <param name="pageIndex"></param>
        /// <param name="pageSize"></param>
        /// <param name="filter"></param>
        /// <param name="sort"></param>
        /// <param name="view"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public PagingResponse GetPaging<T>(int pageIndex, int pageSize, List<FilterCondition> filters, int? viewName, string sort = "")
        {
            DBScriptHelper script = GetPagingScript<T>(pageIndex, pageSize, filters, viewName, sort);
            return DoGetPaging(script);
        }

        /// <summary>
        /// Thực hiện lấy dữ liệu
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="scriptPaging"></param>
        /// <returns></returns>
        public PagingResponse DoGetPaging(DBScriptHelper scriptPaging)
        {
            PagingResponse result = new PagingResponse();
            Dictionary<string, List<object>> data = QueryMultiple(CommandType.Text, scriptPaging);

            if (data.ContainsKey("PageData"))
            {
                result.PageData = data["PageData"];
            }

            if (data.ContainsKey("Total"))
            {
                result.Total = data["Total"].Cast<TotalData>().FirstOrDefault().Total;

            }

            return result;
        }

        /// <summary>
        /// Thực hiện tạo script lấy phân trang
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="pageIndex"></param>
        /// <param name="pageSize"></param>
        /// <param name="filters"></param>
        /// <param name="viewName"></param>
        /// <param name="sort"></param>
        /// <returns></returns>
        public DBScriptHelper GetPagingScript<T>(int pageIndex, int pageSize, List<FilterCondition> filters, int? viewName, string sort = "")
        {
            //Lấy view name
            string viewGetData = string.Empty;
            if (viewName != null)
            {
                var enumViewName = (EnumViewName)viewName;
                viewGetData = enumViewName.ToString();
            }
            else
            {
                viewGetData = ModelHelper.GetTableName<T>(false);
            }

            //Lấy tên schema
            string schemaName = ModelHelper.GetSchemaName(typeof(T));

            DBScriptHelper script = new DBScriptHelper();
            
            //Xử lý câu lệnh where
            string whereSql = "";
            List<string> wheres = new List<string>();
            foreach (var item in filters)
            {
                string columnName = item.property;
                string parameterDefault = $"p_{columnName}";
                string parameter = $":p_{columnName}";
                var condition = item.condition;

                List<EnumFilterCondition> listConditionsNoParam = new List<EnumFilterCondition> { EnumFilterCondition.Empty, EnumFilterCondition.NotEmpty };
                List<EnumFilterCondition> listLikeConditions = new List<EnumFilterCondition>{ EnumFilterCondition.Contain, EnumFilterCondition.NotEqual };
                if (listLikeConditions.Contains(condition))
                {
                    item.value = item.value.Replace(@"\", @"\\").Replace(@"%", @"\%").Replace(@"_", @"\_");
                }

                string conditionSql = GetConditionFilter(item);

                //Convert giá trị lọc từ string sang kiểu cụ thể
                object value = ConvertUtil.ConvertStringToDataType(item.data_type, item.value);

                //chuyển đổi một số điều kiện
                switch (condition)
                {
                    case EnumFilterCondition.Contain:
                    case EnumFilterCondition.NotContain:
                        value = $"%{value}%";
                        break;
                }

                //Ép lại kiểu dữ liệu
                switch (item.data_type)
                {
                    case EnumDataType.Date:
                        parameter = $"({parameter})::date";
                        columnName = $"({columnName})::date";
                        break;
                    case EnumDataType.Text:
                        if (condition == EnumFilterCondition.Empty)
                        {
                            conditionSql = $"{conditionSql} or {columnName} = :string_empty";
                        } 
                        else if (condition == EnumFilterCondition.NotEmpty)
                        {
                            conditionSql = $"{conditionSql} and {columnName} <> :string_empty";
                        }
                        break;
                }

               
                string where = $@"{columnName} {conditionSql}";
               
                if (!listConditionsNoParam.Contains(item.condition))
                {
                    script.AppendParam(parameterDefault, value);
                    where = $@"{where} {parameter}";
                }
                else
                {
                    script.AppendParam("string_empty", "");
                }

                wheres.Add(where);
            }
            //Build câu where
            if(wheres.Count > 0)
            {
                whereSql = $"where {string.Join(" AND ", wheres)}";
            }

            //Xử lý sắp xếp
            string sortSql = GetSortScript(sort);

            //Lấy script phân trang
            string pagingSql = GetPagingScript(pageIndex, pageSize);

            //Build ra câu truy vấn cuối cùng
            string scriptData = $"select * from {schemaName}.{viewGetData} {whereSql} {sortSql} {pagingSql}";
            string scriptTotal = $"select count(1) as Total from {schemaName}.{viewGetData} {whereSql};";
            script.AppendScript(scriptData);
            script.AppendScript(scriptTotal);
            script.AppendType("PageData", typeof(T));
            script.AppendType("Total", typeof(TotalData));

            return script;
        }
        #endregion

        #region Methods base
        protected bool Execute(CommandType? commandType, string sql, object param = null, IDbTransaction transaction = null, int? commandTimeout = null)
        {
            var cnn = GetConnection();
            try
            {
                OpenConnection(cnn);
                var result = Execute(cnn, sql, param, transaction, commandTimeout, commandType);
            }
            finally
            {
                CloseConnection(cnn);
            }
            return true;
        }

        /// <summary>
        /// Thực thì câu lệnh trả về 1 giá trị
        /// </summary>
        /// <param name="commandType"></param>
        /// <param name="sql"></param>
        /// <param name="param"></param>
        /// <param name="transaction"></param>
        /// <param name="commandTimeout"></param>
        /// <returns></returns>
        protected object ExecuteScalar(CommandType commandType, string sql, object param = null, IDbTransaction transaction = null, int? commandTimeout = null)
        {
            var cnn = GetConnection();
            try
            {
                OpenConnection(cnn);
                return _postgresServices.ExecuteScalar(cnn, sql, param, transaction, commandTimeout, commandType);
            }
            finally
            {
                CloseConnection(cnn);
            }
        }

        /// <summary>
        /// Thực thi câu lệnh
        /// </summary>
        /// <param name="cnn"></param>
        /// <param name="sql"></param>
        /// <param name="param"></param>
        /// <param name="transaction"></param>
        /// <param name="commandTimeout"></param>
        /// <param name="commandType"></param>
        /// <returns></returns>
        protected int Execute(IDbConnection cnn, string sql, object param = null, IDbTransaction transaction = null, int? commandTimeout = null, CommandType? commandType = null)
        {
            return _postgresServices.Execute(cnn, sql, param, transaction, commandTimeout, commandType);
        }

        /// <summary>
        /// Query lấy dữ liệu có thể custom cnn
        /// </summary>
        /// <param name="commandType"></param>
        /// <param name="cnn"></param>
        /// <param name="transaction"></param>
        /// <param name="sql"></param>
        /// <param name="param"></param>
        /// <param name="commandTimeout"></param>
        /// <returns></returns>
        protected List<T> Query<T>(CommandType commandType, IDbConnection cnn, IDbTransaction transaction, string sql, object param = null, int? commandTimeout = Constants.TimeoutNone)
        {
            try
            {
                List<T> result = new List<T>();
                if (!string.IsNullOrEmpty(sql))
                {
                    result = _postgresServices.Query<T>(cnn, sql, param, transaction, commandTimeout: commandTimeout, commandType: commandType);
                }
                return result;
            }
            finally
            {

            }
        }

        /// <summary>
        /// Query lấy dữ liệu có thể custom cnn
        /// </summary>
        /// <param name="commandType"></param>
        /// <param name="cnn"></param>
        /// <param name="transaction"></param>
        /// <param name="sql"></param>
        /// <param name="param"></param>
        /// <param name="commandTimeout"></param>
        /// <returns></returns>
        protected IEnumerable<dynamic> QueryDynamic(CommandType commandType, IDbConnection cnn, IDbTransaction transaction, string sql, object param = null, int commandTimeout = Constants.TimeoutNone)
        {
            try
            {
                IEnumerable<dynamic> result = new List<dynamic>();
                if (!string.IsNullOrEmpty(sql))
                {
                    result = _postgresServices.Query(cnn, sql, param, transaction, commandTimeout: commandTimeout, commandType: commandType);
                }
                return result;
            }
            finally { }
        }

        /// <summary>
        /// Query lấy dữ liệu với DB mặc định
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="commandType"></param>
        /// <param name="sql"></param>
        /// <param name="param"></param>
        /// <param name="commandTimeout"></param>
        /// <returns></returns>
        protected List<T> Query<T>(CommandType commandType, string sql, object param, int? commandTimeout = null)
        {
            List<T> result = new List<T>();
            if (!string.IsNullOrEmpty(sql))
            {
                var cnn = GetConnection();
                try
                {
                    OpenConnection(cnn);
                    result = Query<T>(commandType, cnn, null, sql, param, commandTimeout);
                }
                finally
                {
                    CloseConnection(cnn);
                }
            }
            return result;
        }

        protected Dictionary<string, List<object>> QueryMultiple(CommandType commandType, DBScriptHelper script, int? commandTimeout = null)
        {
            Dictionary<string, List<object>> result = new Dictionary<string, List<object>>();
            if (!string.IsNullOrEmpty(script.script))
            {
                var cnn = GetConnection();
                try
                {
                    OpenConnection(cnn);
                    result = QueryMultipleCore(cnn, commandType, script.script, script.param, script.types, commandTimeout);
                }
                finally
                {
                    CloseConnection(cnn);
                }
            }
            return result;
        }

        private Dictionary<string, List<object>> QueryMultipleCore(IDbConnection cnn, CommandType commandType, string sql, object param, Dictionary<string, Type> types, int? commandTimeout = null)
        {
            Dictionary<string, List<object>> result = new Dictionary<string, List<object>>();

            using (var multi = _postgresServices.QueryMultiple(cnn, sql, param, null, commandTimeout, commandType))
            {
                DoQueryMultiple(types, result, multi);
            }

            return result;
        }

        private void DoQueryMultiple(Dictionary<string, Type> types, Dictionary<string, List<object>> result, SqlMapper.GridReader multi)
        {
            if (multi != null)
            {
                int index = 0;
                do
                {
                    string typeKey = types.Keys.ToList()[index];
                    var data = multi.Read(types[typeKey]).ToList();
                    result.Add(typeKey, data);
                    index++;
                }
                while (!multi.IsConsumed && index < types.Count);
            }
        }

        /// <summary>
        /// Query lấy dữ liệu với DB mặc định
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="commandType"></param>
        /// <param name="sql"></param>
        /// <param name="param"></param>
        /// <param name="commandTimeout"></param>
        /// <returns></returns>
        protected IEnumerable<dynamic> QueryDynamic(CommandType commandType, string sql, object param, int commandTimeout = Constants.TimeoutNone)
        {
            IEnumerable<dynamic> result = new List<dynamic>();
            if (!string.IsNullOrEmpty(sql))
            {
                var cnn = GetConnection();
                try
                {
                    OpenConnection(cnn);
                    result = QueryDynamic(commandType, cnn, null, sql, param, commandTimeout);
                }
                finally
                {
                    CloseConnection(cnn);
                }
            }
            return result;
        }

        /// <summary>
        /// Hàm đóng kết nối đến DB
        /// </summary>
        /// <param name="cnn"></param>
        protected void CloseConnection(IDbConnection cnn)
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

        /// <summary>
        /// Hàm mở kết nối đến DB
        /// </summary>
        /// <param name="cnn"></param>
        public void OpenConnection(IDbConnection cnn)
        {
            if (cnn.State != ConnectionState.Open)
            {
                cnn.Open();
            }
        }

        /// <summary>
        /// Fix tạm chuỗi kết nối
        /// </summary>
        /// <returns></returns>
        public IDbConnection GetConnection()
        {
            return _postgresServices.GetConnection(cnnString);
        }

        /// <summary>
        /// Lấy ra script phân trang
        /// </summary>
        /// <param name="pageIndex"></param>
        /// <param name="pageSize"></param>
        /// <returns></returns>
        protected string GetPagingScript(int pageIndex, int pageSize)
        {
            //return $" OFFSET {(pageIndex - 1) * pageSize} ROWS FETCH NEXT {pageSize} ROWS ONLY";
            return $" LIMIT {pageSize} OFFSET {(pageIndex - 1) * pageSize}";
        }

        protected string GetConditionFilter(FilterCondition item)
        {
            switch (item.condition)
            {
                case EnumFilterCondition.Equal:
                    return "=";
                case EnumFilterCondition.NotEqual:
                    return "<>";
                case EnumFilterCondition.Empty:
                    return $"is null";
                case EnumFilterCondition.NotEmpty:
                    return "is not null";
                case EnumFilterCondition.Contain:
                    return "ilike";
                case EnumFilterCondition.NotContain:
                    return "not ilike";
                case EnumFilterCondition.GreaterThan:
                    return "<";
                case EnumFilterCondition.LessThan:
                    return ">";
                case EnumFilterCondition.GreaterThanEqual:
                    return "<=";
                case EnumFilterCondition.LessThanEqual:
                    return ">=";
            }
            return "";
        }

        /// <summary>
        /// Lấy ra script sắp xếp
        /// </summary>
        /// <param name="sorts"></param>
        /// <returns></returns>
        public string GetSortScript(List<Sort> sorts)
        {
            if (sorts?.Count == 0)
            {
                return " order by created_date desc";
            }

            List<string> subSort = new List<string>();

            foreach (var item in sorts)
            {
                string sortType = item.desc ? "desc" : "asc";
                subSort.Add(sortType);
            }

            string suffix = string.Join(", ", subSort);
            return $" order by created_date desc, {suffix}";
        }

        /// <summary>
        /// Lấy ra script sắp xếp
        /// </summary>
        /// <param name="sortString"></param>
        /// <returns></returns>
        public string GetSortScript(string sortString)
        {
            List<Sort> sorts = sortString != null ? ConvertUtil.DeserriallizeObject<List<Sort>>(sortString) : new List<Sort>();
            return GetSortScript(sorts);
        }

        /// <summary>
        /// Insert bản ghi vào DB
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="model"></param>
        /// <returns></returns>
        public bool InsertData(TModel model)
        {
            var cnn = GetConnection();
            bool result = false;
            try
            {
                OpenConnection(cnn);
                result = DoInsertData(cnn, model);
            }
            finally
            {
                CloseConnection(cnn);
            }
            return result;
        }

        /// <summary>
        /// Thực hiện Insert bản ghi vào db
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="cnn"></param>
        /// <param name="model"></param>
        /// <returns></returns>
        public bool DoInsertData(IDbConnection cnn, TModel model)
        {
            string tableName = ModelHelper.GetTableName<TModel>();
            string primaryKey = ModelHelper.GetPrimaryKeyFiled(typeof(TModel));

            var script = new DBScriptHelper();
            if (model.model_state == ModelState.Insert)
            {
                script = GenerateScriptInsert(model, primaryKey, tableName);
            }
            else
            {
                script = GenerateScriptUpdate(model, primaryKey, tableName);
            }
            return Execute(cnn, script.script, script.param) > 0;
        }

        public DBScriptHelper GenerateScriptDelete(TModel model)
        {
            string tableName = ModelHelper.GetTableName<TModel>();
            string primaryKey = ModelHelper.GetPrimaryKeyFiled(typeof(TModel));
            var scripts = new DBScriptHelper();
            scripts.script = $"delete from {tableName} where {primaryKey} = :p_id";
            var id = model.GetPrimaryKeyValue();
            scripts.AppendParam("p_id", id);
            return scripts;
        }

        /// <summary>
        /// Tạo câu insert để thêm dữ liệu vào db
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="model"></param>
        /// <param name="primaryKey"></param>
        /// <param name="tableName"></param>
        /// <returns></returns>
        public DBScriptHelper GenerateScriptInsert(TModel model, string primaryKey, string tableName)
        {
            var fileds = ModelHelper.GetFieldInsert(model.GetType());

            List<string> filedColumns = new List<string>();
            List<string> filedColParams = new List<string>();

            var scripts = new DBScriptHelper();

            foreach (KeyValuePair<string, PropertyInfo> cols in fileds)
            {
                filedColumns.Add(cols.Key);
                filedColParams.Add($":p_{cols.Key}");

                object value = cols.Value.GetValue(model);

                if (cols.Key == primaryKey && ((Guid)value == Guid.Empty || value == null))
                {
                    
                    value = Guid.NewGuid();
                } 
                else if(cols.Key == "created_date" || cols.Key == "modified_date")
                {
                    value = DateTime.Now;
                }
                else if (cols.Key == "created_by" || cols.Key == "modified_by")
                {
                    value = "Hệ thống";
                }
                scripts.AppendParam($"p_{cols.Key}", value);
            }

            scripts.script = $@"insert into {tableName} ({string.Join(", ", filedColumns)}) values({string.Join(", ", filedColParams)});";

            return scripts;
        }

        /// <summary>
        /// Tạo câu lệnh cập nhật dữ liệu
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="model"></param>
        /// <param name="primaryKey"></param>
        /// <param name="tableName"></param>
        /// <returns></returns>
        public DBScriptHelper GenerateScriptUpdate(TModel model, string primaryKey, string tableName, List<string> columnsNotUpdate = null)
        {
            var fileds = ModelHelper.GetFieldInsert(model.GetType());
            List<string> updateColumns = new List<string>();

            var scripts = new DBScriptHelper();

            foreach (KeyValuePair<string, PropertyInfo> cols in fileds)
            {
                string columnName = cols.Key;

                //Loại bỏ những cột không cần update
                if (columnsNotUpdate != null && columnsNotUpdate.Count > 0 && columnName != primaryKey)
                {
                    if (columnsNotUpdate.Contains(columnName)) continue;
                }

                object value = cols.Value.GetValue(model);

                //Không cập nhật những trường null
                if (value == null || columnName == "created_date" || columnName == "created_by") continue;

                if (columnName == "modified_date")
                {
                    value = DateTime.Now;
                }

                if (columnName != primaryKey)
                {
                    updateColumns.Add($"{columnName} = :p_{columnName}");
                    scripts.AppendParam($"p_{columnName}", value);
                }
                else
                {
                    scripts.AppendParam($"p_{primaryKey}", value);
                }
            }

            scripts.script = $@"update {tableName} a set {string.Join(", ", updateColumns)} where {primaryKey} = :p_{primaryKey};";
            return scripts;
        }
        #endregion
    }
}
