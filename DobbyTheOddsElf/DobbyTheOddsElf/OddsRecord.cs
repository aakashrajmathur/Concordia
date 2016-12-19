using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DobbyTheOddsElf
{
    class OddsRecord
    {
        public string teamName;
        public string odds;
        public DateTime dateTime; 

        public OddsRecord(string teamName, string odds)
        {
            this.teamName = teamName;
            this.odds = odds;
            this.dateTime = DateTime.Now;
        }
    }
}
