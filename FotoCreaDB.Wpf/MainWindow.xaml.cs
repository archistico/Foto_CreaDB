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
    }
}