using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Timers;
using System.Windows;
using System.Windows.Input;
using System.Linq;

namespace DobbyTheOddsElf
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        const int TIMER_DURATION = 5000;
        const int MINIMUM_LENGTH_OF_TEAM_NAME = 4;

        Timer timer;
        SQLiteConnection sqlconn;
        public static string databaseName;

        public MainWindow()
        {
            try
            {
                InitializeComponent();
                timer = new Timer(TIMER_DURATION);
                timer.Elapsed += new ElapsedEventHandler(timerElapsed);

                string[] args = Environment.GetCommandLineArgs();
                if (args.Count() == 1)
                {
                    NavigateMainWebBrowser("https://sports.bovada.lv/live-betting/");
                }
                else
                {
                    NavigateMainWebBrowser(args[3]);
                    textBoxTeam1.Text = args[1];
                    textBoxTeam2.Text = args[2];
                    databaseName = args[4];
                    timer.Start();
                }
                CreateSQLiteDatabase();

            }
            catch (Exception ex) {
                MessageBox.Show(ex.Message);
            }
        }

    private void CreateSQLiteDatabase()
        {
            sqlconn = new SQLiteConnection("Data Source=" + GetDatabaseLocation() + GetDatabaseName() + ".sqlite;Version=3;New=True;Compress=True;");
            sqlconn.Open();
            SQLiteCommand createQ = new SQLiteCommand("CREATE TABLE IF NOT EXISTS sportsLiveRates( DateTime TEXT, Team TEXT, Rate TEXT)", sqlconn);
            createQ.ExecuteNonQuery();
            sqlconn.Close();
        }

        private string GetDatabaseLocation()
        {
            string databaseLocation = "c:\\sports\\" + DateTime.Now.ToString("yyyy_MM_dd") +"\\";
            if (!Directory.Exists(databaseLocation))
            {
                Directory.CreateDirectory(databaseLocation);
            }
            return databaseLocation;
        }

        private string GetDatabaseName()
        {
            //Objective: Give Database a unique name, Convention: date time + processID: 
            if (databaseName == null)
                return DateTime.Now.ToString("yyyy_MM_dd_HH_mm_ss");
            else
                return databaseName;
        }

        private void timerElapsed(object sender, ElapsedEventArgs e)
        {
            try
            {
                //MessageBox.Show("timer elapsed");
                Dispatcher.Invoke(new Action(() =>
                {
                    ReportRates();
                }));
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void ReportRates()
        {  
            List<string> processedWebSource = GetProcessedWebSource(readWebPageSource());

            if (processedWebSource.Contains("Moneyline", StringComparer.OrdinalIgnoreCase) || processedWebSource.Contains("Match winner", StringComparer.OrdinalIgnoreCase))
            {
                string currentMetric = processedWebSource.Contains("Moneyline", StringComparer.OrdinalIgnoreCase) ? "Moneyline" : "Match winner";
                string teamA = GetTeamName(textBoxTeam1.Text, processedWebSource);
                string teamB = GetTeamName(textBoxTeam2.Text, processedWebSource);

                int index = processedWebSource.FindIndex(new Predicate<string>(item => item.ToUpper() == currentMetric.ToUpper()));
                if( Validate(processedWebSource[index+1], processedWebSource[index+2], teamA, teamB))
                {
                    CreateRow(processedWebSource[index + 1], GetRate(processedWebSource[index + 2]));
                }                
                if (Validate(processedWebSource[index + 3], processedWebSource[index + 4], teamA, teamB))
                {
                    CreateRow(processedWebSource[index + 3], GetRate(processedWebSource[index + 4]));
                }
            }
        }

        private string GetTeamName(string teamName, List<string> processedWebSource)
        {
            //Place to add any validations in the future.
            //"ST Kitts &amp; Nevis Patriots"
            teamName = teamName.Replace("&", "&amp;");
            teamName = GetFullDisplayName(teamName, processedWebSource);
            return teamName;
        }
        
        private string GetFullDisplayName(string input, List<string> processedWebSource)
        {
            string[] tokens = input.Split(' ');
            foreach (string s in tokens)
            {
                if (s.Length >= MINIMUM_LENGTH_OF_TEAM_NAME)
                {
                    string fullName = GetNameInList(s, processedWebSource);
                    if (fullName.Length > 0)
                    {
                        return fullName;
                    }
                }
            }
            return "";
        }

        private string GetNameInList(string input, List<string> processedWebSource)
        {
            //MessageBox.Show("looking for " + input);
            //Skip till upcoming: 
            bool upcomingSectionSeen = false;
            foreach (string s in processedWebSource)
            {
                if (s.CompareTo("Upcoming") == 0)
                {
                    upcomingSectionSeen = true; continue;
                }
                if (upcomingSectionSeen)
                {
                    if (s.Contains(input))
                    {
                        return s;
                    }
                }
            }
            return "";
        }

        private bool Validate(string webInputTeamName, string webInputRate, string userTeamNameA, string userTeamNameB)
        {
            if ((webInputTeamName.ToUpper().CompareTo(userTeamNameA.ToUpper()) != 0) && (webInputTeamName.ToUpper().CompareTo(userTeamNameB.ToUpper()) != 0))
                return false;

            if (webInputRate.ToUpper().CompareTo("SUSPENDED") == 0)
                return false;

            //default:
            return true;
        }

        private string GetRate(string rate)
        {
            //Place to add any validations in the future.

            if (rate == "EVEN")
            {
                rate = "100";
            }
            
            return rate;
        }
       
        public void CreateRow(string team, string rate)
        {
            sqlconn.Open();
            string dateTime = DateTime.Now.ToString("d / M / yyyy HH:mm:ss");
            string insertCommand = @"INSERT INTO sportsLiveRates (DateTime, Team, Rate) VALUES ("" " + dateTime + @" "","" " + team + @" ""," + rate + ");";
            SQLiteCommand insertSQL = new SQLiteCommand(insertCommand, sqlconn);
            try
            {
                insertSQL.ExecuteNonQuery();
                listBoxQuerySubmitted.Items.Add(insertSQL.CommandText.ToString());                
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
            sqlconn.Close();
        }
        
        private void NavigateMainWebBrowser(string link)
        {
            try
            {
                mainWebBrowser.Navigate(link);//"https://sports.bovada.lv/live-betting/");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error in navigating URL " + Environment.NewLine + "Details: " + ex.Message);
            }
        }        

        private List<string> GetProcessedWebSource(string webPageSource)
        {
            //Remove all new line characters: 
            webPageSource = webPageSource.Replace("\n", "");

            bool isInTag = false;
            List<string> processedWebSource = new List<string>();
            string tempBuffer = "";
            foreach (char c in webPageSource.ToCharArray())
            {
                if(c == '<')
                {
                    tempBuffer = tempBuffer.TrimStart();
                    tempBuffer = tempBuffer.TrimEnd();
                    if (tempBuffer.Length > 0)
                    {
                        processedWebSource.Add(tempBuffer);
                        tempBuffer = "";
                    }
                    isInTag = true;
                }else if (c == '>')
                {
                    isInTag = false;
                    continue;
                }

                if (!isInTag)
                {
                    tempBuffer += c;
                }
            }

            tempBuffer = tempBuffer.TrimStart();
            tempBuffer = tempBuffer.TrimEnd();
            if (tempBuffer.Length > 0) {
                processedWebSource.Add(tempBuffer);
            }
            
            return processedWebSource;
        }

        private string readWebPageSource()
        {
            mshtml.HTMLDocumentClass dom = (mshtml.HTMLDocumentClass)mainWebBrowser.Document;
            return dom.body.innerHTML;
        }

        private void monitorButton_Click(object sender, RoutedEventArgs e)
        {
            if(monitorButton.Content.ToString() == "Start")
            {
                timer.Start();
                monitorButton.Content = "Stop";
            }else
            {
                timer.Stop();
                monitorButton.Content = "Start";
            }
        }
    }
}
