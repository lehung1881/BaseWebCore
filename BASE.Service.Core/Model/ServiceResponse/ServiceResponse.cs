using BASE.Service.Core.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BASE.Service.Core.Model
{
    public class ServiceResponse
    {
        #region Property
        public bool Success { get; set; } = true;
        public ServiceResponseCode Code { get; set; } = 0;
        public string Message { get; set; }
        public object ErrorMessage { get; set; }
        public object Data { get; set; }
        public List<ValidateResult> ValidateInfo { get; set; }
        #endregion

        #region Method
        public void OnSuccess(object data = null)
        {
            this.Success = true;
            this.Data = data;
        }

        public void OnError(string msg = "")
        {
            this.Success = false;
            this.Message = msg;
        }

        public void OnError(ServiceResponseCode code, string msg = "")
        {
            this.Success = false;
            this.Code = code;
            this.Message = msg;
        }
        #endregion
    }
}
