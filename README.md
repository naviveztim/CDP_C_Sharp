##  C# implementation of CDP (Concatenated Decisions Path) algorithm - fast and accurate algorithm for time series classification 

## Overview 
 CDP is a novel method for time-series classification using shapelets. The approach focuses on overcoming the limitations of traditional 
 shapelet-based methods, primarily their slow training times, while maintaining high accuracy. Proposed algorithm 
 involves training small decision trees and combining their decisions to form unique patterns for identifying time-series 
 data. Method is tested on dataset from [UCR](https://www.cs.ucr.edu/~eamonn/time_series_data_2018/))

## Build 
 Clone the project and open solution with VisualStudio. Rebuild project. Build produces console application: CDPMethod.exe   

## Run executable  
 Example: 
 ```sh
 CDPMethod.exe --train "<Filepath to train file>" 
               --test "<Filepath to test file>"
               --compress 2 
               --pattern_length 1500 
               --tree_size 2 
               --norm N 
               --signal S
```

### Donate
If you like this project, consider supporting me by donating.

[![Donate](https://www.paypalobjects.com/en_US/i/btn/btn_donateCC_LG.gif)](https://www.paypal.com/donate/?hosted_button_id=E7U5FRCCUVNL6)

## Main characteristics 
- Very fast to (re)train (usually less than a minute)
- Very small model size (usually less than 1MB)
- Easily add new classes 
- High accuracy (well comparable with state-of-the-art methods)
- Does not depend on any other machine learning package

Table 1. Training time and accuracy of **C# implementation** of CDP method

| UCR Dataset  | Num. classes | Num. train samples | Num. test samples | Training time, [sec] | Accuracy, [%] | Compression rate | Num. decision trees | Normalize | Derivative |
|--------------|--------------|--------------------|-------------------|----------------------|---------------|------------------|---------------------|-----------|------------|
| Swedish Leaf | 15           | 500                | 625               | 16.3                 | 92.7%         | 2                | 700                 | No        | No         |
| Beef         | 5            | 30                 | 30                | 24.1                 | 86.8%         | 1                | 400                 | Yes       | Yes        |
| OliveOil     | 4            | 30                 | 30                | 71.3                 | 90.1%         | 2                | 200                 | Yes       | No         |
| Symbols      | 6            | 25                 | 995               | 3.8                  | 95.6%         | 4                | 600                 | Yes       | Yes        |
| OsuLeaf      | 6            | 200                | 242               | 15.1                 | 88.9%         | 4                | 800                 | Yes       | Yes        |

## Why implementation in C#?

- Produce fast executables (Training is 10 to 100 times faster than python version)
- Can be used in your .NET project 
- Can be easily transferred to code for application development for Android (via Xamarin)
- Can be easily transferred to code for application development for AR glasses, and games (via UnityHub)

### Training 
<pre>
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
</pre>

### Testing 

<pre>
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
Console.WriteLine("Average accuracy: {0:N2}%", averageAccuracy*100.0);
</pre>

### Website: 
[cdp-project.com](https://cdp-project.com)

### References: 

“Concatenated Decision Paths Classification for Datasets with Small Number of Class Labels”, Ivan Mitzev and N.H. Younan, ICPRAM, Porto, Portugal, 24-26 February 2017

“Concatenated Decision Paths Classification for Time Series Shapelets”, Ivan Mitzev and N.H. Younan, International journal for Instrumentation and Control Systems (IJICS), Vol. 6, No. 1, January 2016

“Combined Classifiers for Time Series Shapelets”, Ivan Mitzev and N.H. Younan, CS & IT-CSCP 2016 pp. 173–182, Zurich, Switzerland, January 2016

“Time Series Shapelets: Training Time Improvement Based on Particle Swarm Optimization”, Ivan Mitzev and N.H. Younan, IJMLC 2015 Vol. 5(4): 283-287 ISSN: 2010-3700


