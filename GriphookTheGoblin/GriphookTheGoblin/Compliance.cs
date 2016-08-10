using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GriphookTheGoblin
{
    class Compliance
    {
        public bool IsCurrentGameValid(List<Record> allRecords)
        {
            List<string> teams = GetTeamNames(allRecords);
            if (teams.Count != 2)
            {
                return false;
                //throw new Exception("There must be exactly two teams for this fucntion to work!");
            }
            if (allRecords.Count <= 0)
            {
                return false;
                //Nothing to eval
            }
            //Duration must be an hour or more: 
            if (GetDurationOfEvent(allRecords) < (60 * 60))
            {
                return false;
            }
            
            return true;
        }

        private int GetDurationOfEvent(List<Record> allRecords)
        {


            if (allRecords.Count > 0)
            {
                DateTime start = allRecords[0].datetime;
                DateTime end = allRecords[allRecords.Count - 1].datetime;
                double diffInSeconds = (end - start).TotalSeconds;
                return (int)diffInSeconds;
            }
            return 0;
        }

        private List<string> GetTeamNames(List<Record> records)
        {
            HashSet<string> teams = new HashSet<string>();
            foreach (Record record in records)
            {
                if (!teams.Contains(record.teamName.Trim()))
                    teams.Add(record.teamName);

            }
            return teams.ToList<string>();
        }
    }
}
