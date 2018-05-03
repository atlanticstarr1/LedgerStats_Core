using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace LedgerStats
{
    public class Program
    {
        public class Digraph
        {
            private readonly Int32 _v; //The number of vertices
            private Int32 _e; //The number of edges
            private LinkedList<Int32>[] adj; //Use a LinkedList for the adjacency-list representation

            //Create a new directed graph with V vertices
            public Digraph(Int32 V)
            {
                if (V < 0) throw new Exception("Number of vertices in a Digraph must be nonnegative");
                this._v = V;
                this._e = 0;
                //Create a new adjecency-list for each vertex
                adj = new LinkedList<Int32>[V];
                for (Int32 v = 0; v < V; v++)
                {
                    adj[v] = new LinkedList<Int32>();
                }
            }

            //return the number of vertices.Because we are starting at the first Node (ID=1), indstead ID=0;
            public Int32 V()
            {
                return _v - 1;
            }

            //return the number of edges
            public Int32 E()
            {
                return _e;
            }

            //Add an edge to the directed graph from v to w
            public void AddEdge(Int32 v, Int32 w, Int32 x, Int32 y)
            {
                if (v < 0 || v >= _v) throw new Exception("vertex " + v + " is not between 0 and " + V());
                if (w < 0 || w >= _v) throw new Exception("vertex " + w + " is not between 0 and " + V());
                if (x < 0 || x >= _v) throw new Exception("vertex " + x + " is not between 0 and " + V());
                adj[v].AddFirst(w);
                _e++;
                adj[v].AddLast(x);
                _e++;
                adj[v].AddLast(y);
            }

            /*
                * Return the adjacency-list for vertex v, which
                * are the vertices connected to v pointing from v
                * */
            public IEnumerable<Int32> Adj(Int32 v)
            {
                if (v < 0 || v >= _v) throw new Exception();
                return adj[v];
            }

            //Return the directed graph as a string
            public String toString()
            {
                StringBuilder s = new StringBuilder();
                String NEWLINE = Environment.NewLine;
                s.Append(V() + " vertices, " + _e + " edges " + NEWLINE);
                for (int v = 0; v < _v; v++)
                {
                    s.Append(String.Format("{0:d}: ", v));
                    foreach (int w in adj[v])
                    {
                        s.Append(String.Format("{0:d} ", w));
                    }
                    s.Append(NEWLINE);
                }
                return s.ToString();
            }

            //Print stats.
            public String PrintStats()
            {
                var s = new StringBuilder();
                var NEWLINE = Environment.NewLine;

                var avgDagDepth = GetAvgDagDepth();
                s.Append(String.Format("AVG DAG DEPTH: {0:0.00}", avgDagDepth)).Append(NEWLINE);
                s.Append(String.Format("AVG TXS PER DEPTH: {0:0.00}", GetAvgTxnPerDepth())).Append(NEWLINE);
                s.Append(String.Format("AVG REF: {0:0.000}", GetAvgRef())).Append(NEWLINE);
                s.Append(String.Format("TRANSACTION RATE (): {0:0.00}", GetRateOfIncomingTxns())).Append(NEWLINE);
                s.Append(String.Format("TRANSACTION DELAY (h): {0:0.0}", GetDelayBetweenTransactions())).Append(NEWLINE);

                return s.ToString();
            }

            // AVG DAG DEPTH
            public double GetAvgDagDepth()
            {
                var depth = 0;  // store sum of min depths.
                for (int v = 0; v < _v; v++)
                {
                    depth += minDepth(adj[v]);
                }
                return depth * 1.0 / V();   // divide sum of min depths by total nodes.
            }

            // AVG TXS PER DEPTH
            public double GetAvgTxnPerDepth()
            {
                // Depth 0 (ID=1) is excluded. Assuming last txn is farthest from origin.
                var depth = minDepth(adj[V()]);
                return (V() - 1 * 1.0) / depth;
            }

            // AVG REF
            public double GetAvgRef()
            {
                return (_e * 1.0 / V());
            }

            // TRANSACTION RATE
            public double GetRateOfIncomingTxns()
            {
                var totalTimeUnits = 0;
                double rate = 0.00;
                for (int v = 0; v < _v; v++)
                {
                    if (adj[v].Count > 0)
                        totalTimeUnits += adj[v].Last.Value;
                }

                if (totalTimeUnits != 0)
                    rate = (V() - 1) * 1.0 / totalTimeUnits;
                return rate;
            }

            // TRANSACTION RATE
            public double GetDelayBetweenTransactions()
            {
                var delay = 0.0;
                var lastTime = adj[V()].Last.Value; // last txn.
                var secondToLastTime = adj[V() - 1].Last.Value; //second to last txn.
                delay = lastTime - secondToLastTime;
                return delay;
            }

            // Get sum of shortest paths to ID=1 (Root)
            private int ShortestPathToOrigin(LinkedList<int> node)
            {
                // edge case
                if (node.Count == 0)
                {
                    return 0;
                }

                var neighbor1 = node.First.Value; //trunk
                var neighbor2 = node.First.Next.Value;    //branch

                if (neighbor1 == 1 || neighbor2 == 1)
                {
                    return 1;
                }
                return Math.Min(ShortestPathToOrigin(adj[neighbor1]), ShortestPathToOrigin(adj[neighbor2])) + 1;
            }

            private int MinDepthRecursive(LinkedList<int> node)
            {
                // edge case
                if (node.Count == 0)
                {
                    return 0;
                }
                var neighbor1 = node.First.Value; //trunk
                var neighbor2 = node.First.Next.Value;    //branch

                var ldepth = minDepth(adj[neighbor1]);
                var rdepth = minDepth(adj[neighbor2]);

                // use the minimum one. Assuming every hop has a weight = 1
                if (ldepth < rdepth)
                    return (ldepth + 1);
                else return (rdepth + 1);
            }

            private int minDepth(LinkedList<int> node)
            {
                var branchNode = node;  //save copy for right parent traversal
                var ldepth = 0;
                var rdepth = 0;
                // trunk node (left parent traversal)
                while (node.Count != 0)
                {
                    var neighbor1 = node.First.Value; //trunk
                    if (neighbor1 == 1)
                    {
                        ldepth++;
                        break;
                    }
                    else
                    {
                        ldepth++;
                        node = adj[neighbor1];
                    }
                }
                // branch node
                while (branchNode.Count != 0)
                {
                    var neighbor2 = branchNode.First.Next.Value; //branch
                    if (neighbor2 == 1)
                    {
                        rdepth++;
                        break;
                    }
                    else
                    {
                        rdepth++;
                        branchNode = adj[neighbor2];
                    }
                }

                if (ldepth <= rdepth)
                    return ldepth;
                else
                    return rdepth;
            }

            private int maxDepth(LinkedList<int> node)
            {
                // edge case
                if (node.Count == 0)
                {
                    return 0;
                }
                var neighbor1 = node.First.Value;
                var neighbor2 = node.First.Next.Value;

                var ldepth = maxDepth(adj[neighbor1]);
                var rdepth = maxDepth(adj[neighbor2]);

                // use the max one.  Assuming every hop has a weight = 1
                if (ldepth > rdepth)
                    return (ldepth + 1);
                else return (rdepth + 1);
            }
        }

        static void Main(string[] args)
        {
            //if (args.Length == 0)
            //{
            //    Console.WriteLine("No file name specified");
            //    Console.ReadLine();
            //    return;
            //}
            var N = 0;  // total nodes
            var line = 2; // start of node data
            string path = @"database1.txt";
            //string path = args[0];
            string[] lines = File.ReadAllLines(path);
            if (lines.Length == 0)
            {
                Console.WriteLine("File is empty.");
                Console.ReadLine();
                return;
            }
            N = int.Parse(lines[0]); // get total nodes
            var graph = new Digraph(N + 2); // create DAG

            foreach (string s in lines.Skip(1))
            {
                string[] numbers = s.Split(' ');
                var lparent = int.Parse(numbers[0]);
                var rparent = int.Parse(numbers[1]);
                var timestamp = int.Parse(numbers[2]);
                graph.AddEdge(line, lparent, rparent, timestamp);
                line++;
            }

            Console.WriteLine(graph.toString());
            Console.WriteLine(graph.PrintStats());
            Console.ReadLine();
        }
    }

}
