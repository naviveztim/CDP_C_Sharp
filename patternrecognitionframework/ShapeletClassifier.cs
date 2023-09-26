#define EARLY_ABANDON

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using Utilities;
using System.IO;

namespace Core
{
    public class ShapeletClassifier 
    {
        private BTree<Shapelet> _mostAccurateTree;
        private readonly string _decisionTreeFilepath;
        private static List<string> _classifiersPathsList = null;
        private static Dictionary<string, int> _lastUsedNumberDict = null;

        const string CLASSIFIERS_FOLDER = ".\\_classifiers\\";

        public ShapeletClassifier(IList<int> classesInDataSet
                                  , DataSet dataSet)
        {
            _decisionTreeFilepath = _getClassifierFileName(classesInDataSet, dataSet.DirectoryName);
        }

        private static void _fillsClassifiersPathsList()
        {
            if (!Directory.Exists(CLASSIFIERS_FOLDER))
            {
                Directory.CreateDirectory(CLASSIFIERS_FOLDER);
            }

            _classifiersPathsList = Directory.GetFiles(CLASSIFIERS_FOLDER).ToList();
            _classifiersPathsList = _classifiersPathsList.OrderBy(s => s).ToList();
        }

        private static string _getClassifierFileName(IEnumerable<int> classesInDataSet
                                                     , string workingFolder)
        {
            if (null == _classifiersPathsList)
            {
                _fillsClassifiersPathsList();
            }

            if (null == _lastUsedNumberDict)
            {
                _lastUsedNumberDict = new Dictionary<string, int>();
            }

            var lastExistingNumber = 0;
            var combinationIdentifier = String.Join("_", classesInDataSet);
            var classifierFileName = "Classificatin_tree_" + combinationIdentifier;
            var classifierPath = CLASSIFIERS_FOLDER + classifierFileName;

            // Get the number of the last similar file 
            if (null != _classifiersPathsList && _classifiersPathsList.Any())
            {
                var lastSimilarPath = _classifiersPathsList.LastOrDefault(s => s.StartsWith(classifierPath));
                if (null != lastSimilarPath)
                {
                    var lastSimilarFileName = Path.GetFileNameWithoutExtension(lastSimilarPath);
                    var stringNumber = lastSimilarFileName.Substring(lastSimilarFileName.Length - 3, 3);
                    stringNumber = stringNumber.Substring(1, stringNumber.Length - 2);
                    lastExistingNumber = Int32.Parse(stringNumber);
                }
            }

            // Update dictionary
            if (!_lastUsedNumberDict.ContainsKey(combinationIdentifier))
            {
                _lastUsedNumberDict.Add(combinationIdentifier, 0);
            }
            else
            {
                _lastUsedNumberDict[combinationIdentifier]++;
            }

            classifierFileName += "(" + _lastUsedNumberDict[combinationIdentifier] + ").txt";
            classifierPath = CLASSIFIERS_FOLDER + classifierFileName;

            if (null != _classifiersPathsList && !_classifiersPathsList.Contains(classifierPath))
            {
                _classifiersPathsList.Add(classifierPath);
            }

            return classifierPath;
        }

        private static bool _buildTree(BTree<Shapelet> tree
                                       , IEnumerable<Shapelet> permutation
                                       , int numClasses)
        {
            var candidatesForTree = permutation.ToArray();
            var candidatesCount = candidatesForTree.Count();
            var addedIndexes = new List<int>();

            var i = 0;
            while (candidatesForTree.Any(t => t != null) && !_enlistedCombinationCompleted(addedIndexes))
            {
                var shapelet = candidatesForTree[i];
                if (null != shapelet && tree.Add(tree.Root, shapelet))
                {
                    addedIndexes.Add(shapelet.LeftClassIndex);
                    addedIndexes.Add(shapelet.RightClassIndex);

                    candidatesForTree[i] = null;
                }

                i++;
                if (i == candidatesCount)
                {
                    i = 0;
                }
            }

            var uniqueIndexes = addedIndexes.Select(a => a).Distinct().ToArray();

            return uniqueIndexes.Count() == numClasses;
        }

        private static bool _enlistedCombinationCompleted(IEnumerable<int> usedIndexes)
        {
            var usedIndexesArray = usedIndexes.ToArray();
            if (!usedIndexesArray.Any())
            {
                return false;
            }
            var uniqueIndexes = usedIndexesArray.Select(a => a).Distinct().ToArray();
            foreach (int index in uniqueIndexes)
            {
                var count = uniqueIndexes.Count(t => t == index);
                if (count > 0 || count != 2)
                {
                    return false;
                }
            }

            return true;
        }

