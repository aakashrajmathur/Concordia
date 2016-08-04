using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Globalization;
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
                AnalyzeDatabase(openFileDialog.FileName);
            }
        }

        private void AnalyzeDatabase(string filepathToDatabase)
        {
            SQLiteConnection conn = new SQLiteConnection("Data Source=" + filepathToDatabase);

            conn.Open();
            var command = conn.CreateCommand();
            command.CommandText = "SELECT * FROM sportsLiveRates";
            SQLiteDataReader sdr = command.ExecuteReader();

            List<Record> allRecords = new List<Record>();
            while (sdr.Read())
            {
                //, "d/M/yyyy HH:mm:ss"   31 / 7 / 2016 23:26:57 
                DateTime dateTimeFromDatabase = DateTime.ParseExact(sdr.GetString(0), "d / M / yyyy HH:mm:ss", CultureInfo.InvariantCulture); // Convert.ToDateTime(sdr.GetString(0));



                allRecords.Add(new Record(dateTimeFromDatabase, sdr.GetString(1), int.Parse(sdr.GetString(2))));
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
            DirSearch(@"C:\Data\Week1");
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
                        UpdateDateFormatInCurrentDatabase(f);
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

        private void UpdateDateFormatInCurrentDatabase(string filepathToDatabase)
        {
            try
            {
                string dataSource = filepathToDatabase;
                using (SQLiteConnection connection = new SQLiteConnection())
                {
                    connection.ConnectionString = "Data Source=" + dataSource;
                    connection.Open();
                    using (SQLiteCommand command = new SQLiteCommand(connection))
                    {
                        command.CommandText = "SELECT * FROM sportsLiveRates";
                        SQLiteDataReader sdr = command.ExecuteReader();

                        List<Record> allRecords = new List<Record>();
                        while (sdr.Read())
                        {
                            string dateTimeOriginal = sdr.GetString(0);
                            string teamNameOriginal = sdr.GetString(1);
                            string rateOriginal = sdr.GetString(2);

                            string modifiedDateTime = GetCorrectedDateTime(dateTimeOriginal);
                            SQLiteCommand updateCommand = new SQLiteCommand(connection);
                            updateCommand.CommandText = @"UPDATE sportsLiveRates SET DateTime = """ + modifiedDateTime + @""" WHERE DateTime = """ + dateTimeOriginal + @""" AND team = """ + teamNameOriginal + @""" AND Rate = """ + rateOriginal + @"""";
                            //command.CommandText =
                            //    "update Example set Info = :info, Text = :text where ID=:id";
                            //command.Parameters.Add("info", SQLiteType.Text).Value = textBox2.Text;
                            //command.Parameters.Add("text", SQLiteType.Text).Value = textBox3.Text;
                            //command.Parameters.Add("id", SQLiteType.Text).Value = textBox1.Text;
                            updateCommand.ExecuteNonQuery();
                        }
                        sdr.Close();
                    }
                    connection.Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private string GetCorrectedDateTime(string dateTimeOriginal)
        {
            try {
                string[] delimiters = { "/", " ", ":" }; // 31 / 7 / 2016 23:26:57 
                string[] tokens = dateTimeOriginal.Split(delimiters, StringSplitOptions.RemoveEmptyEntries);

                //MessageBox.Show(tokens.Length.ToString());
                //string Day = tokens[0];
                DateTime dateTimeModified = new DateTime(int.Parse(tokens[2]), int.Parse(tokens[1]), int.Parse(tokens[0]), int.Parse(tokens[3]), int.Parse(tokens[4]), int.Parse(tokens[5]));

                string dateTimeModifiedstr = dateTimeModified.ToString("yyyy/MM/dd HH:mm:ss"); //String.Format("yyyy/MM/dd HH:mm:ss", dateTimeModified);
                //MessageBox.Show(dateTimeOriginal + " changed to: " + dateTimeModifiedstr);
                return dateTimeModifiedstr;
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            return "";
        }
    }
}
