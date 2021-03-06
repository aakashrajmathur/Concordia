﻿using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Timers;
using System.Windows;
using System.Linq;
using mshtml;

namespace DobbyTheOddsElf
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        const int TIMER_DURATION = 1000;    //1second
        const int TIMER_DURATION_GAME_LOG = 300000; //5 minutes
        const int MINIMUM_LENGTH_OF_TEAM_NAME = 4;

        string teamName1 = "";
        string currentOddsTeam1 = "";
        string teamName2 = "";
        string currentOddsTeam2 = "";
        List<OddsRecord> oddsRecords;
        List<string> gameLog;

        Timer timer;
        Timer timerReadTeamNames;
        Timer timerReadGameLog;

        SQLiteConnection sqlconn;
        public static string sport = "";
        public static string databaseName = "";

        bool isGameOver = false;

        public MainWindow()
        {
            try
            {
                InitializeComponent();

                timerReadTeamNames = new Timer(5000);
                timerReadTeamNames.Elapsed += new ElapsedEventHandler(GetTeamNames);

                timer = new Timer(TIMER_DURATION);
                timer.Elapsed += new ElapsedEventHandler(timerElapsed);

                timerReadGameLog = new Timer(TIMER_DURATION_GAME_LOG);
                timer.Elapsed += new ElapsedEventHandler(timerElapsedReadGameLog);


                oddsRecords = new List<OddsRecord>();
                gameLog = new List<string>();

                string[] args = Environment.GetCommandLineArgs();
                if (args.Count() == 1)
                {
                    NavigateMainWebBrowser("https://sports.bovada.lv/live-betting/");
                }
                else
                {
                    //arguments expected: 0-ProgramName, 1-URL, 2-Sport
                    NavigateMainWebBrowser(args[1]);

                    timerReadTeamNames.Start();

                    sport = args[2];
                    WindowState = WindowState.Minimized; //Default Window state Minimized                  
                }             
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error in Main init, Message = " + ex.Message);
            }
        }

        private void GetTeamNames(object sender, ElapsedEventArgs e)
        {
            try
            {
                Dispatcher.Invoke(new Action(() =>
                {
                    //MessageBox.Show("1");
                    IHTMLElement couponHTMLElement = ((HTMLDocument)mainWebBrowser.Document).getElementById("coupon");
                    //MessageBox.Show("2");
                    if (couponHTMLElement != null)
                    {
                        string header = GetHeaderInCoupon(couponHTMLElement.innerHTML);

                        string[] teamsTokens = header.Split(new string[] { "VS", "Vs", "vs", "vS", "@" }, System.StringSplitOptions.None);

                        if (teamsTokens.Length == 2)
                        {
                            teamName1 = teamsTokens[0].Replace("&nbsp;", "").TrimStart().TrimEnd();
                            textBoxTeam1.Text = teamName1;

                            teamName2 = teamsTokens[1].Replace("&nbsp;", "").TrimStart().TrimEnd();
                            textBoxTeam2.Text = teamName2;
                            listBoxQuerySubmitted.Items.Add("team names read succesfully!");
                            listBoxQuerySubmitted.Items.Add(teamName1);
                            listBoxQuerySubmitted.Items.Add(teamName2);
                            timer.Start();
                            timerReadTeamNames.Stop();
                        }
                    }

                }));
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error in Get Team Name, Message = " + ex.Message);
            }
        }

        private string GetHeaderInCoupon(string webPageSource)
        {
            //Remove all new line characters: 
            webPageSource = webPageSource.Replace("\n", "");

            bool isInTag = false;
            //List<string> processedWebSource = new List<string>();
            string tempBuffer = "";
            foreach (char c in webPageSource.ToCharArray())
            {
                if (c == '<')
                {
                    tempBuffer = tempBuffer.TrimStart();
                    tempBuffer = tempBuffer.TrimEnd();
                    if (tempBuffer.Length > 0)
                    {
                        return tempBuffer;
                        //processedWebSource.Add(tempBuffer);
                        //tempBuffer = "";
                    }
                    isInTag = true;
                }
                else if (c == '>')
                {
                    isInTag = false;
                    continue;
                }

                if (!isInTag)
                {
                    tempBuffer += c;
                }
            }
            return "";
        }

        private string GetDatabaseLocation()
        {
            string databaseLocation = "c:\\Data\\" + DateTime.Now.ToString("yyyy_MM_dd") + "\\";
            if (!Directory.Exists(databaseLocation))
            {
                Directory.CreateDirectory(databaseLocation);
            }
            return databaseLocation;
        }

        private string GetDatabaseName()
        {
            //Objective: Give Database a unique name, Convention: date time + processID: 
            if (databaseName.Length == 0) 
                return DateTime.Now.ToString("yyyy_MM_dd_HH_mm_ss_") + "_" + sport + "_" + teamName1 + "_vs_" + teamName2;
            else
                return databaseName;
        }

        private void timerElapsed(object sender, ElapsedEventArgs e)
        {
            try
            {
                Dispatcher.Invoke(new Action(() =>
                {
                    if (!isGameOver)
                    {
                        string updatedOdds = GetClensedString(GetOdds(teamName1));
                        if (currentOddsTeam1.CompareTo(updatedOdds) != 0)
                        {
                            listBoxQuerySubmitted.Items.Add("Odds updated for " + teamName1 + " to " + updatedOdds + " from " + currentOddsTeam1);
                            currentOddsTeam1 = updatedOdds;
                            oddsRecords.Add(new OddsRecord(teamName1, updatedOdds));
                        }

                        updatedOdds = GetClensedString(GetOdds(teamName2));
                        if (currentOddsTeam2.CompareTo(updatedOdds) != 0)
                        {
                            listBoxQuerySubmitted.Items.Add("Odds updated for " + teamName2 + " to " + updatedOdds + " from " + currentOddsTeam2);
                            currentOddsTeam2 = updatedOdds;
                            oddsRecords.Add(new OddsRecord(teamName2, updatedOdds));
                        }
                    }
                    else
                    {
                        listBoxQuerySubmitted.Items.Add("Game over");
                    }
                }));
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void UpdateOdds(string teamName, string currentOdds)
        {
            string updatedOdds = GetClensedString(GetOdds(teamName));
            if (currentOdds.CompareTo(updatedOdds) != 0)
            {
                listBoxQuerySubmitted.Items.Add("Odds updated for " + teamName + " to " + updatedOdds + " from " + currentOdds);
                currentOdds = updatedOdds;
                oddsRecords.Add(new OddsRecord(teamName, updatedOdds));
            }
        }

        private string GetOdds(string teamName)
        {

            IHTMLElementCollection liCollection = ((HTMLDocument)(mainWebBrowser.Document)).getElementsByTagName("li");
            foreach (IHTMLElement elem in liCollection)
            {
                if (elem.innerHTML != null)
                {
                    if ((elem.innerHTML.ToUpper().Contains("MONEYLINE"))|| (elem.innerHTML.ToUpper().Contains("MATCH WINNER")))
                    {
                        List<string> processedWebSource = GetProcessedWebSource(elem.innerHTML);


                        for (int i = 0; i < processedWebSource.Count; i++)
                        {
                            if (processedWebSource[i].Replace("&nbsp;", "").TrimStart().TrimEnd().ToUpper().CompareTo(teamName.Replace("&nbsp;", "").TrimStart().TrimEnd().ToUpper()) == 0)
                            {
                                if (processedWebSource.Count >= i + 1)
                                {
                                    return processedWebSource[i + 1];
                                }
                            }
                        }
                        break;
                    }
                }
            }
            return "";
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
                if (Validate(processedWebSource[index + 1], processedWebSource[index + 2], teamA, teamB))
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
                        return fullName.TrimEnd().TrimStart();
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
                    if (s.IndexOf(input, StringComparison.OrdinalIgnoreCase) >= 0)  //Case insensitive CONTAINS
                    {
                        string[] delimiter = new string[] { "vs", "@" };
                        string[] tokens = s.Split(delimiter, StringSplitOptions.None);

                        foreach (string token in tokens)
                        {
                            if (token.IndexOf(input, StringComparison.OrdinalIgnoreCase) >= 0)
                                return token;
                        }
                    }
                }
            }
            return "";
        }

        private bool Validate(string webInputTeamName, string webInputRate, string userTeamNameA, string userTeamNameB)
        {
            if ((webInputTeamName.ToUpper().CompareTo(userTeamNameA.ToUpper()) != 0) &&
                (webInputTeamName.ToUpper().CompareTo(userTeamNameB.ToUpper()) != 0))
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
            string dateTime = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss");
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
                //mainWebBrowser.Source = link.ToUri();
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
                if (c == '<')
                {
                    tempBuffer = tempBuffer.TrimStart();
                    tempBuffer = tempBuffer.TrimEnd();
                    if (tempBuffer.Length > 0)
                    {
                        processedWebSource.Add(tempBuffer);
                        tempBuffer = "";
                    }
                    isInTag = true;
                }
                else if (c == '>')
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
            if (tempBuffer.Length > 0)
            {
                processedWebSource.Add(tempBuffer);
            }

            return processedWebSource;
        }

        private string readWebPageSource()
        {
            //string html = mainWebBrowser.ExecuteJavascriptWithResult("document.getElementsByTagName('html')[0].innerHTML");
            //return html;

            mshtml.HTMLDocumentClass dom = (mshtml.HTMLDocumentClass)mainWebBrowser.Document;
            return dom.body.innerHTML;
        }

        private string GetClensedString(string input, string replacementText = "")
        {
            return System.Text.RegularExpressions.Regex.Replace(input.Replace("&nbsp;", "").TrimStart().TrimEnd(), @"[^0-9a-zA-Z\-]+", replacementText);
        }

        private void monitorButton_Click(object sender, RoutedEventArgs e)
        {
            if (monitorButton.Content.ToString() == "Start")
            {
                IHTMLElement couponHTMLElement = ((HTMLDocument)mainWebBrowser.Document).getElementById("coupon");
                if (couponHTMLElement != null)
                {
                    string header = GetHeaderInCoupon(couponHTMLElement.innerHTML);

                    string[] teamsTokens = header.Split(new string[] { "VS", "Vs", "vs", "vS", "@" }, System.StringSplitOptions.None);

                    if (teamsTokens.Length == 2)
                    {
                        teamName1 = teamsTokens[0].Replace("&nbsp;", "").TrimStart().TrimEnd();
                        textBoxTeam1.Text = teamName1;
                        teamName2 = teamsTokens[1].Replace("&nbsp;", "").TrimStart().TrimEnd();
                        textBoxTeam2.Text = teamName2;
                        timer.Start();
                        timerReadTeamNames.Stop();
                    }
                }

                monitorButton.Content = "Stop";
            }
            else
            {
                timer.Stop();
                monitorButton.Content = "Start";
            }
        }

        private void timerElapsedReadGameLog(object sender, ElapsedEventArgs e)
        {
            Dispatcher.Invoke(new Action(() =>
            {
                if (!isGameOver)
                {
                    IHTMLElement scoreboardHTMLDoc = ((HTMLDocument)mainWebBrowser.Document).getElementById("scoreboard");
                    if (scoreboardHTMLDoc.innerHTML != null)
                    {
                        if (scoreboardHTMLDoc != null)
                        {
                            gameLog = GetProcessedWebSource(scoreboardHTMLDoc.innerHTML);
                            if (gameLog.Count > 3)
                            {
                                if (gameLog[1].TrimStart().TrimEnd().ToUpper().CompareTo("FINAL") == 0)
                                {
                                    isGameOver = true;
                                    timerReadGameLog.Stop();
                                    //WriteLogs();
                                    this.Close();
                                }
                            }
                        }
                    }
                }
            }));
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            sqlconn = new SQLiteConnection("Data Source=" + GetDatabaseLocation() + GetDatabaseName() + ".sqlite;Version=3;New=True;Compress=True;");
            sqlconn.Open();
            SQLiteCommand createQ = new SQLiteCommand("CREATE TABLE IF NOT EXISTS sportsLiveRates( DateTime TEXT, Team TEXT, Rate TEXT)", sqlconn);
            createQ.ExecuteNonQuery();

            foreach (OddsRecord oddsRecord in oddsRecords)
            {
                string dateTime = oddsRecord.dateTime.ToString("yyyy/MM/dd HH:mm:ss");
                string insertCommand = @"INSERT INTO sportsLiveRates (DateTime, Team, Rate) VALUES ("" " + dateTime + @" "","" " + oddsRecord.teamName + @" "",""" + oddsRecord.odds + @""");";
                SQLiteCommand insertSQL = new SQLiteCommand(insertCommand, sqlconn);
                try
                {
                    insertSQL.ExecuteNonQuery();
                    //listBoxQuerySubmitted.Items.Add(insertSQL.CommandText.ToString());
                }
                catch (Exception ex)
                {
                    throw new Exception(ex.Message);
                }
            }
            sqlconn.Close();

            //Write game log to text file.
            using (StreamWriter writer = new StreamWriter(GetDatabaseLocation() + GetDatabaseName() + ".txt"))
            {
                foreach (string str in gameLog)
                {
                    writer.WriteLine(str);
                }
            }
        }
    }
}
