using GalaSoft.MvvmLight.Command;
using GtInterviewQ2.DataAccess;
using GtInterviewQ2.Models;
using GtInterviewQ2.Models.GuiWrapper;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Data;
using System.Windows.Input;

namespace GtInterviewQ2.Controllers
{
    public class MainWindowController : INotifyPropertyChanged
    {
        private volatile bool _isLoading = false;
        private FilteredValuesGroupViewModel _filteredValuesGroupViewModel = new FilteredValuesGroupViewModel();
        private RelayCommand _refreshCommand;
        private readonly ObservableCollection<Models.GroupableLevelValue> _allValues = new();
        private readonly ObservableCollection<FieldSelectionState> _fieldSelections = new();
        private readonly ObservableCollection<LevelSelectionState> _levelSelections = new();
        private static readonly object _lockObject = new();
        internal IQ2DataProvider Q2DataProvider => new GtInterviewDataProvider();

        public MainWindowController()
        {
            BindingOperations.EnableCollectionSynchronization(_allValues, _lockObject);

            BindingOperations.EnableCollectionSynchronization(_fieldSelections, _lockObject);

            FieldSelections.CollectionChanged += (s, a) =>
            {
                if (a?.OldItems != null)
                    foreach (var fieldSelection in a.OldItems.Cast<FieldSelectionState>())
                        FilteredValuesGroupViewModel.DeregesterFieldSelection(fieldSelection);

                if (a?.NewItems != null)
                    foreach (var fieldSelection in a.NewItems.Cast<FieldSelectionState>())
                        FilteredValuesGroupViewModel.RegesterFieldSelection(fieldSelection);
            };

            BindingOperations.EnableCollectionSynchronization(_levelSelections, _lockObject);

            LevelSelections.CollectionChanged += (s, a) =>
            {
                if (a?.OldItems != null)
                    foreach (var levelSelection in a.OldItems.Cast<LevelSelectionState>())
                        FilteredValuesGroupViewModel.DeregesterLevelSelection(levelSelection);
                if (a?.NewItems != null)
                    foreach (var levelSelection in a.NewItems.Cast<LevelSelectionState>())
                        FilteredValuesGroupViewModel.RegesterLevelSelection(levelSelection);
            };

            LoadData();
        }

        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                if (_isLoading != value)
                {
                    _isLoading = value;
                    OnPropertyChanged();
                }
            }
        }

        public ObservableCollection<Models.GroupableLevelValue> AllValues
        {
            get => _allValues;
        }

        public ObservableCollection<FieldSelectionState> FieldSelections
        {
            get => _fieldSelections;
        }

        public ObservableCollection<LevelSelectionState> LevelSelections
        {
            get => _levelSelections;
        }

        public FilteredValuesGroupViewModel FilteredValuesGroupViewModel
        {
            get => _filteredValuesGroupViewModel;
            set
            {
                if (_filteredValuesGroupViewModel != value)
                {
                    _filteredValuesGroupViewModel = value;
                    OnPropertyChanged();
                }
            }
        }

        private async void LoadData()
        {
            if (IsLoading) return;

            IsLoading = true;

            FilteredValuesGroupViewModel.LevelLabels = new Dictionary<int, string>();
            try
            {
                await Task.WhenAll(Task.Run(ProcessRawValues)
                    ).ContinueWith(async t =>
                    {
                        if (FilteredValuesGroupViewModel.VisualDataGrid?.Dispatcher != null)
                            FilteredValuesGroupViewModel.VisualDataGrid.Dispatcher.Invoke(() => { BuildSelectableLevels(); });
                        else
                            BuildSelectableLevels();
                        FilteredValuesGroupViewModel.AllValues = AllValues.ToList();
                        FilteredValuesGroupViewModel.LevelSelections = LevelSelections.ToList();
                        await FilteredValuesGroupViewModel.RebuildGrid();
                    });
            }
            catch (Exception)
            {
                //log
            }
            finally { IsLoading = false; }
        }

        public ICommand RefreshCommand
        {
            get
            {
                if (_refreshCommand == null)
                {
                    _refreshCommand = new RelayCommand(LoadData, true);
                }
                return _refreshCommand;
            }
        }

        private void ProcessRawValues()
        {
            List<GroupableLevelValue> dataSet;
            AllValues.Clear();
            IEnumerable<LevelSelectionState> selectedLevels = LevelSelections?.SelectMany(LevelSelectionState.Flatten) ?? Enumerable.Empty<LevelSelectionState>();
            //LevelSelectionState.Flatten(LevelSelections.Skip(1).Take(1).Single());
            if (selectedLevels.All(v => v.IsChecked ?? true) &&
               (FieldSelections == null || FieldSelections.All(f => f.IsChecked)))
            {
                dataSet = Q2DataProvider.GetAllGroupValues();
            }
            else
            {
                dataSet = Q2DataProvider.GetGroupValues(selectedLevels.Where(v => v.IsChecked ?? true).Select(l => l.LevelId),
                    FieldSelections.Where(f => f.IsChecked).Select(f => f.FieldLabel).ToArray());
            }
            ResetFieldSelections(dataSet);
        }

        private void ResetFieldSelections(List<GroupableLevelValue> dataSet)
        {
            var distinctFields = new HashSet<string>();
            if (FieldSelections.Any())
            {
                foreach (var v in dataSet)
                {
                    AllValues.Add(v);
                }
            }
            else
            {
                foreach (var v in dataSet)
                {
                    AllValues.Add(v);
                    if (!string.IsNullOrWhiteSpace(v.ValueLabel) && distinctFields.Add(v.ValueLabel))
                        FieldSelections.Add(new FieldSelectionState(v.ValueLabel));
                }
            }
        }

        private void BuildSelectableLevels()
        {
            if (!LevelSelections.Any())
            {
                var valuesDictionary = new Dictionary<string, GroupableLevelValue>();
                var parentsDictionary = new Dictionary<int, GroupableLevelValue>();
                foreach (var v in AllValues)
                {
                    valuesDictionary.TryAdd(v.GroupLabel, v);
                    parentsDictionary.TryAdd(v.GroupableLevelId, v);
                }
                Dictionary<string, LevelSelectionState> groupedSelectionStates = valuesDictionary.ToDictionary(ln => ln.Key, ln =>
                {
                    var columnLabel = FilteredValuesGroupViewModel.LevelLabels.TryGetValue(ln.Value.HierarchyLevel, out var levelLabel) ? levelLabel : string.Empty;
                    return new LevelSelectionState(ln.Value.GroupLabel, ln.Value.GroupableLevelId,
                        isRoot: !ln.Value.GroupableLevelParentId.HasValue, columnLabel, ln.Value.HierarchyLevel);
                });

                foreach (var groupableValue in AllValues)
                {
                    if (groupableValue.GroupableLevelParentId.HasValue)
                    {
                        LevelSelectionState current = groupedSelectionStates[groupableValue.GroupLabel];
                        GroupableLevelValue parentValue = parentsDictionary[groupableValue.GroupableLevelParentId.Value];
                        LevelSelectionState parent = groupedSelectionStates[parentValue.GroupLabel];
                        if (!parent.Children.Contains(current))
                            parent.Children.Add(current);
                    }
                }
                foreach (var groupableValue in groupedSelectionStates.Values.Where(gv => gv.IsRoot))
                {
                    LevelSelections.Add(groupableValue);
                }
            }

        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
