using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.Intrinsics;
using System.Text;
using System.Threading.Tasks;

namespace DTopology
{
    public class Cluster
    {
        public List<string[]> VectorList { get; set; }

        public Cluster()
        {
            VectorList = new List<string[]>();
        }

        public void Add(string[] vector)
        {
            VectorList.Add(vector);
        }

        public int Count
        {
            get { return VectorList.Count; }
        }
    }
}
