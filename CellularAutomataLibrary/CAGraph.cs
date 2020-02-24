using System;
using System.Collections.Concurrent;
//using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CellularAutomataLibrary
{
    public class CAGraph:CAEntity
    {
        public ValueTuple<ushort, ushort, ushort> Dimensions { get; }
        public CAGraphCell[,,] Cells { get;}
        public List<ValueTuple<ushort, ushort, ushort>> AgentCells { get; private set; }
        List<ValueTuple<ushort, ushort, ushort>> Updates { get; set; }
        public CA Parent { get; private set; }
        public GridShape Shape { get; }
        //List<double> times = new List<double>();
        //System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();

        public CAGraph (CA parent, ValueTuple<ushort, ushort, ushort> size, GridShape shape):base(CAEntityType.Graph)
        {
            this.Parent = parent;
            this.Shape = shape;
            AgentCells = new List<ValueTuple<ushort, ushort, ushort>>();
            Updates = new List<ValueTuple<ushort, ushort, ushort>>();
            Dimensions = new ValueTuple<ushort, ushort, ushort>(size.Item1, size.Item2, size.Item3);
            Cells = new CAGraphCell[size.Item1, size.Item2, size.Item3];
            for (ushort i = 0; i < Cells.GetLength(0); i++)
            {
                for (ushort j = 0; j < Cells.GetLength(1); j++)
                {
                    for (ushort k = 0; k < Cells.GetLength(2); k++)
                    {
                        Cells[i, j, k] = new CAGraphCell(this, new ValueTuple<ushort, ushort, ushort>(i, j, k));
                    }
                }
            }
        }

        public CAGraph(CA parent, ValueTuple<ushort, ushort, ushort> size, GridShape shape, List<ValueTuple<ushort, ushort, ushort>> agents, CAGraphCell[,,] cells, Dictionary<string, dynamic> properties) : base(CAEntityType.Graph, properties)
        {
            Parent = parent;
            Shape = shape;
            AgentCells = agents.ConvertAll(x => new ValueTuple<ushort, ushort, ushort>(x.Item1, x.Item2, x.Item3));
            Updates = new List<ValueTuple<ushort, ushort, ushort>>();
            Dimensions = new ValueTuple<ushort, ushort, ushort>(size.Item1, size.Item2, size.Item3);
            Cells = new CAGraphCell[size.Item1, size.Item2, size.Item3];
            for (ushort i = 0; i < size.Item1; i++)
            {
                for (ushort j = 0; j < size.Item2; j++)
                {
                    for (ushort k = 0; k < size.Item3; k++)
                    {
                        //if(i == 500 && j == 500)
                        //{
                        //    Console.WriteLine();
                        //}
                        Cells[i, j, k] = cells[i, j, k].Copy(this);
                    }
                }
            }
        }


        public void AddCell(ValueTuple<ushort, ushort, ushort> location, CAGraphCell cell)
        {
            var thisCell = GetCell(location);
            if (Cells.GetLength(0) >= location.Item1 && Cells.GetLength(1) >= location.Item2 && Cells.GetLength(2) >= location.Item3)
            {
                Cells[location.Item1, location.Item2, location.Item3] = cell;
            }
        }

        public CAGraph Copy(CA ca)
        {
            var graph = new CAGraph(ca, this.Dimensions, this.Shape, this.AgentCells, this.Cells, this.Properties);
            return graph;
        }

        public CAGraphCell GetCell(ValueTuple<ushort, ushort, ushort> location)
        {
            if (Cells != null)
            {
                // switch based on position type
                return Cells[location.Item1, location.Item2, location.Item3];
            }
            else
            {
                // throw error?
                return null;
            }
        }

        public void AddAgentCell(ValueTuple<ushort, ushort, ushort> position)
        {
            AgentCells.Add(position);
        }

        public void RemoveAgentCell(ValueTuple<ushort, ushort, ushort> position)
        {
            AgentCells.Remove(position);
        }

        //public void AddForUpdate(ValueTuple<ushort, ushort, ushort> location)
        //{
        //    Updates.Add(location);
        //}

        public CAGraph GetCopy()
        {
            return this.Parent.GetGraphCopy();
        }

        public void Run(List<CARule> rules)
        {
            this.AgentCells = AgentCells.Distinct().ToList();
            // act on self: before or after?
            if (Parent.Settings.Subprocessing)
            {
                // NOTE: this caused an issue. We were copying the agentcells, thus we weren't making changes to the original version.
                foreach (var item in this.AgentCells.ToArray())
                {
                    this.GetCell(item).Run(rules);
                }
            }
            else
            {
                // Parallel???
                for (int i = 0; i < Cells.GetLength(0); i++)
                {
                    for (int j = 0; j < Cells.GetLength(1); j++)
                    {
                        for (int k = 0; k < Cells.GetLength(2); k++)
                        {
                            Cells[i, j, k].Run(rules);
                        }
                    }
                }
            }
            // for self running: USE GET COPY?????
        }

        public ConcurrentBag<ValueTuple<CAEntityType, List<(string, dynamic)>>> GetCounts()
        {
            var local = this.GetProperties();
            ConcurrentBag<ValueTuple<CAEntityType, List<(string, dynamic)>>> output = new ConcurrentBag<(CAEntityType, List<(string, dynamic)>)>();
            if(local.Item2 != null)
            {
                var localList = local.Item2.Select(x => (x.Key, x.Value)).ToList();
                output.Add((local.Item1, localList));
            }
            foreach (var cellLocation in this.AgentCells.ToArray())
            {
                var cellInfo = Cells[cellLocation.Item1, cellLocation.Item2, cellLocation.Item3].GetCounts();
                foreach (var item in cellInfo)
                {
                    if(item.Item2 != null)
                    {
                        output.Add((item.Item1, item.Item2.Select(x => (x.Key, x.Value)).ToList()));
                    }
                }
            }
            return output;
        }
    }
}
