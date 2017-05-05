using System.Collections.Generic;

namespace CityGen.Struct
{
    public class Block
    {
        private List<Road> edges = new List<Road>();

        internal void addEdges(Road road)
        {
            edges.Add(road);
        }

        internal bool isClosure()
        {
            return edges.Count > 2 ?
                edges[0].start.Equals(edges[edges.Count - 1].end) : false;
        }
    }
}