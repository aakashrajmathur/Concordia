using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace GriphookTheGoblin
{
    class ResultStatistics
    {
        public int initialOddsDifference;
        public string filePath;
        public int totalNumberOfRecords;
        public string durationOfEvent;
        public int numberOfTeams;

        public string teamAName;
        public int teamAinitialOdds;
        public int teamAMaxProfitOrClosestLossOdds;
        public int teamANumberOfRecords;

        public string teamBName;
        public int teamBinitialOdds;
        public int teamBMaxProfitOrClosestLossOdds;
        public int teamBNumberOfRecords;

        public ResultStatistics(List<Record> allRecords, string filePathToDatabase)
        {
            filePath = filePathToDatabase;
            totalNumberOfRecords = allRecords.Count;
            durationOfEvent = GetDurationOfEvent(allRecords);

            List<string> teams = GetTeamNames(allRecords);
            numberOfTeams = teams.Count;

            List<Record> team1Records = GetTeamRecords(teams[0], allRecords);
            teamAinitialOdds = team1Records[0].americanRate;

            List<Record> team2Records = GetTeamRecords(teams[1], allRecords);
            teamBinitialOdds = team2Records[0].americanRate;

            teamAName = teams[0];
            teamAMaxProfitOrClosestLossOdds = GetMaxOdds(teamAinitialOdds, team2Records);
            teamANumberOfRecords = team1Records.Count;

            teamBName = teams[1];
            teamBMaxProfitOrClosestLossOdds = GetMaxOdds(teamBinitialOdds, team1Records);
            teamBNumberOfRecords = team2Records.Count;

            initialOddsDifference = GetOddsDifference(teamAinitialOdds, teamBinitialOdds);

        }

        private int GetOddsDifference(int input1, int input2)
        {
            return Math.Abs(input1 > input2 ? input1 - input2 : input2 - input1);
        }

        public override string ToString()
        {
            string result = "";
            result += "Initial Odds Diff = " + initialOddsDifference;
            result += " , # Records = " + totalNumberOfRecords;
            result += " , Duration = " + durationOfEvent;
            result += " , Name = " + teamAName;
            result += " , initial Odds = " + teamAinitialOdds;
            result += " , Did Cross? = " + (teamAinitialOdds < teamBMaxProfitOrClosestLossOdds);
            result += " , Max Profit Odds = " + teamAMaxProfitOrClosestLossOdds;
            result += " , Name = " + teamBName;
            result += " , Initial Odds = " + teamBinitialOdds;
            result += " , Did Cross? = " + (teamBinitialOdds < teamAMaxProfitOrClosestLossOdds);
            result += " , Max Profit Odds = " + teamBMaxProfitOrClosestLossOdds;
            return result;
        }

        //////

        private int GetMaxOdds(double initialRate, List<Record> otherTeamRecords)
        {
            int maxRate = 0;
            bool firstRecord = true;
            foreach (Record record in otherTeamRecords)
            {
                if (firstRecord)
                {
                    maxRate = record.americanRate;
                    firstRecord = false;
                }

                if (record.americanRate > maxRate)
                    maxRate = record.americanRate;
            }
            return maxRate;
        }

        private double GetPercentageDifference(int initialRate, int maxRate)
        {
            int diff = GetOddsDifference(initialRate, maxRate);
            return diff * 100.0 / initialRate;
            /*
            if (initialRate < 0) //Favorite
            {
                if (maxRate < 0)
                {
                    double tempA = 100 + (100 * 100 / Math.Abs(maxRate));
                    double tempB = 100 + Math.Abs(initialRate);
                    return (100.0 * (tempA - tempB)) / tempB;
                    //return (100.0 * (200.0 - (Math.Abs(initialRate) + Math.Abs(maxRate)))) / (Math.Abs(initialRate) + Math.Abs(maxRate));
                }
                else //initial -ve maxRate +ve:
                {
                    double tempA = 100 + maxRate;
                    double tempB = 100 + Math.Abs(initialRate);
                    return (100.0 * (tempA - tempB)) / tempB;
                    //return (100.0 * ((100.0 + maxRate) - (Math.Abs(initialRate) + 100.0))) / (Math.Abs(initialRate) + 100.0);
                }
            }
            else
            {
                if (maxRate < 0)
                {
                    double tempA = initialRate + (initialRate * 100.0 / Math.Abs(maxRate));
                    double tempB = 100 + initialRate;
                    return (100.0 * (tempA - tempB)) / tempB;
                    //return (100 * ((initialRate + 100.0) - (100 + Math.Abs(maxRate)))) / (100 + Math.Abs(maxRate));
                }
                else
                {
                    double tempA = initialRate + ((double)initialRate * maxRate / 100.0);
                    double tempB = 100.0 + initialRate;
                    return (100.0 * (tempA - tempB)) / tempB;
                    //return 0.0;
                }
            }*/
        }

        private List<Record> GetTeamRecords(string inputTeamName, List<Record> allRecords)
        {
            List<Record> teamRecords = new List<Record>();
            foreach (Record record in allRecords)
            {
                if (record.teamName.CompareTo(inputTeamName) == 0)
                {
                    teamRecords.Add(record);
                }
            }
            return teamRecords;
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

        private string GetDurationOfEvent(List<Record> allRecords)
        {
            if (allRecords.Count > 0)
            {
                DateTime start = allRecords[0].datetime;
                DateTime end = allRecords[allRecords.Count - 1].datetime;
                TimeSpan diff = end - start;
                return diff.ToString();
            }
            return "00:00:00";
        }
    }
}