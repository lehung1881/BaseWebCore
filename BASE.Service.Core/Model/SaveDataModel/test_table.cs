using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using BASE.Service.Core.Attribute;

namespace BASE.Service.Core.Model
{
    /// <summary>
    /// Request model cho UpdateByField - Cập nhật dữ liệu dựa trên trường tùy chỉnh
    /// </summary>
    [ConfigTable("test_table", "")]
    public class TestTable
    {
        [Key]
        public Guid col_id { get; set; }
        public string col_text { get; set; }
        public int? col_int { get; set; }
        public decimal? col_decimal { get; set; }
        public bool col_tinyint { get; set; }  // TINYINT trong MySQL = sbyte trong C#
        public string col_json { get; set; }  // Lưu dạng string, parse khi cần
    }
}
