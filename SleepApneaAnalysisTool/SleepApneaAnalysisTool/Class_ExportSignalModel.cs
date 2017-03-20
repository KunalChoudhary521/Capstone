using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SleepApneaAnalysisTool
{
  public class ExportSignalModel
  {
    public int Subject_ID { get; }
    public int Epochs_From { get; }
    public int Epochs_Length { get; }

    public ExportSignalModel(int subject_id, int epochs_from, int epochs_length)
    {
      this.Subject_ID = subject_id;
      this.Epochs_From = epochs_from;
      this.Epochs_Length = epochs_length;
    }

    public ExportSignalModel(ExportSignalModel signals_to_export)
    {
      this.Subject_ID = signals_to_export.Subject_ID;
      this.Epochs_From = signals_to_export.Epochs_From;
      this.Epochs_Length = signals_to_export.Epochs_Length;
    }
  }
}
