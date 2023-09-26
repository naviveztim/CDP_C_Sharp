#define EARLY_ABANDON

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

namespace Utilities
{
    [Serializable]
    public class TimeSeries
    {
        public int ClassIndex
        {
            get; set;
        }

        public double[] Values
        {
            get; set;
        }
    }

    public class HistogramItem
    {
        public int ClassIndex
        {
            get; set;
        }

        public double Distance
        {
            get; set;
        }
    }

    public class Utils
    {
        public static int MAX_ANSWER_LENGTH = 1;

        private static void _padAnswer(StringBuilder answer)
        {
            var padLength = Utils.MAX_ANSWER_LENGTH - answer.Length;

            var padString = new StringBuilder().Insert(0, "0", padLength);

            answer.Append(padString);
        }

        public static double EuclidianDistance(double[] array1, double[] array2, double currentMinDistance)
        {
            if ((array1 == null) || (array2 == null))
            {
                throw new ArgumentException("Euclidian.NullArray!");
            }

            if (array1.Length != array2.Length)
            {
                throw new ArgumentException("Euclidian.IncorrectArrayLength");
            }

            var euclidian = 0.0;

            for (int i = 0; i < array1.Length; i++)
            {
                euclidian += (array1[i] - array2[i]) * (array1[i] - array2[i]);
                if (euclidian > currentMinDistance)
                {
                    break; 
                }
            }

            return euclidian;
        }

        public static double Entropy(IEnumerable<HistogramItem> histogram)
        {
            double entropy = 0.0;

            var hist = histogram.ToArray();

            var classIndexes = hist.Select(a => a.ClassIndex).Distinct();
            var numAllElements = hist.Count();

            foreach (var index in classIndexes)
            {
                int index1 = index; // Explicitly set as it may have different behaviour in different compilers 
                var numClassElements = hist.Count(x => x.ClassIndex == index1);
                var ratio = (double)numClassElements / (double)numAllElements;

                entropy += -ratio * Math.Log(ratio);
            }

            return entropy;
        }

        public static void CalculateInformationGain(IOrderedEnumerable<HistogramItem> histogram, out double informationGain, 
                                                                                                 out double optimalSplitDistance, 
                                                                                                 out double entropy)
        {
            informationGain = 0.0;
            optimalSplitDistance = 0.0;
            entropy = -1.0; 
            var I = Entropy(histogram);
            var all = histogram.Count();

            var minDistance = histogram.First().Distance;
            //var maxDistance = histogram.Last().Distance;

            var previousDistance = minDistance;

            foreach (var element in histogram)
            {
                if (element.Distance > previousDistance)
                {
                    var d = previousDistance + (element.Distance - previousDistance) / 2.0;
                    previousDistance = element.Distance;

                    var h1 = histogram.Where(x => x.Distance <= d).ToArray();
                    var h2 = histogram.Where(x => x.Distance > d).ToArray();

                    var I1 = Entropy(h1);
                    var I2 = Entropy(h2);

                    var f1 = h1.Count() / (double)all;
                    var f2 = h2.Count() / (double)all;

                    var currentEntropy = f1*I1 + f2*I2; 
                    var currentInformationGain = I - currentEntropy;

                    if (currentInformationGain > informationGain)
                    {
                        informationGain = currentInformationGain;
                        optimalSplitDistance = d;
                        entropy = currentEntropy; 
                    }
                }
            }
        }

        public static double SubsequenceDist(TimeSeries timeSeries, double[] candidateValues)
        {
            var timeSeriesLen = timeSeries.Values.Length;
            var candidateLen = candidateValues.Length;

            var minDistance = Double.MaxValue;
            var currentArray = new double[candidateLen];

            for (var k = 0; k < timeSeriesLen - candidateLen + 1; k++)
            {
                Array.Copy(timeSeries.Values, k, currentArray, 0, candidateLen);
#if EARLY_ABANDON
                minDistance = Math.Min(minDistance, EuclidianDistance(candidateValues, currentArray, minDistance)) ;
                //minDistance = Math.Min(minDistance, EnergyDistance(candidateValues, currentArray, minDistance)); //?? 
#else
                minDistance = Math.Min(minDistance, EuclidianDistance(candidateValues, currentArray));
#endif 
            }

            return minDistance;
        }

        public static void Serialize<T>(T serializedObject, string classificationTreePath)
        {
            var fs = new FileStream(classificationTreePath, FileMode.Create);

            var formatter = new BinaryFormatter();
            try
            {
                formatter.Serialize(fs, serializedObject);
            }
            catch (SerializationException e)
            {
                Console.WriteLine("Failed to serialize. Reason: " + e.Message);
                throw;
            }
            finally
            {
                fs.Close();
            }
        }

