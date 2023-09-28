using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        public static double EuclidianDistance(double[] array1
                                               , double[] array2
                                               , double currentMinDistance)
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

        public static void CalculateInformationGain(IOrderedEnumerable<HistogramItem> histogram
                                                    , out double informationGain
                                                    , out double optimalSplitDistance
                                                    , out double entropy)
        {
            informationGain = 0.0;
            optimalSplitDistance = 0.0;
            entropy = -1.0; 
            var I = Entropy(histogram);
            var all = histogram.Count();

            var minDistance = histogram.First().Distance;
            
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
                // Early abandon
                minDistance = Math.Min(minDistance, EuclidianDistance(candidateValues, currentArray, minDistance)) ;
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

        public static double SimilarityCoefficient(string s1, string s2)
        {
            var result = 0.0;
            var array1 = s1.ToArray(); 
            var array2 = s2.ToArray(); 


            if (s1 == null || s2 == null)
            {
                throw new ArgumentException("input string");
            }

            var length = s1.Length;

            if (length != s2.Length)
            {
                throw new ArgumentException("input string");
            }

            for (var i = 0; i < length; i++)
            {
                if (array1[i] == array2[i]) 
                {
                    result += 1.0; 
                }
            }

            result /= length; 

            return result; 
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
        
        public static void Derivative(double[] signal)
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

        public static void CompressATimeSeries(TimeSeries timeSeries, int compressionIndex)
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

                        // Add new List in the list to hold the new combination
                        combinations.Add(new List<T>());

                        // Add the starting index element from “array”
                        combinations[combinationsListIndex].Add(array[arrayIndex]);
                        while (combinations[combinationsListIndex].Count < combinationLenght)
                        {

                            // Add until we come to the length of the combination
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
        /// Try to create uniformly distributed group of indexes combinations 
        /// Ex. [1, 2], [2, 3], [3, 1] - All numbers in given groups are shown twice
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

        public static List<IList<int>> createGroupOfIndexes(List<int> classIndexes
                                                      , int PatternLength
                                                      , int NumClassLabelsPerTree)
        {
            List<IList<int>> group;
            var allCombinations = Utils.GetCombinations(classIndexes, 0, NumClassLabelsPerTree); // TEST 
            var allCombinationsCount = allCombinations.Count();

            if (PatternLength <= allCombinationsCount)
            {
                group = createGroup(allCombinations, classIndexes);
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
                    var smallGroup = createGroup(allCombinations, classIndexes);
                    group.AddRange(smallGroup);
                }
            }

            return group;
        }

        #endregion
    }
}
