using GtInterviewQ2.Models;
using GtInterviewQ2.Models.GuiWrapper;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Markup;
using System.Xml;

namespace GtInterviewQ2.Controllers
{
    public class FilteredValuesGroupViewModel : INotifyPropertyChanged
    {
        private DataGrid? _dataGrid = default;
        private bool _isRebuilding = false;
        private List<GroupableLevelValue> _allValues = new();
        private DataTable _flatData = new DataTable();
        private CollectionViewSource _flatDataViewSource = new CollectionViewSource();
        private string? _selectedGroupBy;
        private ObservableCollection<string> _groupByColumns = new();
        private static readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        public DataGrid? VisualDataGrid
        {
            get => _dataGrid;
            internal set
            {
                if (_dataGrid != value)
                {
                    _dataGrid = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsRebuilding
        {
            get => _isRebuilding;
            set
            {
                if (_isRebuilding != value)
                {
                    _isRebuilding = value;
                    OnPropertyChanged();
                }
            }
        }

        public List<GroupableLevelValue> AllValues
        {
            get => _allValues;
            set
            {
                if (_allValues != value)
                {
                    _allValues = value;
                    OnPropertyChanged();
                }
            }
        }

        public DataTable FlatData
        {
            get => _flatData;
            set
            {
                if (_flatData != value)
                {
                    _flatData = value;
                    OnPropertyChanged();
                }
            }
        }

        public ObservableCollection<string> GroupByColumns
        {
            get => _groupByColumns;
            set
            {
                if (_groupByColumns != value)
                {
                    _groupByColumns = value;
                    OnPropertyChanged();
                }
            }
        }

        public string? SelectedGroupBy
        {
            get => _selectedGroupBy;
            set
            {
                if (_selectedGroupBy != value)
                {
                    _selectedGroupBy = value;
                    RebuildVisualGroupBy();
                    OnPropertyChanged();
                }
            }
        }

        public Dictionary<int, string> LevelLabels { get; set; } = new Dictionary<int, string>();
        public List<LevelSelectionState> LevelSelections { get; internal set; } = new List<LevelSelectionState>();
        private List<FieldSelectionState> FieldSelections { get; } = new();

        public async Task RebuildGrid(bool cancleCurrentBuild = false)
        {
            if (IsRebuilding)
            {
                if (cancleCurrentBuild)
                    _cancellationTokenSource.Cancel();
                else
                    return;
            }
            try
            {
                IsRebuilding = true;
                await Task.Run(() =>
                {
#if DEBUG_1
                    Thread.Sleep(3000);
#endif
                    RebuildGridInternal();
                }, _cancellationTokenSource.Token);
            }
            catch (Exception)
            {
                //log
            }
            finally
            {
                IsRebuilding = false;
            }
        }

        private void RebuildGridInternal()
        {
            if (VisualDataGrid != null)
            {
                RefreshFlatData(out var groupByColumns, out var valueColumns);

                VisualDataGrid.Dispatcher.Invoke(() =>
                {
                    GroupByColumns.Clear();
                    GroupByColumns.Add(string.Empty);
                    foreach (var col in groupByColumns.Select(c => c.colName).Take(groupByColumns.Count - 1))
                        GroupByColumns.Add(col);

                    RefreshVisualColumns();
                    RebuildVisualGroupBy();
                });
            }
            else if (!_cancellationTokenSource.IsCancellationRequested)
            {
                Thread.Sleep(100);
                RebuildGridInternal();
            }
        }

        private void RebuildVisualGroupBy()
        {
            var collectionView = (VisualDataGrid.Parent as FrameworkElement).Resources["colectionView"] as CollectionViewSource;
            collectionView.GroupDescriptions.Clear();
            if (!string.IsNullOrWhiteSpace(SelectedGroupBy))
            {
                System.Windows.Application.Current.Resources["GroupedHeaders"] = CreateGroupedHeaderDataTemplate();
                collectionView.GroupDescriptions.Add(new PropertyGroupDescription(SelectedGroupBy));
            }
        }


        private DataTemplate CreateGroupedHeaderDataTemplate()
        {
            var dataTemplateXaml =
@"<DataTemplate xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"" xmlns:converters=""clr-namespace:GtInterviewQ2.Converters"">
    <StackPanel Orientation=""Horizontal"" Background=""LightSteelBlue"">
        <TextBlock Text=""{Binding DataContext.Name, RelativeSource={RelativeSource AncestorType=ContentControl}}"" Foreground=""White"" Margin=""5 2 25 2"" FontWeight=""Bold""/>
        ";

            foreach (var field in FieldSelections.Where(f => f.IsChecked))
            {
                var fieldXaml = @$"<TextBlock Text=""{field.FieldLabel}"" Margin=""5 2 2 2"" Foreground=""White""/>
        <TextBlock Text=""{{Binding Path=DataContext.Items, RelativeSource={{RelativeSource AncestorType=ContentControl}}, Converter={{StaticResource GroupAggregationConverter}}, ConverterParameter='{field.FieldLabel}'}}"" Foreground=""White"" Margin=""5 2 5 2""/>";
                dataTemplateXaml += fieldXaml + @"
        ";
            }

            dataTemplateXaml += @"
    </StackPanel>
</DataTemplate>";

            var stringReader = new StringReader(dataTemplateXaml);
            XmlReader xmlReader = XmlReader.Create(stringReader);
            return XamlReader.Load(xmlReader) as DataTemplate;
        }

        private void RefreshVisualColumns()
        {
            int i = VisualDataGrid != null ? 0 : throw new Exception("Illegal State contact a dev.");
            VisualDataGrid.Columns.Clear();
            foreach (DataColumn column in FlatData.Columns)
            {
                var col = new DataGridTextColumn
                {
                    Binding = new Binding(column.ColumnName),
                    Header = column.ColumnName,
                    DisplayIndex = i++
                };
                VisualDataGrid.Columns.Add(col);
            }
        }

        private void RefreshFlatData(out List<(int index, string colName)> groupByColumns, out HashSet<string> valueColumns)
        {
            var tempData = new DataTable();
            groupByColumns = new List<(int index, string colName)>();
            valueColumns = new HashSet<string>();
            var maxHeirarchyLevel = AllValues.Any() ? AllValues.Max(v => v.HierarchyLevel) : 0;
            for (int i = 1; i <= maxHeirarchyLevel; i++)
            {
                var colName = LevelLabels.TryGetValue(i, out var levelLabel) ?
                    levelLabel :
                    $"Column{i}";
                groupByColumns.Add(new(i, colName));
                tempData.Columns.Add(colName);
            }
            foreach (string flatValueLabel in AllValues.Where(v => !string.IsNullOrWhiteSpace(v.ValueLabel))
                .Select(v => v.ValueLabel).Distinct())
            {
                var unique = valueColumns.Add(flatValueLabel);
                Debug.Assert(unique);
                tempData.Columns.Add(flatValueLabel);
            }

            foreach (var valueSet in AllValues.Where(v => v.DecimalValue.HasValue).GroupBy(v => v.GroupableLevelId))
            {
                var groupLabel = valueSet.First().GroupLabel;
                var relatedEntities = GetHierarchyPath(valueSet.First().GroupableLevelId);
                var newRow = tempData.NewRow();
                for (int heriarcyLevel = 1; heriarcyLevel <= groupByColumns.Count; heriarcyLevel++)
                {
                    newRow[heriarcyLevel - 1] = GetParentAtLevel(relatedEntities, heriarcyLevel, groupLabel);
                }
                foreach (var value in valueSet)
                {
                    newRow[value.ValueLabel] = value.DecimalValue!.Value;
                }
                tempData.Rows.Add(newRow);
            }

            tempData.AcceptChanges();
            FlatData = tempData;
        }

        public string GetParentAtLevel(LevelSelectionState root, int targetLevel, string targetLabel)
        {
            string InnerGetParentAtLevel(LevelSelectionState node, string label, int level, Stack<LevelSelectionState> ancestors)
            {
                if (node.LevelLabel == label)
                {
                    if (node.HierarchyLevel == level)
                        return label;

                    foreach (var ancestor in ancestors)
                    {
                        if (ancestor.HierarchyLevel == level)
                        {
                            return ancestor.LevelLabel;
                        }
                    }
                    throw new Exception();
                }
                ancestors.Push(node);
                foreach (var child in node.Children)
                {
                    var result = InnerGetParentAtLevel(child, label, level, ancestors);
                    if (result != null) return result;
                }
                ancestors.Pop();

                return null;
            }
            var recursiveFind = InnerGetParentAtLevel(root, targetLabel, targetLevel, new Stack<LevelSelectionState>());
            return recursiveFind;
        }

        private LevelSelectionState GetHierarchyPath(int groupId, List<LevelSelectionState> searchingIn = null)
        {
            foreach (var level in searchingIn ?? LevelSelections)
            {
                var rootNode = level;
                if (level.LevelId == groupId)
                {
                    return rootNode;
                }
                if (GetHierarchyPath(groupId, level.Children.ToList()) != null)
                    return rootNode;
            }
            return default;
        }

        private void FieldSelection_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {

        }
        private void LevelSelection_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {

        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        internal void DeregesterFieldSelection(FieldSelectionState fieldSelection)
        {
            FieldSelections.Remove(fieldSelection);
            fieldSelection.PropertyChanged -= FieldSelection_PropertyChanged;
        }

        internal void DeregesterLevelSelection(LevelSelectionState levelSelection)
        {
            levelSelection.PropertyChanged -= LevelSelection_PropertyChanged;
        }

        internal void RegesterFieldSelection(FieldSelectionState fieldSelection)
        {
            FieldSelections.Add(fieldSelection);
            fieldSelection.PropertyChanged += FieldSelection_PropertyChanged;
        }

        internal void RegesterLevelSelection(LevelSelectionState levelSelection)
        {
            levelSelection.PropertyChanged += LevelSelection_PropertyChanged;
        }
    }
}
