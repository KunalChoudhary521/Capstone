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

namespace SleepApneaDiagnoser
{
    /// <summary>
    /// Interaction logic for Dialog_ManageCategories.xaml
    /// </summary>
    public partial class Dialog_ManageCategories : Window
    {
        public Dialog_ManageCategories()
        {
            InitializeComponent();
        }

        private void button_AddCategory_Click(object sender, RoutedEventArgs e)
        {
            listBox_Categories.Items.Add(new TextBox() { Text = "New Channel" });
        }
    }
}
