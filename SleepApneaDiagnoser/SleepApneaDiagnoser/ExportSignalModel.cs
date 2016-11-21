using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SleepApneaDiagnoser
{
  public class ExportSignalModel
  {
    public string Signal_Name { get; }
    public string Epochs_From { get; }
    public string Epochs_To { get; }

    public ExportSignalModel(string signal_name, string epochs_from, string epochs_to)
    {
      this.Signal_Name = signal_name;
      this.Epochs_From = epochs_from;
      this.Epochs_To = epochs_to;
    }
  }
}
