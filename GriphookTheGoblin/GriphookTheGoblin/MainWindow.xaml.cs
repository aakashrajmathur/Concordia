using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Windows;


namespace GriphookTheGoblin
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void button_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            if (openFileDialog.ShowDialog() == true)
            {
                textBox.Text = openFileDialog.FileName;
                SQLiteConnection conn = new SQLiteConnection("Data Source=" + openFileDialog.FileName);

                conn.Open();
                var command = conn.CreateCommand();
                command.CommandText = "SELECT * FROM sportsLiveRates";
                SQLiteDataReader sdr = command.ExecuteReader();

                List<Record> allRecords = new List<Record>();
                while (sdr.Read())
                {
                    //, "d/M/yyyy HH:mm:ss"
                    allRecords.Add(new Record(DateTime.Parse(sdr.GetString(0)), sdr.GetString(1), int.Parse(sdr.GetString(2))));
                }
                sdr.Close();
                conn.Close();

                allRecords.Sort();

                List<string> teams = GetTeamNames(allRecords);
                if (teams.Count != 2)
                {
                    throw new Exception("There must be exactly two teams for this fucntion to work!");
                }

                List<Record> team1Records = GetTeamRecords(teams[0], allRecords);
                int initialRateTeam1 = team1Records[0].americanRate;
                List<Record> team2Records = GetTeamRecords(teams[1], allRecords);
                int initialRateTeam2 = team2Records[0].americanRate;

                bool didOtherTeamCrossTeam1 = GetDidOtherTeamCross(initialRateTeam1, team2Records);
                labelFavorite.Content = "Team1 was crossed? " + didOtherTeamCrossTeam1;
                int closestOdds = GetMaxOdds(initialRateTeam1, team2Records);
                labelFavorite.Content += " Initial odds = " + initialRateTeam1 + " Closest odds = " + closestOdds;
                labelFavorite.Content += " Max profit / least loss perc diff = " + GetPercentageDifference(initialRateTeam1, closestOdds);

                bool didOtherTeamCrossTeam2 = GetDidOtherTeamCross(initialRateTeam2, team1Records);
                labelUnderDog.Content = "Team2 was crossed? " + didOtherTeamCrossTeam2;
                closestOdds = GetMaxOdds(initialRateTeam2, team1Records);
                labelUnderDog.Content += " Initial odds = " + initialRateTeam2 + " Closest odds = " + closestOdds;
                labelUnderDog.Content += " Max profit / least loss perc diff = " + GetPercentageDifference(initialRateTeam2, closestOdds);

                listBox.Items.Clear();
                foreach (Record record in team1Records)
                {
                    listBox.Items.Add(record.ToString());
                }

                listBox1.Items.Clear();
                foreach (Record record in team2Records)
                {
                    listBox1.Items.Add(record.ToString());
                }
            }
        }

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

        private bool GetDidOtherTeamCross(double initialRate, List<Record> otherTeamRecords, double percentageMargin = 0)
        {
            double adjustmentFactor = GetAdjustmentFactor(percentageMargin);

            foreach (Record record in otherTeamRecords)
            {
                if (initialRate < 0)
                {
                    if (record.americanRate * -1 < initialRate * adjustmentFactor)
                        return true;
                }
                else
                {
                    if (record.americanRate * -1 > initialRate * adjustmentFactor)
                        return true;
                }
            }
            return false;
        }

        private double GetPercentageDifference(int initialRate, int maxRate)
        {
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
            }

        }

        private double GetAdjustmentFactor(double percentageMargin)
        {
            if (percentageMargin == 0)
                return 1.0;
            return 1.0 + (percentageMargin / 100.0);
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

        private void button1_Click(object sender, RoutedEventArgs e)
        {
            DirSearch(@"C:\Data");
        }

        void DirSearch(string sDir)
        {
            try
            {
                foreach (string d in Directory.GetDirectories(sDir))
                {
                    foreach (string f in Directory.GetFiles(d))
                    {
                        listBox1.Items.Add(f);
                        //Console.WriteLine(f);
                    }
                    DirSearch(d);
                }
            }
            catch (System.Exception excpt)
            {
                Console.WriteLine(excpt.Message);
            }
        }
        
    }
}
