using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BASE.Service.Core.Model
{
    public class PagingResponse
    {
        public PagingResponse()
        {
            
        }
        public PagingResponse(object pageData, int total)
        {
            PageData = pageData;
            Total = total;
        }
        public object PageData { get; set; }

        public int Total { get; set; }
    }

    public class TotalData
    {
        public int Total { get; set; }
    }
}
