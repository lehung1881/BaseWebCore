using BASE.Service.Core.Attribute;
using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace BASE.Service.Core
{
    public static class ExtensionUtility
    {
        /// <summary>
        /// Lấy về tên cột khóa chính
        /// </summary>
        /// <returns>Trả về tên key của object</returns>
        public static string GetPrimaryKeyFieldName(this Type type)
        {
            return type.GetFieldName(typeof(KeyAttribute));
        }

        /// <summary>
        /// Lấy về tên cột theo attrType
        /// </summary>
        /// <returns>Trả về tên key của object</returns>
        public static string GetFieldName(this Type type, Type attrType)
        {
            string primaryKeyName = string.Empty;
            PropertyInfo[] props = type.GetProperties();
            if (props != null)
            {
                var propertyInfoKey = props.SingleOrDefault(p => p.GetCustomAttribute(attrType, true) != null);
                if (propertyInfoKey != null)
                {
                    primaryKeyName = propertyInfoKey.Name;
                }
            }
            return primaryKeyName;
        }

        /// <summary>
        /// Lấy tên bảng mapping trong database
        /// </summary>
        public static string GetViewOrTableName(this Type type)
        {
            var configTable = ((ConfigTable)type.GetCustomAttributes(typeof(ConfigTable), true).FirstOrDefault());
            string tableName = configTable?.ViewName;
            if (string.IsNullOrEmpty(tableName))
            {
                tableName = configTable?.TableName;
            }
            return tableName;
        }

    }
}
