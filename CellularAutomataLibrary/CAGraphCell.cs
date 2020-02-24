using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CellularAutomataLibrary
{
    public class CAGraphCell:CAEntity
    {
        public CAGraph Parent { get; set; }
        public CAGraphCellAgent Agent { get; private set; }
        public ValueTuple<ushort, ushort, ushort> Position { get; private set; }
        //private Tuple<ushort, ushort, ushort>[][] Neighbors { get; set; }
        //System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();

        public CAGraphCell(CAGraph parent, ValueTuple<ushort, ushort, ushort> position):base(CAEntityType.Cell)
        {
            this.Parent = parent;
            this.Position = position;
            //this.Neighbors = new Tuple<ushort, ushort, ushort>[Enum.GetNames(typeof(CANeighborhoodType)).Count()][];
        }

        public CAGraphCell(CAGraph parent, ValueTuple<ushort, ushort, ushort> position, Dictionary<string, dynamic> properties, CAGraphCellAgent agent) :base(CAEntityType.Cell, properties)
        {
            this.Parent = parent;
            this.Position = position;
            if(agent != null)
            {
                this.Agent = agent.Copy(this);
            }
        }

        public bool ContainsAgent()
        {
            return (Agent != null);
        }

        public void DestroyAgent()
        {
            Agent = null;
            Parent.RemoveAgentCell(Position);
        }

        public void AddAgent(CAGraphCellAgent agent)
        {
            Agent = agent.Copy(this);
            Parent.AddAgentCell(Position);
        }

        public void AddAgent(Dictionary<string, dynamic> properties)
        {
            Agent = new CAGraphCellAgent(this, properties);
            Parent.AddAgentCell(Position);
        }

        public CAGraphCell Copy(CAGraph graph)
        {
            var cell = new CAGraphCell(graph, this.Position, this.Properties, this.Agent);
            return cell;
        }

        public CAGraphCell GetCopy()
        {
            ValueTuple<ushort, ushort, ushort> location = this.Position;
            return this.Parent.GetCopy().GetCell(location);
        }

        public void Run(List<CARule> rules)
        {
            // act on self: before or after?
            if (ContainsAgent())
            {
                Agent.Run(rules);
            }
            // for self running: USE GET COPY???
        }

        public List<ValueTuple<CAEntityType, Dictionary<string, dynamic>>> GetCounts()
        {
            List<ValueTuple<CAEntityType, Dictionary<string, dynamic>>> output = new List<(CAEntityType, Dictionary<string, dynamic>)>
            {
                this.GetProperties()
            };
            if(Agent != null)
            {
                output.Add(Agent.GetCounts());
            }
            return output;
        }

    }
}
