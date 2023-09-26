using System;
using System.Collections.Generic;
using System.Linq;
using Utilities;

namespace Core
{
    public class Cdp
    {
        public int CompressonFactor {get; private set ; }
        public int NumClassLabelsPerTree { get; private set; }
        public string Delimiter { get; private set; }
        public int PatternLength { get; private set; }
        public bool UseSignal { get; private set;  }
        public bool Normalize { get; private set;  }

        private List<ShapeletClassifier> _classifiers = new List<ShapeletClassifier>();
        private readonly List<Tuple<int, string>> _decisionPatterens = new List<Tuple<int, string>>(); 

        public Cdp(int patternLength
                   , int compressionFactor
                   , bool useSignal
                   , bool normalize
                   , int numClassLabelsPerTree)
        {
            CompressonFactor = compressionFactor;
            NumClassLabelsPerTree = numClassLabelsPerTree;
            // TODO: Remove such definition 
            Utils.MAX_ANSWER_LENGTH = NumClassLabelsPerTree - 1;
            Delimiter = "";
            PatternLength = patternLength;
            UseSignal = useSignal;
            Normalize = normalize;
        }

        private static ShapeletClassifier _createAndTrainClassifier(IList<int> classesInDataSet
                                                                   , DataSet dataSet)
        {
            var classifier = new ShapeletClassifier(classesInDataSet
                                                    , dataSet);

            if (!classifier.LoadClassifier())
            {
                var newDataSet = dataSet.Clone(classesInDataSet).CloneRandom(10);
                if (!classifier.Train(newDataSet))
                {
                    return null;
                }
            }

            return classifier;
        }
  
        private void _createAndTrainClassifiers(List<int> classIndexes, DataSet dataSet)
        {
            // Create groups of indexes  
            var group = Utils.createSpecifiedGroup(classIndexes, PatternLength, NumClassLabelsPerTree);
            Utils.printGroup(group, "");

            // Create decision trees 
            var p = 0;
            var numDecisionTrees = group.Count();  
            foreach (var combination in group)
            {
                var classifier = _createAndTrainClassifier(combination, dataSet);
                if (classifier != null)
                {
                    _classifiers.Add(classifier); 
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
     
        private List<int> _getMostPopularIndexesSimilarityCoefficient(string result)
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

        private int _classify(TimeSeries timeSeries)
        {
            List<int> mostPopularIndexes;

            var stringResults = _classifiers.Select(classifier => classifier.GetDecisionPath(timeSeries)).ToList();

            var totalResult = stringResults.Aggregate("", (current, result) => current + result);

            var mostPopularStringIndexes = _getMostPopularIndexesSimilarityCoefficient(totalResult);

            mostPopularIndexes = mostPopularStringIndexes;
            
            mostPopularIndexes.RemoveAll(i => i == int.MinValue); 

            if (mostPopularIndexes.Count == 1)
            {
                return mostPopularIndexes[0];
            }

            return int.MinValue;  
        }

        private void _prepareTrainingPatterns(DataSet trainDataSet)
        {
            foreach (var timeSeries in trainDataSet.TimeSeries)
            {
                var decisionPattern = _classifiers.Aggregate("", (current, classifier) => current + classifier.GetDecisionPath(timeSeries));
                _decisionPatterens.Add(new Tuple<int, string>(timeSeries.ClassIndex, decisionPattern));
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

            // Create decision trees and add them into a list 
            _createAndTrainClassifiers(classIndexes, trainDataSet);

            // Collect and aggregate decission paths for every decision tree 
            // [(1, 'LLRLLR0LLLL')
            //  (1, 'LLRLL0RLLLL')
            //  (2, 'LLLLRRRR0LL')
            //  (2, 'LLLLLRRRRLL')]
            _prepareTrainingPatterns(trainDataSet);

        }

        public List<int> Predict(IEnumerable<IEnumerable<double>> timeSeriesMatrix)
        {
            Console.Write("Classifying...");
            var classifiedLabels = new List<int>();

            // Do compression, S/D and normalization over the test signal as well, if acquired 
            var indexes = new List<int>();
            var seriesMatrix = timeSeriesMatrix as IEnumerable<double>[] ?? timeSeriesMatrix.ToArray();
            indexes.AddRange(Enumerable.Range(1, seriesMatrix.Count())); // False class indexes

            var testDataSet = new DataSet(indexes, seriesMatrix, CompressonFactor, UseSignal, Normalize);

            var timeSeriesArray = testDataSet.TimeSeries.ToArray();
            var count = timeSeriesArray.Length;

            for (var i = 0; i < count; i++)
            {
                var resultIndex = _classify(timeSeriesArray[i]);
                classifiedLabels.Add(resultIndex);
            }

            return classifiedLabels;
        }
        
    }
}