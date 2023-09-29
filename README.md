## CDP_C_Sharp
 This is C# implementation of CDP (Concatenated Decisions Path) algorithm 

## Main advantages
- Very fast to train
- Very small model size 
- Easily add new classes 
- High accuracy 

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

### Contacts: 
cdp_project@outlook.com

### References: 

_“Concatenated Decision Paths Classification for Datasets with Small Number of Class Labels”, Ivan Mitzev and N.H. Younan, ICPRAM, Porto, Portugal, 24-26 February 2017_

_“Concatenated Decision Paths Classification for Time Series Shapelets”, Ivan Mitzev and N.H. Younan, International journal for Instrumentation and Control Systems (IJICS), Vol. 6, No. 1, January 2016_

_“Combined Classifiers for Time Series Shapelets”, Ivan Mitzev and N.H. Younan, CS & IT-CSCP 2016 pp. 173–182, Zurich, Switzerland, January 2016_

_“Time Series Shapelets: Training Time Improvement Based on Particle Swarm Optimization”, Ivan Mitzev and N.H. Younan, IJMLC 2015 Vol. 5(4): 283-287 ISSN: 2010-3700_


