﻿﻿using BASE.Service.Core.BL;
using BASE.Service.Core.Enum;
using BASE.Service.Core.Model;
using BASE.Service.Core.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BASE.Service.Core.Web
{
    [Route("v1/[controller]")]
    [ApiController]
    /// <summary>
    /// Base controller cung cấp các API endpoints cơ bản cho thao tác CRUD
    /// </summary>
    /// <typeparam name="TModel">Model kế thừa từ BaseModelCore, đại diện cho entity trong database</typeparam>
    /// <typeparam name="TBL">Business Logic layer tương ứng với model, kế thừa từ BaseBL</typeparam>
    /// <remarks>
    /// Để sử dụng, tạo controller kế thừa từ class này và implement method CreateBL
    /// Route mặc định là "v1/[controller]" với [controller] là tên của controller kế thừa
    /// </remarks>
    public abstract class BaseServicesController<TModel, TBL> : ControllerBase where TModel : BaseModel where TBL : BaseBL
    {
        #region Constructor and fields
        /// <summary>
        /// Collection chứa các services được inject
        /// </summary>
        private readonly CoreWebServiceCollection _serviceCollection;

        /// <summary>
        /// Service xử lý authentication/authorization
        /// </summary>
        protected IAuthService _authService { get => _serviceCollection.AuthService(); }

        /// <summary>
        /// Định danh của user hiện tại
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
            set
            {
                _userID = value;
            }
        }

        public BaseServicesController(CoreWebServiceCollection serviceCollection)
        {
            _serviceCollection = serviceCollection;
        }

        /// <summary>
        /// Property để truy cập business logic layer
        /// Tự động khởi tạo instance nếu chưa tồn tại (lazy loading)
        /// </summary>
        private TBL _bLObject;
        protected TBL BLObject
        {
            get
            {
                if (_bLObject == null)
                {
                    _bLObject = CreateBL(_serviceCollection);
                }
                return _bLObject;
            }
        }

        /// <summary>
        /// Factory method để tạo instance của business logic layer
        /// </summary>
        public abstract TBL CreateBL(CoreWebServiceCollection serviceCollection);

        #endregion

        #region Methods
        /// <summary>
        /// API lấy bản ghi theo ID
        /// </summary>
        /// <param name="id">ID của bản ghi cần lấy</param>
        /// <returns>
        [HttpGet("{id}")]
        public ServiceResponse GetByID(Guid id)
        {
            var res = new ServiceResponse();
            try
            {
                var data = BLObject.GetByID(id);
                if (data != null)
                {
                    res.OnSuccess(data);
                }
            }
            catch (Exception ex)
            {
                res.OnError(ServiceResponseCode.Exception, ex.Message);
            }
            return res;
        }

        /// <summary>
        /// API thêm/sửa/xóa bản ghi
        /// </summary>
        /// <param name="model">Dữ liệu của bản ghi cần thêm</param>
        /// <returns>
        [HttpPost("save_data_async")]
        public async Task<ServiceResponse> SaveDataAsync(BaseModel model)
        {
            var res = new ServiceResponse();
            try
            {
                res = await BLObject.SaveDataAsync(model);
            }
            catch (Exception ex)
            {
                res.OnError(ServiceResponseCode.Exception, ex.Message);
            }
            return res;
        }

        /// <summary>
        /// API lấy danh sách bản ghi có phân trang và lọc
        /// </summary>
        /// <param name="request">Object chứa các tham số phân trang và lọc</param>
        /// <returns>
        [HttpPost("paging_filter")]
        public ServiceResponse PagingFilter(PagingRequest request)
        {
            var res = new ServiceResponse();
            try
            {
                PagingResponse data = BLObject.GetPaging<TModel>(request.pageIndex, request.pageSize, request.filters, request.view, request.sort);
                if (data == null)
                {
                    res.OnError(ServiceResponseCode.NotFound);
                }
                else
                {
                    res.OnSuccess(data);
                }
            }
            catch (Exception ex)
            {
                res.OnError(ServiceResponseCode.Exception, ex.Message);
            }
            return res;
        }
        #endregion
    }
}
