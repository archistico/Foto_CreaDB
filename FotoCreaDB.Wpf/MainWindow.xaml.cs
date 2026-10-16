using System.Windows;
using FotoCreaDB.Wpf.ViewModels;

namespace FotoCreaDB.Wpf
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = new MainViewModel();
        }

        protected override void OnClosed(System.EventArgs e)
        {
            if (DataContext is System.IDisposable disposable) disposable.Dispose();
            base.OnClosed(e);
        }
    }
}
