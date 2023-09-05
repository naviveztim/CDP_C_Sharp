using System.Collections.Generic;
using Utilities;

namespace Framework
{
    public interface IAgent
    {
        void Train(DataSet dataSet);
        int Classify(TimeSeries timeSeries);
        string ClassifyWithPath(TimeSeries timeSeries);
        string GetClassificationPath(int classIndex, int trial); 
    }
}
