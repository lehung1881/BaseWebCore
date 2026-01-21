using System;

namespace BASE.Service.Core.Attribute
{
    /// <summary>
    /// Class cấu hình các thuộc tính của bảng
    /// </summary>
    public class ConfigTable : System.Attribute
    {
        /// <summary>
        /// Tên bảng trong database
        /// </summary>
        public string TableName { get; set; }

        /// <summary>
        /// Tên view trong database
        /// </summary>
        public string ViewName { get; set; }

        /// <summary>
        /// Khởi tạo đối tượng ConfigTable với các tham số
        /// </summary>
        /// <param name="schema">Schema của bảng</param>
        /// <param name="tableName">Tên bảng</param>
        /// <param name="viewName">Tên view</param>
        public ConfigTable(string tableName, string viewName)
        {
            TableName = tableName;
            ViewName = viewName;
        }
    }
}
