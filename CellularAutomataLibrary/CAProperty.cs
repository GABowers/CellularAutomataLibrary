using System;
using System.Collections.Concurrent;
//using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CellularAutomataLibrary
{
    //public struct CAProperty
    //{
    //    public readonly string name;
    //    public readonly dynamic value;
    //    //public readonly Type type;
    //    public CAProperty(string name, dynamic value)
    //    {
    //        this.name = name;
    //        this.value = value;
    //    }
    //}

    public enum CAScale
    {
        Local = 0, // this refers to properties of the agent itself.
        Regional = 1, // this refers to the agent's neighborhood. Neighbor type defined elsewhere
        Global = 2 // this refers to the "grid" itself.
    }

    public enum CAEquality
    {
        Less_than = 0,
        Less_than_or_equal_to = 1,
        Equal_to = 2,
        Greater_than_or_equal_to = 3,
        Greater_than = 4
    }

    public enum CAOperator
    {
        Equal = 0,
        Addition = 1,
        Subtraction = 2,
        Division = 3,
        Multiplication = 4,
        Power = 5,
        Logarithm = 6,
    }

    public enum CANeighborhoodType
    {
        None = 0,
        Edge = 1, // +1 or -1 on any ONE axis (at a time) of the graph.
        Vertex = 2, // +1 or -1 - every combination.
        //Custom_Cardinal = 3, // +? or -? on any one axis (at a time).
        //Custom_nrgular = 4, // +? or -? - every combination.
        Advanced = 3, // completely user defined. Might skip cells in-between the active one and a neighbor.
    }

    public enum CACopyFormat
    {
        Reference = 0,
        //ShallowCopy = 1,
        DeepCopy = 2,
    }
    
    public enum CAExitConditionType
    {
        Iteration = 0, // some number of iterations
        Count = 1, // count of a given state
        Convergeance = 2, // when cells have chenged by less than X%, for Y% of total iterations
    }

    public abstract class CAExitCondition
    {
        public CAExitConditionType Condition { get; set; }
        public CAExitCondition(CAExitConditionType type)
        {
            this.Condition = type;
        }
    }

    public class CAExitConditionIteration:CAExitCondition
    {
        public int Iteration { get; set; }
        public CAExitConditionIteration(int iteration):base(CAExitConditionType.Iteration)
        {
            this.Iteration = iteration;
        }
    }

    public class CAExitConditionCount: CAExitCondition
    {
        public ValueTuple<int, ushort, CAEquality> Count { get; set; }
        public CAExitConditionCount(ValueTuple<int, ushort, CAEquality> count) :base(CAExitConditionType.Count)
        {
            this.Count = count;
        }
    }

    public class CAExitConditionConvergeance:CAExitCondition
    {
        public int ConvergeanceDelay { get; private set; } // recommended iteration delay before doing comparison
        public double ConvergeanceValue { get; private set; }
        public double ConvergeanceLength { get; private set; }
        public List<List<int>> Counts { get; set; }
        public CAExitConditionConvergeance(int delay, double value, double length):base(CAExitConditionType.Convergeance)
        {
            this.ConvergeanceDelay = delay;
            this.ConvergeanceValue = value;
            this.ConvergeanceLength = length;
            this.Counts = new List<List<int>>();
        }
    }

    public class CARecord
    {
        public List<Dictionary<(CAEntityType, string, dynamic),int>> PropertyCount { get; private set; }
        public List<Dictionary<(CAEntityType, string, dynamic, dynamic), int>> TransitionCount { get; private set; } // list for iterations, the list for each type of change and its count (and the entity)
        public CARecord()
        {
            PropertyCount = new List<Dictionary<(CAEntityType, string, dynamic), int>>();
            TransitionCount = new List<Dictionary<(CAEntityType, string, dynamic, dynamic), int>>();
        }

        public void AddCount(Dictionary<(CAEntityType, string, dynamic), int> input)
        {
            this.PropertyCount.Add(input);
        }

        public void AddTransCount (Dictionary<(CAEntityType, string, dynamic, dynamic), int> input)
        {
            this.TransitionCount.Add(input);
        }

        public void Save(List<List<string>> mainHeader, int iterations, (List<(CAEntityType, string, dynamic)>, List<(CAEntityType, string, dynamic, dynamic)>) toSave, string filename)
        {
            List<(CAEntityType, string, dynamic)> countProperties = GetCountProperties().Where(x => toSave.Item1.Contains(x)).ToList();
            countProperties.Sort();
            List<(CAEntityType, string, dynamic, dynamic)> changeProperties = GetTransitionProperties().Where(x => toSave.Item2.Contains(x)).ToList();
            changeProperties.Sort();

            List<List<string>> output = new List<List<string>>();
            output.AddRange(mainHeader);
            output.Add(new List<string>());
            // intro info?
            List<string> header = new List<string> { "Iteration" };
            header.AddRange(countProperties.Select(x => x.Item1 + " " + x.Item2 + " | " + (string)Convert.ToString(x.Item3)));
            header.AddRange(changeProperties.Select(x => (x.Item1.ToString() + " " + (x.Item2) + " | " + (string)Convert.ToString(x.Item3) + " -> " + (string)Convert.ToString(x.Item4))));
            output.Add(header);
            for (int i = 0; i < iterations; i++)
            {
                List<string> curLine = new List<string>() { i.ToString() };
                if(PropertyCount.Count > i)
                {
                    foreach (var item in countProperties)
                    {
                        if (PropertyCount[i].ContainsKey(item))
                        {
                            curLine.Add(PropertyCount[i][item].ToString());
                        }
                        else
                        {
                            curLine.Add("X");
                        }
                    }
                }

                if (TransitionCount.Count > i)
                {
                    foreach (var item in changeProperties)
                    {
                        if (TransitionCount[i].ContainsKey(item))
                        {
                            curLine.Add(TransitionCount[i][item].ToString());
                        }
                        else
                        {
                            curLine.Add("0");
                        }
                    }
                }
                //go through find Xs. If iteration > 0 go back, else use 0
                for (int j = 0; j < curLine.Count; j++)
                {
                    if (i == 0)
                    {
                        if (curLine[j].Equals("X"))
                        {
                            curLine[j] = 0.ToString();
                        }
                    }
                    else
                    {
                        if (curLine[j].Equals("X"))
                        {
                            curLine[j] = output.Last()[j];
                        }
                    }
                }
                output.Add(curLine);
            }
            List<string> final = output.Select(x => String.Join(", ", x)).ToList();
            using (System.IO.StreamWriter sw = new System.IO.StreamWriter(filename))
            {
                foreach (var line in final)
                {
                    sw.WriteLine(line);
                }
            }
        }

        public List<(CAEntityType, string, dynamic)> GetCountProperties()
        {
            List<(CAEntityType, string, dynamic)> output = new List<(CAEntityType, string, dynamic)>();
            foreach (var item in PropertyCount)
            {
                output.AddRange(item.Keys);
            }
            return output.Distinct().ToList();
        }

        public List<(CAEntityType, string, dynamic, dynamic)> GetTransitionProperties()
        {
            List<(CAEntityType, string, dynamic, dynamic)> output = new List<(CAEntityType, string, dynamic, dynamic)>();
            foreach (var item in TransitionCount)
            {
                output.AddRange(item.Keys);
            }
            return output.Distinct().ToList();
        }
    }

    public struct CAOperation
    {
        readonly List<ValueTuple<CATarget, string, CAOperator>> operations;

        public dynamic Operate(CAEntity entity, dynamic entityValue)
        {
            dynamic answer = 0;
            for (int i = 0; i < operations.Count; i++)
            {
                var operation = operations[i];
                var targets = operation.Item1.GetTargets(entity);
                for (int j = 0; j < targets.Count; j++)
                {
                    var target = targets[j];
                    //if (target.HasProperty(operation.Item2))
                    //{
                        var property = target.GetProperty(operation.Item2);
                        var result = StaticMethods.Operate(answer, operation.Item3, property);
                        answer = result;
                    //}
                }
            }
            return answer;
        }
    }

    public struct CANeighborhood
    {
        public readonly CANeighborhoodType type;
        public readonly List<ValueTuple<int, int, int>> Offsets;
        public CANeighborhood(CANeighborhoodType type, List<ValueTuple<int, int, int>> offsets = null)
        {
            this.type = type;
            Offsets = new List<ValueTuple<int, int, int>>();
            switch(type)
            {
                case CANeighborhoodType.None:
                    break;
                case CANeighborhoodType.Edge:
                    this.Offsets.Add(new ValueTuple<int, int, int>(1, 0, 0));
                    this.Offsets.Add(new ValueTuple<int, int, int>(0, 1, 0));
                    this.Offsets.Add(new ValueTuple<int, int, int>(0, 0, 1));
                    this.Offsets.Add(new ValueTuple<int, int, int>(-1, 0, 0));
                    this.Offsets.Add(new ValueTuple<int, int, int>(0, -1, 0));
                    this.Offsets.Add(new ValueTuple<int, int, int>(0, 0, -1));
                    break;
                case CANeighborhoodType.Vertex:
                    for (int i = -1; i <= 1; i++)
                    {
                        for (int j = -1; j <= 1; j++)
                        {
                            for (int k = -1; k <= 1; k++)
                            {
                                if (i == 0 && j == 0 && k == 0)
                                {
                                    continue;
                                }
                                else
                                {
                                    this.Offsets.Add(new ValueTuple<int, int, int>(i, j, k));
                                }
                            }
                        }
                    }
                    break;
                case CANeighborhoodType.Advanced:
                    if(offsets != null)
                    {
                        for (int i = 0; i < offsets.Count; i++)
                        {
                            this.Offsets.Add(offsets[i]);
                        }
                    }
                    break;
            }
        }

        public override string ToString()
        {
            string type = this.type.ToString();
            var list = this.Offsets.ToList();
            list.Sort();
            string offsets = String.Join(", ", list);
            return type + "-" + offsets;
        }
    }

    public enum CAAction
    {
        Create = 0,
        Destroy = 1,
        Change = 2,
        Move = 3,
    }

    public abstract class CAThen
    {
        // new one of these for each action, with the specifications
        CAAction Action { get; }
        protected double Probability { get; }
        //CAScale actionSubject; // this is the entity that changes - the self, the neighborhood, or the CA graph itself???
        public CAThen(CAAction action, double probability)
        {
            if(probability < 0 && probability > 1)
            {
                throw new Exception("Probability must be in the range [0 - 1]");
            }
            this.Action = action;
            this.Probability = probability;
        }
        public abstract void Act(CAEntity entity);
    }

    public class CAThenDestroy : CAThen
    {
        CATarget Target { get; }
        public CAThenDestroy(CATarget target, double probability):base(CAAction.Destroy, probability)
        {
            this.Target = target;
        }

        public override void Act(CAEntity entity)
        {
            if (entity is CAGraphCellAgent cagca)
            {
                if(cagca.Parent.Parent.Parent.GetRandomDouble() < this.Probability)
                {
                    var targets = Target.GetTargets(cagca);
                    for (int i = 0; i < targets.Count; i++)
                    {
                        if (targets[i] is CAGraphCellAgent agent)
                        {
                            agent.Parent.DestroyAgent();
                        }
                        else if (targets[i] is CAGraphCell cell)
                        {
                            cell.DestroyAgent();
                        }
                    }
                }
            }
        }
    }

    public class CAThenChange : CAThen
    {
        string ChangeProperty { get; }
        CATarget ChangeTarget { get; }
        CAOperator Operator { get; }
        object ChangeValue { get; }
        ChangeMethod Method { get; }
        CAOperation Operation { get; }

        enum ChangeMethod
        {
            Value = 0,
            Property = 1
        }

        public CAThenChange(CATarget target, string property, CAOperator Operator, object value, double probability) : base(CAAction.Change, probability)
        {
            this.ChangeTarget = target;
            this.ChangeProperty = property;
            this.Operator = Operator;
            this.ChangeValue = value;
            Method = ChangeMethod.Value;
        }

        public CAThenChange(CATarget target, string property, CAOperation operation, double probability) : base(CAAction.Change, probability)
        {
            this.ChangeTarget = target;
            this.ChangeProperty = property;
            this.Operation = operation;
            Method = ChangeMethod.Property;
        }

        public override void Act (CAEntity entity)
        {
            if (entity is CAGraphCellAgent cagca)
            {
                if(cagca.Parent.Parent.Parent.GetRandomDouble() < this.Probability)
                {
                    var cellPos = cagca.Parent.Position;
                    var x = cellPos.Item1 - (cagca.Parent.Parent.Dimensions.Item1 / 2);
                    var y = cellPos.Item2 - (cagca.Parent.Parent.Dimensions.Item2 / 2);
                    var distance = Math.Sqrt((x * x) + (y * y));
                    //Console.WriteLine("\rDistance from center: " + distance + "                "); // this should be removed...we're only doing this for DLA.
                    var targets = ChangeTarget.GetTargets(cagca);
                    for (int i = 0; i < targets.Count; i++)
                    {
                        var target = targets[i];
                        var name = ChangeProperty;
                        var property = target.GetProperty(name); // danger here of getting a property that doesn't exist...
                        //cagca.Parent.Parent.Parent.Trans[(int)property] -= 1;
                        if (Method == ChangeMethod.Value)
                        {
                            dynamic changed = StaticMethods.Operate(property, Operator, ChangeValue);
                            target.AddProperty((name, changed));
                            //cagca.Parent.Parent.Parent.Trans[(int)changed] += 1;
                            if(cagca.Parent.Parent.Parent.Settings.StoreTransitions)
                            {
                                cagca.Parent.Parent.Parent.AddTransition((target.Type, name, property, changed));
                            }
                        }
                        else if (Method == ChangeMethod.Property)
                        {
                            var result = Operation.Operate(target, property);
                            //var type = result.GetType();
                            //dynamic changed = new CAProperty(name, result);
                            target.AddProperty((name, result));
                            //cagca.Parent.Parent.Parent.Trans[(int)result] += 1;
                            if (cagca.Parent.Parent.Parent.Settings.StoreTransitions)
                            {
                                cagca.Parent.Parent.Parent.AddTransition((target.Type, name, property, result));
                            }
                        }
                    }
                }
            }
        }
    }

    public class CAThenCreate : CAThen
    {
        public event AgentCreationFailure Failure;
        public event AgentCreationSuccess Success;
        public CALocation Location { get; set; }
        //CAGraph Graph { get; } // doesn't make sense to "create" at a different scale
        ushort State { get; }
        public CAThenCreate(CALocation Location, ushort state, double probability) : base(CAAction.Create, probability)
        {
            this.Location = Location;
            this.State = state;
        }
        // do RNG on list of locations
        public override void Act(CAEntity entity)
        {
            if(entity is CAGraphCellAgent agent)
            {
                if(agent.Parent.Parent.Parent.GetRandomDouble() < this.Probability)
                {
                    var Graph = agent.Parent.Parent;
                    List<CAGraphCell> realLocations = new List<CAGraphCell>();
                    var locations = Location.GetLocations(agent);
                    for (int i = 0; i < locations.Length; i++)
                    {
                        var cell = Graph.GetCell(locations[i]);
                        if (cell != null)
                        {
                            if (!cell.ContainsAgent())
                            {
                                realLocations.Add(cell);
                                Success?.Invoke(this, new CreationEventArgs(CreationEventArgs.CreationStatus.Success, locations[i], cell));
                            }
                            else
                            {
                                Failure?.Invoke(this, new CreationEventArgs(CreationEventArgs.CreationStatus.Failure_AgentExists, locations[i], cell));
                            }
                        }
                        else
                        {
                            Failure?.Invoke(this, new CreationEventArgs(CreationEventArgs.CreationStatus.Failure_CellNull, locations[i]));
                        }
                    }
                    if (realLocations.Count > 0)
                    {
                        int select = (int)Math.Floor(agent.Parent.Parent.Parent.GetRandomDouble() * realLocations.Count);
                        CAGraphCellAgent newAgent = new CAGraphCellAgent(realLocations[select], State);
                        realLocations[select].AddAgent(newAgent);
                        Success?.Invoke(this, new CreationEventArgs(CreationEventArgs.CreationStatus.Success, realLocations[select].Position, realLocations[select]));
                    }
                    else
                    {
                        Failure?.Invoke(this, new CreationEventArgs(CreationEventArgs.CreationStatus.Failure_NoLocations, (ushort.MaxValue, ushort.MaxValue, ushort.MaxValue)));
                    }
                }
            }
        }
    }

    public class CAThenMove : CAThen
    {
        CANeighborhood Neighborhood { get; }
        List<double> MoveProbs { get; }
        List<double> MoveProbsModified { get; set; }
        public CAThenMove(CANeighborhood neighborhood, List<double> moveProbs, double probability) : base(CAAction.Move, probability)
        {
            this.Neighborhood = neighborhood;
            this.MoveProbs = moveProbs;
            var temp = MoveProbs.Select(x => x * (1 / MoveProbs.Sum())).ToList();
            this.MoveProbsModified = new List<double>() { temp[0] };
            for (int i = 1; i < temp.Count; i++)
            {
                this.MoveProbsModified.Add(temp[i] + this.MoveProbsModified[i - 1]);
            }
        }
        // do RNG on list of locations
        public override void Act(CAEntity entity)
        {
            if (entity is CAGraphCellAgent agent)
            {
                if(agent.Parent.Parent.Parent.GetRandomDouble() < this.Probability)
                {
                    var oldParent = agent.Parent;
                    var neighbors = Neighborhood.Offsets.Select(x => new ValueTuple<int, int, int>(oldParent.Position.Item1 + x.Item1, oldParent.Position.Item2 + x.Item2, oldParent.Position.Item3 + x.Item3))
                        .Where(x => (x.Item1 >= 0 && x.Item1 < oldParent.Parent.Dimensions.Item1) && (x.Item2 >= 0 && x.Item2 < oldParent.Parent.Dimensions.Item2) && (x.Item3 >= 0 && x.Item3 < oldParent.Parent.Dimensions.Item3))
                        .Select(x => new ValueTuple<ushort, ushort, ushort>(Convert.ToUInt16(x.Item1), Convert.ToUInt16(x.Item2), Convert.ToUInt16(x.Item3))).ToList();
                    double random = agent.Parent.Parent.Parent.GetRandomDouble();
                    int use = 0;
                    for (int i = 0; i < MoveProbsModified.Count; i++)
                    {
                        if (random < MoveProbsModified[i])
                        {
                            use = i;
                            break;
                        }
                    }
                    if (neighbors.Count > use)
                    {
                        var newParent = oldParent.Parent.GetCell(neighbors[use]);
                        if (newParent != null)
                        {
                            if (newParent.ContainsAgent() == false)
                            {
                                newParent.AddAgent(agent);
                                oldParent.DestroyAgent();
                            }
                            else
                            {
                                // ??? elastic behavior?
                            }
                        }
                    }
                }
            }
        }
    }

    public delegate void AgentCreationFailure(object source, CreationEventArgs e);
    public delegate void AgentCreationSuccess(object source, CreationEventArgs e);
    public delegate void PropertyChanged(object source, ChangeEventArgs e);

    // TODO change name, or add metadata for successful creation
    public class CreationEventArgs: EventArgs
    {
        public enum CreationStatus
        {
            Success=0,
            Failure_AgentExists = 1,
            Failure_CellNull = 2,
            Failure_NoLocations = 3
        }
        public CreationStatus Cause { get; }
        public ValueTuple<ushort, ushort, ushort> Location { get; }
        public CAGraphCell Cell { get; }
        public CreationEventArgs(CreationStatus cause, ValueTuple<ushort, ushort, ushort> location, CAGraphCell cell = null)
        {
            this.Cause = cause;
            this.Location = location;
            this.Cell = cell;
        }
    }

    public class ChangeEventArgs:EventArgs
    {
        public (string, dynamic) Property { get; }
        public bool Success { get; }
        public CAEntity Source { get; }
        public ChangeEventArgs(CAEntity source, (string, dynamic) property, bool success)
        {
            this.Source = source;
            this.Property = property;
            this.Success = success;
        }
    }

    public enum CACreationLocationType
    {
        Position = 0,
        Relative = 1,
        Shape = 2,
    }

    public abstract class CALocation
    {
        CACreationLocationType LocationType { get; }
        protected ValueTuple<ushort, ushort, ushort>[] Locations { get; set; }
        public CALocation(CACreationLocationType type)
        {
            LocationType = type;
        }

        public abstract ValueTuple<ushort, ushort, ushort>[] GetLocations(CAEntity entity = null);
    }

    public class CALocationPosition: CALocation
    {
        // add tuples
        public CALocationPosition(List<ValueTuple<ushort, ushort, ushort>> locations) :base(CACreationLocationType.Position)
        {
            this.Locations = locations.ToArray();
        }

        public override ValueTuple<ushort, ushort, ushort>[] GetLocations(CAEntity entity = null)
        {
            return Locations;
        }
    }

    public class CALocationRelative: CALocation
    {
        CANeighborhood Neighborhood { get; }
        // add target. get source at runtime
        public CALocationRelative(CANeighborhood Neighborhood) : base(CACreationLocationType.Relative)
        {
            this.Neighborhood = Neighborhood;
        }

        public override ValueTuple<ushort, ushort, ushort>[] GetLocations(CAEntity entity)
        {
            List<ValueTuple<ushort, ushort, ushort>> output = new List<ValueTuple<ushort, ushort, ushort>>();
            if (entity is CAGraphCellAgent agent)
            {
                output.AddRange(Neighborhood.Offsets.Select(x => new ValueTuple<int, int, int>(agent.Parent.Position.Item1 + x.Item1, agent.Parent.Position.Item2 + x.Item2, agent.Parent.Position.Item3 + x.Item3))
                    .Where(x => (x.Item1 >= 0 && x.Item1 < agent.Parent.Parent.Dimensions.Item1) && (x.Item2 >= 0 && x.Item2 < agent.Parent.Parent.Dimensions.Item2) && (x.Item3 >= 0 && x.Item3 < agent.Parent.Parent.Dimensions.Item3))
                    .Select(x => new ValueTuple<ushort, ushort, ushort>(Convert.ToUInt16(x.Item1), Convert.ToUInt16(x.Item2), Convert.ToUInt16(x.Item3))).ToList());
            }
            else if (entity is CAGraphCell cell)
            {
                output.AddRange(Neighborhood.Offsets.Select(x => new ValueTuple<int, int, int>(cell.Position.Item1 + x.Item1, cell.Position.Item2 + x.Item2, cell.Position.Item3 + x.Item3))
                    .Where(x => (x.Item1 >= 0 && x.Item1 < cell.Parent.Dimensions.Item1) && (x.Item2 >= 0 && x.Item2 < cell.Parent.Dimensions.Item2) && (x.Item3 >= 0 && x.Item3 < cell.Parent.Dimensions.Item3))
                    .Select(x => new ValueTuple<ushort, ushort, ushort>(Convert.ToUInt16(x.Item1), Convert.ToUInt16(x.Item2), Convert.ToUInt16(x.Item3))).ToList());
            }
            return output.ToArray();
        }
    }

    public class CALocationShape: CALocation
    {
        public double Scale { get; }
        // add shape. build from CA graph
        public CALocationShape(CACreationLocationShapeType type, ValueTuple<ushort, ushort, ushort> dimensions, double scale) : base(CACreationLocationType.Shape)
        {
            this.Scale = scale;
            switch(type)
            {
                case CACreationLocationShapeType.Ellipsoid:
                    Locations = StaticMethods.AddEllipsoid(dimensions, scale).ToArray();
                    break;
                case CACreationLocationShapeType.Circle:
                    Locations = StaticMethods.AddCircle((dimensions.Item1, dimensions.Item2), scale).ToArray();
                    break;
            }
        }

        public override ValueTuple<ushort, ushort, ushort>[] GetLocations(CAEntity entity = null)
        {
            return Locations;
        }
    }

    public enum CACreationLocationShapeType
    {
        Ellipsoid = 0,
        Circle = 1
    }

    public abstract class CAEntity
    {
        public CAEntityType Type { get; }
        public Dictionary<string, dynamic> Properties { get; private set; }

        public CAEntity(CAEntityType type)
        {
            this.Type = type;
        }

        public CAEntity(CAEntityType type, Dictionary<string, dynamic> properties)
        {
            this.Type = type;
            if(properties != null)
            {
                this.Properties = new Dictionary<string, dynamic>(properties);
            }
        }

        public void AddProperty((string, dynamic) property)
        {
            //if(property.Item2 is IComparable, IComparable<T>, IConvertible, IEquatable<T>, IFormattable)
            if (this.Properties  == null)
            {
                this.Properties = new Dictionary<string, dynamic>();
            }
            if(this.Properties.ContainsKey(property.Item1))
            {
                this.Properties[property.Item1] = property.Item2;
            }
            else
            {
                this.Properties.Add(property.Item1, property.Item2);
            }
        }

        public dynamic GetProperty(string name) /*where T : IComparable, IComparable<T>, IConvertible, IEquatable<T>, IFormattable*/
        {
            if(this.Properties == null)
            {
                throw new Exception("The properties Dictionary is null.");
            }
            else
            {
                if(this.Properties.ContainsKey(name))
                {
                    return this.Properties[name];
                }
                else
                {
                    throw new Exception("The key " + name + " does not exist.");
                }
            }
        }

        public ValueTuple<CAEntityType, Dictionary<string, dynamic>> GetProperties()
        {
            ValueTuple<CAEntityType, Dictionary<string, dynamic>> output = (Type, null);
            if (Properties != null)
            {
                output.Item2 = new Dictionary<string, dynamic>(this.Properties);
            }
            return output;
        }
    }

    public enum CAEntityType
    {
        Agent = 0,
        Cell = 1,
        Graph = 2
    }

    public struct CAIf
    {
        // if >4 neighbors of agent are of state 1, act.
        readonly (string, dynamic) comparisonValue;
        readonly CATarget target;
        readonly CATargetType targetType;
        // target amount - all, any, none, certain number
        readonly string comparisonProperty;
        readonly CAEquality comparisonPropertyEquality;
        public CAIf(string comparisonProperty, CATargetType type, CATarget target, CAEquality comparisonPropertyEquality, (string, dynamic) comparisonValue)
        {
            this.comparisonValue = comparisonValue;
            this.targetType = type;
            this.target = target;
            this.comparisonProperty = comparisonProperty;
            this.comparisonPropertyEquality = comparisonPropertyEquality;
        }

        public bool Check(CAEntity entity)
        {
            string compare = comparisonProperty;
            var equality = this.comparisonPropertyEquality;
            // should this be all? Should this be Any? Target amount.
            var value = comparisonValue.Item2;
            var others = target.GetTargets(entity);
            var otherProperties = others.Select(x => x.GetProperty(compare)).ToList();
            //Console.WriteLine("Others: " + others.Count);
            bool result = false;
            switch (targetType)
            {
                case CATargetType.None:
                    result = !otherProperties.Any(x => StaticMethods.CheckEquality(x, equality, value));
                    break;
                case CATargetType.Any:
                    result = otherProperties.Any(x => StaticMethods.CheckEquality(x, equality, value));
                    break;
                case CATargetType.All:
                    result = otherProperties.All(x => StaticMethods.CheckEquality(x, equality, value));
                    break;
            }
            return result;
        }
    }

    public enum CATargetType
    {
        None = 0,
        Any = 1,
        All = 2,
    }

    public struct CATarget
    {
        public readonly CAScale targetScale;
        public readonly CAEntityType type;
        public readonly CANeighborhood targetRegionalNeighborhood; // only active if the scale is "regional"
        public CATarget(CAScale scale, CAEntityType type, CANeighborhood neighborhood)
        {
            if(scale == CAScale.Global)
            {
                if(type != CAEntityType.Graph)
                {
                    throw new Exception("A CA graph target must be paired with a global scale.");
                }
            }

            if (type == CAEntityType.Graph)
            {
                if (scale != CAScale.Global)
                {
                    throw new Exception("A CA graph target must be paired with a global scale.");
                }
            }

            this.targetScale = scale;
            this.type = type;
            this.targetRegionalNeighborhood = neighborhood;
        }

        // how to deal with cells vs agents? if we make a second dimension, "regional" + "global" will refer to the same thing, no?
        public List<CAEntity> GetTargets(CAEntity entity)
        {
            List<CAEntity> entities = new List<CAEntity>();
            if(targetScale == CAScale.Local)
            {
                if(type == CAEntityType.Agent)
                {
                    if(entity is CAGraphCellAgent localAgent)
                    {
                        entities.Add(localAgent);
                    }
                    else
                    {
                        throw new Exception("Entity is not an agent.");
                    }
                }
                else if(type == CAEntityType.Cell)
                {
                    if(entity is CAGraphCellAgent agent)
                    {
                        entities.Add(agent.Parent);
                    }
                    else if (entity is CAGraphCell cell)
                    {
                        entities.Add(cell);
                    }
                    else
                    {
                        throw new Exception("Entity is not an agent or cell.");
                    }
                }
            }
            else if(targetScale == CAScale.Global)
            {
                if (entity is CAGraphCellAgent agent)
                {
                    entities.Add(agent.Parent.Parent);
                }
                else if (entity is CAGraphCell cell)
                {
                    entities.Add(cell);
                }
                else if (entity is CAGraph graph)
                {
                    entities.Add(graph);
                }
                else
                {
                    throw new Exception("Entity is not an agent, cell, or graph.");
                }
            }
            else
            {
                if (entity is CAGraphCellAgent localAgent)
                {
                    List<CAGraphCell> neighborhood = targetRegionalNeighborhood.Offsets.Select(x => new ValueTuple<int, int, int>(localAgent.Parent.Position.Item1 + x.Item1, localAgent.Parent.Position.Item2 + x.Item2, localAgent.Parent.Position.Item3 + x.Item3))
                    .Where(x => (x.Item1 >= 0 && x.Item1 < localAgent.Parent.Parent.Dimensions.Item1) && (x.Item2 >= 0 && x.Item2 < localAgent.Parent.Parent.Dimensions.Item2) && (x.Item3 >= 0 && x.Item3 < localAgent.Parent.Parent.Dimensions.Item3))
                    .Select(x => localAgent.Parent.Parent.GetCell(new ValueTuple<ushort, ushort, ushort>(Convert.ToUInt16(x.Item1), Convert.ToUInt16(x.Item2), Convert.ToUInt16(x.Item3)))).ToList();
                    if (type == CAEntityType.Agent)
                    {
                        neighborhood = neighborhood.Where(x => x.ContainsAgent() == true).ToList();
                        var agents = neighborhood.Select(x => x.Agent).ToList();
                        entities.AddRange(agents);
                    }
                    else
                    {
                        entities.AddRange(neighborhood);
                    }
                }
                else
                {
                    throw new Exception("A Regional target requires a local agent entity.");
                }
            }
            return entities;
        }
    }

    public struct CARule
    {
        public readonly List<CAIf> Ifs;
        public readonly List<CAThen> Thens;

        public CARule(List<CAIf> ifs, List<CAThen> thens)
        {
            this.Ifs = ifs;
            this.Thens = thens;
        }

        public bool Check(CAEntity entity)
        {
            bool pass = true;
            for (int i = 0; i < Ifs.Count; i++)
            {
                var _if = Ifs[i];
                if(!_if.Check(entity))
                {
                    pass = false;
                    break;
                }
            }
            return pass;
        }

        public void Act(CAEntity entity)
        {
            for (int i = 0; i < Thens.Count; i++)
            {
                var then = Thens[i];
                then.Act(entity);
            }
        }
    }

    public enum GridShape
    {
        Square = 0,
        Triangle = 1,
        Hexagon = 2
    }
}
