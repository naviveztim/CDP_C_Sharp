﻿using System;
using System.Collections.Generic;
using Utilities;

namespace Framework
{
    public class ClassificationAgent : IAgent
    {
        public IClassifier ClassifierInstance { get; private set; }

        public ClassificationAgent(IClassifier aClassifierInstance)
        {
            ClassifierInstance = aClassifierInstance;
        }

        public void Train(DataSet dataSet)
        {
            if (ClassifierInstance != null)
            {
                ClassifierInstance.Train(dataSet);
            }
        }

        public int Classify(TimeSeries aTimeSeries)
        {
            if (ClassifierInstance != null)
            {
                return ClassifierInstance.Classify(aTimeSeries);
            }

            throw new Exception("No data mining instance is present!");
        }

        public string ClassifyWithPath(TimeSeries timeSeries)
        {
            if (ClassifierInstance != null)
            {
                return ClassifierInstance.ClassifyWithPath(timeSeries);
            }

            throw new Exception("No data mining instance is present!");
        }

        public string GetClassificationPath(int classIndex, int trial)
        {
            if (ClassifierInstance != null)
            {
                return ClassifierInstance.GetClassificationPath(classIndex, trial);
            }

            throw new Exception("No data mining instance is present!");
        }

        
    }
}