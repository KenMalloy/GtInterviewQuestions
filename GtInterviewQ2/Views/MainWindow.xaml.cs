using GtInterviewQ2.Controllers;
using System.Windows;

namespace GtInterviewQ2
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            this.DataContext = new MainWindowController();
        }
    }
}