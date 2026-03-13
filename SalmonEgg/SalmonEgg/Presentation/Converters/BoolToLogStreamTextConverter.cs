using System;
using Microsoft.UI.Xaml.Data;

namespace SalmonEgg.Presentation.Converters
{
    /// <summary>
    /// 将布尔值转换为日志流按钮文本
    /// </summary>
    public class BoolToLogStreamTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is bool boolValue)
            {
                return boolValue ? "关闭流式查看" : "开启流式查看";
            }
            return "开启流式查看";
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
