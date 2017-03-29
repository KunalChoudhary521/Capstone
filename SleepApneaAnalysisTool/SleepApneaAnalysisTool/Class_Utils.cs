using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.IO;

using OxyPlot;
using OxyPlot.Series;

using EDF;

using MathWorks.MATLAB.NET.Arrays;
using MATLAB_496;

using Excel = Microsoft.Office.Interop.Excel;

namespace SleepApneaAnalysisTool
{
  /// <summary>
  /// Helper functions used in the business logic
  /// </summary>
  partial class Utils
  {
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
    /// Converts an epoch point in time to a DateTime structure
    /// </summary>
    /// <param name="epoch"> The epoch point in time to convert </param>
    /// <param name="file"> 
    /// The EDFFile class used to determine the start 
    /// DateTime corresponding to epoch 0 
    /// </param>
    /// <returns> A DateTime structure corresponding the input epoch point in time </returns>
    public static DateTime EpochtoDateTime(int epoch, DateTime start)
    {
      // DateTime = StartTime + (epoch - 1) * EPOCH_SEC
      return start + new TimeSpan(0, 0, (epoch - 1) * EPOCH_SEC);
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
      // Save Signal Name
      string OrigName = Signal;

      // Check to see if the Signal Y Bounds have already been calculated 
      SignalYAxisExtremes find = sm.SignalsYAxisExtremes.Find(temp => temp.SignalName.Trim() == Signal.Trim());

      // If the Signal Y Bounds have not been calculated
      if (find == null)
      {
        List<float> values = new List<float>();

        // Check if this signal needs filtering 
        FilteredSignal filteredSignal = sm.FilteredSignals.Find(temp => temp.SignalName == Signal);
        if (filteredSignal != null)
          Signal = sm.FilteredSignals.Find(temp => temp.SignalName == Signal).OriginalName;

        if (LoadedEDFFile.Header.Signals.Find(temp => temp.Label.Trim() == Signal) != null) // Regular Signal
        {
          // Get the EDF Signal Values
          EDFSignal edfsignal = LoadedEDFFile.Header.Signals.Find(temp => temp.Label.Trim() == Signal);
          values = LoadedEDFFile.retrieveSignalSampleValues(edfsignal);
        }
        else // Derivative Signal 
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

        // Remove repeated values 
        int last_unique = 0;
        for (int x = 0; x < values.Count; x++)
        {
          if (x > 0 && values[x] == values[last_unique])
            values[x] = float.NaN;
          else
            last_unique = x;
        }
        values.RemoveAll(temp => float.IsNaN(temp));

        // Find the high and low percentiles of the signal and the average value of the signal
        values.Sort();
        int high_index = (int)(percent_high / 100 * (values.Count - 1));
        int low_index = (int)(percent_low / 100 * (values.Count - 1));
        float range = values[high_index] - values[low_index];
        float high_value = values[high_index] + range * (100 - (float)percent_high) / 100;
        float low_value = values[low_index] - range * ((float)percent_low) / 100;
        float av_value = values.Average();

        // Save the values so that they do not have to be recalculated 
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
      // Check if the Y Bounds have already been calculated 
      SignalYAxisExtremes find = sm.SignalsYAxisExtremes.Find(temp => temp.SignalName.Trim() == Signal.Trim());
      if (find != null) // If the Y Bounds have been calculated 
      {
        if (!Double.IsNaN(find.yMax) && !Double.IsNaN(find.yAvr)) // Double checking if the Y Bounds have been calculated
        {
          // Remove DC bias?
          if (woBias) // Yes
            return find.yMax - find.yAvr; 
          else // No
            return find.yMax;
        }
        else // Calculate Y Bounds 
        {
          SetYBounds(Signal, LoadedEDFFile, sm);
          return GetMaxSignalValue(Signal, woBias, LoadedEDFFile, sm);
        }
      }
      else  // Calculate Y Bounds 
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
      // Check if the Y Bounds have already been calculated
      SignalYAxisExtremes find = sm.SignalsYAxisExtremes.Find(temp => temp.SignalName.Trim() == Signal.Trim());
      if (find != null) // If the Y Bounds have been calculated
      {
        if (!Double.IsNaN(find.yMin) && !Double.IsNaN(find.yAvr)) // Double check if the Y Bounds have been calculated
        {
          // Remove DC bias?
          if (woBias) // Yes
            return find.yMin - find.yAvr;
          else // No
            return find.yMin;
        }
        else // Calculate Y Bounds 
        {
          SetYBounds(Signal, LoadedEDFFile, sm);
          return GetMinSignalValue(Signal, woBias, LoadedEDFFile, sm);
        }
      }
      else // Calculate Y Bounds 
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

    /// <summary>
    /// The directory path of where the application saves all settings files
    /// </summary>
    public static string settings_folder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\SleepApneaAnalysisTool\\Settings";
    /// <summary>
    /// Loads the signal category definitions into memory
    /// </summary>
    /// <param name="AllSignals"> All signals loaded from the EDF file or specified as derivatives or filtered signals </param>
    /// <returns> The signal category definitions </returns>
    public static SignalCategory[] LoadCategoriesFile(string[] AllSignals)
    {
      // Check if Settings directory exists
      if (!Directory.Exists(settings_folder))
        Directory.CreateDirectory(settings_folder);

      // Return Value
      List<SignalCategory> temp = new List<SignalCategory>();

      // If the settings file exists
      if (File.Exists(settings_folder + "\\signal_categories.txt"))
      {
        // Open settings file
        StreamReader sr = new StreamReader(settings_folder + "\\signal_categories.txt");
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
    /// <summary>
    /// Writes the signal category definitions to the settings text file
    /// </summary>
    /// <param name="SignalCategories"> The signal category definitions </param>
    /// <param name="AllSignals"> All signals loaded from the EDF file or specified as derivatives or filtered signals </param>
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

      // Write merged list to settings file
      StreamWriter sw = new StreamWriter(settings_folder + "\\signal_categories.txt");
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
    /// <summary>
    /// Loads the derivative definitions into memory
    /// </summary>
    /// <param name="LoadedEDFFile"> The loaded EDF structure </param>
    /// <returns> The derivative definitions </returns>
    public static DerivativeSignal[] LoadDerivativesFile(EDFFile LoadedEDFFile)
    {
      if (!Directory.Exists(settings_folder))
        Directory.CreateDirectory(settings_folder);

      List<DerivativeSignal> output = new List<DerivativeSignal>();

      if (File.Exists(settings_folder + "\\common_derivatives.txt"))
      {
        StreamReader sr = new StreamReader(settings_folder + "\\common_derivatives.txt");
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
    /// <summary>
    /// Writes the derivative definitions to the settings text file
    /// </summary>
    /// <param name="DerivativeSignals"> The derivative definitions </param>
    /// <param name="AllSignals"> All signals loaded from the EDF file or specified as derivatives or filtered signals </param>
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
      StreamWriter sw = new StreamWriter(settings_folder + "\\common_derivatives.txt");
      for (int x = 0; x < current_DerivativeSignals.Count; x++)
      {
        sw.WriteLine(current_DerivativeSignals[x].DerivativeName + "," + current_DerivativeSignals[x].Signal1Name + "," + current_DerivativeSignals[x].Signal2Name);
      }
      sw.Close();
    }
    /// <summary>
    /// Loads the filtered signal definitions into memory
    /// </summary>
    /// <param name="AllSignals"> All signals loaded from the EDF file or specified as derivatives or filtered signals </param>
    /// <returns> The filtered signal definitions </returns>
    public static FilteredSignal[] LoadFilteredSignalsFile(string[] AllSignals)
    {
      if (!Directory.Exists(settings_folder))
        Directory.CreateDirectory(settings_folder);

      List<FilteredSignal> filteredSignals = new List<FilteredSignal>();

      if (File.Exists(settings_folder + "\\filtered.txt"))
      {
        StreamReader sr = new StreamReader(settings_folder + "\\filtered.txt");
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
    /// <summary>
    /// Writes the filtered signal definitions to the settings text file
    /// </summary>
    /// <param name="FilteredSignals"> The filtered signal definitions </param>
    /// <param name="AllSignals"> All signals loaded from the EDF file or specified as derivatives or filtered signals </param>
    public static void WriteToFilteredSignalsFile(FilteredSignal[] FilteredSignals, string[] AllSignals)
    {
      if (!Directory.Exists(settings_folder))
        Directory.CreateDirectory(settings_folder);

      List<FilteredSignal> curr_filterSignals = LoadFilteredSignalsFile(null).ToList();
      curr_filterSignals.RemoveAll(temp => AllSignals.ToList().Contains(temp.OriginalName));
      curr_filterSignals.AddRange(FilteredSignals);

      StreamWriter sw = new StreamWriter(settings_folder + "\\filtered.txt");
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
    /// <summary>
    /// Loads the list of names of hidden signals into memory
    /// </summary>
    /// <returns> A list of hidden signal names </returns>
    public static string[] LoadHiddenSignalsFile()
    {
      if (!Directory.Exists(settings_folder))
        Directory.CreateDirectory(settings_folder);

      List<string> output = new List<string>();

      if (File.Exists(settings_folder + "\\hiddensignals.txt"))
      {
        StreamReader sr = new StreamReader(settings_folder + "\\hiddensignals.txt");
        output = sr.ReadToEnd().Replace("\r\n", "\n").Split('\n').ToList();
        output = output.Select(temp => temp.Trim()).Where(temp => temp != "").ToList();
        sr.Close();
      }

      return output.ToArray();
    }
    /// <summary>
    /// Writes the list of names of hidden signals to a settings text file
    /// </summary>
    /// <param name="hidden_signals"> A list of hidden signal names </param>
    public static void WriteToHiddenSignalsFile(string[] hidden_signals)
    {
      if (!Directory.Exists(settings_folder))
        Directory.CreateDirectory(settings_folder);

      StreamWriter sw = new StreamWriter(settings_folder + "\\hiddensignals.txt");
      for (int x = 0; x < hidden_signals.Length; x++)
      {
        sw.WriteLine(hidden_signals[x]);
      }
      sw.Close();
    }
    
    /// <summary>
    /// Applies a Moving Average Filter to the input signal
    /// </summary>
    /// <param name="series"> The series of X and Y values to be filtered </param>
    /// <param name="LENGTH"> The half length of the discrete-time filter's impulse response </param>
    /// <returns> The filtered signal values </returns>
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
    /// <summary>
    /// Applies a discretized Low Pass Single Pole Filter to the input signal
    /// </summary>
    /// <param name="series"> The series of X and Y values to be filtered </param>
    /// <param name="cutoff"> The desired cutoff frequence (Hz) </param>
    /// <param name="sample_period"> The sample period of the signal </param>
    /// <returns> The filtered signal values </returns>
    public static LineSeries ApplyLowPassFilter(LineSeries series, float cutoff, float sample_period)
    {
      double RC = 1 / (2 * Math.PI * cutoff);
      double alpha = sample_period / (RC + sample_period);

      double[] result = new double[series.Points.Count];

      result[0] = series.Points[0].Y;
      for (int x = 1; x < result.Length; x++)
      {
        result[x] = alpha * series.Points[x].Y + (1 - alpha) * result[x-1];
      }

      LineSeries new_series = new LineSeries();
      for (int x = 0; x < result.Length; x++)
      {
        new_series.Points.Add(new DataPoint(series.Points[x].X, result[x]));
      }

      return new_series;
    }
    
    /// <summary>
    /// Exports an OxyPlot PlotModel to a PNG image file
    /// </summary>
    /// <param name="plot"> The PlotModel to export </param>
    /// <param name="fileName"> The path to the image file to be created </param>
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
 
    /// <summary>
    /// Writes an Excel worksheet containing a Respiratory Signal's values and Plot
    /// </summary>
    /// <param name="ws"> The Excel worksheet object </param>
    /// <param name="SignalName"> The name of the respiratory signal </param>
    /// <param name="table"> The respiratory signal values and peak/onset locations </param>
    /// <param name="ROWS"> The number of rows in the table </param>
    /// <param name="COLUMNS"> The number of columns in the table </param>
    public static void AddRespiratorySignalToWorksheet(Excel.Worksheet ws, string SignalName, object[,] table, int ROWS, int COLUMNS)
    {
      // Make Table with Values
      Excel.Range range = ws.Range[ws.Cells[3, 2], ws.Cells[3 + ROWS - 1, 2 + COLUMNS - 1]];
      range.Value = table;
      ws.ListObjects.Add(Excel.XlListObjectSourceType.xlSrcRange, range, System.Reflection.Missing.Value, Excel.XlYesNoGuess.xlGuess, System.Reflection.Missing.Value).Name = ws.Name;
      ws.ListObjects[ws.Name].TableStyle = "TableStyleLight9";
      ws.Columns["A:I"].ColumnWidth = 20;
      ws.Columns["E:H"].Hidden = true;
      ws.Columns["B:H"].HorizontalAlignment = Excel.XlHAlign.xlHAlignCenter;

      // Add Conditional Formatting 
      Excel.Range range2 = ws.Range[ws.Cells[4, 2], ws.Cells[3 + ROWS - 1, 2 + COLUMNS - 1]];
      range2.FormatConditions.Add(Excel.XlFormatConditionType.xlExpression, System.Reflection.Missing.Value, "=NOT(ISBLANK($E4))", System.Reflection.Missing.Value, System.Reflection.Missing.Value, System.Reflection.Missing.Value, System.Reflection.Missing.Value, System.Reflection.Missing.Value);
      range2.FormatConditions.Add(Excel.XlFormatConditionType.xlExpression, System.Reflection.Missing.Value, "=NOT(ISBLANK($F4))", System.Reflection.Missing.Value, System.Reflection.Missing.Value, System.Reflection.Missing.Value, System.Reflection.Missing.Value, System.Reflection.Missing.Value);
      range2.FormatConditions.Add(Excel.XlFormatConditionType.xlExpression, System.Reflection.Missing.Value, "=NOT(ISBLANK($G4))", System.Reflection.Missing.Value, System.Reflection.Missing.Value, System.Reflection.Missing.Value, System.Reflection.Missing.Value, System.Reflection.Missing.Value);
      range2.FormatConditions.Add(Excel.XlFormatConditionType.xlExpression, System.Reflection.Missing.Value, "=NOT(ISBLANK($H4))", System.Reflection.Missing.Value, System.Reflection.Missing.Value, System.Reflection.Missing.Value, System.Reflection.Missing.Value, System.Reflection.Missing.Value);
      range2.FormatConditions[1].Interior.Color = 5296274;
      range2.FormatConditions[2].Interior.Color = 255;
      range2.FormatConditions[3].Interior.Color = 65535;
      range2.FormatConditions[4].Interior.Color = 15773696;
      range2.Columns[2].NumberFormat = "m/d/yyyy h:mm:ss.000";

      // Add Chart
      Excel.Chart chart = ((Excel.ChartObject)((Excel.ChartObjects)ws.ChartObjects()).Add(500, 100, 900, 500)).Chart;
      chart.SetSourceData(range.Columns["B:G"]);
      chart.ChartType = Microsoft.Office.Interop.Excel.XlChartType.xlXYScatterLines;
      chart.ChartWizard(Source: range.Columns["B:G"], Title: SignalName, CategoryTitle: "Time", ValueTitle: SignalName);
      chart.PlotVisibleOnly = false;

      ((Excel.Series)chart.SeriesCollection(1)).ChartType = Excel.XlChartType.xlXYScatterLinesNoMarkers;
      ((Excel.Series)chart.SeriesCollection(2)).MarkerStyle = Excel.XlMarkerStyle.xlMarkerStyleSquare;
      ((Excel.Series)chart.SeriesCollection(3)).MarkerStyle = Excel.XlMarkerStyle.xlMarkerStyleSquare;
      ((Excel.Series)chart.SeriesCollection(4)).MarkerStyle = Excel.XlMarkerStyle.xlMarkerStyleSquare;
      ((Excel.Series)chart.SeriesCollection(5)).MarkerStyle = Excel.XlMarkerStyle.xlMarkerStyleSquare;

      ((Excel.Series)chart.SeriesCollection(2)).Format.Fill.ForeColor.RGB = 5296274;
      ((Excel.Series)chart.SeriesCollection(3)).Format.Fill.ForeColor.RGB = 255;
      ((Excel.Series)chart.SeriesCollection(4)).Format.Fill.ForeColor.RGB = 65535;
      ((Excel.Series)chart.SeriesCollection(5)).Format.Fill.ForeColor.RGB = 15773696;

      ((Excel.Series)chart.SeriesCollection(1)).Format.Line.ForeColor.RGB = 38450;
      ((Excel.Series)chart.SeriesCollection(2)).Format.Line.ForeColor.RGB = 5296274;
      ((Excel.Series)chart.SeriesCollection(3)).Format.Line.ForeColor.RGB = 255;
      ((Excel.Series)chart.SeriesCollection(4)).Format.Line.ForeColor.RGB = 65535;
      ((Excel.Series)chart.SeriesCollection(5)).Format.Line.ForeColor.RGB = 15773696;

      System.Runtime.InteropServices.Marshal.ReleaseComObject(chart);
      System.Runtime.InteropServices.Marshal.ReleaseComObject(range);
      System.Runtime.InteropServices.Marshal.ReleaseComObject(range2);
    }
    /// <summary>
    /// Writes an Excel worksheet containing a signal's values and Plot
    /// </summary>
    /// <param name="ws"> The Excel worksheet object </param>
    /// <param name="SignalName"> The name of the signal </param>
    /// <param name="table"> The signal values </param>
    /// <param name="ROWS"> The number of rows in the table </param>
    /// <param name="COLUMNS"> The number of columns in the table </param>
    /// <param name="color"> The color of the plot </param>
    public static void AddSignalToWorksheet(Excel.Worksheet ws, string SignalName, object[,] table, int ROWS, int COLUMNS, OxyColor color)
    {
      Excel.Range range = ws.Range[ws.Cells[3, 2], ws.Cells[3 + ROWS - 1, 2 + COLUMNS - 1]];
      range.Value = table;
      ws.ListObjects.Add(Excel.XlListObjectSourceType.xlSrcRange, range, System.Reflection.Missing.Value, Excel.XlYesNoGuess.xlGuess, System.Reflection.Missing.Value).Name = ws.Name;
      ws.ListObjects[ws.Name].TableStyle = "TableStyleLight9";
      ws.Columns["A:I"].ColumnWidth = 20;
      ws.Columns["B:H"].HorizontalAlignment = Excel.XlHAlign.xlHAlignCenter;

      range.Columns[2].NumberFormat = "m/d/yyyy h:mm:ss.000";

      Excel.Chart chart = ((Excel.ChartObject)((Excel.ChartObjects)ws.ChartObjects()).Add(500, 100, 900, 500)).Chart;
      chart.SetSourceData(range.Columns["B:C"]);
      chart.ChartType = Microsoft.Office.Interop.Excel.XlChartType.xlXYScatterLines;
      chart.ChartWizard(Source: range.Columns["B:C"], Title: SignalName, CategoryTitle: "Time", ValueTitle: SignalName);
      ((Excel.Series)chart.SeriesCollection(1)).ChartType = Excel.XlChartType.xlXYScatterLinesNoMarkers;
      ((Excel.Series)chart.SeriesCollection(1)).Format.Line.ForeColor.RGB = System.Drawing.ColorTranslator.ToOle(System.Drawing.Color.FromArgb(color.A, color.R, color.G, color.B));
      System.Runtime.InteropServices.Marshal.ReleaseComObject(chart);
      System.Runtime.InteropServices.Marshal.ReleaseComObject(range);
    }
  }
}
