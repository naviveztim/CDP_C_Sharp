using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;

namespace Core
{
    public static class Program
    {
        static string TRAIN_FILE_PATH = "";
        static string TEST_FILE_PATH = "";
        static string DELIMITER = ",";
        static int COMPRESSION_FACTOR = 1;
        static bool USE_SIGNAL = true; 
        static bool NORMALIZE = false; 
        static int NUM_TREES = 100; 
        static int NUM_CLASS_LABELS_PER_TREE = 2; 
        
        private static bool show_parameters()
        {
            Console.WriteLine();
            Console.WriteLine("Concatenated decision paths (CDP) classification. (C)");
            Console.WriteLine();
            Console.WriteLine("Train file: {0}", "\"" + TRAIN_FILE_PATH + "\"");
            Console.WriteLine("Test file: {0}", "\"" + TEST_FILE_PATH + "\"");
            Console.WriteLine("Delimiter: '{0}'", DELIMITER);
            Console.WriteLine("Compression factor: {0}", COMPRESSION_FACTOR);
            Console.WriteLine("Use signal(S)/derivate(D): {0}", USE_SIGNAL ? "S" : "D");
            Console.WriteLine("Normalize: {0}", NORMALIZE ? "Yes" : "No");
            Console.WriteLine("Number of class labels per tree: {0}", NUM_CLASS_LABELS_PER_TREE);
            Console.WriteLine("Number of decision trees: {0}", NUM_TREES);
            Console.WriteLine();
            Console.WriteLine("Press any key to continue or 'n' to stop the process.");
            if (Console.Read() == 'n')
            {
                usage();
                return false;
            }

            return true; 
        }
        private static void parse_arguments(string[] args)
        {
            for (var i = 0; i < args.Count(); i++)
            {
                if (args[i].Contains("--train"))
                {
                    TRAIN_FILE_PATH = args[i + 1];
                }
                else if (args[i].Contains("--test"))
                {
                    TEST_FILE_PATH = args[i + 1];
                }
                else if (args[i].Contains("--delimiter"))
                {
                    DELIMITER = args[i + 1];
                }
                else if (args[i].Contains("--compress"))
                {
                    COMPRESSION_FACTOR = int.Parse(args[i + 1]);
                }
                else if (args[i].Contains("--signal"))
                {
                    var s = args[i + 1];
                    USE_SIGNAL = s.Equals("S") || s.Equals("s");
                }
                else if (args[i].Contains("--norm"))
                {
                    var n = args[i + 1];
                    NORMALIZE = n.Equals("Y") || n.Equals("y");
                }
                else if (args[i].Contains("--tree_size"))
                {
                    NUM_CLASS_LABELS_PER_TREE = int.Parse(args[i + 1]);
                }
                else if (args[i].Contains("--pattern_length"))
                {
                    NUM_TREES = int.Parse(args[i + 1]);
                }
                
            }

            if (args.Count() <= 1)
            {
                usage();
                return;
            }
        }
        private static void usage()
        {
            Console.WriteLine();
            Console.WriteLine("Usage:");
            Console.WriteLine("CDPMethod");
            Console.WriteLine("\t--train \"Train file path\"");
            Console.WriteLine("\t--test \"Test file path\"");
            Console.WriteLine("\t[--delimiter \"Delimiter\"]");
            Console.WriteLine("\t[--compress (1,...)]");
            Console.WriteLine("\t[--signal [S|D]]");
            Console.WriteLine("\t[--norm [Y/N]]");
            Console.WriteLine("\t[--tree_size (2:4)]");
            Console.WriteLine("\t[--pattern_length (1,...)");
            Console.WriteLine(); 
        }
        private static void generateTimeSeriesMatrixFromFile(string filePath
                                                            , string delimiter
                                                            , List<int> classLabels
                                                            , List<List<double>> timeSeriesMatrix)
        {
            if (!File.Exists(filePath))
            {
                return;
            }

            foreach (var line in File.ReadLines(filePath))
            {
                if (line.Equals(""))
                {
                    continue; 
                }

                // Index of the class must be written at the front of the line 
                var numbers = line.Split(delimiter.ToCharArray(), StringSplitOptions.RemoveEmptyEntries);

                classLabels.Add((int)double.Parse(numbers.First()));
                var values = new double[numbers.Length - 1]; 
                for (var i = 1; i < numbers.Length; i++)
                {
                    values[i - 1] = double.Parse(numbers[i], NumberStyles.Float);
                }

                timeSeriesMatrix.Add(values.ToList());
            }

        }

        private static void process()
        {
            var sw = new Stopwatch();
            var averageTrainingTime = 0.0;
            var averageAccuracy = 0.0;

            sw.Restart();

            // ----- Training -----

            // Create model 
            var model = new Cdp(NUM_TREES
                                , COMPRESSION_FACTOR
                                , USE_SIGNAL
                                , NORMALIZE
                                , NUM_CLASS_LABELS_PER_TREE);

            // Obtain train dataset
            var trainClassLabels = new List<int>();
            var trainTimeSeriesMatrix = new List<List<double>>();
            generateTimeSeriesMatrixFromFile(TRAIN_FILE_PATH
                                             , DELIMITER
                                             , trainClassLabels
                                             , trainTimeSeriesMatrix);

            // Fit model 
            model.Fit(trainClassLabels, trainTimeSeriesMatrix);

            sw.Stop();
            averageTrainingTime += sw.ElapsedMilliseconds;

            // ----- Testing -----

            // Obtain test dataset
            var testClassLabels = new List<int>();
            var testTimeSeriesMatrix = new List<List<double>>();
            generateTimeSeriesMatrixFromFile(TEST_FILE_PATH
                                             , DELIMITER
                                             , testClassLabels
                                             , testTimeSeriesMatrix);

            // Predict
            var resultClassLabels = model.Predict(testTimeSeriesMatrix);

            // Evaluate results 
            var countSame = 0;
            var countAll = resultClassLabels.Count();
            for (var j = 0; j < countAll; j++)
            {
                if (testClassLabels[j] == resultClassLabels[j])
                {
                    countSame++;
                }
            }
            averageAccuracy += (countSame / (float)countAll); 
            
            // Show results 
            Console.Write("\r                   ");
            Console.WriteLine();
            Console.WriteLine("Average training time: {0} seconds", averageTrainingTime/1000.0);
            Console.WriteLine("Average accuracy: {0:N2}%", averageAccuracy*100.0);
            Console.WriteLine("Done!");
            Console.WriteLine();
            Console.Read();
            Console.Read();
        }
        static void Main(string[] args)
        {
            try
            {
                parse_arguments(args);
                if (show_parameters())
                    process();
            }
            catch (Exception e)
            {
                Console.WriteLine("ERROR: " + e.Message); 
            }
        }
    }
}
