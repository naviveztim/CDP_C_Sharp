using System;

namespace Utilities
{
    [Serializable]
    public class TimeSeries
    {
        public int ClassIndex
        {
            get; set;
        }
        
        public double[] Values
        {
            get; set;
        }
    }
}
