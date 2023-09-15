using System;
using System.Collections.Generic;
using System.Linq;
// using Framework;
using Utilities;

namespace Core
{
    public class Cdp
    {
        //private ManagingAgent headManagingAgent; 
        private List<ShapeletClassifier> Agents = new List<ShapeletClassifier>();
        public bool ClassifiyWithPath { get; set; }

        private readonly List<Tuple<int, string>> _decisionPatterens = new List<Tuple<int, string>>(); // TEST 


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
                    //var agent = new ClassificationAgent(classifier);

                    // headManagingAgent.Add(classifier/*agent*/);
                    Agents.Add(classifier); 
                }
                p++; 
                Console.Write("\rPercent trained decision trees: {0:F1}%", (p * 100.0) / numDecisionTrees);
            }
            Console.Write("\r                                                  ");
            Console.WriteLine();
        }

        private static List<int> _getMostPopularIndexes(IEnumerable<int> results)
        {
            var resultsArray = results.ToArray();

            var mostPopularResult = resultsArray.GroupBy(s => s).OrderByDescending(g => g.Count()).First().Key;

            var mostPopularIndexes = new List<int>();

            // ´There might be more than one popular answer Ex: {1, 1, 2, 2, 3, 4}
            var numMostPopular = resultsArray.Count(e => e == mostPopularResult);

            var distinctResults = resultsArray.Distinct();

            foreach (var r in distinctResults)
            {
                var count = resultsArray.Count(e => e == r);
                if (count == numMostPopular)
                {
                    mostPopularIndexes.Add(r);
                }
            }

            return mostPopularIndexes;
        }

        
        private List<int> getMostPopularIndexesSimilarityCoefficient(string result)
        {
            var trainResults = new List<Tuple<int, string>>();
            var similarityDistances = new List<Tuple<int, double>>();

            foreach (var touple in _decisionPatterens)
            {
                var toupleClassIndex = touple.Item1;
                var pattern = touple.Item2;

                similarityDistances.Add(new Tuple<int, double>(toupleClassIndex, Utils.SimilarityCoefficient(result, pattern)));
            }
            var similarityLevel = 1.00;

            while (!similarityDistances.Any(t => t.Item2 >= similarityLevel))
            {
                similarityLevel -= 0.01;
            }

            similarityDistances.RemoveAll(t => t.Item2 < similarityLevel);

            var popularIndexes = similarityDistances.Select(t => t.Item1);

            var mostPopularIndexes = _getMostPopularIndexes(popularIndexes);

            if (mostPopularIndexes.Count() > 1)
            {
                var largestSimilarityDistance = similarityDistances.Max(x => x.Item2);
                var largestElementIndex = similarityDistances.First(t => t.Item2 >= largestSimilarityDistance).Item1;
                mostPopularIndexes.Clear();
                mostPopularIndexes.Add(largestElementIndex);
            }

            return mostPopularIndexes;
        }

        public int Classify(TimeSeries timeSeries)
        {
            List<int> mostPopularIndexes;

            if (ClassifiyWithPath)
            {
                var stringResults = Agents.Select(agent => agent.ClassifyWithPath(timeSeries)).ToList();

                var totalResult = stringResults.Aggregate("", (current, result) => current + result);

                //appendFeaturesToFile(timeSeries.ClassIndex, totalResult); // TEST

                var mostPopularStringIndexes = getMostPopularIndexesSimilarityCoefficient(totalResult);

                mostPopularIndexes = mostPopularStringIndexes;
            }
            else
            {
                var results = Agents.Select(agent => agent.Classify(timeSeries)).ToList();

                mostPopularIndexes = _getMostPopularIndexes(results);
            }

            mostPopularIndexes.RemoveAll(i => i == int.MinValue/*-1*/); // TEST 

            if (mostPopularIndexes.Count == 1)
            {
                return mostPopularIndexes[0];
            }

            return int.MinValue;  //-1; // TEST 
        }

        public void PrepareTrainingPatterns(DataSet TrainDataSet)
        {
            foreach (var i in TrainDataSet.ClassIndexes)
            {
                for (var j = 0; j < TrainDataSet.TimeSeriesIndexes[i].Count(); j++)
                {
                    var decisionPatten = Agents.Aggregate("", (current, agent) => current + agent.GetClassificationPath(i, j));
                    _decisionPatterens.Add(new Tuple<int, string>(i, decisionPatten));
                }
            }
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

            //headManagingAgent = new ManagingAgent(/*trainDataSet
            //                                      , DataMiningMethod.PsoShapelets*/)
            //{
            //    ClassifiyWithPath = false
            //};

            createAndTrainClassifiers(classIndexes, trainDataSet);

            // Create and collect decission paths 
            foreach (var timeSeries in trainDataSet.TimeSeries)
            {
                //headManagingAgent.Classify(timeSeries);
                Classify(timeSeries);
            }

            TypicalPath.DistancesCollected = true;

            //headManagingAgent.PrepareTrainingPatterns(trainDataSet);
            PrepareTrainingPatterns(trainDataSet);

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

            // headManagingAgent.ClassifiyWithPath = true;
            ClassifiyWithPath = true;

            var timeSeriesArray = testDataSet.TimeSeries.ToArray();
            var count = timeSeriesArray.Length;

            for (var i = 0; i < count; i++)
            {
                //var resultIndex = headManagingAgent.Classify(timeSeriesArray[i]);
                var resultIndex = Classify(timeSeriesArray[i]);
                classifiedLabels.Add(resultIndex);
            }

            return classifiedLabels;
        }
        
    }
}