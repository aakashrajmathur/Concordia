using System.Collections.Generic;
using System.Linq;

namespace GriphookTheGoblin
{
    public class TeamResult
    {
        public string name;
        public int initialOdds;
        public int maxOdds;
        public List<Record> allRecords;
        public string databaseFilePath;
        
        public TeamResult(List<Record> allRecords, string filePath)
        {
            this.allRecords = allRecords;
            databaseFilePath = filePath;


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