using System.Windows;
using FritzboxPhonebookConv.ViewModels;

namespace FritzboxPhonebookConv
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Propagates the PasswordBox content to the view-model.
        /// PasswordBox cannot be directly bound for security reasons, so we
        /// update the VM's Password property from the code-behind event handler.
        /// </summary>
        private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (DataContext is MainViewModel vm)
                vm.Password = PasswordBox.Password;
        }
    }
}
