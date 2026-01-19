using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BASE.Service.Core.Enum
{
    public enum ServiceResponseCode : int
    {
        Success = 0,

        InvalidData = 1,

        NotFound = 2,

        Exception = 3,

        ExistsCode = 4,

        Duplicate = 5
    }

    public enum ModelState : int
    {
        Insert = 0,

        Update = 1,

        Delete = 2,
    }

    public enum EnumFilterCondition : int
    {
        Empty = 1,
        NotEmpty = 2,
        Equal = 3,
        NotEqual = 4,
        Contain = 5,
        NotContain = 6,
        GreaterThan = 7,
        LessThan = 8,
        GreaterThanEqual = 9,
        LessThanEqual = 10
    }

    public enum EnumDataType : int
    {
        Text = 1,
        Date = 2,
        DateTime = 3,
        Boolean = 4,
        Radio = 5,
        Password = 6,
    }
}
