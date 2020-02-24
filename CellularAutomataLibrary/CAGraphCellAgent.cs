using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CellularAutomataLibrary
{
    public class CAGraphCellAgent:CAEntity
    {
        // have to change...this has to be a property? Or no, it's a CAEntity which has a list of CAProperties.
        //public int State { get; set; }
        public CAGraphCell Parent { get; set; }
        //System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();

        public CAGraphCellAgent(CAGraphCell parent, ushort state):base(CAEntityType.Agent)
        {
            this.Parent = parent;
            AddProperty(("state", state));
        }

        public CAGraphCellAgent(CAGraphCell parent, Dictionary<string, dynamic> properties) :base(CAEntityType.Agent, properties)
        {
            this.Parent = parent;
        }

        public Dictionary<string, dynamic> GetOtherProperties()
        {
            var all = this.GetProperties();
            all.Item2.Remove("state");
            return new Dictionary<string, dynamic>(all.Item2);
            //int index = CA.GetPropertyIndex("state");
            //others.RemoveAt(index);
            //return others;
        }

        public dynamic GetStateProperty()
        {
            return this.GetProperty("state");
        }

        public CAGraphCellAgent GetCopy()
        {
            return this.Parent.GetCopy().Agent;
        }

        public CAGraphCellAgent Copy(CAGraphCell cell)
        {
            var agent = new CAGraphCellAgent(cell, this.Properties);
            return agent;
        }

        public void Run(List<CARule> rules)
        {
            foreach(CARule rule in rules)
            {
                bool act = rule.Check(this.GetCopy());
                if (act)
                {
                    rule.Act(this);
                }
            }
        }

        public ValueTuple<CAEntityType, Dictionary<string, dynamic>> GetCounts()
        {
            return this.GetProperties();
        }

    }
}
