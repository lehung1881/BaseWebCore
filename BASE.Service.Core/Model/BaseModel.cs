using BASE.Service.Core.Attribute;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using BASE.Service.Core.Enum;

namespace BASE.Service.Core.Model
{
    public class BaseModel
    {
        [NotMapped]
        public ModelState ModelState { get; set; } = ModelState.Insert;

        /// <summary>
        /// Danh sách cột update
        /// </summary>
        [NotMapped]
        public List<string> UpdateColumns { get; set; }

        #region Method
        /// <summary>
        /// Set giá trị
        /// </summary>
        /// <param name="value"></param>
        public void SetPrimaryKey(string value)
        {
            PropertyInfo[] props = this.GetType().GetProperties();

            PropertyInfo propertyInfoKey = null;
            if (props != null)
            {
                propertyInfoKey = props.SingleOrDefault(
                    p => p.GetCustomAttribute<KeyAttribute>(true) != null
                );

                if (propertyInfoKey != null)
                {
                    if (propertyInfoKey.PropertyType == typeof(long))
                        propertyInfoKey.SetValue(this, long.Parse(value));
                    else if (propertyInfoKey.PropertyType == typeof(int))
                        propertyInfoKey.SetValue(this, int.Parse(value));
                    else if (propertyInfoKey.PropertyType == typeof(Guid))
                        propertyInfoKey.SetValue(this, Guid.Parse(value));
                    else
                        propertyInfoKey.SetValue(this, value);
                }
            }
        }


        /// <summary>
        /// Lấy tên bảng trong Database
        /// </summary>
        /// <param name="hasSchema"></param>
        /// <returns></returns>
        public string GetViewOrTableName(bool hasSchema = true)
        {
            var tableAttr = (ConfigTable)GetType().GetCustomAttributes(typeof(ConfigTable), false).FirstOrDefault();
            string viewOrTabble = !string.IsNullOrEmpty(tableAttr.ViewName) ? tableAttr.ViewName : tableAttr.TableName;
            return viewOrTabble;
        }

        /// <summary>
        /// Lấy ra khóa chính
        /// </summary>
        /// <returns></returns>
        public string GetPrimaykeyField()
        {
            var properties = this.GetType().GetProperties();
            var keyProperty = properties.FirstOrDefault(p => p.GetCustomAttribute<KeyAttribute>() != null);
            return keyProperty?.Name;
        }

        /// <summary>
        /// Lấy giá trị của khóa chính
        /// </summary>
        /// <returns></returns>
        public object GetPrimaryKeyValue()
        {
            return this.GetValueByAttribute(typeof(KeyAttribute));
        }

        /// <summary>
        /// Lấy ra filed theo Attribute
        /// </summary>
        /// <param name="typeAttr"></param>
        /// <returns></returns>
        public string GetFieldNameByAttribute(Type typeAttr)
        {
            PropertyInfo[] props = this.GetType().GetProperties().Where(p => p.GetIndexParameters().Length == 0).ToArray();
            var customProperty = props.FirstOrDefault(p => p.GetCustomAttribute(typeAttr, true) != null);
            return customProperty?.Name;
        }

        /// <summary>
        /// Lấy giá trị của một field bằng thuộc tính
        /// </summary>
        /// <param name="typeAttr"></param>
        /// <returns></returns>
        public object GetValueByAttribute(Type typeAttr)
        {
            PropertyInfo[] props = this.GetType().GetProperties().Where(p => p.GetIndexParameters().Length == 0).ToArray();
            PropertyInfo oProp = null;
            if (props != null)
            {
                oProp = props.SingleOrDefault(p => p.GetCustomAttribute(typeAttr, true) != null);
            }
            if (oProp != null)
            {
                return oProp.GetValue(this);
            }
            return null;
        }

        /// <summary>
        /// Clone instance
        /// </summary>
        /// <returns></returns>
        public object Clone()
        {
            return MemberwiseClone();
        }
        #endregion
    }
}
