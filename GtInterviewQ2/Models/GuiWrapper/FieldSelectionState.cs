using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace GtInterviewQ2.Models.GuiWrapper
{
    public class FieldSelectionState(string fieldLabel, bool isChecked = true) : INotifyPropertyChanged, ICloneable
    {
        private bool _isChecked = isChecked;

        public string FieldLabel { get; } = fieldLabel;

        public bool IsChecked
        {
            get => _isChecked;
            set
            {
                if (_isChecked != value)
                {
                    _isChecked = value;
                    OnPropertyChanged();
                }
            }
        }


        public event PropertyChangedEventHandler? PropertyChanged;

        public object Clone()
        {
            return new FieldSelectionState(FieldLabel, IsChecked);
        }

        protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
