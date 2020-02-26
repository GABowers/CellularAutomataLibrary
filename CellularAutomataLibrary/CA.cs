using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CellularAutomataLibrary
{
    public class CA
    {
        private CAGraph GraphCopy { get; set; }
        public CAGraph Graph { get; private set; }
        List<CARule> Rules { get; set; }
        public int Iteration { get; private set; }
        public List<int> CurrentCounts { get; private set; }
        private ConcurrentQueue<double> RandomQueue { get; set; }
        private Task RandomBuilder { get; set; }
        private Task StoreDataTask { get; set; }
        public CASettings Settings { get; set; }
        private CARecord Record { get; set; }
        private ConcurrentQueue<Dictionary<(CAEntityType, string, dynamic), int>> CountQueue { get; set; }
        private ConcurrentQueue<Dictionary<(CAEntityType, string,dynamic, dynamic), int>> TransitionQueue { get; set; }
        private List<CAExitCondition> Conditions { get; set; }
        public bool Exit { get; private set; }
        public Dictionary<(CAEntityType, string, dynamic, dynamic), int> Transitions { get; private set; }
        //System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
        //List<CAProperty> Properties { get; set; }

        public CA(ValueTuple<ushort, ushort, ushort> dimensions, GridShape shape)
        {
            Exit = false;
            Rules = new List<CARule>();
            //Properties = new List<CAProperty>();
            Iteration = 0;
            Settings = new CASettings();
            Record = new CARecord();
            Conditions = new List<CAExitCondition>();
            CountQueue = new ConcurrentQueue<Dictionary<(CAEntityType, string, dynamic), int>>();
            TransitionQueue = new ConcurrentQueue<Dictionary<(CAEntityType, string, dynamic, dynamic), int>>();
            RandomQueue = new ConcurrentQueue<double>();
            RandomBuilder = Task.Run(() =>
            {
                while(true) // check for cancellation token?
                {
                    while(RandomQueue.Count < 100) // arbitrary number...
                    {
                        RandomQueue.Enqueue(StaticMethods.GetRandomNumber());
                    }
                }
            });
            // use actionblocks?
            StoreDataTask = Task.Run(() =>
            {
                while (true) // check for cancellation token?
                {
                    while (TransitionQueue.TryDequeue(out var transCount))
                    {
                        Record.AddTransCount(transCount);
                    }

                    while (CountQueue.TryDequeue(out var count))
                    {
                        Record.AddCount(count);
                    }

                    //while (CountQueue.TryDequeue(out var count))
                    //{
                    //    int iteration = count.Item1;
                    //    List<ValueTuple<string, CAEntityType>> counts = new List<(string, CAEntityType)>();
                    //    ConcurrentBag<(CAEntityType, List<(string, dynamic)>)> data = count.Item2;
                    //    foreach (var item in data)
                    //    {
                    //        var entityType = item.Item1;
                    //        if (item.Item2 != null)
                    //        {
                    //            foreach (var item2 in item.Item2)
                    //            {
                    //                string name = item2.Item1;
                    //                if (name.Equals("state"))
                    //                {
                    //                    name += "-" + item2.Item2;
                    //                }
                    //                counts.Add((name, entityType));
                    //            }
                    //        }
                    //    }
                    //    foreach (var item in counts)
                    //    {
                    //        Record.AddCount(iteration, item);
                    //    }
                    //}
                }
            });
            Graph = new CAGraph(this, dimensions, shape);
            Transitions = new Dictionary<(CAEntityType, string, dynamic, dynamic), int>();
        }

        public double GetRandomDouble()
        {
            double random = 0;
            while(!RandomQueue.TryDequeue(out random))
            {

            }
            return random;
        }

        public void AddAgents(List<ValueTuple<Dictionary<string, dynamic>, ValueTuple<ushort, ushort, ushort>>> agentData)
        {
            foreach (ValueTuple<Dictionary<string, dynamic>, ValueTuple<ushort, ushort, ushort>> agent in agentData)
            {
                var location = agent.Item2;
                var cell = Graph.GetCell(location);
                if (cell != null)
                {
                    cell.AddAgent(agent.Item1);
                }
            }
        }

        /// <summary>
        /// Add agents to the grid.
        /// </summary>
        /// <param name="amount">The total number of agents to add. If this is lesser than or equal to 1, it will be treated as a percentage. If above 1, the total will be added to the graph (up to the graph's max)</param>
        /// <param name="state">The state of the cells to be added.</param>
        /// <param name="randomized">Should the positions be randomized? If not, they will be added incrementally from the lowest index with no existing agents.</param>
        public void AddAgents(double amount, ushort state, bool randomized)
        {
            int total = Graph.Dimensions.Item1 * Graph.Dimensions.Item2 * Graph.Dimensions.Item3;
            int use = 0;
            if (amount <=1)
            {
                use = Math.Min(total, (int)Math.Floor(total * amount));
            }
            else
            {
                use = Math.Min(total, (int)Math.Floor(amount));
            }
            List<ValueTuple<ushort, ushort, ushort>> positions = new List<(ushort, ushort, ushort)>();
            for (ushort i = 0; i < Graph.Dimensions.Item1; i++)
            {
                for (ushort j = 0; j < Graph.Dimensions.Item2; j++)
                {
                    for (ushort k = 0; k < Graph.Dimensions.Item3; k++)
                    {
                        if(Graph.Cells[i, j, k].ContainsAgent() == false)
                        {
                            positions.Add((i, j, k));
                        }
                    }
                }
            }

            List<int> picks = Enumerable.Range(0, positions.Count).ToList();
            picks.Shuffle();
            List<ValueTuple<ushort, ushort, ushort>> positionsToUse;
            if (randomized)
            {
                positionsToUse = picks.GetRange(0, use).Select(x => positions[x]).ToList();
            }
            else
            {
                positionsToUse = positions.GetRange(0, use);
            }
            Dictionary<string, dynamic> properties = new Dictionary<string, dynamic> { { "state", state } };
            List<ValueTuple<Dictionary<string, dynamic>, ValueTuple<ushort, ushort, ushort>>> agentData = positionsToUse.Select(x => (properties, x)).ToList();
            AddAgents(agentData);
        }

        public void AddRule(CARule rule)
        {
            Rules.Add(rule);
        }

        public CAGraph GetGraphCopy()
        {
            return GraphCopy;
        }

        public void Run()
        {
            if (Settings.StoreTransitions)
            {
                var keys = Transitions.Keys.ToList();
                for (int i = 0; i < keys.Count; i++)
                {
                    Transitions[keys[i]] = 0;
                }
            }
            if (Settings.CopyFormat == CACopyFormat.Reference)
            {
                GraphCopy = Graph;
            }
            else if (Settings.CopyFormat == CACopyFormat.DeepCopy)
            {
                GraphCopy = Graph.Copy(this);
            }
            Graph.Run(Rules);
            ConcurrentBag<ValueTuple<CAEntityType, List<(string, dynamic)>>> counts = Graph.GetCounts();
            this.CurrentCounts = Enumerable.Repeat(0, Settings.States).ToList();
            foreach (var item in counts)
            {
                var entityType = item.Item1;
                if(entityType == CAEntityType.Agent)
                {
                    foreach (var item2 in item.Item2)
                    {
                        string name = item2.Item1;
                        ushort state = item2.Item2;
                        if (name.Equals("state"))
                        {
                            while (this.CurrentCounts.Count < (state + 1))
                            {
                                this.CurrentCounts.Add(0);
                            }
                            this.CurrentCounts[state] += 1;
                        }
                    }
                }
            }
            if (Settings.StoreCounts)
            {
                Dictionary<(CAEntityType, string, dynamic), int> countDict = new Dictionary<(CAEntityType, string, dynamic), int>();
                foreach (var entity in counts)
                {
                    foreach (var property in entity.Item2)
                    {
                        var key = (entity.Item1, property.Item1, property.Item2);
                        if(countDict.ContainsKey(key))
                        {
                            countDict[key] += 1;
                        }
                        else
                        {
                            countDict.Add(key, 1);
                        }
                    }
                }
                CountQueue.Enqueue(countDict);
            }
            if(Settings.StoreTransitions)
            {
                TransitionQueue.Enqueue(new Dictionary<(CAEntityType, string, dynamic, dynamic), int>(this.Transitions));
            }
            for (int i = 0; i < Conditions.Count; i++)
            {
                if(Conditions[i] is CAExitConditionIteration caeci)
                {
                    if (Iteration >= caeci.Iteration)
                    {
                        this.Exit = true;
                        break;
                    }
                }
                else if (Conditions[i] is CAExitConditionCount caecc)
                {
                    for (ushort j = 0; j < CurrentCounts.Count; j++)
                    {
                        if(caecc.Count.Item2 == j)
                        {
                            if (StaticMethods.CheckEquality(this.CurrentCounts[j], caecc.Count.Item3, caecc.Count.Item1))
                            {
                                this.Exit = true;
                                break;
                            }
                        }
                    }
                }
                else if (Conditions[i] is CAExitConditionConvergeance caecco)
                {
                    double length = caecco.ConvergeanceLength;
                    int iteration = 0;
                    if(length < 1)
                    {
                        iteration = Math.Max(caecco.ConvergeanceDelay, (int)(length * Iteration));
                    }
                    else
                    {
                        iteration = Math.Max(caecco.ConvergeanceDelay, (int)length);
                    }
                    caecco.Counts.Add(this.CurrentCounts.Select(x => x).ToList());
                    while(caecco.Counts.Count > (iteration + 1))
                    {
                        caecco.Counts.RemoveAt(0); // we're checdking the last X values--we want to get rid of any before that time point that we don't need.
                    }
                    List<int> lows = Enumerable.Repeat(0, Settings.States).ToList();
                    for (int j = (caecco.Counts.Count - 1); j > 0; j--)
                    {
                        if(!this.Exit)
                        {
                            for (int k = 0; k < caecco.Counts[j].Count; k++)
                            {
                                if(!this.Exit)
                                {
                                    if (lows[k] >= iteration)
                                    {
                                        this.Exit = true;
                                        break;
                                    }
                                    int cur = caecco.Counts[j][k];
                                    int prev = caecco.Counts[j - 1][k];
                                    int diff = Math.Abs(prev - cur);
                                    if (caecco.ConvergeanceValue > 1) // this means it's actually counting cells
                                    {
                                        if (diff < ((int)caecco.ConvergeanceValue))
                                        {
                                            lows[k] += 1;
                                        }
                                    }
                                    else // this means it's a proportionality
                                    {
                                        double prop = (double)diff / this.CurrentCounts.Sum();
                                        if (prop > 1)
                                        {
                                            prop = prop - 1;
                                        }
                                        if (prop <= caecco.ConvergeanceValue)
                                        {
                                            lows[k] += 1;
                                        }
                                    }
                                }
                            }
                        }
                    }
                    for (int j = 0; j < lows.Count; j++)
                    {
                        if (lows[j] >= iteration)
                        {
                            this.Exit = true;
                            break;
                        }
                    }
                }
            }
            Iteration += 1;
        }

        //public void AddChange(ValueTuple<CAEntityType, string> input)
        //{
        //    //ChangeCountQueue.Enqueue((this.Iteration, input.Item1, input.Item2));
        //}

        public void AddTransition((CAEntityType, string, dynamic, dynamic) key)
        {
            if(this.Transitions.ContainsKey(key))
            {
                this.Transitions[key] += 1;
            }
            else
            {
                this.Transitions.Add(key, 1);
            }
        }

        public void Save(List<List<string>> mainHeader, (List<(CAEntityType, string, dynamic)>, List<(CAEntityType, string, dynamic, dynamic)>) toSave, string path)
        {
            while (TransitionQueue.Count > 0)
            {
                ;
            }
            while (CountQueue.Count > 0)
            {
                ;
            }
            Record.Save(mainHeader, Iteration, toSave, path);
        }

        public (List<(CAEntityType, string, dynamic)>, List<(CAEntityType, string, dynamic, dynamic)>) ListSaveProperties()
        {
            return (Record.GetCountProperties(), Record.GetTransitionProperties());
        }

        public void CreateExitCondition(CAExitCondition condition)
        {
            Conditions.Add(condition);
        }
    }
}
