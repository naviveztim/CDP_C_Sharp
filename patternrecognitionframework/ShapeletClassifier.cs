using System;
using System.Collections.Generic;
using System.Linq;
using Framework;
using Utilities;

namespace Core
{
    // Implements FindShapelet abstract method
    public class ShapeletClassifier : ShapeletClassifierBase
    {
        private TimeSeries[] _trainTimeSeries;
        
        public ShapeletClassifier(int minLength, int maxLength, string classificationTreePath)
            : base(minLength, maxLength, classificationTreePath) {}

        protected override Shapelet FindShapelet(int classIndexA, int classIndexB, DataSet dataSet)
        {
            _trainTimeSeries = dataSet.ExtractFromDataSet(classIndexA, classIndexB).ToArray();

            var pso = new BasicPSO(dataSet.MinLength
                                 , dataSet.MaxLength
                                 , dataSet.Step
                                 , _trainTimeSeries[0].Values.Min()
                                 , _trainTimeSeries[0].Values.Max()
                                 , _trainTimeSeries);
            
            Console.WriteLine("ClassA: {0}, ClassB: {1}", classIndexA, classIndexB);
            pso.InitPSO();
            pso.StartPSO();

            // Attach shapelets parameters from PSO 
            var shapelet = new Shapelet
            {
                OptimalSplitDistance = pso.BestParticle.OptimalSplitDistance,
                ShapeletsValues = pso.BestParticle.Position,
                BestInformationGain = pso.BestParticle.BestInformationGain,
            };

            //Define left and right class index of the shapelet 
            var dataSetClassA = _trainTimeSeries.Where(t => t.ClassIndex == classIndexA).ToArray();
            splitShapeletClasses(shapelet, classIndexA, dataSetClassA);
            var dataSetClassB = _trainTimeSeries.Where(t => t.ClassIndex == classIndexB).ToArray();
            splitShapeletClasses(shapelet, classIndexB, dataSetClassB);

            return shapelet; 
        }

        private static void splitShapeletClasses(Shapelet shapelet, int classIndex, TimeSeries[] dataSetTimeSeries)
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
    }
}
