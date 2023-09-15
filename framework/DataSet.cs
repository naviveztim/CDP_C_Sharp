using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using Utilities;

namespace Framework
{
    public class DoubleComparer : IEqualityComparer<double>
    {
        public bool Equals(double x, double y)
        {
            return !(Math.Abs(x - y) > double.Epsilon);
        }

        public int GetHashCode(double obj)
        {
            return obj.GetHashCode(); 
        }
    }

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
            DirectoryName = Directory.GetCurrentDirectory();
            ClassIndexes = classIndexes.Distinct().ToList();
            NumClasses = ClassIndexes.Count;
            MinLength = 3;

            TimeSeries = generateTimeSeriesFromMatrix(classIndexes, timeSeriesMatrix, useSignal, normalize);

            if (compressionIndex > 1)
            {
                Utils.CompressTimeSeries(TimeSeries, compressionIndex);
            }

            MaxLength = (TimeSeries.Max(s => s.Values.Count()));
            Step = (MaxLength - MinLength) / 20;  
            if (NumClasses <= 4 || Step == 0)
            {
                Step = 1;
            }

            updateTimeSeriesIndexesDataSet();

        }

        public static bool operator ==(DataSet d1, DataSet d2)
        {
            if (ReferenceEquals(d1, null) || ReferenceEquals(d2, null))
            {
                return false; 
            }

            if (d1.TimeSeries.Count() != d2.TimeSeries.Count())
            {
                return false; 
            }

            var count = d1.TimeSeries.Count();

            for (var i = 0; i < count; i++)
            {
                if (!d1.TimeSeries[i].Values.SequenceEqual(d2.TimeSeries[i].Values, new DoubleComparer()))
                {
                    return false; 
                }

                if (d1.TimeSeries[i].ClassIndex != d2.TimeSeries[i].ClassIndex)
                {
                    return false; 
                }

            }

            return true; 
        }

        public static bool operator !=(DataSet d1, DataSet d2)
        {
            return true; 
        }

        public DataSet(string aFileName, string aDelimiter,
                        List<int> usedClassIndexes,
                        int compressionIndex,
                        bool useSignal, bool normalize)
        {
            DirectoryName = Path.GetDirectoryName(aFileName);
            ClassIndexes = usedClassIndexes;
            NumClasses = ClassIndexes.Count;
            MinLength = 3;

            TimeSeries = generateTimeSeriesFromFile(aFileName, aDelimiter, useSignal, normalize);
            if (compressionIndex > 1)
            {
                Utils.CompressTimeSeries(TimeSeries, compressionIndex);
            }
            
            MaxLength = TimeSeries.Max(s => s.Values.Count());
            Step = (MaxLength - MinLength) / 20; // TEST
            if (NumClasses <= 4 || Step == 0)
            {
                Step = 1;
            }

            updateTimeSeriesIndexesDataSet();

            
        }

        public DataSet Clone(ICollection<int> classesInDataSet)
        {
            var newDataSet = Clone();

            newDataSet.ClassIndexes = classesInDataSet.ToList();
            newDataSet.NumClasses = classesInDataSet.Count;
            newDataSet.TimeSeries = newDataSet.TimeSeries.Where(t => classesInDataSet.Contains(t.ClassIndex)).ToList();

            newDataSet.updateTimeSeriesIndexesDataSet();

            return newDataSet;
        }

        public DataSet CloneRandom(int maxNumTimeSeriesPerClass)
        {
            var newDataSet = Clone();

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
                newDataSet.updateTimeSeriesIndexesDataSet();

            }

            return newDataSet;
        }

        public DataSet Clone()
        {
            var ms = new MemoryStream();
            var bf = new BinaryFormatter();

            bf.Serialize(ms, this);

            ms.Position = 0;
            object obj = bf.Deserialize(ms);
            ms.Close();

            return obj as DataSet;
        }

        public IEnumerable<TimeSeries> ExtractFromDataSet(int classIndexA, int classIndexB)
        {
            return TimeSeries.Where(t => (t.ClassIndex == classIndexA || t.ClassIndex == classIndexB));
        }

        private static List<TimeSeries> generateTimeSeriesFromMatrix(IList<int> classIndexes, IEnumerable<IEnumerable<double>> timeSeriesMatrix,
                                                                     bool useSignal, bool normalize)
        {
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
                    Utils.Derivate(timeSeries.Values); 
                }

                if (normalize)
                {
                    Utils.Normalize(timeSeries.Values);
                }

                dataSet.Add(timeSeries);
            }

            return dataSet; 
        }

        private static List<TimeSeries> generateTimeSeriesFromFile(string filePath, string delimiter, bool useSignal, bool normalize)
        {
            var dataSet = new List<TimeSeries>();

            if (!File.Exists(filePath))
            {
                return dataSet;
            }

            foreach (var line in File.ReadLines(filePath))
            {
                // Index of the class must be written at the front of the line 
                var numbers = line.Split(delimiter.ToCharArray(), StringSplitOptions.RemoveEmptyEntries);

                var timeSeries = new TimeSeries
                {
                    ClassIndex = (int)double.Parse(numbers.First()),
                    Values = new double[numbers.Length - 1],
                };

                for (var i = 1; i < numbers.Length; i++)
                {
                    timeSeries.Values[i - 1] = double.Parse(numbers[i], NumberStyles.Float);
                }

                // First derivate to the time series data 
                if (!useSignal)
                {
                    Utils.Derivate(timeSeries.Values);

                }

                if (normalize)
                {
                    Utils.Normalize(timeSeries.Values); 
                }
                
                dataSet.Add(timeSeries);
            }

            return dataSet;
        }

        private void updateTimeSeriesIndexesDataSet()
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

