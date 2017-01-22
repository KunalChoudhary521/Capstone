using System;
using System.Collections.Generic;
using System.Text;
using System.Collections;

namespace EDF
{
    /**
     * A DataRecord holds all of the signals/channels for a defined interval.  Each of the signals/channels has all of the samples for that interval bound to it.
     * 
     */ 
    public class EDFDataRecord:SortedList<string, List<float>>
    {        
        //a datarecord is a SortedList where the key is the channel/signal and the value is the List of Samples (floats) within the datarecord
    }
}
