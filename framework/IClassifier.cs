using System.Collections.Generic;
using Utilities;

namespace Framework
{
    public interface IClassifier
    {
        bool LoadClassifier(); 
        bool Train(DataSet aDataSet);
        int Classify(TimeSeries timeSeries);
        string ClassifyWithPath(TimeSeries timeSeries);
        string GetClassificationPath(int classIndex, int trial); 
    }
}
