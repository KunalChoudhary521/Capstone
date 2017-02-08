using System;
using System.Collections.Generic;
using System.Linq;
using OxyPlot;
using OxyPlot.Series;
using EDF;
using MathWorks.MATLAB.NET.Arrays;
using MATLAB_496;
using System.IO;
using MathNet.Filtering.FIR;
using MathNet.Filtering.IIR;
using MahApps.Metro;
using System.Windows.Media;
using System.Windows;

namespace SleepApneaDiagnoser
{
  partial class Utils
  {
    // Personalization

    /// <summary>
    /// Modified From Sample MahApps.Metro Project
    /// https://github.com/punker76/code-samples/blob/master/MahAppsMetroThemesSample/MahAppsMetroThemesSample/ThemeManagerHelper.cs
    /// </summary>
    public static Accent ThemeColorToAccent(Color color)
    {
      byte a = color.A;
      byte g = color.G;
      byte r = color.R;
      byte b = color.B;

      // create a runtime accent resource dictionary

      var resourceDictionary = new ResourceDictionary();

      resourceDictionary.Add("HighlightColor", Color.FromArgb(a, r, g, b));
      resourceDictionary.Add("AccentColor", Color.FromArgb(a, r, g, b));
      resourceDictionary.Add("AccentColor2", Color.FromArgb(a, r, g, b));
      resourceDictionary.Add("AccentColor3", Color.FromArgb(a, r, g, b));
      resourceDictionary.Add("AccentColor4", Color.FromArgb(a, r, g, b));

      resourceDictionary.Add("HighlightBrush", new SolidColorBrush((Color)resourceDictionary["HighlightColor"]));
      resourceDictionary.Add("AccentColorBrush", new SolidColorBrush((Color)resourceDictionary["AccentColor"]));
      resourceDictionary.Add("AccentColorBrush2", new SolidColorBrush((Color)resourceDictionary["AccentColor2"]));
      resourceDictionary.Add("AccentColorBrush3", new SolidColorBrush((Color)resourceDictionary["AccentColor3"]));
      resourceDictionary.Add("AccentColorBrush4", new SolidColorBrush((Color)resourceDictionary["AccentColor4"]));
      resourceDictionary.Add("WindowTitleColorBrush", new SolidColorBrush((Color)resourceDictionary["AccentColor"]));

      resourceDictionary.Add("ProgressBrush", new LinearGradientBrush(
          new GradientStopCollection(new[]
          {
                new GradientStop((Color)resourceDictionary["HighlightColor"], 0),
                new GradientStop((Color)resourceDictionary["AccentColor3"], 1)
          }),
          new Point(0.001, 0.5), new Point(1.002, 0.5)));

      resourceDictionary.Add("CheckmarkFill", new SolidColorBrush((Color)resourceDictionary["AccentColor"]));
      resourceDictionary.Add("RightArrowFill", new SolidColorBrush((Color)resourceDictionary["AccentColor"]));

      resourceDictionary.Add("IdealForegroundColor", Colors.White);
      resourceDictionary.Add("IdealForegroundColorBrush", new SolidColorBrush((Color)resourceDictionary["IdealForegroundColor"]));
      resourceDictionary.Add("AccentSelectedColorBrush", new SolidColorBrush((Color)resourceDictionary["IdealForegroundColor"]));

      // DataGrid brushes since latest alpha after 1.1.2
      resourceDictionary.Add("MetroDataGrid.HighlightBrush", new SolidColorBrush((Color)resourceDictionary["AccentColor"]));
      resourceDictionary.Add("MetroDataGrid.HighlightTextBrush", new SolidColorBrush((Color)resourceDictionary["IdealForegroundColor"]));
      resourceDictionary.Add("MetroDataGrid.MouseOverHighlightBrush", new SolidColorBrush((Color)resourceDictionary["AccentColor3"]));
      resourceDictionary.Add("MetroDataGrid.FocusBorderBrush", new SolidColorBrush((Color)resourceDictionary["AccentColor"]));
      resourceDictionary.Add("MetroDataGrid.InactiveSelectionHighlightBrush", new SolidColorBrush((Color)resourceDictionary["AccentColor2"]));
      resourceDictionary.Add("MetroDataGrid.InactiveSelectionHighlightTextBrush", new SolidColorBrush((Color)resourceDictionary["IdealForegroundColor"]));

      // applying theme to MahApps

      var resDictName = string.Format("ApplicationAccent_{0}.xaml", Color.FromArgb(a, r, g, b).ToString().Replace("#", string.Empty));
      var fileName = System.IO.Path.Combine(System.IO.Path.GetTempPath(), resDictName);
      using (var writer = System.Xml.XmlWriter.Create(fileName, new System.Xml.XmlWriterSettings { Indent = true }))
      {
        System.Windows.Markup.XamlWriter.Save(resourceDictionary, writer);
        writer.Close();
      }

      resourceDictionary = new ResourceDictionary() { Source = new Uri(fileName, UriKind.Absolute) };

      return new Accent { Name = string.Format("ApplicationAccent_{0}.xaml", Color.FromArgb(a, r, g, b).ToString().Replace("#", string.Empty)), Resources = resourceDictionary };
    }
  }
}
