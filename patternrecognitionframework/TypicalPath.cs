using System.Collections.Generic;

namespace Core
{
    public class TypicalPath
    {
        public TypicalPath()
        {
            DistancesCollected = false; 
        }

        public static bool DistancesCollected
        {
            get;  
            set; 
        }

        public Dictionary<int, List<double>> TypicalDistances = new Dictionary<int, List<double>>();
        //public Dictionary<int, double> LowerLimit = new Dictionary<int, double>();
        //public Dictionary<int, double> UpperLimit = new Dictionary<int, double>();
        //public Dictionary<int, double> Averages = new Dictionary<int, double>(); 
        public Dictionary<int, List<string>> PathString = new Dictionary<int, List<string>>();

    }
}
