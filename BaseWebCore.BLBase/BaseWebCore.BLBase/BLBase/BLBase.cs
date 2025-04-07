using BaseWebCore.Common.Enum;
using BaseWebCore.Common.Model;
using BaseWebCore.DLBase;
using BaseWebCore.DLBase.PostgresSQL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace BaseWebCore.BLBase
{
    public abstract class BLBase<TModel, TDL> where TModel : BaseModelCore where TDL : DLBase<TModel>
    {
        #region Fields and constructor

        private TDL _dlObject;

        protected IPostgresServices _postgresServices;

        /// <summary>
        /// Phương thức khởi tạo
        /// </summary>
        public BLBase()
        {
            _postgresServices = new PostgresServices();
        }

        protected TDL DLObject 
        { 
            get 
            { 
                if(_dlObject == null)
                {
                    _dlObject = CreateDL();
                }
                return _dlObject; 
            } 
        }

        /// <summary>
        /// Khởi tạo DL
        /// </summary>
        /// <returns></returns>
        abstract public TDL CreateDL();

        #endregion

        #region Methods
        /// <summary>
        /// Lấy theo id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public virtual TModel GetByID(Guid id)
        {
            return _dlObject.GetByID(id);
        }

        /// <summary>
        /// Insert 1 bản ghi
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public virtual ServicesResponse Insert(TModel model)
        {
            ServicesResponse res = new ServicesResponse();
            try
            {
                //Kiểm tra check null
                if (model == null)
                {
                    res.OnError(ServiceResponseCode.InvalidData);
                    return res;
                }
                model.model_state = ModelState.Insert;
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
                bool result = _dlObject.InsertByState(model);
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
        /// Cập nhật 1 bản ghi
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public virtual ServicesResponse Update(TModel model)
        {
            ServicesResponse res = new ServicesResponse();
            try
            {
                //Kiểm tra check null
                if (model == null)
                {
                    res.OnError(ServiceResponseCode.InvalidData);
                    return res;
                }
                model.model_state = ModelState.Update;
                //Validate trước khi thêm mới
                var valid = ValidateBeforeUpdate(model);
                if (!valid.Success)
                {
                    return res;
                }
                //Xử lý trước khi Update
                BeforeUpdate(model);
                //Thực hiện Update dữ liệu
                bool result = _dlObject.InsertByState(model);
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

        public ServicesResponse Delete(TModel model)
        {
            ServicesResponse res = new ServicesResponse();
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
                bool result = _dlObject.Delete(model);
                res.OnSuccess();
            }
            catch (Exception ex)
            {
                res.OnError(ServiceResponseCode.Exception, ex.Message);
            }
            return res;
        }

        /// <summary>
        /// Lấy danh sách bản ghi phân trang
        /// </summary>
        /// <param name="pageIndex">Trang hiện tại</param>
        /// <param name="pageSize">Số bản ghi trên mỗi trang</param>
        /// <param name="filter">Bộ lọc</param>
        /// <param name="sort">Sắp xếp</param>
        /// <param name="view">Chế độ xem</param>
        public PagingResponse GetPaging<T>(int pageIndex, int pageSize, List<FilterCondition> filters, int? viewName, string sort = "")
        {
            return _dlObject.GetPaging<T>(pageIndex, pageSize, filters, viewName, sort);
        }

        #endregion

        #region Sub methods
        /// <summary>
        /// Validate trước khi cập nhật
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public virtual ServicesResponse ValidateBeforeInsert(TModel model)
        {
            var res = new ServicesResponse();
            return res;
        }

        /// <summary>
        /// Xử lý trước khi Insert dữ liệu
        /// </summary>
        /// <param name="model"></param>
        /// <param name="isSuccess"></param>
        public virtual void BeforeInsert(TModel model)
        {

        }

        /// <summary>
        /// Xử lý sau khi Insert dữ liệu
        /// </summary>
        /// <param name="model"></param>
        /// <param name="isSuccess"></param>
        public virtual void AfterInsert(TModel model, bool isSuccess)
        {

        }

        /// <summary>
        /// Validate trước khi cập nhật
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public virtual ServicesResponse ValidateBeforeUpdate(TModel model)
        {
            var res = new ServicesResponse();
            return res;
        }

        /// <summary>
        /// Validate trước khi cập nhật
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public virtual ServicesResponse ValidateBeforeDelete(TModel model)
        {
            var res = new ServicesResponse();
            return res;
        }

        /// <summary>
        /// Xử lý trước khi Insert dữ liệu
        /// </summary>
        /// <param name="model"></param>
        /// <param name="isSuccess"></param>
        public virtual void BeforeUpdate(TModel model)
        {

        }

        /// <summary>
        /// Xử lý trước khi Insert dữ liệu
        /// </summary>
        /// <param name="model"></param>
        /// <param name="isSuccess"></param>
        public virtual void BeforeDelete(TModel model)
        {

        }

        /// <summary>
        /// Xử lý sau khi Insert dữ liệu
        /// </summary>
        /// <param name="model"></param>
        /// <param name="isSuccess"></param>
        public virtual void AfterUpdate(TModel model, bool isSuccess)
        {

        }
        #endregion
    }
}
