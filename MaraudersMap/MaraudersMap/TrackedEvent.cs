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
        public Game game;
        public Process processHandle;
        public DateTime startDateTime;
        public string databaseName; 

        public TrackedEvent(Game game, Process process, string databaseName)
        {
            this.game = game;
            this.processHandle = process;
            this.startDateTime = DateTime.Now;
            this.databaseName = databaseName; 
        }
    }
}