        public static T Deserialize<T>(string fileName)
        {
            if (!File.Exists(fileName))
            {
                return default(T);
            }

            T serialized;
            // Open the file containing the data that you want to deserialize.
            var fs = new FileStream(fileName, FileMode.Open);
            try
            {
                var formatter = new BinaryFormatter();

                // Deserialize the hashtable from the file and  
                // assign the reference to the local variable.
                serialized = (T)formatter.Deserialize(fs);
            }
            catch (SerializationException e)
            {
                Console.WriteLine("Failed to deserialize. Reason: " + e.Message);
                throw;
            }
            finally
            {
                fs.Close();
            }

            return serialized;
        }

        public static void AddHistogramItem<T>(List<List<T>> histogram, T item, Func<T, double> func,  
                                               double minValue, double maxValue)
        {

            if (histogram == null)
                throw new InvalidDataException("histogram");

            var numBins = histogram.Count;
            var range = (maxValue - minValue);

            var i = (int)(((func(item) - minValue) / range) * (numBins - 1));

            histogram[i].Add(item);
        }

        public static IEnumerable<List<T>> CreateHistogram<T>(int numBeans, IEnumerable<T> extendedHistItems,
                                                              Func<T, double> func, double minValue, double maxValue)
        {
            // Init histogram
            var histogram = new List<List<T>>();
            for (var i = 0; i < numBeans; i++)
            {
                histogram.Add(new List<T>());
            }

            // Create histogram
            foreach (var item in extendedHistItems)
            {
                AddHistogramItem(histogram, item, func, minValue, maxValue);
            }

            return histogram;
        }
        
        public static double AverageValue(IList<double> array, int startIndex, int numElements)
        {
            if (array.Count < startIndex + numElements)
            {
                throw new ArgumentException("averageValueArguments");
            }
            var sumValue = 0.0;
            for (var i = startIndex; i < (startIndex + numElements); i++)
            {
                sumValue += array[i];
            }

            return sumValue / numElements;
        }

        public static double Std(IList<double> array, double average)
        {
            var sumValue = 0.0;
            var numeElements = array.Count;
            if (numeElements == 1)
            {
                return 0.0;
            }
            
            for (var i = 0; i < numeElements; i++)
            {
                sumValue += (array[i] - average) * (array[i] - average);
            }

            return Math.Sqrt(sumValue/(numeElements - 1)); 
        }

        public static double SimilarityCoefficient(string s1, string s2)
        {
            var result = 0.0;
            var array1 = s1.ToArray(); // TEST 
            var array2 = s2.ToArray(); // TEST 


            if (s1 == null || s2 == null)
            {
                throw new ArgumentException("input string");
            }

            var length = s1.Length;

            if (length != s2.Length)
            {
                throw new ArgumentException("input string");
            }

            //var length = s1.Length;

            for (var i = 0; i < length; i++)
            {
                //if (s1[i] == s2[i])
                if (array1[i] == array2[i]) // TEST 
                {
                    result += 1.0; 
                }
            }

            result /= length; 

            return result; 
        }

        public static double Similarity1NNCoefficient(string s1, string s2)
        {
            var array1 = s1.ToArray(); // TEST 
            var array2 = s2.ToArray(); // TEST 
            
            if (s1 == null || s2 == null)
            {
                throw new ArgumentException("input string");
            }

            var length = s1.Length;

            if (length != s2.Length)
            {
                throw new ArgumentException("input string");
            }

            //var length = s1.Length;
            var distance = 0.0; 
            for (var i = 0; i < length; i++)
            {
                var value1 = (float) Char.GetNumericValue(array1[i]);
                var value2 = (float) Char.GetNumericValue(array2[i]); 
                distance +=  (value1 - value2) * (value1 - value2);
            }

            return Math.Sqrt(distance);
        }

        public static double StdDev(IEnumerable<double> values)
        {
            double ret = 0;
            IEnumerable<double> enumerable = values as double[] ?? values.ToArray();

            if (enumerable.Any())
            {
                //Compute the Average      
                var avg = enumerable.Average();

                //Perform the Sum of (value-avg)_2_2      
                var sum = enumerable.Sum(d => Math.Pow(d - avg, 2));

                //Put it all together      
                ret = Math.Sqrt((sum) / (enumerable.Count() - 1));
            }

            return ret;
        }
        
        public static void Derivate(double[] signal)
        {
            for (var i = signal.Length-1; i >= 1 ; i--)
            {
                signal[i] = (signal[i] - signal[i-1]);
            }
            signal[0] = 0; 

        }

