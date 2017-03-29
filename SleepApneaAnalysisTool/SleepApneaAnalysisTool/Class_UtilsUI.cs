using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.IO;
using System.Windows.Media;

using OxyPlot;

namespace SleepApneaAnalysisTool
{
  /// <summary>
  /// Helper functions used for UI purposes
  /// </summary>
  partial class Utils
  {
    /// <summary>
    /// Given a PlotModel, makes the axes and text of the PlotModel black or gray depending on whether the user selected a Dark theme or not
    /// </summary>
    /// <param name="plot"> The PlotModel to theme </param>
    /// <param name="UseDarkTheme"> True if the user is using a Dark theme </param>
    public static void ApplyThemeToPlot(PlotModel plot, bool UseDarkTheme)
    {
      if (plot != null)
      {
        // If using the Dark Theme the plot color should be Light Gray
        // If not using the Dark Theme the plot color should be Black
        var color = UseDarkTheme ? OxyColors.LightGray : OxyColors.Black;

        // Set all plot elements to the plot color
        plot.LegendTextColor = color;
        plot.TitleColor = color;
        plot.PlotAreaBorderColor = color;
        for (int x = 0; x < plot.Axes.Count; x++)
        {
          plot.Axes[x].AxislineColor = color;
          plot.Axes[x].ExtraGridlineColor = color;
          plot.Axes[x].MajorGridlineColor = color;
          plot.Axes[x].MinorGridlineColor = color;
          plot.Axes[x].MinorTicklineColor = color;
          plot.Axes[x].TextColor = color;
          plot.Axes[x].TicklineColor = color;
          plot.Axes[x].TitleColor = color;
        }
      }
    }

    /// <summary>
    /// Loads user personalization preferences into memory 
    /// </summary>
    /// <param name="UseCustomColor"> False if the UI should just use Window's theme color </param>
    /// <param name="ThemeColor"> The current user selected theme color of the UI </param>
    /// <param name="UseDarkTheme"> True if the UI should use a dark theme </param>
    public static void LoadPersonalization(out bool UseCustomColor, out Color ThemeColor, out bool UseDarkTheme)
    {
      if (!Directory.Exists(settings_folder))
        Directory.CreateDirectory(settings_folder);

      if (File.Exists(settings_folder + "\\personalization.txt"))
      {
        StreamReader sr = new StreamReader(settings_folder + "\\personalization.txt");
        UseCustomColor = bool.Parse(sr.ReadLine());
        string temp = sr.ReadLine();
        ThemeColor = Color.FromArgb(byte.Parse(temp.Split(',')[0]), byte.Parse(temp.Split(',')[1]), byte.Parse(temp.Split(',')[2]), byte.Parse(temp.Split(',')[3]));
        UseDarkTheme = bool.Parse(sr.ReadLine());
        sr.Close();
      }
      else
      {
        UseCustomColor = false;
        ThemeColor = Colors.AliceBlue;
        UseDarkTheme = false;
      }
    }
    /// <summary>
    /// Writes user personalization preferences into memory
    /// </summary>
    /// <param name="UseCustomColor"> False if the UI should just use Window's theme color </param>
    /// <param name="ThemeColor"> The current user selected theme color of the UI </param>
    /// <param name="UseDarkTheme"> True if the UI should use a dark theme </param>
    public static void WriteToPersonalization(bool UseCustomColor, Color ThemeColor, bool UseDarkTheme)
    {
      if (!Directory.Exists(settings_folder))
        Directory.CreateDirectory(settings_folder);

      StreamWriter sw = new StreamWriter(settings_folder + "\\personalization.txt");
      sw.WriteLine(UseCustomColor.ToString());
      sw.WriteLine(ThemeColor.A.ToString() + "," + ThemeColor.R.ToString() + "," + ThemeColor.G.ToString() + "," + ThemeColor.B.ToString());
      sw.WriteLine(UseDarkTheme.ToString());
      sw.Close();
    }
  }
}
