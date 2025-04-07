using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace BaseWebCore.Common.Utils
{
    public class ModelHelper
    {
        /// <summary>
        /// Lấy ra khóa chính của Model
        /// </summary>
        /// <param name="modelType"></param>
        /// <returns></returns>
        public static string GetPrimaryKeyFiled(Type modelType)
        {
            string keyName = GetFieldName(modelType, typeof(KeyAttribute));
            return keyName;
        }

        /// <summary>
        /// Lấy ra field theo Attribute(Thuộc tính)
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="type"></param>
        /// <returns></returns>
        public static string GetFieldName<TModel>(Type type)
        {
            Type t = typeof(TModel);
            string fieldName = GetFieldName(t, type);
            return fieldName;
        }

        /// <summary>
        /// Lấy ra tên bảng
        /// </summary>
        /// <typeparam name="TModel"></typeparam>
        /// <param name="hasSchema"></param>
        /// <returns></returns>
        public static string GetTableName<TModel>(bool hasSchema = true)
        {
            string tableName = "";
            var tableAttr = (TableAttribute)typeof(TModel).GetCustomAttributes(typeof(TableAttribute), false).FirstOrDefault();
            if (hasSchema)
            {
                tableName = $"{tableAttr.Schema}.{tableAttr.Name}";
            }
            else
            {
                tableName = $"{tableAttr.Name}";
            }
            return tableName;
        }
        /// <summary>
        /// Lấy ra field theo Attribute(Thuộc tính)
        /// </summary>
        /// <param name="modelType"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public static string GetFieldName(Type modelType, Type type)
        {
            string fieldName = "";
            PropertyInfo[] props = modelType.GetProperties();
            if (props != null)
            {
                var propInfoKey = props.SingleOrDefault(p => p.GetCustomAttribute(type, true) != null);
                if (propInfoKey != null)
                {
                    fieldName = propInfoKey.Name;
                }
            }
            return fieldName;
        }

        /// <summary>
        /// Lấy danh sách cột phục vụ Insert dữ kiệu
        /// </summary>
        /// <param name="modelType"></param>
        /// <param name="primaryKey"></param>
        /// <returns></returns>
        public static Dictionary<string, PropertyInfo> GetFieldInsert(Type modelType)
        {
            Dictionary<string, PropertyInfo> columnMapping = new Dictionary<string, PropertyInfo>();
            PropertyInfo[] props = modelType.GetProperties();
            IEnumerable<PropertyInfo> listProps = props.Where(item => item.GetCustomAttribute(typeof(NotMappedAttribute)) == null);
            foreach (PropertyInfo prop in listProps)
            {
                if (columnMapping.ContainsKey(prop.Name))
                {
                    if (prop.DeclaringType == modelType)
                    {
                        columnMapping[prop.Name] = prop;
                    }
                }
                else
                {
                    columnMapping.Add(prop.Name, prop);
                }
            }
            return columnMapping;
        }

        public static string GenerateScriptGetAll<TModel>()
        {
            return $"select * from {GetTableName<TModel>()}";
        }

        /// <summary>
        /// Lấy ra tên bảng
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        public static string GetTableName(Type t)
        {
            string tableName = "";
            TableAttribute tableAttribute = (TableAttribute)t.GetCustomAttributes(typeof(TableAttribute), false).FirstOrDefault();
            if (tableAttribute != null)
            {
                tableName = tableAttribute.Name;
            }
            return tableName;
        }

        /// <summary>
        /// Lấy ra tên schema
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        public static string GetSchemaName(Type t)
        {
            string schemaName = "";
            TableAttribute tableAttribute = (TableAttribute)t.GetCustomAttributes(typeof(TableAttribute), false).FirstOrDefault();
            if (tableAttribute != null)
            {
                schemaName = tableAttribute.Schema;
            }
            return schemaName;
        }
    }
}
