using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using BaseWebCore.Common.Enum;
using BaseWebCore.Common.Utils;
using BaseWebCore.Core.Attribute;

namespace BaseWebCore.Common.Model
{
    public class BaseModelCore
    {
        [NotMapped]
        public ModelState model_state { get; set; } = ModelState.Insert;

        #region Method
        /// <summary>
        /// Lấy tên bảng trong Database
        /// </summary>
        /// <param name="hasSchema"></param>
        /// <returns></returns>
        public string GetViewOrTableName(bool hasSchema = true)
        {
            string tableName = "";
            var tableAttr = (ConfigTable)GetType().GetCustomAttributes(typeof(ConfigTable), false).FirstOrDefault();

            string viewOrTabble = !string.IsNullOrEmpty(tableAttr.ViewName) ? tableAttr.ViewName : tableAttr.TableName;
            if (hasSchema)
            {
                tableName = $"{tableAttr.Schema}.{viewOrTabble}";
            }
            else
            {
                tableName = $"{viewOrTabble}";
            }
            return tableName;
        }
        /// <summary>
        /// Lấy ra khóa chính
        /// </summary>
        /// <returns></returns>
        public string GetPrimayKeyFieldName()
        {
            return ModelHelper.GetPrimaryKeyFiled(this.GetType());
        }
        /// <summary>
        /// Lấy ra filed theo thuộc tính
        /// </summary>
        /// <param name="typeAttr"></param>
        /// <returns></returns>
        public string GetFieldName(Type typeAttr)
        {
            return ModelHelper.GetFieldName(this.GetType(), typeAttr);
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
        /// Lấy giá trị của khóa chính
        /// </summary>
        /// <returns></returns>
        public object GetPrimaryKeyValue()
        {
            return this.GetValueByAttribute(typeof(KeyAttribute));
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
