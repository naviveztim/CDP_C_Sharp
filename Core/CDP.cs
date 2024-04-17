using System;
using System.Collections.Generic;
using System.Linq;
using Utilities;

namespace Core
{
    public class Cdp
    {
        private int _compressonFactor;
        private int _numClassLabelsPerTree;
        private int _patternLength;
        private bool _useSignal;
        private bool _normalize; 
        private List<ShapeletClassifier> _classifiers = new List<ShapeletClassifier>();
        private readonly List<Tuple<int, string>> _trainDecisionPatterens = new List<Tuple<int, string>>(); 

        public Cdp(int patternLength
                   , int compressionFactor
                   , bool useSignal
                   , bool normalize
                   , int numClassLabelsPerTree)
        {
            _compressonFactor = compressionFactor;
            _numClassLabelsPerTree = numClassLabelsPerTree;
            _patternLength = patternLength;
            _useSignal = useSignal;
            _normalize = normalize;
        }

        private ShapeletClassifier _createAndTrainClassifier(IList<int> classesInDataSet
                                                                   , DataSet dataSet)
        {
            var classifier = new ShapeletClassifier(classesInDataSet
                                                    , dataSet
                                                    , _numClassLabelsPerTree-1);

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
            var group = Utils.createGroupOfIndexes(classIndexes, _patternLength, _numClassLabelsPerTree);
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
                
                Console.Write("\rPercent trained decision trees: {0:F1}%", (p++ * 100.0) / numDecisionTrees);
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
     
        private List<int> _getMostPopularIndexes(string decisionPattern)
        {
            // Fill out list with tuples <real class index, similarity between 
            // incoming decision patterns and one kept as class index representation>
            var similarityDistances = new List<Tuple<int, double>>();
            foreach (var touple in _trainDecisionPatterens)
            {
                var trainClassIndex = touple.Item1;
                var trainPattern = touple.Item2;

                similarityDistances.Add(new Tuple<int, double>(trainClassIndex
                                                               , Utils.SimilarityCoefficient(decisionPattern, trainPattern)));
            }

            // Select closest distances 
            var similarityLevel = 1.00;
            while (!similarityDistances.Any(t => t.Item2 >= similarityLevel))
            {
                similarityLevel -= 0.01;
            }
            similarityDistances.RemoveAll(t => t.Item2 < similarityLevel);
            
            // Get most popular index among selected distances
            var mostPopularIndexes = _getMostPopularIndexes(similarityDistances.Select(t => t.Item1));
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
            // Obtain decision pattern for gvrn time series 
            var decisionPattern = _classifiers.Select(classifier => classifier.GetDecisionPath(timeSeries)).ToList();
            var stringDecisionPattern = decisionPattern.Aggregate("", (current, result) => current + result);

            // Get class indexes of closest training decision patterns 
            // The result may be based on an outlier, thus better option is to
            // use _getMostPopularIndexes(stringDecisionPattern)- although it might be slower
            //  
            var mostPopularIndexes = _getMostPopularIndexes(stringDecisionPattern); 
            mostPopularIndexes.RemoveAll(i => i == int.MinValue);

            return (mostPopularIndexes.Count == 1) ? mostPopularIndexes[0] : int.MinValue;

        }

        private void _prepareTrainingPatterns(DataSet trainDataSet)
        {
            foreach (var timeSeries in trainDataSet.TimeSeries)
            {
                var decisionPattern = _classifiers.Aggregate("", (current, classifier) => current + classifier.GetDecisionPath(timeSeries));
                _trainDecisionPatterens.Add(new Tuple<int, string>(timeSeries.ClassIndex, decisionPattern));
            }

        }

        public void Fit(IList<int> classLabels, IEnumerable<IEnumerable<double>> timeSeriesMatrix)
        {
            var classIndexes = new List<int>();
            classIndexes.AddRange(classLabels.Distinct().OrderBy(x => x)); 

            // Load dataset and preprocess
            var trainDataSet = new DataSet(classLabels
                                           , timeSeriesMatrix
                                           , _compressonFactor
                                           , _useSignal
                                           , _normalize);

            // Create decision trees and add them into a list 
            _createAndTrainClassifiers(classIndexes, trainDataSet);

            // Collect and aggregate decission paths for every decision tree 
            // [(1, 'LLRLLR0LLLL')
            // ...
            //  (k, 'LLLLRRRR0LL')]
            _prepareTrainingPatterns(trainDataSet);

        }

        public List<int> Predict(IEnumerable<IEnumerable<double>> timeSeriesMatrix)
        {
            Console.Write("Classifying...");
            var classifiedLabels = new List<int>();

            // Create test dataset from given sample and pre-process in same way as trained dataset
            //var seriesMatrix = timeSeriesMatrix as IEnumerable<double>[] ?? timeSeriesMatrix.ToArray();
            var indexes = Enumerable.Repeat(-1, timeSeriesMatrix.Count()).ToList();
            var testDataSet = new DataSet(indexes, timeSeriesMatrix.ToArray(), _compressonFactor, _useSignal, _normalize);

            // Get pre-processed time series and classify them 
            var timeSeriesArray = testDataSet.TimeSeries.ToArray();
            for (var i = 0; i < timeSeriesArray.Length; i++)
            {
                var resultIndex = _classify(timeSeriesArray[i]);
                classifiedLabels.Add(resultIndex);
            }

            return classifiedLabels;
        }
        
    }
}