        public static void Normalize(double[] signal)
        {
            var average = signal.Average();
            var std = StdDev(signal);
            for (var i = 0; i < signal.Length; i++)
            {
                signal[i] = (signal[i] - average) / std;
            }

        }
 
        public static void CompressTimeSeries(List<TimeSeries> TimeSeries, int compressionIndex)
        {
            foreach (var timeSeries in TimeSeries)
            {
                var newTimeSeriesLength = timeSeries.Values.Length / compressionIndex;
                var newValues = new double[newTimeSeriesLength];
                var startIndex = 0;
                for (var i = 0; i < newTimeSeriesLength; i++)
                {
                    newValues[i] = AverageValue(timeSeries.Values, startIndex, compressionIndex);
                    startIndex += compressionIndex;
                }

                timeSeries.Values = newValues;
            }
        }

        /*
         * Permutation usage: 
         * IEnumerable<IEnumerable<int>> result = GetPermutations(Enumerable.Range(1, 3), 3);
         * Output - a list of integer-lists:
         * {1,2,3} {1,3,2} {2,1,3} {2,3,1} {3,1,2} {3,2,1}
         * 
         */
        public static IEnumerable<IEnumerable<T>> GetPermutations<T>(IEnumerable<T> list, int length)
        {
            if (length == 1) return list.Select(t => new T[] { t });

            return GetPermutations(list, length - 1)
                .SelectMany(t => list.Where(e => !t.Contains(e)),
                    (t1, t2) => t1.Concat(new T[] { t2 }));
        }

        public static List<List<T>> GetCombinations<T>(List<T> array, int startingIndex, int combinationLenght)
        {

            var combinations = new List<List<T>>();
            if (combinationLenght == 1)
            {
                combinations.Add(array);
                return combinations;
            }

            if (combinationLenght == 2)
            {

                int combinationsListIndex = 0;
                for (var arrayIndex = startingIndex; arrayIndex < array.Count(); arrayIndex++)
                {

                    for (int i = arrayIndex + 1; i < array.Count(); i++)
                    {

                        //add new List in the list to hold the new combination
                        combinations.Add(new List<T>());

                        //add the starting index element from “array”
                        combinations[combinationsListIndex].Add(array[arrayIndex]);
                        while (combinations[combinationsListIndex].Count < combinationLenght)
                        {

                            //add until we come to the length of the combination
                            combinations[combinationsListIndex].Add(array[i]);
                        }
                        combinationsListIndex++;
                    }

                }

                return combinations;
            }

            List<List<T>> combinationsofMore = new List<List<T>>();
            for (var i = startingIndex; i < array.Count() - combinationLenght + 1; i++)
            {
                //generate combinations of lenght-1(if lenght > 2 we enter into recursion)
                combinations = GetCombinations(array, i + 1, combinationLenght - 1);

                //add the starting index Elemetn in the begginnig of each newly generated list
                for (int index = 0; index < combinations.Count; index++)
                {
                    combinations[index].Insert(0, array[i]);
                }

                for (int y = 0; y < combinations.Count; y++)
                {
                    combinationsofMore.Add(combinations[y]);
                }
            }

            return combinationsofMore;
        }

        #region Groups definitions
        ///
        /// Rules: 
        /// 1. Make 3-, 4- class index members classification trees Ex. {1, 2, 3} and {1, 2, 3, 4}
        /// 2. 3-members classification tree should overlap each other with one member, 4- class index classification
        /// tree should overlap each other with two members. Ex: {1, 2, 3}- {3, 4, 5} and {1, 2, 3, 4}- {3, 4, 5, 6}
        /// 3. Make tree managing agents with different set of classifiers (to have a majority of managing classifiers- at least two equal)
        /// 4. Every class index should be seen equal number of times in managing agents (usually twice) Ex: {1, 2, 3}; {3, 4, 5}; {5, 1}, {4, 2} 
        /// 

