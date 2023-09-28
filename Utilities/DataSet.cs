using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;

namespace Utilities
{
    [Serializable]
    public class DataSet
    {
        public string DirectoryName { get; private set;}       
        public List<TimeSeries> TimeSeries { get; private set;}
        public Dictionary<int, IEnumerable<TimeSeries>> TimeSeriesIndexes { get; private set;}
        public int NumClasses { get; private set;}
        public List<int> ClassIndexes { get; private set;}
        public int MinLength { get; private set;}
        public int MaxLength { get; private set;}
        public int Step { get; private set; }

        public DataSet(IList<int> classIndexes
                       , IEnumerable<IEnumerable<double>> timeSeriesMatrix
                       , int compressionIndex
                       , bool useSignal
                       , bool normalize )
        {
            // Set parameters 
            DirectoryName = Directory.GetCurrentDirectory();
            ClassIndexes = classIndexes.Distinct().ToList();
            NumClasses = ClassIndexes.Count;
            MinLength = 3;

            // Pre-process time series 
            TimeSeries = _preProcessTimeSeries(classIndexes
                                                , timeSeriesMatrix
                                                , compressionIndex
                                                , useSignal
                                                , normalize);

            // Define max length and step base on processed time series 
            MaxLength = (TimeSeries.Max(s => s.Values.Count()));
            Step = (MaxLength - MinLength) / 20;  
            if (NumClasses <= 4 || Step == 0)
            {
                Step = 1;
            }

            _updateTimeSeriesIndexesDataSet();
        }
        
        public DataSet Clone(ICollection<int> classesInDataSet)
        {
            // Create Dataset with selected class indexes 
            var newDataSet = _clone();

            newDataSet.ClassIndexes = classesInDataSet.ToList();
            newDataSet.NumClasses = classesInDataSet.Count;
            newDataSet.TimeSeries = newDataSet.TimeSeries.Where(t => classesInDataSet.Contains(t.ClassIndex)).ToList();

            newDataSet._updateTimeSeriesIndexesDataSet();

            return newDataSet;
        }

        public DataSet CloneRandom(int maxNumTimeSeriesPerClass)
        {
            // Create Dataset with given number of time series per class index 
            var newDataSet = _clone();

            if (maxNumTimeSeriesPerClass > 0)
            {
                var newTimeSeries = new List<TimeSeries>();
                foreach (var index in newDataSet.ClassIndexes)
                {
                    var index1 = index;
                    var currentIndexTimeSeries = newDataSet.TimeSeries.Where(t => t.ClassIndex == index1).ToList();
                    var numTimeSeriesCurrentIndex = newDataSet.TimeSeries.Count(t => t.ClassIndex == index1);

                    var random = new Random();
                    var randomIndexes = Enumerable.Range(0, numTimeSeriesCurrentIndex).OrderBy(x => random.Next()).Take(maxNumTimeSeriesPerClass).ToArray();
                    foreach (var randomIndex in randomIndexes)
                    {
                        newTimeSeries.Add(currentIndexTimeSeries[randomIndex]);
                    }
                }

                newDataSet.TimeSeries = newTimeSeries;
                newDataSet._updateTimeSeriesIndexesDataSet();

            }

            return newDataSet;
        }

        public IEnumerable<TimeSeries> ExtractFromDataSet(int classIndexA, int classIndexB)
        {
            // Get time series that corresponds to given class indexes 
            return TimeSeries.Where(t => (t.ClassIndex == classIndexA || t.ClassIndex == classIndexB));
        }

        private DataSet _clone()
        {
            // Clone dataset 
            var ms = new MemoryStream();
            var bf = new BinaryFormatter();

            bf.Serialize(ms, this);

            ms.Position = 0;
            object obj = bf.Deserialize(ms);
            ms.Close();

            return obj as DataSet;
        }

        private static List<TimeSeries> _preProcessTimeSeries(IList<int> classIndexes
                                                                      , IEnumerable<IEnumerable<double>> timeSeriesMatrix
                                                                      , int compressionIndex
                                                                      , bool useSignal
                                                                      , bool normalize)
        {
            // Process time series- compress, apply derivative, normalize (if required)
            var i = 0;
            var dataSet = new List<TimeSeries>();

            foreach (var series in timeSeriesMatrix)
            {
                var timeSeries = new TimeSeries
                {
                    ClassIndex = classIndexes[i++],
                    Values = series.ToArray(),
                };

                if (!useSignal)
                {
                    Utils.Derivative(timeSeries.Values); 
                }

                if (normalize)
                {
                    Utils.Normalize(timeSeries.Values);
                }

                if (compressionIndex > 1)
                {
                    Utils.CompressATimeSeries(timeSeries, compressionIndex);
                }

                dataSet.Add(timeSeries);
            }

            return dataSet; 
        }

        private void _updateTimeSeriesIndexesDataSet()
        {
            if (NumClasses > 0)
            {
                TimeSeriesIndexes = new Dictionary<int, IEnumerable<TimeSeries>>();
                foreach (var classIndex in ClassIndexes)
                {
                    var timeSeriesList = TimeSeries.Where(t => (t.ClassIndex == classIndex)).ToList();
                    TimeSeriesIndexes.Add(classIndex, timeSeriesList);
                }
            }
        }
    }
}

