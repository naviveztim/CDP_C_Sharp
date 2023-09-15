using System;
using System.Collections.Generic;
//using System.IO;
using System.Linq;
//using System.Text;
//using System.Text.RegularExpressions;
using Utilities;

namespace Framework
{
    /*
    public enum DataMiningMethod
    {
        None,
        PsoShapelets
    }*/

    public class ManagingAgent //: IClassifier
    {
        public List<ClassificationAgent> Agents { get; private set; }
        
        // public DataSet TrainDataSet { get; private set; }
        
        // public string WorkingFolder { get; private set; }
        
        // public Func<IList<int>, DataSet, DataMiningMethod, IClassifier> CreateAndTrainClassifier { get; private set; }
        
        // public DataMiningMethod MiningMethod { get; private set; }
        
        public bool ClassifiyWithPath { get; set; }

        private readonly List<Tuple<int, string>> _decisionPatterens = new List<Tuple<int, string>>(); // TEST 

        public ManagingAgent(/*DataSet dataSet
                             , DataMiningMethod dataMiningMethod = DataMiningMethod.None*/
                             /*, Func<IList<int>
                                    , DataSet
                                    , DataMiningMethod
                                    , IClassifier> createAndTrainClassifier = null*/)
        {
            Agents = new List<ClassificationAgent>();
            //TrainDataSet = dataSet;
            //WorkingFolder = dataSet.DirectoryName;
            //CreateAndTrainClassifier = createAndTrainClassifier;
            //MiningMethod = dataMiningMethod; 
        }

        public void Add(ClassificationAgent agent)
        {
            /*
            if (agent is ManagingAgent)
            {
                (agent as ManagingAgent).TrainDataSet = TrainDataSet;
                (agent as ManagingAgent).WorkingFolder = WorkingFolder;
                (agent as ManagingAgent).CreateAndTrainClassifier = CreateAndTrainClassifier;
                (agent as ManagingAgent).MiningMethod = MiningMethod; 
            }*/
            
            Agents.Add(agent); 
        }

        /*
        public bool LoadClassifier()
        {
            throw new NotImplementedException();
        }*/

        /*
        public bool Train(DataSet dataSet)
        {
            foreach (var agent in Agents)
            {
                agent.Train(dataSet);
            }

            return true;
        }*/

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
        } // TEST 

        /*
        public string ClassifyWithPath(TimeSeries timeSeries)
        {
            throw new NotImplementedException();
        }*/

        /*
        public string GetClassificationPath(int classIndex, int trial)
        {
            throw new NotImplementedException();
        }*/

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

        /*
        private static string fromDecisionPatternToFeaturesArray(string decisionPattern)
        {
            var featuresStr = Regex.Replace(decisionPattern, ".{1}", "$0,");
            featuresStr = featuresStr.Replace('L', '1').Replace('R', '2');
            featuresStr = featuresStr.Remove(featuresStr.Length - 1);

            return featuresStr;
        }*/

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
    }
}
