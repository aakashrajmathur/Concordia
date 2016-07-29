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
        DateTime startDateTime;
        string databaseName; 

        public TrackedEvent(Game game, Process process, string databaseName)
        {
            this.game = game;
            this.processHandle = process;
            this.startDateTime = DateTime.Now;
            this.databaseName = databaseName; 
        }
    }
}
