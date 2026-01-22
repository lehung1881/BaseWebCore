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
    public partial class BaseBL
    {
        #region Batch Operations (SaveListData)

        /// <summary>
        /// Lưu dữ liệu (Insert / Update / Delete) theo ModelState
        /// </summary>
        public virtual async Task<ServiceResponse> SaveDataAsync(BaseModel model)
        {
            var res = new ServiceResponse();
            IDbTransaction tran = null;
            IDbConnection cnn = null;

            try
            {
                // Bước 1: Validate đầu vào
                if (model == null)
                {
                    res.OnError(ServiceResponseCode.InvalidData);
                    return res;
                }

                // Bước 2: Validate nghiệp vụ
                var validateResults = ValidateBeforeSaveData(model);
                if (validateResults != null && validateResults.Any())
                {
                    res.Success = false;
                    res.ValidateInfo = validateResults;
                    return res;
                }

                // Bước 3: Hook trước khi lưu (async)
                await BeforeSaveData(model);

                // Bước 4: Mở connection
                cnn = await GetConnectionAsync();
                if (cnn.State != ConnectionState.Open)
                    cnn.Open();

                // Bước 5: Bắt đầu transaction
                tran = cnn.BeginTransaction();

                // Bước 6: Thực hiện lưu dữ liệu (async)
                var success = await DoSaveData(model, cnn, tran);

                if (!success)
                {
                    tran.Rollback();
                    res.OnError(ServiceResponseCode.Exception, "SaveData failed");
                    return res;
                }

                // Bước 7: Commit transaction
                tran.Commit();
                res.OnSuccess();

                // Bước 8: Hook sau khi lưu (async)
                await AfterSaveData(model, success);
            }
            catch (Exception ex)
            {
                tran?.Rollback();
                res.OnError(ServiceResponseCode.Exception, ex.Message);

                // Gọi AfterSaveData với isSuccess = false
                await AfterSaveData(model, false);
            }
            finally
            {
                // Đóng connection
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
        /// Thực hiện Insert / Update / Delete theo ModelState (MySQL) - Async version
        /// </summary>
        protected virtual async Task<bool> DoSaveData(BaseModel model, IDbConnection cnn, IDbTransaction tran)
        {
            if (model == null)
                throw new ArgumentNullException(nameof(model));

            var tableName = model.GetViewOrTableName();
            var primaryKeyName = model.GetPrimaykeyField();

            if (string.IsNullOrEmpty(primaryKeyName))
                throw new InvalidOperationException($"Primary key not defined for table '{tableName}'");

            // Lấy danh sách cột từ DB (đã có cache)
            var dbColumns = await GetColumnByTableNameAsync(tableName, cnn);
            var dbColumnSet = new HashSet<string>(dbColumns, StringComparer.OrdinalIgnoreCase);

            // Lấy properties có thể map
            var props = GetMappableProperties(model.GetType(), dbColumnSet);

            var pkProp = props.FirstOrDefault(p =>
                string.Equals(p.Name, primaryKeyName, StringComparison.OrdinalIgnoreCase));

            if (pkProp == null)
                throw new InvalidOperationException($"Primary key '{primaryKeyName}' not found in model properties");

            // Xử lý theo ModelState với async
            return model.ModelState switch
            {
                ModelState.Insert => await ExecuteInsert(model, tableName, props, pkProp, cnn, tran),
                ModelState.Update => await ExecuteUpdate(model, tableName, props, pkProp, primaryKeyName, cnn, tran),
                ModelState.Delete => await ExecuteDelete(model, tableName, pkProp, primaryKeyName, cnn, tran),
                _ => throw new InvalidOperationException($"Unsupported ModelState: {model.ModelState}")
            };
        }

        /// <summary>
        /// Thực thi câu lệnh INSERT - Async version
        /// </summary>
        private static async Task<bool> ExecuteInsert(
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

            // Execute với ExecuteAsync
            var parameters = BuildParameters(props, model);
            var affected = await cnn.ExecuteAsync(sql, parameters, tran);

            return affected > 0;
        }

        /// <summary>
        /// Thực thi câu lệnh UPDATE - Async version
        /// </summary>
        private static async Task<bool> ExecuteUpdate(
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

            // Execute với ExecuteAsync
            var parameters = BuildParameters(props, model);
            var affected = await cnn.ExecuteAsync(sql, parameters, tran);

            return affected >= 0; // MySQL: affected = 0 vẫn OK
        }

        /// <summary>
        /// Thực thi câu lệnh DELETE (Hard Delete) - Async version
        /// </summary>
        private static async Task<bool> ExecuteDelete(
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

            // Execute với ExecuteAsync
            var affected = await cnn.ExecuteAsync(sql, parameters, tran);

            return affected > 0;
        }

        /// <summary>
        /// Lưu danh sách dữ liệu với Batch Processing (không đệ quy)
        /// </summary>
        /// <param name="models">Danh sách model cần lưu</param>
        /// <param name="batchSize">Số lượng bản ghi mỗi batch (mặc định 1000)</param>
        /// <returns>ServiceResponse chứa kết quả và thông tin lỗi (nếu có)</returns>
        public virtual async Task<ServiceResponse> SaveListDataAsync(List<BaseModel> models, int batchSize = 1000)
        {
            var res = new ServiceResponse();

            try
            {
                // Bước 1: Validate đầu vào
                if (models == null || !models.Any())
                {
                    res.OnError(ServiceResponseCode.InvalidData, "Danh sách dữ liệu không được rỗng");
                    return res;
                }

                if (batchSize <= 0)
                {
                    res.OnError(ServiceResponseCode.InvalidData, "Batch size phải lớn hơn 0");
                    return res;
                }

                // Bước 2: Validate tất cả models
                var validationErrors = ValidateListData(models);
                if (validationErrors != null && validationErrors.Any())
                {
                    res.Success = false;
                    res.ValidateInfo = validationErrors;
                    return res;
                }

                // Bước 3: Chia thành các batch
                var totalCount = models.Count;
                var totalBatches = (int)Math.Ceiling((double)totalCount / batchSize);
                var successCount = 0;
                var failedBatches = new List<string>();

                // Bước 4: Xử lý từng batch tuần tự
                for (int batchIndex = 0; batchIndex < totalBatches; batchIndex++)
                {
                    var startIndex = batchIndex * batchSize;
                    var currentBatchSize = Math.Min(batchSize, totalCount - startIndex);
                    var batch = models.GetRange(startIndex, currentBatchSize);

                    // Xử lý batch hiện tại với await
                    var batchResult = await ProcessSingleBatch(batch, batchIndex + 1);

                    if (batchResult.Success)
                    {
                        successCount += currentBatchSize;
                    }
                    else
                    {
                        failedBatches.Add($"Batch {batchIndex + 1}: {batchResult.Message}");
                    }
                }

                // Bước 5: Tổng hợp kết quả
                if (failedBatches.Any())
                {
                    res.OnError(ServiceResponseCode.Exception,
                        $"Lưu thành công {successCount}/{totalCount} bản ghi. Lỗi: {string.Join("; ", failedBatches)}");
                }
                else
                {
                    res.OnSuccess($"Lưu thành công {successCount}/{totalCount} bản ghi");
                }
            }
            catch (Exception ex)
            {
                res.OnError(ServiceResponseCode.Exception, $"Lỗi hệ thống: {ex.Message}");
            }

            return res;
        }

        /// <summary>
        /// Xử lý một batch dữ liệu (Insert/Update/Delete)
        /// </summary>
        /// <param name="batch">Danh sách model trong batch</param>
        /// <param name="batchNumber">Số thứ tự batch (để log)</param>
        /// <returns>ServiceResponse của batch</returns>
        private async Task<ServiceResponse> ProcessSingleBatch(List<BaseModel> batch, int batchNumber)
        {
            var res = new ServiceResponse();
            IDbConnection cnn = null;
            IDbTransaction tran = null;

            try
            {
                // Bước 1: Mở connection
                cnn = await GetConnectionAsync();
                if (cnn.State != ConnectionState.Open)
                    cnn.Open();

                // Bước 2: Bắt đầu transaction
                tran = cnn.BeginTransaction();

                // Bước 3: Nhóm models theo ModelState
                var insertModels = batch.Where(m => m.ModelState == ModelState.Insert).ToList();
                var updateModels = batch.Where(m => m.ModelState == ModelState.Update).ToList();
                var deleteModels = batch.Where(m => m.ModelState == ModelState.Delete).ToList();

                var batchSuccessCount = 0;

                // Bước 4: Xử lý Insert với await
                if (insertModels.Any())
                {
                    var insertCount = await ExecuteBatchInsert(insertModels, cnn, tran);
                    batchSuccessCount += insertCount;
                }

                // Bước 5: Xử lý Update với await
                if (updateModels.Any())
                {
                    var updateCount = await ExecuteBatchUpdate(updateModels, cnn, tran);
                    batchSuccessCount += updateCount;
                }

                // Bước 6: Xử lý Delete với await
                if (deleteModels.Any())
                {
                    var deleteCount = await ExecuteBatchDelete(deleteModels, cnn, tran);
                    batchSuccessCount += deleteCount;
                }

                // Bước 7: Commit transaction
                tran.Commit();
                res.OnSuccess($"Batch {batchNumber}: Lưu thành công {batchSuccessCount}/{batch.Count} bản ghi");

                // Bước 8: Gọi AfterSaveData với await
                await AfterSaveListData(batch, true);
            }
            catch (Exception ex)
            {
                // Rollback nếu có lỗi
                tran?.Rollback();
                res.OnError(ServiceResponseCode.Exception, $"Batch {batchNumber}: {ex.Message}");

                // Gọi AfterSaveData với isSuccess = false
                await AfterSaveListData(batch, false);
            }
            finally
            {
                // Đóng connection
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
        /// Validate danh sách dữ liệu
        /// </summary>
        public virtual List<ValidateResult> ValidateListData(List<BaseModel> models)
        {
            var validateResults = new List<ValidateResult>();

            for (int i = 0; i < models.Count; i++)
            {
                var model = models[i];
                var results = ValidateBeforeSaveData(model);
                if (results != null && results.Any())
                {
                    validateResults.AddRange(results);
                }
            }

            return validateResults;
        }

        /// <summary>
        /// Thực thi Batch Insert - chèn nhiều bản ghi trong một câu lệnh SQL
        /// </summary>
        private async Task<int> ExecuteBatchInsert(List<BaseModel> models, IDbConnection cnn, IDbTransaction tran)
        {
            if (!models.Any())
                return 0;

            // Lấy thông tin từ model đầu tiên
            var firstModel = models.First();
            var tableName = firstModel.GetViewOrTableName();
            var primaryKeyName = firstModel.GetPrimaykeyField();

            // Lấy danh sách cột
            var dbColumns = await GetColumnByTableNameAsync(tableName, cnn);
            var dbColumnSet = new HashSet<string>(dbColumns, StringComparer.OrdinalIgnoreCase);
            var props = GetMappableProperties(firstModel.GetType(), dbColumnSet);

            var pkProp = props.FirstOrDefault(p =>
                string.Equals(p.Name, primaryKeyName, StringComparison.OrdinalIgnoreCase));

            // Gọi BeforeSaveData và sinh PK cho tất cả models
            foreach (var model in models)
            {
                if (pkProp != null)
                {
                    EnsurePrimaryKey(model, pkProp);
                }
            }

            // Xử lý trước khi lưu dữ liệu với await
            await BeforeSaveListData(models);

            // Build SQL với nhiều VALUES
            var columns = string.Join(", ", props.Select(p => $"`{p.Name}`"));
            var valueRows = new List<string>();
            var parameters = new DynamicParameters();

            for (int i = 0; i < models.Count; i++)
            {
                var model = models[i];
                var valueParams = new List<string>();

                foreach (var prop in props)
                {
                    var paramName = $"{prop.Name}_{i}";
                    valueParams.Add($"@{paramName}");
                    parameters.Add(paramName, prop.GetValue(model) ?? DBNull.Value);
                }

                valueRows.Add($"({string.Join(", ", valueParams)})");
            }

            var sql = $"INSERT INTO `{tableName}` ({columns}) VALUES {string.Join(", ", valueRows)}";

            // Execute với ExecuteAsync
            var affected = await cnn.ExecuteAsync(sql, parameters, tran);
            return affected;
        }

        /// <summary>
        /// Thực thi Batch Update - cập nhật nhiều bản ghi bằng CASE WHEN
        /// </summary>
        private async Task<int> ExecuteBatchUpdate(List<BaseModel> models, IDbConnection cnn, IDbTransaction tran)
        {
            if (!models.Any())
                return 0;

            var firstModel = models.First();
            var tableName = firstModel.GetViewOrTableName();
            var primaryKeyName = firstModel.GetPrimaykeyField();

            var dbColumns = await GetColumnByTableNameAsync(tableName, cnn);
            var dbColumnSet = new HashSet<string>(dbColumns, StringComparer.OrdinalIgnoreCase);
            var props = GetMappableProperties(firstModel.GetType(), dbColumnSet);

            var pkProp = props.FirstOrDefault(p =>
                string.Equals(p.Name, primaryKeyName, StringComparison.OrdinalIgnoreCase));

            if (pkProp == null)
                throw new InvalidOperationException($"Primary key '{primaryKeyName}' not found");

            // Gọi BeforeSaveListData với await
            await BeforeSaveListData(models);

            // Lấy danh sách cột cần update (loại bỏ PK)
            var updateProps = GetUpdateProperties(props, firstModel.UpdateColumns, primaryKeyName).ToList();

            if (!updateProps.Any())
                return models.Count; // Không có gì để update

            // Build SQL với CASE WHEN
            var setClauses = new List<string>();
            var parameters = new DynamicParameters();
            var pkValues = new List<object>();

            foreach (var prop in updateProps)
            {
                var caseParts = new List<string>();

                for (int i = 0; i < models.Count; i++)
                {
                    var model = models[i];
                    var pkValue = pkProp.GetValue(model);
                    var propValue = prop.GetValue(model);

                    var pkParamName = $"pk_{i}";
                    var valueParamName = $"{prop.Name}_{i}";

                    caseParts.Add($"WHEN `{primaryKeyName}` = @{pkParamName} THEN @{valueParamName}");

                    parameters.Add(pkParamName, pkValue);
                    parameters.Add(valueParamName, propValue ?? DBNull.Value);

                    if (prop == updateProps.First())
                    {
                        pkValues.Add(pkValue);
                    }
                }

                var caseStatement = $"`{prop.Name}` = CASE {string.Join(" ", caseParts)} ELSE `{prop.Name}` END";
                setClauses.Add(caseStatement);
            }

            // Build WHERE IN clause
            var whereInParams = string.Join(", ", pkValues.Select((_, i) => $"@pk_{i}"));
            var sql = $"UPDATE `{tableName}` SET {string.Join(", ", setClauses)} WHERE `{primaryKeyName}` IN ({whereInParams})";

            // Execute với ExecuteAsync
            var affected = await cnn.ExecuteAsync(sql, parameters, tran);
            return affected;
        }

        /// <summary>
        /// Thực thi Batch Delete - xóa nhiều bản ghi bằng WHERE IN
        /// </summary>
        private async Task<int> ExecuteBatchDelete(List<BaseModel> models, IDbConnection cnn, IDbTransaction tran)
        {
            if (!models.Any())
                return 0;

            var firstModel = models.First();
            var tableName = firstModel.GetViewOrTableName();
            var primaryKeyName = firstModel.GetPrimaykeyField();

            var dbColumns = await GetColumnByTableNameAsync(tableName, cnn);
            var dbColumnSet = new HashSet<string>(dbColumns, StringComparer.OrdinalIgnoreCase);
            var props = GetMappableProperties(firstModel.GetType(), dbColumnSet);

            var pkProp = props.FirstOrDefault(p =>
                string.Equals(p.Name, primaryKeyName, StringComparison.OrdinalIgnoreCase));

            if (pkProp == null)
                throw new InvalidOperationException($"Primary key '{primaryKeyName}' not found");

            // Gọi BeforeSaveListData với await
            await BeforeSaveListData(models);

            // Lấy danh sách PK values
            var pkValues = new List<object>();
            var parameters = new DynamicParameters();

            for (int i = 0; i < models.Count; i++)
            {
                var model = models[i];
                var pkValue = pkProp.GetValue(model);

                if (pkValue == null || (pkProp.PropertyType == typeof(Guid) && (Guid)pkValue == Guid.Empty))
                {
                    throw new InvalidOperationException($"Primary key must have valid value at index {i}");
                }

                var paramName = $"pk_{i}";
                parameters.Add(paramName, pkValue);
                pkValues.Add($"@{paramName}");
            }

            // Build SQL
            var whereInClause = string.Join(", ", pkValues);
            var sql = $"DELETE FROM `{tableName}` WHERE `{primaryKeyName}` IN ({whereInClause})";

            // Execute với ExecuteAsync
            var affected = await cnn.ExecuteAsync(sql, parameters, tran);
            return affected;
        }

        #endregion

        #region UpdateByField Async

        /// <summary>
        /// Cập nhật dữ liệu dựa trên một trường cụ thể (không nhất thiết là Primary Key) - Async version
        /// </summary>
        /// <param name="request">Request chứa thông tin cập nhật</param>
        /// <returns>ServiceResponse chứa kết quả và số bản ghi bị ảnh hưởng</returns>
        public virtual async Task<ServiceResponse> UpdateByFieldAsync(UpdateByFieldRequest request)
        {
            var res = new ServiceResponse();
            IDbConnection cnn = null;
            IDbTransaction tran = null;

            try
            {
                // Bước 1: Validate đầu vào
                if (request == null)
                {
                    return res;
                }

                if (request.Model == null)
                {
                    return res;
                }

                if (string.IsNullOrWhiteSpace(request.ConditionField))
                {
                    res.OnError(ServiceResponseCode.InvalidData, "Tên trường điều kiện không được rỗng");
                    return res;
                }

                if (request.ConditionValue == null)
                {
                    res.OnError(ServiceResponseCode.InvalidData, "Giá trị điều kiện không được null");
                    return res;
                }

                // Bước 2: Mở connection và transaction
                cnn = await GetConnectionAsync();
                if (cnn.State != ConnectionState.Open)
                    cnn.Open();

                tran = cnn.BeginTransaction();

                // Bước 3: Thực hiện update
                var affectedRows = await DoUpdateByFieldAsync(
                    request.Model,
                    request.ConditionField,
                    request.ConditionValue,
                    request.UpdateFields,
                    cnn,
                    tran);

                // Bước 4: Kiểm tra kết quả
                if (affectedRows == 0)
                {
                    tran.Rollback();
                    res.OnError(ServiceResponseCode.NotFound,
                        $"Không tìm thấy bản ghi nào với điều kiện {request.ConditionField} = {request.ConditionValue}");
                    return res;
                }

                // Bước 5: Commit transaction
                tran.Commit();
                res.OnSuccess($"Cập nhật thành công {affectedRows} bản ghi");
                res.Data = affectedRows;
            }
            catch (InvalidOperationException ex)
            {
                tran?.Rollback();
                res.OnError(ServiceResponseCode.InvalidData, ex.Message);
            }
            catch (Exception ex)
            {
                tran?.Rollback();
                res.OnError(ServiceResponseCode.Exception, $"Lỗi khi cập nhật dữ liệu: {ex.Message}");
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
        /// Thực thi câu lệnh UPDATE dựa trên trường tùy chỉnh - Async version
        /// </summary>
        /// <param name="model">Model chứa dữ liệu</param>
        /// <param name="conditionField">Tên trường điều kiện</param>
        /// <param name="conditionValue">Giá trị điều kiện</param>
        /// <param name="updateFields">Danh sách trường cần update</param>
        /// <param name="cnn">Database connection</param>
        /// <param name="tran">Transaction</param>
        /// <returns>Số bản ghi bị ảnh hưởng</returns>
        private async Task<int> DoUpdateByFieldAsync(
            BaseModel model,
            string conditionField,
            object conditionValue,
            List<string> updateFields,
            IDbConnection cnn,
            IDbTransaction tran)
        {
            // Lấy thông tin bảng
            var tableName = model.GetViewOrTableName();
            var primaryKeyName = model.GetPrimaykeyField();

            // Lấy danh sách cột từ DB
            var dbColumns = await GetColumnByTableNameAsync(tableName, cnn);
            var dbColumnSet = new HashSet<string>(dbColumns, StringComparer.OrdinalIgnoreCase);

            // Kiểm tra trường điều kiện có tồn tại không
            if (!dbColumnSet.Contains(conditionField))
            {
                throw new InvalidOperationException($"Trường '{conditionField}' không tồn tại trong bảng '{tableName}'");
            }

            // Lấy properties có thể map
            var props = GetMappableProperties(model.GetType(), dbColumnSet);

            // Kiểm tra property điều kiện
            var conditionProp = props.FirstOrDefault(p =>
                string.Equals(p.Name, conditionField, StringComparison.OrdinalIgnoreCase));

            if (conditionProp == null)
            {
                throw new InvalidOperationException($"Property '{conditionField}' không tồn tại trong model");
            }

            // Xác định các trường cần update
            IEnumerable<PropertyInfo> propsToUpdate;

            // Nếu UpdateFields null hoặc rỗng → cập nhật tất cả trường (trừ PK và trường điều kiện)
            if (updateFields == null || !updateFields.Any())
            {
                propsToUpdate = props
                    .Where(p => !string.Equals(p.Name, primaryKeyName, StringComparison.OrdinalIgnoreCase))
                    .Where(p => !string.Equals(p.Name, conditionField, StringComparison.OrdinalIgnoreCase));
            }
            else
            {
                // Update chỉ các trường được chỉ định
                var updateSet = new HashSet<string>(updateFields, StringComparer.OrdinalIgnoreCase);
                propsToUpdate = props.Where(p => updateSet.Contains(p.Name));

                // Kiểm tra tất cả các trường được chỉ định có tồn tại không
                var missingFields = updateFields
                    .Where(f => !props.Any(p => string.Equals(p.Name, f, StringComparison.OrdinalIgnoreCase)))
                    .ToList();

                if (missingFields.Any())
                {
                    throw new InvalidOperationException($"Các trường sau không tồn tại: {string.Join(", ", missingFields)}");
                }
            }

            var updatePropsList = propsToUpdate.ToList();

            if (!updatePropsList.Any())
            {
                throw new InvalidOperationException("Không có trường nào để cập nhật");
            }

            // Build SQL
            var setClause = string.Join(", ", updatePropsList.Select(p => $"`{p.Name}` = @{p.Name}"));
            var sql = $"UPDATE `{tableName}` SET {setClause} WHERE `{conditionField}` = @ConditionValue";

            // Build parameters
            var parameters = new Dictionary<string, object>(updatePropsList.Count + 1, StringComparer.OrdinalIgnoreCase);

            foreach (var prop in updatePropsList)
            {
                parameters[prop.Name] = prop.GetValue(model) ?? DBNull.Value;
            }

            parameters["ConditionValue"] = conditionValue;

            // Execute async
            var affected = await cnn.ExecuteAsync(sql, parameters, tran);
            return affected;
        }

        /// <summary>
        /// Lấy danh sách column của bảng (cache theo database + table) - Async version
        /// </summary>
        protected virtual async Task<List<string>> GetColumnByTableNameAsync(string tableName, IDbConnection cnn)
        {
            var cacheKey = $"{tableName}_{_databaseID.ToString()}";

            if (_columnCache.TryGetValue(cacheKey, out var cached))
                return cached;

            const string sql = "SELECT COLUMN_NAME FROM information_schema.COLUMNS WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = @TableName AND LENGTH(generation_expression) = 0 ORDER BY ORDINAL_POSITION";

            var columns = (await cnn.QueryAsync<string>(sql, new { TableName = tableName })).ToList();

            lock (_columnLock)
            {
                if (!_columnCache.ContainsKey(cacheKey))
                {
                    _columnCache[cacheKey] = columns;
                }
            }

            return columns;
        }

        #endregion

        #region Hook Methods (Override)

        /// <summary>
        /// Validate dữ liệu trước khi lưu (override để custom validation logic)
        /// </summary>
        /// <param name="model">Model cần validate</param>
        /// <returns>Danh sách lỗi validation (rỗng nếu hợp lệ)</returns>
        public virtual List<ValidateResult> ValidateBeforeSaveData(BaseModel model)
        {
            return new List<ValidateResult>();
        }

        /// <summary>
        /// Hook thực thi trước khi lưu một bản ghi (override để xử lý logic nghiệp vụ)
        /// </summary>
        /// <param name="model">Model cần xử lý</param>
        /// <returns>Task async</returns>
        public virtual async Task BeforeSaveData(BaseModel model)
        {
            await Task.CompletedTask;
        }

        /// <summary>
        /// Hook thực thi trước khi lưu danh sách bản ghi (override để xử lý logic nghiệp vụ hàng loạt)
        /// Mặc định sẽ gọi BeforeSaveData cho từng model
        /// </summary>
        /// <param name="models">Danh sách model cần xử lý</param>
        /// <returns>Task async</returns>
        public virtual async Task BeforeSaveListData(List<BaseModel> models)
        {
            foreach (var model in models)
            {
                await BeforeSaveData(model);
            }
        }

        /// <summary>
        /// Hook thực thi sau khi lưu một bản ghi (override để xử lý post-processing, logging, notification...)
        /// </summary>
        /// <param name="model">Model đã được lưu</param>
        /// <param name="isSuccess">True nếu lưu thành công, False nếu có lỗi</param>
        /// <returns>Task async</returns>
        public virtual async Task AfterSaveData(BaseModel model, bool isSuccess)
        {
            await Task.CompletedTask;
        }

        /// <summary>
        /// Hook thực thi sau khi lưu danh sách bản ghi (override để xử lý post-processing hàng loạt)
        /// Mặc định sẽ gọi AfterSaveData cho từng model
        /// </summary>
        /// <param name="models">Danh sách model đã được xử lý</param>
        /// <param name="isSuccess">True nếu batch lưu thành công, False nếu có lỗi</param>
        /// <returns>Task async</returns>
        public virtual async Task AfterSaveListData(List<BaseModel> models, bool isSuccess)
        {
            foreach (var model in models)
            {
                await AfterSaveData(model, isSuccess);
            }
        }

        #endregion
    }
}