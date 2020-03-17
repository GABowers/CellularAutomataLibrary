using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace CellularAutomataLibrary
{
    public class CASettings
    {
        public List<Color> StateColorMap { get; set; }
        public bool Subprocessing { get; set; } // only process cells in the AgentCells list
        /// <summary>
        /// Set the format of the the grid copy created each iteration. For simulations where only one agent is active, reference is acceptable (and fastest). For simulations with more than one agent, a deep copy is needed for neighbor checks, but a reference copy is necessary for moving agents.
        /// </summary>
        public CACopyFormat CopyFormat { get; set; }
        public bool StoreCounts { get; set; }
        public bool StoreTransitions { get; set; }
        public ushort States { get; set; }

        public CASettings()
        {
            Subprocessing = false;
            CopyFormat = CACopyFormat.DeepCopy;
            StoreCounts = false;
            StoreTransitions = false;
        }
    }
}
