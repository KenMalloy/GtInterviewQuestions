namespace GtInterviewQ2.Controllers
{
    public delegate void IsCheckChangedEventHandler(object? sender, IsCheckChangedEventArgs args);

    public class IsCheckChangedEventArgs(object? sender, bool? isChecked) : EventArgs
    {
        public object? Sender { get; } = sender;
        public bool? IsChecked { get; } = isChecked;
    }
}
