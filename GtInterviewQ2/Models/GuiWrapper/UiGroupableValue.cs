using System.Collections.ObjectModel;

namespace GtInterviewQ2.Models.GuiWrapper
{
    public class UiGroupableValue(string displayLabel, Dictionary<string, decimal>? values = null)
    {
        public string GroupLabel { get; } = displayLabel;

        public Dictionary<string, decimal> Values { get; } = values ?? new();

        public ObservableCollection<UiGroupableValue> Children { get; } = new ObservableCollection<UiGroupableValue>();
    }
}
