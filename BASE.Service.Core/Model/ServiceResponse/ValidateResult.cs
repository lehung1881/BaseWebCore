namespace BASE.Service.Core.Model
{
    public class ValidateResult
    {
        /// <summary>
        /// ID của bản ghi lỗi
        /// </summary>
        public object ID { get; set; }
    
        /// <summary>
        /// Mã lỗi
        /// </summary>
        public string Code { get; set; }
    
        /// <summary>
        /// Nội dung lỗi
        /// </summary>
        public string ErrorMessage { get; set; }
    
        /// <summary>
        /// Dữ liệu tùy biến mang thêm
        /// </summary>
        public object AdditionInfo { get; set; }
    }
}
