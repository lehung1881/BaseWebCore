namespace BASE.Service.Core.Model
{
    /// <summary>
    /// Request model cho UpdateByField - Cập nhật dữ liệu dựa trên trường tùy chỉnh
    /// </summary>
    public class UpdateByFieldRequest
    {
        /// <summary>
        /// Model chứa dữ liệu cần cập nhật
        /// </summary>
        public BaseModel Model { get; set; }

        /// <summary>
        /// Tên trường dùng làm điều kiện WHERE
        /// </summary>
        public string ConditionField { get; set; }

        /// <summary>
        /// Giá trị của trường điều kiện
        /// </summary>
        public object ConditionValue { get; set; }

        /// <summary>
        /// Danh sách tên các trường cần cập nhật
        /// - Nếu null hoặc rỗng: cập nhật tất cả trường (trừ PK và trường điều kiện)
        /// - Nếu có giá trị: chỉ cập nhật các trường được chỉ định
        /// </summary>
        public List<string> UpdateFields { get; set; }
    }
}
