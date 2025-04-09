using BASE.Service.Core.Enum;
using BASE.Service.Core.Model;
using BASE.Service.Core.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace BASE.Service.Core.BL
{
    /// <summary>
    /// Base class cho Business Logic layer, cung cấp các chức năng CRUD cơ bản
    /// </summary>
    /// <typeparam name="TModel">Model kế thừa từ BaseModelCore, đại diện cho một entity trong database</typeparam>
    public abstract class BaseBL<TModel> where TModel : BaseModelCore
    {
        #region Fields and constructor

        /// <summary>
        /// Collection chứa các services được inject
        /// </summary>
        private readonly CoreWebServiceCollection _serviceCollection;

        /// <summary>
        /// Service xử lý authentication/authorization
        /// </summary>
        protected IAuthService _authService { get => _serviceCollection.AuthService(); }

        /// <summary>
        /// Service tương tác với PostgreSQL database
        /// </summary>
        protected IPostgresSQLService _postgresSQLService { get => _serviceCollection.PostgresSQLService(); }

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
        /// Lấy bản ghi theo ID
        /// </summary>
        /// <param name="id">ID của bản ghi cần lấy</param>
        /// <returns>Entity tương ứng với ID. Null nếu không tìm thấy</returns>
        public virtual TModel GetByID(Guid id)
        {
            return _postgresSQLService.GetByID<TModel>(_databaseID, id);
        }

        /// <summary>
        /// Thêm mới một bản ghi
        /// </summary>
        /// <param name="model">Dữ liệu của bản ghi cần thêm</param>
        /// <returns>
        /// ServiceResponse với:
        /// - Success = true nếu thêm thành công
        /// - Success = false và ErrorCode tương ứng nếu có lỗi:
        ///   + InvalidData: model = null
        ///   + Exception: lỗi trong quá trình xử lý
        /// </returns>
        public virtual ServiceResponse Insert(TModel model)
        {
            ServiceResponse res = new ServiceResponse();
            try
            {
                //Kiểm tra check null
                if (model == null)
                {
                    res.OnError(ServiceResponseCode.InvalidData);
                    return res;
                }
                model.ModelState = ModelState.Insert;
                //model.created_by = "Hệ thống";
                //model.created_date = DateTime.Now;
                //model.modified_by = model.created_by;
                //model.modified_date = model.created_date;

                //Validate trước khi thêm mới
                res = ValidateBeforeInsert(model);
                if (!res.Success)
                {
                    return res;
                }

                //Xử lý trước khi Insert
                BeforeInsert(model);

                //Thực hiện Insert dữ liệu
                bool result = _postgresSQLService.Insert(_databaseID, model);

                res.OnSuccess();

                //Xử lý sau khi insert
                AfterInsert(model, result);
            }
            catch (Exception ex)
            {
                res.OnError(ServiceResponseCode.Exception, ex.Message);
            }
            return res;
        }

        /// <summary>
        /// Cập nhật một bản ghi
        /// </summary>
        /// <param name="model">Dữ liệu mới của bản ghi cần cập nhật</param>
        /// <returns>
        /// ServiceResponse với:
        /// - Success = true nếu cập nhật thành công
        /// - Success = false và ErrorCode tương ứng nếu có lỗi:
        ///   + InvalidData: model = null
        ///   + Exception: lỗi trong quá trình xử lý
        /// </returns>  
        public virtual ServiceResponse Update(TModel model)
        {
            ServiceResponse res = new ServiceResponse();
            try
            {
                //Kiểm tra check null
                if (model == null)
                {
                    res.OnError(ServiceResponseCode.InvalidData);
                    return res;
                }
                model.ModelState = ModelState.Update;
                //Validate trước khi thêm mới
                var valid = ValidateBeforeUpdate(model);
                if (!valid.Success)
                {
                    return res;
                }
                //Xử lý trước khi Update
                BeforeUpdate(model);
                //Thực hiện Update dữ liệu
                bool result = _postgresSQLService.Update(_databaseID, model);
                res.OnSuccess();
                //Xử lý sau khi Update
                AfterUpdate(model, result);
            }
            catch (Exception ex)
            {
                res.OnError(ServiceResponseCode.Exception, ex.Message);
            }
            return res;
        }

        /// <summary>
        /// Xóa một bản ghi
        /// </summary>
        /// <param name="model">Bản ghi cần xóa</param>
        /// <returns>
        /// ServiceResponse với:
        /// - Success = true nếu xóa thành công
        /// - Success = false và ErrorCode tương ứng nếu có lỗi:
        ///   + InvalidData: model = null
        ///   + Exception: lỗi trong quá trình xử lý
        /// </returns>
        public ServiceResponse Delete(TModel model)
        {
            ServiceResponse res = new ServiceResponse();
            try
            {
                //Kiểm tra check null
                if (model == null)
                {
                    res.OnError(ServiceResponseCode.InvalidData);
                    return res;
                }
                //Validate trước khi xóa
                var valid = ValidateBeforeDelete(model);
                if (!valid.Success)
                {
                    return res;
                }
                //Xử lý trước khi Xóa
                BeforeDelete(model);
                //Thực hiện Update dữ liệu
                bool result = _postgresSQLService.Delete(_databaseID, model);
                res.OnSuccess();
            }
            catch (Exception ex)
            {
                res.OnError(ServiceResponseCode.Exception, ex.Message);
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
        /// Validate dữ liệu trước khi thêm mới
        /// </summary>
        /// <param name="model">Dữ liệu cần validate</param>
        /// <returns>ServiceResponse với Success = true nếu dữ liệu hợp lệ</returns>
        /// <remarks>
        /// Override phương thức này để thêm các rules validate cho model:
        /// - Kiểm tra tính hợp lệ của dữ liệu
        /// - Kiểm tra ràng buộc nghiệp vụ
        /// - Kiểm tra trùng lặp dữ liệu
        /// </remarks>
        public virtual ServiceResponse ValidateBeforeInsert(TModel model)
        {
            var res = new ServiceResponse();
            return res;
        }

        /// <summary>
        /// Xử lý trước khi thêm mới dữ liệu
        /// </summary>
        /// <param name="model">Dữ liệu sẽ được thêm mới</param>
        /// <remarks>
        /// Override phương thức này để:
        /// - Cập nhật các trường tự động (created date, created by...)
        /// - Xử lý logic nghiệp vụ trước khi thêm mới
        /// - Chuẩn bị dữ liệu liên quan
        /// </remarks>
        public virtual void BeforeInsert(TModel model)
        {

        }

        /// <summary>
        /// Xử lý sau khi thêm mới dữ liệu
        /// </summary>
        /// <param name="model">Dữ liệu đã được thêm mới</param>
        /// <param name="isSuccess">Kết quả thêm mới: true nếu thành công</param>
        /// <remarks>
        /// Override phương thức này để:
        /// - Xử lý logic sau khi thêm mới thành công/thất bại
        /// - Cập nhật dữ liệu liên quan
        /// - Ghi log, thông báo
        /// </remarks>
        public virtual void AfterInsert(TModel model, bool isSuccess)
        {

        }

        /// <summary>
        /// Validate dữ liệu trước khi cập nhật
        /// </summary>
        /// <param name="model">Dữ liệu cần validate</param>
        /// <returns>ServiceResponse với Success = true nếu dữ liệu hợp lệ</returns>
        /// <remarks>
        /// Override phương thức này để thêm các rules validate cho model:
        /// - Kiểm tra tính hợp lệ của dữ liệu
        /// - Kiểm tra ràng buộc nghiệp vụ 
        /// - Kiểm tra trùng lặp dữ liệu
        /// </remarks>
        public virtual ServiceResponse ValidateBeforeUpdate(TModel model)
        {
            var res = new ServiceResponse();
            return res;
        }

        /// <summary>
        /// Validate dữ liệu trước khi xóa
        /// </summary>
        /// <param name="model">Dữ liệu cần validate</param>
        /// <returns>ServiceResponse với Success = true nếu cho phép xóa</returns>
        /// <remarks>
        /// Override phương thức này để:
        /// - Kiểm tra các điều kiện được phép xóa
        /// - Kiểm tra ràng buộc với dữ liệu liên quan
        /// - Kiểm tra quyền xóa dữ liệu
        /// </remarks>
        public virtual ServiceResponse ValidateBeforeDelete(TModel model)
        {
            var res = new ServiceResponse();
            return res;
        }

        /// <summary>
        /// Xử lý trước khi cập nhật dữ liệu
        /// </summary>
        /// <param name="model">Dữ liệu sẽ được cập nhật</param>
        /// <remarks>
        /// Override phương thức này để:
        /// - Cập nhật các trường tự động (modified date, modified by...)
        /// - Xử lý logic nghiệp vụ trước khi cập nhật
        /// - Chuẩn bị dữ liệu liên quan
        /// </remarks>
        public virtual void BeforeUpdate(TModel model)
        {

        }

        /// <summary>
        /// Xử lý trước khi xóa dữ liệu
        /// </summary>
        /// <param name="model">Dữ liệu sẽ bị xóa</param>
        /// <remarks>
        /// Override phương thức này để:
        /// - Xử lý dữ liệu liên quan trước khi xóa
        /// - Backup dữ liệu nếu cần
        /// - Cập nhật các bảng liên quan
        /// </remarks>
        public virtual void BeforeDelete(TModel model)
        {

        }

        /// <summary>
        /// Xử lý sau khi cập nhật dữ liệu
        /// </summary>
        /// <param name="model">Dữ liệu đã được cập nhật</param>
        /// <param name="isSuccess">Kết quả cập nhật: true nếu thành công</param>
        /// <remarks>
        /// Override phương thức này để:
        /// - Xử lý logic sau khi cập nhật thành công/thất bại
        /// - Cập nhật dữ liệu liên quan
        /// - Ghi log, thông báo
        /// </remarks>
        public virtual void AfterUpdate(TModel model, bool isSuccess)
        {

        }
        #endregion
    }
}
