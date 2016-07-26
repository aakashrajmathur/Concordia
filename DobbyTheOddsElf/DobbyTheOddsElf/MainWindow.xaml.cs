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
        Timer timer;
        const int TIMER_DURATION = 5000;
        SQLiteConnection sqlconn;
        public static string URL; 

        public MainWindow()
        {
            InitializeComponent();
            NavigateMainWebBrowser();
            timer = new Timer(TIMER_DURATION);
            timer.Elapsed += new ElapsedEventHandler(timerElapsed);
            CreateSQLiteDatabase();

            string[] args = Environment.GetCommandLineArgs();
            foreach(string s in args)
            {
                // MessageBox.Show(s);
            }
        }

        private void CreateSQLiteDatabase()
        {
            sqlconn = new SQLiteConnection("Data Source="+GetDatabaseLocation()+GetDatabaseName()+".sqlite;Version=3;New=True;Compress=True;");
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
            return DateTime.Now.ToString("yyyy_MM_dd_HH_mm");
        }

        private void timerElapsed(object sender, ElapsedEventArgs e)
        {
            Dispatcher.Invoke(new Action(() =>  {
                                                    ReportRates();
                                                }));
        }

        private void ReportRates()
        {  
            List<string> processedWebSource = GetProcessedWebSource(readWebPageSource());

            if (processedWebSource.Contains("Moneyline", StringComparer.OrdinalIgnoreCase) || processedWebSource.Contains("Match winner", StringComparer.OrdinalIgnoreCase))
            {
                string currentMetric = processedWebSource.Contains("Moneyline", StringComparer.OrdinalIgnoreCase) ? "Moneyline" : "Match winner";
                string teamA = GetTeamName(textBoxTeam1.Text);
                string teamB = GetTeamName(textBoxTeam2.Text);

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

        private string GetTeamName(string teamName)
        {
            //Place to add any validations in the future.
            //"ST Kitts &amp; Nevis Patriots"
            teamName = teamName.Replace("&", "&amp;");

            return teamName;
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

        private void urlTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                NavigateMainWebBrowser();
            }
        }

        private void NavigateMainWebBrowser()
        {
            try
            {
                mainWebBrowser.Navigate("https://sports.bovada.lv/live-betting/");
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