        private static bool exceedMaxRepetitions(ICollection<IList<int>> selectedCombinations
                                          , IEnumerable<int> candidateCombination)
        {
            foreach (var number in candidateCombination)
            {
                var numberCounts = 0;
                foreach (var combination in selectedCombinations)
                {
                    if (combination.Contains(number))
                    {
                        numberCounts++;
                        if (numberCounts >= 100)
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        private static bool moreThanTwoOverlappings(IEnumerable<IList<int>> selectedCombinations
                                             , ICollection<int> candidateCombination)
        {
            var orderedCandidateCombination = candidateCombination.OrderByDescending(n => n).ToArray();
            foreach (var combination in selectedCombinations)
            {
                if (combination.Count() != candidateCombination.Count)
                {
                    return true; // Do not take that candidate
                }

                var numberEqulas = 0;
                var orderedCombination = combination.OrderByDescending(n => n).ToArray();
                for (var i = 0; i < candidateCombination.Count; i++)
                {
                    if (orderedCandidateCombination[i] == orderedCombination[i])
                    {
                        numberEqulas++;
                    }
                }

                if (numberEqulas > 2)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool isProperCandidate(ICollection<IList<int>> selectedCombinations
                                       , ICollection<int> candidateCombination)
        {
            if (selectedCombinations != null)
            {
                if (!exceedMaxRepetitions(selectedCombinations, candidateCombination) &&
                    !moreThanTwoOverlappings(selectedCombinations, candidateCombination))
                {
                    return true;
                }
            }

            return false;
        }

        private static List<int> getUnpairedNumbers(IEnumerable<IList<int>> selectedCombinations
                                             , IEnumerable<int> usedClasses)
        {
            var unpairedNumbers = new List<int>();

            foreach (var number in usedClasses)
            {
                var numberCounts = 0;
                foreach (var combination in selectedCombinations)
                {
                    if (combination.Contains(number))
                    {
                        numberCounts++;
                    }
                }
                if (numberCounts < 2)
                {
                    unpairedNumbers.Add(number);
                }
            }

            return unpairedNumbers;
        }

        private static List<IList<int>> selectCombinations(IEnumerable<IList<int>> combinations)
        {
            var selectedCombinations = new List<IList<int>>();

            foreach (var combination in combinations)
            {
                if (selectedCombinations.Count == 0 ||
                    isProperCandidate(selectedCombinations, combination))
                {
                    selectedCombinations.Add(combination);
                }
            }

            return selectedCombinations;
        }

        private static List<IList<int>> createGroup(IEnumerable<IList<int>> combinations
                                            , IEnumerable<int> usedClasses)
        {
            List<int> unPairedNumbers;
            List<IList<int>> selectedCombinations;

            var combinationsArray = combinations as IList<int>[] ?? combinations.ToArray();
            var usedClassesArray = usedClasses.ToArray();
            var distictIndexesCount = 0;
            do
            {
                selectedCombinations = selectCombinations(combinationsArray.OrderBy(a => Guid.NewGuid()));

                unPairedNumbers = getUnpairedNumbers(selectedCombinations, usedClassesArray);

                distictIndexesCount = unPairedNumbers.Distinct().Count();

            } while (unPairedNumbers.Count != 0 && distictIndexesCount > usedClassesArray.Count());

            return selectedCombinations;
        }

        public static void printGroup(IEnumerable<IList<int>> group, string groupName)
        {
            Console.WriteLine("Decision trees:");
            var enumerable = @group as IList<int>[] ?? @group.ToArray();

            var groupAsString = "";
            foreach (var line in enumerable)
            {
                var decisionTree = String.Join(", ", line);
                groupAsString += "[" + decisionTree + "]" + "; ";
            }

            Console.WriteLine(groupAsString);
            Console.WriteLine();
            Console.WriteLine("Number of generated decision trees: {0}", enumerable.Count());
            Console.WriteLine("(Might not exactly coinside with requested pattern length, as the process tries to keep uniform distribution of class labels.)\n");
        }

        public static List<IList<int>> createSpecifiedGroup(List<int> classIndexes
                                                      , int PatternLength
                                                      , int NumClassLabelsPerTree)
        {
            //int maxRepetitionsPerGroup; 
            List<IList<int>> group;
            var allCombinations = Utils.GetCombinations(classIndexes, 0, NumClassLabelsPerTree); // TEST 
            var allCombinationsCount = allCombinations.Count();

            if (PatternLength <= allCombinationsCount)
            {
                //maxRepetitionsPerGroup = (PatternLength * NumClassLabelsPerTree) / NumClassLabels;
                group = Utils.createGroup(allCombinations, classIndexes);
            }
            else
            {
                var numRepetitions = PatternLength / allCombinationsCount;
                var numRestGroups = PatternLength % allCombinationsCount;
                group = new List<IList<int>>();
                for (var i = 0; i < numRepetitions; i++)
                {
                    group.AddRange(allCombinations);
                }

                if (numRestGroups > 0)
                {
                    //maxRepetitionsPerGroup = PatternLength / NumClassLabels;
                    var smallGroup = createGroup(allCombinations, classIndexes);
                    group.AddRange(smallGroup);
                }
            }

            return group;
        }

        #endregion
    }
}
