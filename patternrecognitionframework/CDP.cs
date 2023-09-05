using System;
using System.Collections.Generic;
using System.Linq;
using Framework;
using Utilities;

namespace Core
{
    public class Cdp
    {
        private ManagingAgent headManagingAgent; 
        
        public string TrainFileName { get; private set;  }
        public string TestFileName { get; private set;  }
        public int CompressonFactor {get; private set ; }
        public int NumClassLabelsPerTree { get; private set; }
        public string Delimiter { get; private set; }
        public int PatternLength { get; private set; }
        public bool UseSignal { get; private set;  }
        public bool Normalize { get; private set;  }

        public Cdp(int patternLength
                   , int compressionFactor
                   , bool useSignal
                   , bool normalize
                   , int numClassLabelsPerTree)
        {
            TrainFileName = "";
            TestFileName = "";
            CompressonFactor = compressionFactor;
            NumClassLabelsPerTree = numClassLabelsPerTree;
            // TODO: Remove such definition 
            Utils.MAX_ANSWER_LENGTH = NumClassLabelsPerTree - 1;
            Delimiter = "";
            PatternLength = patternLength;
            UseSignal = useSignal;
            Normalize = normalize;
        }

        private void createAndTrainClassifiers(List<int> classIndexes, DataSet dataSet)
        {
            // Create group of decision trees 
            var group = Utils.createSpecifiedGroup(classIndexes, PatternLength, NumClassLabelsPerTree);
            Utils.printGroup(group, "");

            // Fit decision trees 
            var p = 0;
            var numDecisionTrees = group.Count();  
            foreach (var combination in group)
            {
                var classifier = ShapeletsDataMiningUtils.CreateAndTrainClassifier(combination
                                                                                   , dataSet);
                if (classifier != null)
                {
                    var agent = new ClassificationAgent(classifier);

                    headManagingAgent.Add(agent);
                }
                p++; 
                Console.Write("\rPercent trained decision trees: {0:F1}%", (p * 100.0) / numDecisionTrees);
            }
            Console.Write("\r                                                  ");
            Console.WriteLine();
        }

        public void Fit(IList<int> classLabels, IEnumerable<IEnumerable<double>> timeSeriesMatrix)
        {
            var classIndexes = new List<int>();
            classIndexes.AddRange(classLabels.Distinct().OrderBy(x => x)); 

            // Load dataset and preprocess
            var trainDataSet = new DataSet(classLabels
                                           , timeSeriesMatrix
                                           , CompressonFactor
                                           , UseSignal
                                           , Normalize);

            headManagingAgent = new ManagingAgent(trainDataSet
                                                  , DataMiningMethod.PsoShapelets)
            {
                ClassifiyWithPath = false
            };

            createAndTrainClassifiers(classIndexes, trainDataSet);

            // Create and collect decission paths 
            foreach (var timeSeries in trainDataSet.TimeSeries)
            {
                headManagingAgent.Classify(timeSeries);
            }

            TypicalPath.DistancesCollected = true;

            headManagingAgent.PrepareTrainingPatterns();
            
        }

        public List<int> Predict(IEnumerable<IEnumerable<double>> timeSeriesMatrix, bool printDecisionPatterns = false)
        {
            Console.Write("Classifying...");
            var classifiedLabels = new List<int>();

            // We do that to keep compression, S/D and normalization over the test signal as well 
            var indexes = new List<int>();
            var seriesMatrix = timeSeriesMatrix as IEnumerable<double>[] ?? timeSeriesMatrix.ToArray();
            indexes.AddRange(Enumerable.Range(1, seriesMatrix.Count())); // False class indexes

            var testDataSet = new DataSet(indexes, seriesMatrix, CompressonFactor, UseSignal, Normalize);

            headManagingAgent.ClassifiyWithPath = true;

            var timeSeriesArray = testDataSet.TimeSeries.ToArray();
            var count = timeSeriesArray.Length;

            for (var i = 0; i < count; i++)
            {
                var resultIndex = headManagingAgent.Classify(timeSeriesArray[i]);
                classifiedLabels.Add(resultIndex);
            }

            return classifiedLabels;
        }
        
    }
}