using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace DB_projectWPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private ViewModel model;
        public MainWindow()
        {
            model = new ViewModel();
            DataContext = model;
            InitializeComponent();
        }

        public interface IControlView
        {
            INotifyPropertyChanged ViewModel { get; set; }
        }

        private void Row_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            DataGridRow row = sender as DataGridRow;
            model.OpenSelectedMachine();
            // Some operations with this row
        }

    }
}
