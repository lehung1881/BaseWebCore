using BASE.Service.Core.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BASE.Service.Core.Model
{
    public class PagingRequest
    {
        /// <summary>
        /// Lấy ở trang bao nhiêu
        /// </summary>
        public int pageIndex { get; set; } = 1;
        /// <summary>
        /// Số bản ghi lấy tối đa
        /// </summary>
        public int pageSize { get; set; } = 20;
        /// <summary>
        /// Danh sách điều kiện lọc
        /// </summary>
        public List<FilterCondition> filters { get; set; }
        /// <summary>
        /// Danh sách điều kiện sắp xếp
        /// </summary>
        public string sort { get; set; }
        /// <summary>
        /// Có lấy từ view hay không
        /// </summary>
        public int view { get; set; }
    }

    public class CustomFilter
    {
        /// <summary>
        /// Điều kiện lọc(bằng, trống, lớn hơn, bé hơn...)
        /// </summary>
        public string condition { get; set; }
        /// <summary>
        /// Giá trị lọc
        /// </summary>
        public object value { get; set; }
        /// <summary>
        /// Trường cần lọc
        /// </summary>
        public string property { get; set; }
    }
    public class Sort
    {
        /// <summary>
        /// Trường cần sắp xếp
        /// </summary>
        public string property { get; set; }
        /// <summary>
        /// Giá trị sắp xếp
        /// </summary>
        public bool desc { get; set; }
    }

    /// <summary>
    /// Đại diện cho một điều kiện lọc trong hệ thống.
    /// </summary>
    public class FilterCondition
    {
        /// <summary>
        /// Điều kiện lọc kiểu Enum, xác định loại điều kiện sẽ được áp dụng (ví dụ: Equals, NotEquals).
        /// </summary>
        public EnumFilterCondition condition { get; set; }

        /// <summary>
        /// Giá trị mà điều kiện lọc sẽ so sánh với thuộc tính.
        /// Kiểu dữ liệu có thể thay đổi, vì vậy nó được khai báo là object.
        /// </summary>
        public string value { get; set; }

        /// <summary>
        /// Tên của thuộc tính mà điều kiện lọc sẽ được áp dụng.
        /// </summary>
        public string property { get; set; }

        /// <summary>
        /// Kiểu dữ liệu của thuộc tính được chỉ định.
        /// Giúp xác định loại giá trị nào được sử dụng cho điều kiện lọc (ví dụ: "string", "int", "DateTime").
        /// </summary>
        public EnumDataType data_type { get; set; }
    }
}
