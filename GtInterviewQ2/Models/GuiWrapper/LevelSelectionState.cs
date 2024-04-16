using GtInterviewQ2.Controllers;
using System.Collections;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace GtInterviewQ2.Models.GuiWrapper
{
    public class LevelSelectionState : INotifyPropertyChanged, IEnumerable<LevelSelectionState>, ICloneable
    {
        private bool? _isChecked;

        public LevelSelectionState(string label, int levelId, bool isRoot, string columnLabel, int hierarchyLevel, bool? isChecked = true)
        {
            LevelLabel = label;
            LevelId = levelId;
            IsRoot = isRoot;
            ColumnLabel = columnLabel;
            HierarchyLevel = hierarchyLevel;
            _isChecked = isChecked;

            Children.CollectionChanged += (sender, args) =>
            {
                if (args == null) return;
                var empty = Enumerable.Empty<LevelSelectionState>();
                foreach (var item in args.OldItems?.Cast<LevelSelectionState>() ?? empty)
                {
                    item._isCheckChanged -= ChildIsCheckedChanged;
                }
                foreach (var item in args.NewItems?.Cast<LevelSelectionState>() ?? empty)
                {
                    item._isCheckChanged += ChildIsCheckedChanged;
                }
            };
        }

        private void ChildIsCheckedChanged(object? sender, IsCheckChangedEventArgs args)
        {
            bool? isChecked = IsChecked;
            if (Children.All(c => c.IsChecked.HasValue && c.IsChecked.Value))
                isChecked = true;
            else if (Children.All(c => c.IsChecked.HasValue && !c.IsChecked.Value))
                isChecked = false;
            else if (Children.Any(c => c.IsChecked.HasValue && !c.IsChecked.Value) && Children.Any(c => c.IsChecked.HasValue && c.IsChecked.Value))
                isChecked = null;

            UpdateIsCheckedAndNotify(this, isChecked);
        }

        public string LevelLabel { get; }
        public int LevelId { get; }

        public bool? IsChecked
        {
            get => _isChecked;
            set
            {
                if (_isChecked != value)
                {
                    _isChecked = value;
                    FireIsCheckedChanged(this);
                    foreach (var child in Children)
                    {
                        child.UpdateIsCheckedFromParent(IsChecked);
                    }
                    OnPropertyChanged();
                }
            }
        }

        internal bool IsRoot { get; set; }
        public string ColumnLabel { get; }
        public int HierarchyLevel { get; }
        public ObservableCollection<LevelSelectionState> Children { get; } = new();

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private event IsCheckChangedEventHandler? _isCheckChanged;
        protected void FireIsCheckedChanged(LevelSelectionState sender)
        {
            _isCheckChanged?.Invoke(sender, new IsCheckChangedEventArgs(sender, sender.IsChecked));
        }

        private void UpdateIsCheckedAndNotify(LevelSelectionState sender, bool? isChecked)
        {
            if (_isChecked != isChecked)
            {
                _isChecked = isChecked;
                OnPropertyChanged(nameof(IsChecked));
                FireIsCheckedChanged(sender);
            }
        }

        private void UpdateIsCheckedFromParent(bool? parentIsChecked)
        {
            if (_isChecked != parentIsChecked)
            {
                _isChecked = parentIsChecked;
                foreach (var child in Children)
                {
                    child.UpdateIsCheckedFromParent(IsChecked);
                }
                OnPropertyChanged(nameof(IsChecked));
            }
        }

        public IEnumerator<LevelSelectionState> GetEnumerator()
        {
            return Children.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public static IEnumerable<LevelSelectionState> Flatten(LevelSelectionState parent)
        {
            var flat = new List<LevelSelectionState>() { parent };
            flat.AddRange(Flatten(parent.Children));
            return flat;
        }

        public static IEnumerable<LevelSelectionState> Flatten(IEnumerable<LevelSelectionState> collection)
        {
            foreach (var element in collection)
            {
                foreach (LevelSelectionState t in Flatten((IEnumerable<LevelSelectionState>)element))
                {
                    yield return t;
                }
                yield return element;
            }
        }

        public object Clone()
        {
            var newLevel = new LevelSelectionState(LevelLabel, LevelId, IsRoot, ColumnLabel, HierarchyLevel, IsChecked);
            foreach (var c in Children)
            {
                newLevel.Children.Add((LevelSelectionState)c.Clone());
            }
            return newLevel;
        }
    }
}
