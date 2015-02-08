using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace PeoplePicker_Port_Tester
{
    /// <summary>
    /// Interaction logic for AboutDialogBox.xaml
    /// </summary>
    public partial class AboutDialogBox : Window
    {
        public AboutDialogBox()
        {
            InitializeComponent();
        }

        private void CloseClick(Object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Hyperlink_OnRequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));
            e.Handled = true;
        }
    }
}
