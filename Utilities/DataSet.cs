using System;
using System.Collections.Generic;
using System.Globalization;
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
        public int NumClasses { get; private set;}
        public List<int> ClassIndexes { get; private set;}
        public int MinLength { get; private set;}
        public int MaxLength { get; private set;}
        public int Step { get; private set; }

        public DataSet(string filePath
                       , string delimiter
                       , int compressionIndex
                       , bool useSignal
                       , bool normalize
                       )
        {
            // Read file 
            var classLabels = new List<int>();
            var timeSeriesMatrix = new List<double[]>();

            foreach (var line in File.ReadLines(filePath))
            {
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                // Index of the class must be written at the front of the line 
                var numbers = line.Split(delimiter.ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                if (numbers.Length == 0) continue; // Skip lines that are just delimiters

                // Parse class label from the begining of the line 
                if (!double.TryParse(numbers[0], NumberStyles.Any, CultureInfo.InvariantCulture, out double classLabel))
                {
                    continue; 
                }
                classLabels.Add((int)classLabel);

                // Parse time series values from the line 
                var values = new double[numbers.Length - 1];
                bool valid = true;
                for (int i = 1; i < numbers.Length; i++)
                {
                    if (!double.TryParse(numbers[i], NumberStyles.AllowExponent | NumberStyles.Number, CultureInfo.InvariantCulture, out values[i - 1]))
                    {
                        valid = false;
                        break; 
                    }
                }

                // Skip this line if any number failed to parse
                if (!valid) continue; 

                timeSeriesMatrix.Add(values);
            }

            // Set parameters 
            DirectoryName = Directory.GetCurrentDirectory();
            ClassIndexes = classLabels.Distinct().ToList();
            NumClasses = ClassIndexes.Count;
            MinLength = 3;

            // Pre-process time series 
            TimeSeries = _preProcessTimeSeries(classLabels
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

        }

        public DataSet Clone(ICollection<int> classesInDataSet)
        {
            // Create Dataset with selected class indexes 
            var newDataSet = _clone();

            newDataSet.ClassIndexes = classesInDataSet.ToList();
            newDataSet.NumClasses = classesInDataSet.Count;
            newDataSet.TimeSeries = newDataSet.TimeSeries.Where(t => classesInDataSet.Contains(t.ClassIndex)).ToList();

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

    }
}

