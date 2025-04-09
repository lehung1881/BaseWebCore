using BaseWebCore.Common.Enum;
using BaseWebCore.Common.Constant;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaseWebCore.Common.Utils
{
    public static class ConvertUtil
    {
        public static string SerriallizeObject(object obj)
        {
            return JsonConvert.SerializeObject(obj, Formatting.Indented);
        }
        public static T? DeserriallizeObject<T>(string jsonString)
        {
            return JsonConvert.DeserializeObject<T>(jsonString);
        }

        /// <summary>
        /// Convert dữ liệu từ string kiểu dữ liệu cụ thể (Ngày/tháng, Boolean, string...)
        /// </summary>
        /// <param name="dataType"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static object ConvertStringToDataType(EnumDataType dataType, string value)
        {
            if (value == null) return "";

            string dataValue = value.ToString();

            switch (dataType)
            {
                case EnumDataType.Text:
                    return dataValue;
                case EnumDataType.Date:
                case EnumDataType.DateTime:
                    DateTime resultDate;
                    var format = new string[] { Constants.DatetimeFormat };
                    if (DateTime.TryParseExact(dataValue, format, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out resultDate))
                    {
                        return resultDate;
                    }
                    break;
                default:
                    return dataValue;
            }
            return value;
        }
    }
}
