using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Framework;
using Utilities;

namespace Core
{
    // Implements IClassifier functionality
    public abstract class ShapeletClassifierBase : IClassifier
    {
        private readonly int _minLength;
        private readonly int _maxLength;
        private readonly string _classificationTreePath; 
        private BTree<Shapelet> _bestClassificationTree;
        private IAgent _agent;
        public TypicalPath TypicalPath = new TypicalPath();
        
        protected abstract Shapelet FindShapelet(int classIndexA, int classIndexB, DataSet dataSet);
        
        protected ShapeletClassifierBase(int minLength, int maxLength, string classificationTreePath)
        {
            _minLength = minLength;
            _maxLength = maxLength;
            _classificationTreePath = classificationTreePath;
        }

        #region IClassifier overrides

        public bool LoadClassifier()
        {
            _bestClassificationTree = Utils.Deserialize<BTree<Shapelet>>(_classificationTreePath);

            if (_bestClassificationTree != null)
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
                        var shapelet = FindShapelet(classIndexA, classIndexB, dataSet);

                        //if (shapelet.LeftClassIndex != -1 && // TEST 
                        //    shapelet.RightClassIndex != -1 &&
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
                _bestClassificationTree = ShapeletsDataMiningUtils.FindBestClassificationTree(shapeletsList, dataSet);

                if (_bestClassificationTree == null)
                {
                    return false; 
                }

                //Console.WriteLine("\nClassification Tree Accuracy: {0}\n", _bestClassificationTree.Accuracy);

                Utils.Serialize(_bestClassificationTree, _classificationTreePath); // TEST

                return true; 
            }

            return false; 
        }

        public int Classify(TimeSeries timeSeries)
        {
            if (_bestClassificationTree == null)
            {
                return int.MinValue;  //-1; // TEST 
            }

            var distances = new List<double>(); 
            var pathString = new StringBuilder();
            var foundIndex = ShapeletsDataMiningUtils.Classify(_bestClassificationTree, timeSeries, distances, pathString);

            if (!TypicalPath.DistancesCollected)
            {
                if (!TypicalPath.TypicalDistances.ContainsKey(timeSeries.ClassIndex))
                {
                    TypicalPath.TypicalDistances.Add(timeSeries.ClassIndex, new List<double>());
                    
                }
                if (!TypicalPath.PathString.ContainsKey(timeSeries.ClassIndex))
                {
                    TypicalPath.PathString.Add(timeSeries.ClassIndex, new List<string>());
                }
                var path = distances.Sum(); 
                TypicalPath.TypicalDistances[timeSeries.ClassIndex].Add(path);
                TypicalPath.PathString[timeSeries.ClassIndex].Add(pathString.ToString());
            }

            return foundIndex;
        }

        public string ClassifyWithPath(TimeSeries timeSeries)
        {
            if (_bestClassificationTree == null)
            {
                return "";
            }

            var distances = new List<double>();
            var classificationPath = new StringBuilder();
            ShapeletsDataMiningUtils.Classify(_bestClassificationTree, timeSeries, distances, classificationPath);

            return classificationPath.ToString(); 
        }

        public string GetClassificationPath(int classIndex, int trial)
        {
            return TypicalPath.PathString[classIndex][trial]; 
        }

        #endregion   
    }
}
