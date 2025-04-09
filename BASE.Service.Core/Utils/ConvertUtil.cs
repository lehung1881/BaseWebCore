using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BASE.Service.Core.Utils
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
    }
}
