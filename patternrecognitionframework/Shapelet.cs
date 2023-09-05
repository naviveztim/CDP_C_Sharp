using System;

namespace Core
{
    [Serializable]
    public class Shapelet : IComparable<Shapelet>
    {
        public Shapelet()
        {
            ShapeletsValues = null;
            OptimalSplitDistance = -1.0;
            BestInformationGain = 0.0;
            LeftClassIndex = int.MinValue;
            RightClassIndex = int.MinValue; 
        }
        
        public double[] ShapeletsValues
        {
            get; protected internal set;
        }
        
        public double OptimalSplitDistance
        {
            get; protected internal set;
        }

        public double BestInformationGain
        {
            get; protected internal set; 
        }

        public int LeftClassIndex
        {
            get; set;
        }
        
        public int RightClassIndex
        {
            get; set;
        }

        public int CompareTo(Shapelet other)
        {
            if (Equals(other))
            {
                return 0;
            }

            if ((LeftClassIndex == other.LeftClassIndex) ||
                (LeftClassIndex == other.RightClassIndex))
            {
                return -1; 
            }

            if ((RightClassIndex == other.LeftClassIndex) ||
                (RightClassIndex == other.RightClassIndex))
            {
                return 1;
            }

            return -2; // This item should note be put into the tree 
        }
    }
}