        private static double _testTreeAccuracy(BTree<Shapelet> tree, DataSet dataSet)
        {
            var result = 0.0;
            foreach (var classIndex in dataSet.ClassIndexes)
            {
                result += _testTreeOnGivenIndex(classIndex, tree, dataSet.TimeSeriesIndexes[classIndex]);
            }

            result /= dataSet.NumClasses;

            return result;
        }

        private static double _testTreeOnGivenIndex(int classIndex
                                                    , BTree<Shapelet> classificationTree
                                                    , IEnumerable<TimeSeries> testDataSet)
        {
            if (testDataSet == null)
            {
                throw new Exception("No data set is given!");
            }

            var correctclyClassified = 0;

            var dataSet = testDataSet as TimeSeries[] ?? testDataSet.ToArray();
            foreach (var timeSeries in dataSet)
            {
                // Classify the time series 
                var currentNode = classificationTree.Root;
                while (true)
                {
                    if (currentNode.Data == null)
                    {
                        break;
                    }

                    var shapeletNode = currentNode.Data;
                    if (shapeletNode != null)
                    {
                        var distance = Utils.SubsequenceDist(timeSeries, shapeletNode.ShapeletsValues);

                        // Left wing 
                        if (distance <= shapeletNode.OptimalSplitDistance)
                        {
                            if (currentNode.Left != null)
                            {
                                currentNode = currentNode.Left;
                                continue;
                            }

                            if (currentNode.Data.LeftClassIndex == classIndex)
                            {
                                correctclyClassified++;
                            }

                            break;
                        }

                        // Right wing
                        if (distance > shapeletNode.OptimalSplitDistance)
                        {
                            if (currentNode.Right != null)
                            {
                                currentNode = currentNode.Right;
                                continue;
                            }

                            if (currentNode.Data.RightClassIndex == classIndex)
                            {
                                correctclyClassified++;
                            }

                            break;
                        }
                    }
                }
            }

            return ((double)correctclyClassified / dataSet.Count());
        }

        private static void _padAnswer(StringBuilder answer)
        {
            var padLength = Utils.MAX_ANSWER_LENGTH - answer.Length;

            var padString = new StringBuilder().Insert(0, "0", padLength);

            answer.Append(padString);
        }

        private static void _buildClassificationPath(BTree<Shapelet> classificationTree
                                   , TimeSeries timeSeries
                                   , StringBuilder pathString)
        {
            if (timeSeries == null)
            {
                throw new InvalidEnumArgumentException("No data set is given");
            }

            if (classificationTree == null)
            {
                throw new InvalidEnumArgumentException("Classification tree does not exists!");
            }

            var currentNode = classificationTree.Root;
            while (true)
            {
                if (currentNode.Data == null)
                {
                    break;
                }

                var shapeletNode = currentNode.Data;
                if (shapeletNode != null)
                {
                    var distance = Utils.SubsequenceDist(timeSeries, shapeletNode.ShapeletsValues);
                    
                    // Left wing 
                    if (distance <= shapeletNode.OptimalSplitDistance)
                    {
                        if (currentNode.Left != null)
                        {
                            pathString.Append("L");
                            currentNode = currentNode.Left;
                            continue;
                        }

                        if (currentNode.Data.LeftClassIndex != int.MinValue) 
                        {
                            pathString.Append("L");
                            _padAnswer(pathString);
                            return; 
                        }

                        return;    
                    }

                    // Right wing
                    if (distance > shapeletNode.OptimalSplitDistance)
                    {
                        if (currentNode.Right != null)
                        {
                            pathString.Append("R");
                            currentNode = currentNode.Right;
                            continue;
                        }

                        if (currentNode.Data.RightClassIndex != int.MinValue) 
                        {
                            pathString.Append("R");
                            _padAnswer(pathString);
                            return; 
                        }

                        return;   
                    }
                }
            }

            return;  
        }

