#define EARLY_ABANDON

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
// using Framework;
using Utilities;

namespace Core
{
    public class ShapeletsDataMiningUtils
    {
        const string CLASSIFIERS_FOLDER = ".\\Classifiers\\";
        private static List<string> classifiersPathsList = null;
        private static Dictionary<string, int> lastUsedNumberDict = null; // Keeps what is the last used number of the available classifiers. Ex. ...1_2(5) -> ('1-2':5)

        public static int Classify(BTree<Shapelet> classificationTree
                                   , TimeSeries timeSeries
                                   , List<double> distances
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
                    if (distances != null)
                    {
                        distances.Add(distance);
                    }

                    // Left wing 
                    if (distance <= shapeletNode.OptimalSplitDistance)
                    {
                        if (currentNode.Left != null)
                        {
                            pathString.Append("L"); 
                            currentNode = currentNode.Left;
                            continue;
                        }

                        if (currentNode.Data.LeftClassIndex != int.MinValue)//-1) // TEST 
                        {
                            pathString.Append("L");
                            padAnswer(pathString); 
                            return currentNode.Data.LeftClassIndex;
                        }

                        return int.MinValue;  //-1; // TEST 
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

                        if (currentNode.Data.RightClassIndex != int.MinValue)//-1) // TEST 
                        {
                            pathString.Append("R");
                            padAnswer(pathString); 
                            return currentNode.Data.RightClassIndex;
                        }

                        return int.MinValue;  // -1; // TEST 
                    }
                }
            }

            return int.MinValue;  //-1; // TEST 
        }

        public static ShapeletClassifier CreateAndTrainClassifier(IList<int> classesInDataSet
                                                           , DataSet dataSet)
        {
            var classifier = new ShapeletClassifier(dataSet.MinLength
                                                    , dataSet.MaxLength
                                                    , getClassifierFileName(classesInDataSet, dataSet.DirectoryName));

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

        public static BTree<Shapelet> FindBestClassificationTree(List<Shapelet> shapeletsList, DataSet dataSet)
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

                    if (!buildTree(tree, permutation, dataSet.NumClasses))
                    {
                        break;
                    }

                    var result = testTreeAccuracy(tree, dataSet);
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

        private static void fillsClassifiersPathsList()
        {
            if (!Directory.Exists(CLASSIFIERS_FOLDER))
            {
                Directory.CreateDirectory(CLASSIFIERS_FOLDER);
            }

            classifiersPathsList = Directory.GetFiles(CLASSIFIERS_FOLDER).ToList();
            classifiersPathsList = classifiersPathsList.OrderBy(s => s).ToList(); 
        }

        private static string getClassifierFileName(IEnumerable<int> classesInDataSet, string workingFolder)
        {
            if (null == classifiersPathsList)
            {
                fillsClassifiersPathsList();
            }

            if (null == lastUsedNumberDict)
            {
                lastUsedNumberDict = new Dictionary<string, int>();
            }

            var lastExistingNumber = 0;
            var combinationIdentifier = String.Join("_", classesInDataSet);
            var classifierFileName = "Classificatin_tree_" + combinationIdentifier;
            var classifierPath = CLASSIFIERS_FOLDER + classifierFileName;
            
            // Get the number of the last similar file 
            if (null != classifiersPathsList && classifiersPathsList.Any())
            {
                var lastSimilarPath = classifiersPathsList.LastOrDefault(s => s.StartsWith(classifierPath));
                if (null != lastSimilarPath)
                {
                    var lastSimilarFileName = Path.GetFileNameWithoutExtension(lastSimilarPath);
                    var stringNumber = lastSimilarFileName.Substring(lastSimilarFileName.Length - 3, 3);
                    stringNumber = stringNumber.Substring(1, stringNumber.Length - 2);
                    lastExistingNumber = Int32.Parse(stringNumber);
                }
            }

            // Update dictionary
            if (!lastUsedNumberDict.ContainsKey(combinationIdentifier))
            {
                lastUsedNumberDict.Add(combinationIdentifier, 0);
            }
            else
            {
                lastUsedNumberDict[combinationIdentifier]++;
            }

            classifierFileName += "(" + lastUsedNumberDict[combinationIdentifier] + ").txt";
            classifierPath = CLASSIFIERS_FOLDER + classifierFileName;

            if (null != classifiersPathsList && !classifiersPathsList.Contains(classifierPath))
            {
                classifiersPathsList.Add(classifierPath);
            }

            return classifierPath; 
        }
        
        private static bool buildTree(BTree<Shapelet> tree, IEnumerable<Shapelet> permutation, int numClasses)
        {
            var candidatesForTree = permutation.ToArray();
            var candidatesCount = candidatesForTree.Count();
            var addedIndexes = new List<int>();

            var i = 0;
            while (candidatesForTree.Any(t => t != null) && !enlistedCombinationCompleted(addedIndexes))
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

        private static bool enlistedCombinationCompleted(IEnumerable<int> usedIndexes)
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

        private static double testTreeAccuracy(BTree<Shapelet> tree, DataSet dataSet)
        {
            var result = 0.0;
            foreach (var classIndex in dataSet.ClassIndexes)
            {
                result += testClassify(classIndex, tree, dataSet.TimeSeriesIndexes[classIndex]);
            }

            result /= dataSet.NumClasses;

            return result;
        }

        private static void padAnswer(StringBuilder answer)
        {
            var padLength = Utils.MAX_ANSWER_LENGTH - answer.Length;

            var padString = new StringBuilder().Insert(0, "0", padLength);

            answer.Append(padString);
        }

        private static double testClassify(int classIndex, BTree<Shapelet> classificationTree, IEnumerable<TimeSeries> testDataSet)
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

    }
}
