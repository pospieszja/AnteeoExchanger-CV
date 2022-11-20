using System;

namespace AnteeoExchanger.Helpers
{
    public static class DateTimeExtensions
    {
        public static int ConvertDateToClarionFormat(this DateTime date)
        {
            return (date - new DateTime(1800, 12, 28, 0, 0, 0)).Days;
        }
    }
}
