using System.ComponentModel.DataAnnotations;

namespace HeatExchange.Web.Models;

public sealed class InputRangeAttribute : RangeAttribute
{
    public InputRangeAttribute(double minimum, double maximum) : base(minimum, maximum)
    {
        ErrorMessage = "Значение «{0}» должно находиться в диапазоне от {1} до {2}.";
    }
}