        private Shapelet _findShapelet(int classIndexA, int classIndexB, DataSet dataSet)
        {
            // Extract train time series 
            var trainTimeSeries = dataSet.ExtractFromDataSet(classIndexA, classIndexB).ToArray();

            // Find optimal shapelet that distinguish between the two class indexes 
            var pso = new BasicPSO(dataSet.MinLength
                                 , dataSet.MaxLength
                                 , dataSet.Step
                                 , trainTimeSeries[0].Values.Min()
                                 , trainTimeSeries[0].Values.Max()
                                 , trainTimeSeries);
            pso.InitPSO();
            pso.StartPSO();

            // Get shapelets parameters
            var shapelet = new Shapelet
            {
                OptimalSplitDistance = pso.BestParticle.OptimalSplitDistance,
                ShapeletsValues = pso.BestParticle.Position,
                BestInformationGain = pso.BestParticle.BestInformationGain,
            };

            //Set left and right class index of the shapelet 
            var dataSetClassA = trainTimeSeries.Where(t => t.ClassIndex == classIndexA).ToArray();
            _splitClasses(shapelet, classIndexA, dataSetClassA);
            var dataSetClassB = trainTimeSeries.Where(t => t.ClassIndex == classIndexB).ToArray();
            _splitClasses(shapelet, classIndexB, dataSetClassB);

            return shapelet;
        }

        private static void _splitClasses(Shapelet shapelet
                                          , int classIndex
                                          , TimeSeries[] dataSetTimeSeries)
        {
            var classifiedLessThatDistance = dataSetTimeSeries.Select(timeSeries =>
                        Utils.SubsequenceDist(timeSeries, shapelet.ShapeletsValues)).
                        Count(distance => distance < shapelet.OptimalSplitDistance);

            var classifiedMoreThanDistance = dataSetTimeSeries.Count() - classifiedLessThatDistance;

            if (classifiedLessThatDistance > classifiedMoreThanDistance)
            {
                shapelet.LeftClassIndex = classIndex;
            }
            else
            {
                shapelet.RightClassIndex = classIndex;
            }
        }

        private static BTree<Shapelet> _findMostAccurateTree(List<Shapelet> shapeletsList
                                                                 , DataSet dataSet)
        {
            var bestResult = 0.0;
            BTree<Shapelet> bestTree = null;

            // Load test time series for every class
            var combinations = Utils.GetCombinations(shapeletsList, 0, dataSet.NumClasses - 1);

            foreach (var combination in combinations)
            {
                var permutations = Utils.GetPermutations(combination, combination.Count());
                foreach (var permutation in permutations)
                {
                    // Create classification tree
                    var tree = new BTree<Shapelet> { Root = new BTree<Shapelet>.Node(permutation.First()) };

                    if (!_buildTree(tree, permutation, dataSet.NumClasses))
                    {
                        break;
                    }

                    var result = _testTreeAccuracy(tree, dataSet);
                    if (bestResult < result)
                    {
                        bestResult = result;
                        bestTree = tree;
                        bestTree.Accuracy = bestResult;
                    }
                }
            }

            return bestTree;
        }

        public bool LoadClassifier()
        {
            _mostAccurateTree = Utils.Deserialize<BTree<Shapelet>>(_decisionTreeFilepath);

            if (_mostAccurateTree != null)
            {
                return true;
            }

            return false;
        }

        public bool Train(DataSet dataSet)
        {
            var shapeletsList = new List<Shapelet>();

            foreach (var classIndexA in dataSet.ClassIndexes)
            {
                foreach (var classIndexB in dataSet.ClassIndexes)
                {
                    if (classIndexA < classIndexB)
                    {
                        var shapelet = _findShapelet(classIndexA, classIndexB, dataSet);

                        if (shapelet.LeftClassIndex != int.MinValue &&
                            shapelet.RightClassIndex != int.MinValue &&
                            shapelet.LeftClassIndex != shapelet.RightClassIndex)
                        {
                            shapeletsList.Add(shapelet);
                        }
                    }
                }
            }

            if (shapeletsList.Count > 0)
            {
                _mostAccurateTree = _findMostAccurateTree(shapeletsList, dataSet);

                if (_mostAccurateTree == null)
                {
                    return false; 
                }

                Utils.Serialize(_mostAccurateTree, _decisionTreeFilepath); 

                return true; 
            }

            return false; 
        }

        public string GetDecisionPath(TimeSeries timeSeries)
        {
            if (_mostAccurateTree == null)
            {
                return "";
            }
            
            var pathString = new StringBuilder();
            _buildClassificationPath(_mostAccurateTree
                                      , timeSeries
                                      , pathString);

            return pathString.ToString();
        }

    }
}
