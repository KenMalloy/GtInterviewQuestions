using GtInterviewQ2.Controllers;
using System.Windows.Controls;

namespace GtInterviewQ2.Views
{
    /// <summary>
    /// Interaction logic for FilteredValuesGroupVIew.xaml
    /// </summary>
    public partial class FilteredValuesGroupView : UserControl
    {
        public FilteredValuesGroupView()
        {
            InitializeComponent();
            this.DataContextChanged += (s, a) =>
            {
                if (a.NewValue is FilteredValuesGroupViewModel vm)
                {
                    vm.VisualDataGrid = this.ValuesDataGrid;
                }
            };
        }

        public FilteredValuesGroupViewModel? ViewModel => (DataContext as FilteredValuesGroupViewModel);
    }
}
