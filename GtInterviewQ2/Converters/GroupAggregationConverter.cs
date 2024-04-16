using System.Collections.ObjectModel;
using System.Data;
using System.Globalization;
using System.Windows.Data;

namespace GtInterviewQ2.Converters
{
    public class GroupAggregationConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            decimal summedValue = 0;
            if (value is ReadOnlyObservableCollection<object> rows)
            {
                foreach (DataRowView row in rows)
                {
                    summedValue += decimal.TryParse(row.Row[(string)parameter]?.ToString() ?? string.Empty, out var d) ? d : 0;
                }
            }
            return summedValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
