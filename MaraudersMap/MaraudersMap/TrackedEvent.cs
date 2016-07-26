using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MaraudersMap
{
    class TrackedEvent
    {
        Game game;
        Process processHandle;

        public TrackedEvent(Game game, Process process)
        {
            this.game = game;
            this.processHandle = process;
        }
    }
}
