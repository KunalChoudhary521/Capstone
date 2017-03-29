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
using MahApps.Metro.Controls;

namespace SleepApneaAnalysisTool
{
  /// <summary>
  /// Interaction logic for Dialog_ManageCategories.xaml
  /// </summary>
  public partial class Dialog_Manage_Categories
  {
    /// <summary>
    /// From stack overflow http://stackoverflow.com/questions/5181063/how-to-access-a-specific-item-in-a-listbox-with-datatemplate
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="depObj"></param>
    /// <returns></returns>
    private T FindDescendant<T>(DependencyObject obj) where T : DependencyObject
    {
      // Check if this object is the specified type
      if (obj is T)
        return obj as T;

      // Check for children
      int childrenCount = VisualTreeHelper.GetChildrenCount(obj);
      if (childrenCount < 1)
        return null;

      // First check all the children
      for (int i = 0; i < childrenCount; i++)
      {
        DependencyObject child = VisualTreeHelper.GetChild(obj, i);
        if (child is T)
          return child as T;
      }

      // Then check the childrens children
      for (int i = 0; i < childrenCount; i++)
      {
        DependencyObject child = FindDescendant<T>(VisualTreeHelper.GetChild(obj, i));
        if (child != null && child is T)
          return child as T;
      }

      return null;
    }
    
    public List<SignalCategory> categories;

    private string[] all_signals;
    private MetroWindow window;
    private SettingsModelView model;

    public Dialog_Manage_Categories(MainWindow i_window, SettingsModelView i_model)
    {
      InitializeComponent();
      VirtualizingStackPanel.SetIsVirtualizing(listBox_Categories, false);

      categories = i_model.sm.SignalCategories.ToArray().ToList();
      all_signals = i_model.AllSignals.ToArray();

      for (int x = 0; x < categories.Count; x++)
      {
        TextBox tb = new TextBox();
        tb.Text = categories[x].CategoryName.Substring(categories[x].CategoryName.IndexOf('.') + 1).Trim();
        listBox_Categories.Items.Add(tb);
      }

      window = i_window;
      model = i_model;
    }

    private void button_AddCategory_Click(object sender, RoutedEventArgs e)
    {
      TextBox tb = new TextBox();
      tb.Text = "New Category";

      listBox_Categories.Items.Add(tb);
      categories.Add(new SignalCategory(tb.Text));
    }
    private void button_RemoveCategory_Click(object sender, RoutedEventArgs e)
    {
      if (listBox_Categories.SelectedItem != null)
      {
        categories.RemoveAt(listBox_Categories.SelectedIndex);
        listBox_Categories.Items.Remove(listBox_Categories.SelectedItem);
      }
    }

    private void listBox_Categories_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
      int u = listBox_Categories.SelectedIndex;

      if (u != -1)
      {
        listBox_Signals.SelectionChanged -= listBox_Signals_SelectionChanged;
        listBox_Signals.Items.Clear();
        for (int x = 0; x < all_signals.Length; x++)
        {
          listBox_Signals.Items.Add(all_signals[x]);
          if (categories[u].Signals.Contains(all_signals[x]))
            listBox_Signals.SelectedItems.Add(all_signals[x]);
        }
        listBox_Signals.SelectionChanged += listBox_Signals_SelectionChanged;
      }
      else
      {
        listBox_Signals.Items.Clear();
      }
    }
    private void listBox_Signals_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
      int u = listBox_Categories.SelectedIndex;

      if (u != -1)
      {
        categories[u].Signals.Clear();
        for (int x = 0; x < listBox_Signals.SelectedItems.Count; x++)
          categories[u].Signals.Add(listBox_Signals.SelectedItems[x].ToString());
      }
    }

    private void button_Done_Click(object sender, RoutedEventArgs e)
    {
      for (int x = 0; x < categories.Count; x++)
        categories[x].CategoryName = (x + 1).ToString() + ". " + ((TextBox)listBox_Categories.Items[x]).Text;

      model.ManageCategoriesOutput(categories.ToArray());
      DialogManager.HideMetroDialogAsync(window, this);
    }
  }
}
