using System;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.IO;
using EDF;

namespace SleepApneaAnalysisTool
{
  public class CommonModelView : INotifyPropertyChanged
  {
    #region Actions

    // Load EDF File
    public Action EDF_Loading_Finished;
    /// <summary>
    /// Background process for loading edf file
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void BW_LoadEDFFile(object sender, DoWorkEventArgs e)
    {
      // Read EDF File
      EDFFile temp = new EDFFile();
      temp.readFile(e.Argument.ToString());
      LoadedEDFFile = temp;
    }
    /// <summary>
    /// Function called after background process for loading edf file finishes
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void BW_FinishLoad(object sender, RunWorkerCompletedEventArgs e)
    {
      EDF_Loading_Finished();
    }
    /// <summary>
    /// Loads an EDF File into memory
    /// </summary>
    /// <param name="fileNameIn"> Path to the EDF file to load </param>
    public void LoadEDFFile(string fileNameIn)
    {
      LoadedEDFFile = null;
      LoadedEDFFileName = fileNameIn;
      BackgroundWorker bw = new BackgroundWorker();
      bw.DoWork += BW_LoadEDFFile;
      bw.RunWorkerCompleted += BW_FinishLoad;
      bw.RunWorkerAsync(LoadedEDFFileName);
    }

    #endregion

    #region Members
    
    /// <summary>
    /// The Window
    /// </summary>
    private MainWindow p_window;
    /// <summary>
    /// The EDF file structure
    /// </summary>
    private EDFFile p_LoadedEDFFile;
    /// <summary>
    /// The file path to the loaded EDF file
    /// </summary>
    private string p_LoadedEDFFileName = null;
    
    #endregion

    #region Properties 

    // Update Actions
    /// <summary>
    /// Function called when the loaded EDF file changes
    /// </summary>
    private void LoadedEDFFile_Changed()
    {
      // Preview Time Picker
      if (p_LoadedEDFFile == null)
      {
        LoadedEDFFileName = null;
      }
      else
      {
      }

      // Misc
      OnPropertyChanged(nameof(IsEDFLoaded));
    }

    // Loaded EDF Info
    /// <summary>
    /// The EDF file structure
    /// </summary>
    public EDFFile LoadedEDFFile
    {
      get
      {
        return p_LoadedEDFFile;
      }
      set
      {
        p_LoadedEDFFile = value;
        LoadedEDFFile_Changed();
      }
    }
    /// <summary>
    /// The file path to the loaded EDF file
    /// </summary>
    public string LoadedEDFFileName
    {
      get
      {
        return p_LoadedEDFFileName ?? "No File Loaded";
      }
      set
      {
        p_LoadedEDFFileName = value;
        OnPropertyChanged(nameof(LoadedEDFFileName));
      }
    }
    /// <summary>
    /// True if a EDF file is loaded
    /// </summary>
    public bool IsEDFLoaded
    {
      get
      {
        return LoadedEDFFile != null;
      }
    }
    /// <summary>
    /// The time stamp of the beginning of the signal recordings in the EDF file
    /// </summary>
    public DateTime EDFStartTime
    {
      get
      {
        if (IsEDFLoaded)
          return LoadedEDFFile.Header.StartDateTime;
        else
          return new DateTime();
      }
    }
    /// <summary>
    /// The time stamp of the end of the signal recordings in the EDF file
    /// </summary>
    public DateTime EDFEndTime
    {
      get
      {
        if (IsEDFLoaded)
        {
          DateTime EndTime = LoadedEDFFile.Header.StartDateTime
                             + new TimeSpan(
                               (long)(TimeSpan.TicksPerSecond * LoadedEDFFile.Header.DurationOfDataRecordInSeconds * LoadedEDFFile.Header.NumberOfDataRecords)
                               );
          return EndTime;
        }
        else
          return new DateTime();
      }
    }

    #endregion

    #region etc

    // INotify Interface
    /// <summary>
    /// Event raised when a property in this class changes value
    /// </summary>
    public event PropertyChangedEventHandler PropertyChanged;
    /// <summary>
    /// Function to raise PropertyChanged event 
    /// </summary>
    /// <param name="propertyName"></param>
    public void OnPropertyChanged(string propertyName)
    {
      PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
    }

    #endregion

    /// <summary>
    /// Constructor for the CommonModelView
    /// </summary>
    /// <param name="i_window"></param>
    /// <param name="i_common_data"></param>
    /// <param name="i_sm"></param>
    public CommonModelView(MainWindow i_window)
    {
      p_window = i_window;

      #region Preload MATLAB functions into memory
      {
        BackgroundWorker bw = new BackgroundWorker();
        bw.DoWork += new DoWorkEventHandler(
          delegate (object sender1, DoWorkEventArgs e1)
          {
            Utils.MATLAB_Coherence(new float[] { 1, 1, 1, 1, 1, 1, 1, 1 }, new float[] { 1, 1, 1, 1, 1, 1, 1, 1 });
          }
          );
        bw.RunWorkerAsync();
      }
      {
        BackgroundWorker bw = new BackgroundWorker();
        bw.DoWork += new DoWorkEventHandler(
          delegate (object sender1, DoWorkEventArgs e1)
          {
            Utils.MATLAB_Resample(new float[] { 1, 1, 1, 1, 1, 1, 1, 1 }, 2);
          }
          );
        bw.RunWorkerAsync();
      }
      #endregion 
    }
  }
}
