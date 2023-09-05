using System;
using System.Collections.Generic;
using System.Linq;
using Utilities;

namespace Core
{
    //TODO: Delete this - it is only for TEST purposes 
    /*class CircularListIterator
    {
        private List<float> floatList = new List<float> { 0.25f, 0.71f, 0.22f, 0.12f, 0.43f, 0.03f};
        private int currentIndex = -1;

        public float NextDouble()
        {
            if (floatList.Count == 0)
            {
                throw new System.Exception("List is empty");
            }

            currentIndex = (currentIndex + 1) % floatList.Count;
            return floatList[currentIndex];
        }
    }
    */

    public class CandidateShapelet 
    {
        public double OptimalSplitDistance { get; set; }
        public int ParticleLength { get; set; }  // It does not coinside with Position.Length
        public double[] Position { get; set; }
        public double[] Velocity { get; set; }
        public double[] BestPosition { get; set; }
        public double BestInformationGain { get; set; }

        public CandidateShapelet(int particleLength)
        {
            InitParticle(particleLength); 
        }
        private void InitParticle(int particleLength)
        {
            OptimalSplitDistance = -1.0;
            ParticleLength = particleLength;
            Position = new double[particleLength];
            Velocity = new double[particleLength];
            BestPosition = new double[particleLength];
            BestInformationGain = double.MinValue;
        }

        public void Copy(CandidateShapelet particle)
        {
            OptimalSplitDistance = particle.OptimalSplitDistance;
            ParticleLength = particle.ParticleLength;
            particle.Position.CopyTo(Position, 0);
            particle.Velocity.CopyTo(Velocity, 0);
            BestPosition = particle.BestPosition;
            BestInformationGain = particle.BestInformationGain;
        }
    }

    public class  BasicPSO
    {
        // Swarm definition 
        public CandidateShapelet BestParticle;
        private readonly List<CandidateShapelet> _swarm = new List<CandidateShapelet>();
        private readonly Random _ran = new Random(); 
        // private readonly CircularListIterator _ran = new CircularListIterator(); // TEST ONLY!!! 
        private int _maxParticleLength;
        private int _minParticleLength;
        private int _step; 
        // PSO constants: http://msdn.microsoft.com/en-us/magazine/hh335067.aspx
        private const double W = 0.729; // inertia weight
        private const double C1 = 1.49445; // cognitive/local weight 
        private const double C2 = 1.49445; // social/global weight
        private double R1, R2; // cognitive and social randomizations
        // PSO variables
        // private int _maxIterations = 6;
        public const double ITERATION_EPSILON = 0.0000001;
        private readonly double _minPositionValue = 3.0;
        private readonly double _maxPositionValue = -3.0;
        private readonly double _minVelocity = 3.0;
        private readonly double _maxVelocity = -3.0;
        private TimeSeries[] _trainTimeSeries; 

        public BasicPSO(int minLength
                        , int maxLength
                        , int step
                        , double minValue
                        , double maxValue
                        , TimeSeries[] trainTimeSeries)
        {
            _maxParticleLength = maxLength;
            _minParticleLength = minLength;
            _maxPositionValue = maxValue;
            _maxVelocity = maxValue;
            _minPositionValue = minValue;
            _minVelocity = minValue; 
            _step = step;
            _minParticleLength = (minLength / _step) * _step;
            _trainTimeSeries = trainTimeSeries;

            BestParticle = new CandidateShapelet(maxLength);
        }

        private static void updateParticlesGain(CandidateShapelet candidateShapelet
                                                , double informationGain
                                                , double splitPoint)
        {
            candidateShapelet.OptimalSplitDistance = splitPoint;
            candidateShapelet.Position.CopyTo(candidateShapelet.BestPosition, 0);
            candidateShapelet.BestInformationGain = informationGain;
        }

        private void psoShapeletFitness(CandidateShapelet particle)
        {
            var distances = new List<HistogramItem>();

            foreach (var timeSeries in _trainTimeSeries)
            {
                var histogramItem = new HistogramItem
                {
                    ClassIndex = timeSeries.ClassIndex,
                    Distance = Utils.SubsequenceDist(timeSeries, particle.Position)
                };

                distances.Add(histogramItem);
            }

            var histogram = distances.OrderBy(v => v.Distance);
            double informationGain = 0.0;
            double splitPoint= 0.0;
            double entropy= 0.0;

            Utils.CalculateInformationGain(histogram, out informationGain, out splitPoint, out entropy);

            if (particle.BestInformationGain < informationGain)
            {
                updateParticlesGain(particle, informationGain, splitPoint);
            }
        }

        public void InitPSO()
        {
            var particleLength = _minParticleLength;

            while (particleLength <= _maxParticleLength)
            {
                var particle = new CandidateShapelet(particleLength);
                
                for (var j = 0; j < particleLength; j++)
                {
                    particle.Velocity[j] = (_maxVelocity - _minVelocity) * _ran.NextDouble() + _minVelocity;    
                }

                for (var j = 0; j < particleLength; j++)
                {
                    particle.Position[j] = (_maxPositionValue - _minPositionValue) * _ran.NextDouble() + _minPositionValue;
                }

                psoShapeletFitness(particle);
                
                _swarm.Add(particle);

                if (particle.BestInformationGain > BestParticle.BestInformationGain)
                {
                    BestParticle.Copy(particle);
                }

                particleLength += _step;    
                
            }
        }

        public void StartPSO()
        {
            var iteration = 0;
            var oldBestGain = 0.0;
            var newBestGain = 0.0;
            
            do
            {
                iteration++;
                
                foreach (var particle in _swarm)
                {
                    // Update velocities of the current particle 
                    for (var j = 0; j < particle.Velocity.Length; j++)
                    {
                        R1 = _ran.NextDouble();
                        R2 = _ran.NextDouble();

                        particle.Velocity[j] = W*particle.Velocity[j] +
                                               C1*R1*(particle.BestPosition[j] - particle.Position[j]) +
                                               C2*R2*(BestParticle.Position[j] - particle.Position[j]);
                    }

                    // Calculate new positions for current particle 
                    for (var j = 0; j < particle.Position.Length; j++)
                    {
                        particle.Position[j] += particle.Velocity[j];
                    }

                    // Calculate particle information gain 
                    psoShapeletFitness(particle);

                    // Update best particle 
                    if (particle.BestInformationGain > BestParticle.BestInformationGain)
                    {
                        BestParticle.Copy(particle);
                    }

                }  

                oldBestGain = newBestGain;
                newBestGain = BestParticle.BestInformationGain;  
                Console.WriteLine("Iteration: {0}", iteration);
                
            } while ( Math.Abs(oldBestGain - newBestGain) > ITERATION_EPSILON); 
    
            // Finalize best particle parameters 
            var position = BestParticle.Position;
            Array.Resize(ref position, BestParticle.ParticleLength);
            BestParticle.Position = position; 
        }
       
    } 
}
