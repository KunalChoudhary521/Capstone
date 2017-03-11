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
    #region Static Functions 

    // Personalization
    /// <summary>
    /// Given a PlotModel, makes the axes and text of the PlotModel black or gray depending on whether the user selected a Dark theme or not
    /// </summary>
    /// <param name="plot"> The PlotModel to theme </param>
    /// <param name="UseDarkTheme"> True if the user is using a Dark theme </param>
    public static void ApplyThemeToPlot(PlotModel plot, bool UseDarkTheme)
    {
      if (plot != null)
      {
        var color = UseDarkTheme ? OxyColors.LightGray : OxyColors.Black;

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

    // Epoch <-> DateTime

    /// <summary>
    /// The definition of epochs in seconds
    /// </summary>
    public static int EPOCH_SEC = 30;
    /// <summary>
    /// Converts an epoch point in time to a DateTime structure
    /// </summary>
    /// <param name="epoch"> The epoch point in time to convert </param>
    /// <param name="file"> 
    /// The EDFFile class used to determine the start 
    /// DateTime corresponding to epoch 0 
    /// </param>
    /// <returns> A DateTime structure corresponding the input epoch point in time </returns>
    public static DateTime EpochtoDateTime(int epoch, EDFFile file)
    {
      // DateTime = StartTime + (epoch - 1) * EPOCH_SEC
      return file.Header.StartDateTime + new TimeSpan(0, 0, (epoch - 1) * EPOCH_SEC);
    }
    /// <summary>
    /// Converts an epoch duration into a TimeSpan structure
    /// </summary>
    /// <param name="period"> The epoch duration to convert </param>
    /// <returns> The TimeSpan structure corresponding to the epoch duration </returns>
    public static TimeSpan EpochPeriodtoTimeSpan(int period)
    {
      // TimeSpan = period * EPOCH_SEC
      return new TimeSpan(0, 0, 0, period * EPOCH_SEC);
    }
    /// <summary>
    /// Converts a DateTime structure into an epoch point in time
    /// </summary>
    /// <param name="time"> The DateTime structure to convert </param>
    /// <param name="file"> 
    /// The EDFFile class used to determine the start
    /// DateTime corresponding to epoch 0 
    /// </param>
    /// <returns> The epoch point in time corresponding to the input DateTime </returns>
    public static int DateTimetoEpoch(DateTime time, EDFFile file)
    {
      // epoch = (DateTime - StartTime) / EPOCH_SEC
      return (int)((time - file.Header.StartDateTime).TotalSeconds / (double)EPOCH_SEC) + 1;
    }
    /// <summary>
    /// Converts a TimeSpan structure into an epoch duration
    /// </summary>
    /// <param name="period"> The TimeSpan structure to convert </param>
    /// <returns> The epoch duration corresponding to the input TimeSpan </returns>
    public static int TimeSpantoEpochPeriod(TimeSpan period)
    {
      // epoch = TimeSpan / EPOCH_SEC
      return (int)(period.TotalSeconds / (double)EPOCH_SEC);
    }

    /// <summary>
    /// Converts a DateTime structure into an epoch point in time
    /// </summary>
    /// <param name="time"> The DateTime structure to convert </param>
    /// <param name="start">
    /// The Start Time of epoch = 1
    /// </param>
    /// <returns> The epoch point in time corresponding to the input DateTime </returns>
    public static int DateTimetoEpoch(DateTime time, DateTime start)
    {
      // epoch = (DateTime - StartTime) / EPOCH_SEC
      return (int)((time - start).TotalSeconds / (double)EPOCH_SEC) + 1;
    }

    // Determining Signal Y Axis Extremes

    /// <summary>
    /// this percentile of a signal's values is used as the maximum Y axes value
    /// </summary>
    public static double percent_high = 99;
    /// <summary>
    /// this percentile of a signal's values is used as the minimum Y axes value
    /// </summary>
    public static double percent_low = 1;
    /// <summary>
    /// Gets a value at a specified percentile from an array
    /// </summary>
    /// <param name="values_array"> The input array </param>
    /// <param name="percentile"> The percentile of the desired value </param>
    /// <returns> The desired value at the specified percentile </returns>
    public static double? GetPercentileValue(float[] values_array, double percentile)
    {
      // Sort values in ascending order
      List<float> values = values_array.ToList();
      values.Sort();

      // index = percent * length 
      int index = (int)((double)percentile / (double)100 * (double)values.Count);

      // return desired value
      return values[Math.Max(0, Math.Min(index, values.Count - 1))];
    }
    /// <summary>
    /// Gets a value at a specified percentile from the difference between two arrays
    /// </summary>
    /// <param name="values_array_1"> The input minuend array </param>
    /// <param name="values_array_2"> The input subtrahend array </param>
    /// <param name="percentile"> The percentile of the desired value </param>
    /// <returns> The desired value at the specified percentile </returns>
    public static double? GetPercentileValueDeriv(float[] values_array_1, float[] values_array_2, double percentile)
    {
      // Subtract two input arrays from each other
      List<float> values1 = values_array_1.ToList();
      List<float> values2 = values_array_2.ToList();
      List<float> values = new List<float>();
      for (int x = 0; x < Math.Min(values_array_1.Length, values_array_2.Length); x++)
        values.Add(values_array_1[x] - values_array_2[x]);

      // Call GetPercentileValue on difference
      return GetPercentileValue(values.ToArray(), percentile);
    }
    /// <summary>
    /// Given a settings model and edf file, sets the Y axis bounds of a given signal
    /// </summary>
    /// <param name="Signal"> The signal to set the bounds for </param>
    /// <param name="LoadedEDFFile"> The EDF file with the signal's values </param>
    /// <param name="sm"> The settings model that bounds are stored in </param>
    public static void SetYBounds(string Signal, EDFFile LoadedEDFFile, SettingsModel sm)
    {
      string OrigName = Signal;
      SignalYAxisExtremes find = sm.SignalsYAxisExtremes.Find(temp => temp.SignalName.Trim() == Signal.Trim());

      if (find == null)
      {
        List<float> values = new List<float>();

        // Check if this signal needs filtering 
        FilteredSignal filteredSignal = sm.FilteredSignals.Find(temp => temp.SignalName == Signal);
        if (filteredSignal != null)
          Signal = sm.FilteredSignals.Find(temp => temp.SignalName == Signal).OriginalName;
        if (LoadedEDFFile.Header.Signals.Find(temp => temp.Label.Trim() == Signal) != null) // Regular Signal
        {
          EDFSignal edfsignal = LoadedEDFFile.Header.Signals.Find(temp => temp.Label.Trim() == Signal);
          values = LoadedEDFFile.retrieveSignalSampleValues(edfsignal);
        }
        else // EDF Signal 
        {
          // Get Signals
          DerivativeSignal deriv_info = sm.DerivedSignals.Find(temp => temp.DerivativeName == Signal);
          EDFSignal edfsignal1 = LoadedEDFFile.Header.Signals.Find(temp => temp.Label.Trim() == deriv_info.Signal1Name.Trim());
          EDFSignal edfsignal2 = LoadedEDFFile.Header.Signals.Find(temp => temp.Label.Trim() == deriv_info.Signal2Name.Trim());

          // Get Arrays and Perform Resampling if needed
          List<float> values1;
          List<float> values2;
          if (edfsignal1.NumberOfSamplesPerDataRecord == edfsignal2.NumberOfSamplesPerDataRecord) // No resampling
          {
            values1 = LoadedEDFFile.retrieveSignalSampleValues(edfsignal1);
            values2 = LoadedEDFFile.retrieveSignalSampleValues(edfsignal2);
          }
          else if (edfsignal1.NumberOfSamplesPerDataRecord > edfsignal2.NumberOfSamplesPerDataRecord) // Upsample signal 2
          {
            values1 = LoadedEDFFile.retrieveSignalSampleValues(edfsignal1);
            values2 = LoadedEDFFile.retrieveSignalSampleValues(edfsignal2);
            values2 = Utils.MATLAB_Resample(values2.ToArray(), edfsignal1.NumberOfSamplesPerDataRecord / edfsignal2.NumberOfSamplesPerDataRecord);
          }
          else // Upsample signal 1
          {
            values1 = LoadedEDFFile.retrieveSignalSampleValues(edfsignal1);
            values2 = LoadedEDFFile.retrieveSignalSampleValues(edfsignal2);
            values1 = Utils.MATLAB_Resample(values1.ToArray(), edfsignal2.NumberOfSamplesPerDataRecord / edfsignal1.NumberOfSamplesPerDataRecord);
          }

          for (int x = 0; x < Math.Min(values1.Count, values2.Count); x += 1)
          {
            values.Add(values1[x] - values2[x]);
          }
        }
        int last_unique = 0;
        for (int x = 0; x < values.Count; x++)
        {
          if (x > 0 && values[x] == values[last_unique])
            values[x] = float.NaN;
          else
            last_unique = x;
        }
        values.RemoveAll(temp => float.IsNaN(temp));
        values.Sort();
        int high_index = (int)(percent_high / 100 * (values.Count - 1));
        int low_index = (int)(percent_low / 100 * (values.Count - 1));
        float range = values[high_index] - values[low_index];
        float high_value = values[high_index] + range * (100 - (float)percent_high) / 100;
        float low_value = values[low_index] - range * ((float)percent_low) / 100;
        float av_value = values.Average();
        sm.SignalsYAxisExtremes.Add(new SignalYAxisExtremes(OrigName) { yMax = high_value, yMin = low_value, yAvr = av_value });
      }
    }
    /// <summary>
    /// Returns a signal's Y axis maximum bound
    /// </summary>
    /// <param name="Signal"> The signal to return Y axis bounds for </param>
    /// <param name="woBias"> True if the returned bounds assumes 0 DC </param>
    /// <param name="LoadedEDFFile"> The EDF File with all the signal's values in it </param>
    /// <param name="sm"> The settings model containing the bounds </param>
    /// <returns> The max y axis bounds of a signal</returns>
    public static double GetMaxSignalValue(string Signal, bool woBias, EDFFile LoadedEDFFile, SettingsModel sm)
    {
      SignalYAxisExtremes find = sm.SignalsYAxisExtremes.Find(temp => temp.SignalName.Trim() == Signal.Trim());

      if (find != null)
      {
        if (!Double.IsNaN(find.yMax) && !Double.IsNaN(find.yAvr))
        {
          if (woBias)
            return find.yMax - find.yAvr;
          else
            return find.yMax;
        }
        else
        {
          SetYBounds(Signal, LoadedEDFFile, sm);
          return GetMaxSignalValue(Signal, woBias, LoadedEDFFile, sm);
        }
      }
      else
      {
        SetYBounds(Signal, LoadedEDFFile, sm);
        return GetMaxSignalValue(Signal, woBias, LoadedEDFFile, sm);
      }
    }
    /// <summary>
    /// Returns a signal's Y axis minimum bound
    /// </summary>
    /// <param name="Signal"> The signal to return Y axis bounds for </param>
    /// <param name="woBias"> True if the returned bounds assumes 0 DC </param>
    /// <param name="LoadedEDFFile"> The EDF File with all the signal's values in it </param>
    /// <param name="sm"> The settings model containing the bounds </param>
    /// <returns> The min y axis bounds of a signal</returns>
    public static double GetMinSignalValue(string Signal, bool woBias, EDFFile LoadedEDFFile, SettingsModel sm)
    {
      SignalYAxisExtremes find = sm.SignalsYAxisExtremes.Find(temp => temp.SignalName.Trim() == Signal.Trim());

      if (find != null)
      {
        if (!Double.IsNaN(find.yMin) && !Double.IsNaN(find.yAvr))
        {
          if (woBias)
            return find.yMin - find.yAvr;
          else
            return find.yMin;
        }
        else
        {
          SetYBounds(Signal, LoadedEDFFile, sm);
          return GetMinSignalValue(Signal, woBias, LoadedEDFFile, sm);
        }
      }
      else
      {
        SetYBounds(Signal, LoadedEDFFile, sm);
        return GetMinSignalValue(Signal, woBias, LoadedEDFFile, sm);
      }
    }
    
    /// <summary>
    /// Performs upsampling and downsampling on an array of values
    /// </summary>
    /// <param name="values"> The input array to resample </param>
    /// <param name="ratio"> The ratio between upsampling and downsampling to perform </param>
    /// <returns> The resampled array </returns>
    public static List<float> MATLAB_Resample(float[] values, float ratio)
    {
      // Prepare Input for MATLAB function
      Processing proc = new Processing();
      MWArray[] input = new MWArray[2];
      input[0] = new MWNumericArray(values);
      input[1] = ratio;
      // Call MATLAB function
      return (
                  (double[])(
                      (MWNumericArray)proc.m_resample(1, input[0], input[1])[0]
                  ).ToVector(MWArrayComponent.Real)
                ).ToList().Select(temp => (float)temp).ToList();
    }
    /// <summary>
    /// Performs coherence analysis on 2 lists of values
    /// </summary>
    /// <param name="values1"> First list of values </param>
    /// <param name="values2"> Second list of values </param>
    /// <returns> Index 1 is Y axis, Index 2 is X axis </returns>
    public static LineSeries MATLAB_Coherence(float[] values1, float[] values2)
    {
      // Prepare Input for MATLAB function
      Processing proc = new Processing();
      MWArray[] input = new MWArray[3];
      input[0] = new MWNumericArray(values1.ToArray());
      input[1] = new MWNumericArray(values2.ToArray());
      input[2] = Math.Round(Math.Sqrt(Math.Max(values1.Length, values2.Length)));

      // Call MATLAB function
      MWArray[] output = proc.m_cohere(2, input[0], input[1], input[2]);
      double[] y_values = (double[])((MWNumericArray)output[0]).ToVector(MWArrayComponent.Real);
      double[] x_values = (double[])((MWNumericArray)output[1]).ToVector(MWArrayComponent.Real);

      LineSeries series = new LineSeries();
      for (int x = 0; x < y_values.Length; x++)
        series.Points.Add(new DataPoint(x_values[x], y_values[x]));

      return series;
    }

    // Settings Persistence
    public static SignalCategory[] LoadCategoriesFile(string[] AllSignals)
    {
      // Check if Settings directory exists
      if (!Directory.Exists("Settings"))
        Directory.CreateDirectory("Settings");

      // Return Value
      List<SignalCategory> temp = new List<SignalCategory>();

      // If the settings file exists
      if (File.Exists("Settings\\signal_categories.txt"))
      {
        // Open settings file
        StreamReader sr = new StreamReader("Settings\\signal_categories.txt");
        string[] text = sr.ReadToEnd().Replace("\r\n", "\n").Split('\n');

        // Foreach line in the settings file
        for (int x = 0; x < text.Length; x++)
        {
          string line = text[x];
           
          // The category name is the first value in the CSV line
          string category = line.Split(',')[0].Trim();
          List<string> category_signals = new List<string>();

          // For the other CSV values
          for (int y = 1; y < line.Split(',').Length; y++)
          {
            // If the value exists in the EDF file signals or derivatives
            if (AllSignals == null || AllSignals.ToList().Contains(line.Split(',')[y].Trim()))
            {
              // add signal
              category_signals.Add(line.Split(',')[y]);
            }
          }

          // If there is a non-zero number of signals in the category
          if (category_signals.Count > 0)
          {
            // Add to output
            temp.Add(new SignalCategory((temp.Count + 1) + ". " + category));
            temp[temp.Count - 1].Signals = category_signals;
          }
        }

        // Close text file
        sr.Close();
      }

      // Return values
      return temp.ToArray();
    }
    public static void WriteToCategoriesFile(SignalCategory[] SignalCategories, string[] AllSignals)
    {
      List<SignalCategory> current_SignalCategories = LoadCategoriesFile(null).ToList();
      List<string> AllSignalsList = AllSignals.ToList().Select(temp => temp.Trim()).ToList();

      // Check for removals
      for (int x = 0; x < current_SignalCategories.Count; x++)
      {
        List<string> to_remove = new List<string>();
        for (int y = 0; y < current_SignalCategories[x].Signals.Count; y++)
        {
          if (AllSignalsList.Contains(current_SignalCategories[x].Signals[y]))
          {
            to_remove.Add(current_SignalCategories[x].Signals[y]);
          }
        }
        for (int y = 0; y < to_remove.Count; y++)
        {
          current_SignalCategories[x].Signals.Remove(to_remove[y]);
        }
      }

      // Merge SignalCategories and current_SignalCategories
      for (int x = 0; x < SignalCategories.Length; x++)
      {
        bool match = false;
        for (int y = 0; y < current_SignalCategories.Count; y++)
        {
          if (current_SignalCategories[y].CategoryNameNoNumber == SignalCategories[x].CategoryNameNoNumber)
          {
            current_SignalCategories[y].Signals.AddRange(SignalCategories[x].Signals.ToArray());
            current_SignalCategories[y].Signals = current_SignalCategories[y].Signals.Distinct().ToList();
            match = true;
            break;
          }
        }
        
        if (!match)
        {
          current_SignalCategories.Add(SignalCategories[x]);
        }
      }

      StreamWriter sw = new StreamWriter("Settings\\signal_categories.txt");
      for (int x = 0; x < current_SignalCategories.Count; x++)
      {
        if (current_SignalCategories[x].Signals.Count > 0)
        {
          string line = current_SignalCategories[x].CategoryNameNoNumber.Trim();
          if (line.Trim() != "")
          {
            for (int y = 0; y < current_SignalCategories[x].Signals.Count; y++)
              line += "," + current_SignalCategories[x].Signals[y].Trim();

            sw.WriteLine(line);
          }
        }
      }
      sw.Close();
    }
    public static DerivativeSignal[] LoadDerivativesFile(EDFFile LoadedEDFFile)
    {
      if (!Directory.Exists("Settings"))
        Directory.CreateDirectory("Settings");

      List<DerivativeSignal> output = new List<DerivativeSignal>();

      if (File.Exists("Settings\\common_derivatives.txt"))
      {
        StreamReader sr = new StreamReader("Settings\\common_derivatives.txt");
        List<string> text = sr.ReadToEnd().Replace("\r\n", "\n").Split('\n').ToList();

        for (int x = 0; x < text.Count; x++)
        {
          string[] new_entry = text[x].Split(',');

          if (new_entry.Length == 3)
          {
            if (LoadedEDFFile == null || LoadedEDFFile.Header.Signals.Find(temp => temp.Label.Trim() == new_entry[1].Trim()) != null) // Signals Exist
            {
              if (LoadedEDFFile == null || LoadedEDFFile.Header.Signals.Find(temp => temp.Label.Trim() == new_entry[2].Trim()) != null) // Signals Exist
              {
                if (LoadedEDFFile == null || LoadedEDFFile.Header.Signals.Find(temp => temp.Label.Trim() == new_entry[0].Trim()) == null) // Unique Name
                {
                  if (LoadedEDFFile == null || output.Where(temp => temp.DerivativeName.Trim() == new_entry[0].Trim()).ToList().Count == 0) // Unique Name
                  {
                    output.Add(new DerivativeSignal(new_entry[0], new_entry[1], new_entry[2]));
                  }
                }
              }
            }
          }
        }

        sr.Close();
      }

      return output.ToArray();
    }
    public static void WriteToDerivativesFile(DerivativeSignal[] DerivativeSignals, string[] AllSignals)
    {
      List<DerivativeSignal> current_DerivativeSignals = LoadDerivativesFile(null).ToList();
      List<string> AllSignalsList = AllSignals.ToList().Select(temp => temp.Trim()).ToList();

      // Check for removals
      List<DerivativeSignal> to_remove = new List<DerivativeSignal>();
      for (int x = 0; x < current_DerivativeSignals.Count; x++)
      {
        if (AllSignalsList.Contains(current_DerivativeSignals[x].Signal1Name))
        {
          if (AllSignalsList.Contains(current_DerivativeSignals[x].Signal2Name))
          {
            to_remove.Add(current_DerivativeSignals[x]);
          }
        }
      }
      for (int x = 0; x < to_remove.Count; x++)
      {
        current_DerivativeSignals.Remove(to_remove[x]);
      }

      // Merge current and loaded
      for (int x = 0; x < DerivativeSignals.Length; x++)
      {
        if (current_DerivativeSignals.Where(temp =>
        temp.DerivativeName == DerivativeSignals[x].DerivativeName &&
        temp.Signal1Name == DerivativeSignals[x].Signal1Name &&
        temp.Signal2Name == DerivativeSignals[x].Signal2Name).ToList().Count == 0)
        {
          current_DerivativeSignals.Add(DerivativeSignals[x]);
        }
      }

      // Write to file
      StreamWriter sw = new StreamWriter("Settings\\common_derivatives.txt");
      for (int x = 0; x < current_DerivativeSignals.Count; x++)
      {
        sw.WriteLine(current_DerivativeSignals[x].DerivativeName + "," + current_DerivativeSignals[x].Signal1Name + "," + current_DerivativeSignals[x].Signal2Name);
      }
      sw.Close();
    }
    public static FilteredSignal[] LoadFilteredSignalsFile(string[] AllSignals)
    {
      if (!Directory.Exists("Settings"))
        Directory.CreateDirectory("Settings");

      List<FilteredSignal> filteredSignals = new List<FilteredSignal>();

      if (File.Exists("Settings\\filtered.txt"))
      {
        StreamReader sr = new StreamReader("Settings\\filtered.txt");
        string[] lines = sr.ReadToEnd().Replace("\r\n", "\n").Split('\n');

        for (int x = 0; x < lines.Length; x++)
        {
          string[] curr = lines[x].Split(',');

          if (curr.Length == 6)
          {
            FilteredSignal fr = new FilteredSignal();
            fr.SignalName = curr[0];
            fr.OriginalName = curr[1];
            fr.LowPass_Enabled = bool.Parse(curr[2]);
            fr.LowPassCutoff = float.Parse(curr[3]);
            fr.Average_Enabled = bool.Parse(curr[4]);
            fr.Average_Length = float.Parse(curr[5]);

            if (AllSignals == null || AllSignals.Contains(fr.OriginalName))
            {
              filteredSignals.Add(fr);
            }
          }
        }

        sr.Close();
      }

      return filteredSignals.ToArray();
    }
    public static void WriteToFilteredSignalsFile(FilteredSignal[] FilteredSignals, string[] AllSignals)
    {
      if (!Directory.Exists("Settings"))
        Directory.CreateDirectory("Settings");

      List<FilteredSignal> curr_filterSignals = LoadFilteredSignalsFile(null).ToList();
      curr_filterSignals.RemoveAll(temp => AllSignals.ToList().Contains(temp.OriginalName));
      curr_filterSignals.AddRange(FilteredSignals);

      StreamWriter sw = new StreamWriter("Settings\\filtered.txt");
      for (int x = 0; x < curr_filterSignals.Count; x++)
      {
        sw.WriteLine(
          curr_filterSignals[x].SignalName + "," +
          curr_filterSignals[x].OriginalName + "," +
          curr_filterSignals[x].LowPass_Enabled.ToString() + "," +
          curr_filterSignals[x].LowPassCutoff.ToString() + "," +
          curr_filterSignals[x].Average_Enabled.ToString() + "," +
          curr_filterSignals[x].Average_Length.ToString());
      }
      sw.Close();
    }
    public static string[] LoadHiddenSignalsFile()
    {
      if (!Directory.Exists("Settings"))
        Directory.CreateDirectory("Settings");

      List<string> output = new List<string>();

      if (File.Exists("Settings\\hiddensignals.txt"))
      {
        StreamReader sr = new StreamReader("Settings\\hiddensignals.txt");
        output = sr.ReadToEnd().Replace("\r\n", "\n").Split('\n').ToList();
        output = output.Select(temp => temp.Trim()).Where(temp => temp != "").ToList();
        sr.Close();
      }

      return output.ToArray();
    }
    public static void WriteToHiddenSignalsFile(string[] hidden_signals)
    {
      if (!Directory.Exists("Settings"))
        Directory.CreateDirectory("Settings");

      StreamWriter sw = new StreamWriter("Settings\\hiddensignals.txt");
      for (int x = 0; x < hidden_signals.Length; x++)
      {
        sw.WriteLine(hidden_signals[x]);
      }
      sw.Close();
    }
    public static void LoadPersonalization(out bool UseCustomColor, out Color ThemeColor, out bool UseDarkTheme)
    {
      if (!Directory.Exists("Settings"))
        Directory.CreateDirectory("Settings");

      if (File.Exists("Settings\\personalization.txt"))
      {
        StreamReader sr = new StreamReader("Settings\\personalization.txt");
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
    public static void WriteToPersonalization(bool UseCustomColor, Color ThemeColor, bool UseDarkTheme)
    {
      if (!Directory.Exists("Settings"))
        Directory.CreateDirectory("Settings");

      StreamWriter sw = new StreamWriter("Settings\\personalization.txt");
      sw.WriteLine(UseCustomColor.ToString());
      sw.WriteLine(ThemeColor.A.ToString() + "," + ThemeColor.R.ToString() + "," + ThemeColor.G.ToString() + "," + ThemeColor.B.ToString());
      sw.WriteLine(UseDarkTheme.ToString());
      sw.Close();
    }
    
    // Filters
    public static LineSeries ApplyAverageFilter(LineSeries series, int LENGTH)
    {
      double[] input = series.Points.Select(temp => temp.Y).ToArray();
      double[] result = new double[series.Points.Count];

      for (int x = 0; x < input.Length; x++)
      {
        double sum = 0;
        for (int y = x - LENGTH; y < x + LENGTH; y++)
          sum += input[Math.Min(Math.Max(y, 0), input.Length - 1)];

        result[x] = sum / (2 * LENGTH);
      }
      
      LineSeries series_new = new LineSeries();
      for (int x = 0; x < result.Length; x++)
      {
        series_new.Points.Add(new DataPoint(series.Points[x].X, result[x]));
      }

      return series_new;
    }
    public static LineSeries ApplyLowPassFilter(LineSeries series, float cutoff, float sample_period)
    {
      OnlineFirFilter filter = (OnlineFirFilter) OnlineFirFilter.CreateLowpass(MathNet.Filtering.ImpulseResponse.Finite, (double)(1 / sample_period), cutoff);
      double[] result = filter.ProcessSamples(series.Points.Select(temp => temp.Y).ToArray());

      LineSeries new_series = new LineSeries();
      for (int x = 0; x < result.Length; x++)
      {
        new_series.Points.Add(new DataPoint(series.Points[x].X, result[x]));
      }

      return new_series;
    }

    // Export
    public static void ExportImage(PlotModel plot, string fileName)
    {
      var export = new OxyPlot.Wpf.PngExporter();
      export.Width = 1280;
      export.Height = 720;
      export.Background = OxyColors.White;

      MemoryStream stream = new MemoryStream();
      FileStream file = new FileStream(fileName, FileMode.Create);

      export.Export(plot, stream);
      stream.WriteTo(file);
      file.Close();
      stream.Close();
    }

    #endregion 
  }
}
