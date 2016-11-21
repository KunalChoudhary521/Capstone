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
using System.Windows.Shapes;
using MahApps.Metro.Controls.Dialogs;

namespace SleepApneaDiagnoser
{
  /// <summary>
  /// Interaction logic for Dialog_Error.xaml
  /// </summary>
  public partial class Dialog_Error
  {
    public Dialog_Error(string error_message)
    {
      InitializeComponent();
      this.textBox_message.Text = error_message;
    }

    private void button_close_Click(object sender, RoutedEventArgs e)
    {
      this.DialogResult = false;
      this.Close();
    }
  }
}
