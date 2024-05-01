using System;

namespace Utilities
{
    [Serializable]
    public class DecisionTree<T> where T : IComparable<T>
    {
        [Serializable]
        public class Node
        {
            public T Data
            {
                get; private set;
            }

            public Node Left
            {
                get; set;
            }

            public Node Right
            {
                get; set;
            }

            public int Depth
            {
                get; set;
            }
            
            public Node (T item)
            {
                Data = item;
                Depth = 0; 
                Left = null;
                Right = null; 
            }

            public override string ToString()
            {
                var propertyRepresentation = ""; 
                var propertiesInfo = Data.GetType().GetProperties();
                foreach (var propertyInfo in propertiesInfo)
                {
                    propertyRepresentation += String.Format("{0} : {1} ; ", 
                                        propertyInfo.Name, propertyInfo.GetValue(Data, new object[]{})); 
                }

                if ((Left == null) && (Right == null))
                {
                    return String.Format("LEAF: Depth: {0}, {1}\n", Depth, propertyRepresentation); 
                }
                return String.Format("Depth: {0}, {1}\n", Depth, propertyRepresentation); 
            }
        }

        public Node Root
        {
            get; set;
        }

        public double Accuracy
        {
            get; set;
        }

        public DecisionTree()
        {
            Root = null; 
        }

        public bool Add(Node node, T item)
        {
            var result = false; 
            if (item == null)
            {
                return false; 
            }

            if (node == null)
            {
                return false; 
            }

            if (node.Data.CompareTo(item) == 0)
            {
                return true; 
            }

            if (node.Data.CompareTo(item) == -1)
            {
                if (node.Left == null)
                {
                    node.Left = new Node(item) { Depth = node.Depth + 1 };
                }

                return Add(node.Left, item);
            }

            if (node.Data.CompareTo(item) == 1)
            {
                if (node.Right == null)
                {
                    node.Right = new Node(item) { Depth = node.Depth + 1 };
                }

                return Add(node.Right, item);
            }

            if (node.Data.CompareTo(item) == -2)
            {
                result = Add(node.Left, item);
                if (!result)
                {
                    result = Add(node.Right, item);
                }
            }

            return result; 
        }
    }
}
