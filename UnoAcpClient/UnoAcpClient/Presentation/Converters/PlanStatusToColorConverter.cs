using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using UnoAcpClient.Domain.Models.Plan;

namespace UnoAcpClient.Presentation.Converters
{
    /// <summary>
    /// 将计划条目状态转换为对应的颜色
    /// </summary>
    public class PlanStatusToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is PlanEntryStatus status)
            {
                return status switch
                {
                    PlanEntryStatus.Pending => new SolidColorBrush(Colors.Gray),
                    PlanEntryStatus.InProgress => new SolidColorBrush(Colors.Blue),
                    PlanEntryStatus.Completed => new SolidColorBrush(Colors.Green),
                    _ => new SolidColorBrush(Colors.Gray)
                };
            }
            return new SolidColorBrush(Colors.Gray);
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
