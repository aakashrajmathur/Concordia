using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Timers;
using System.Windows;
using System.Windows.Input;

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

        public MainWindow()
        {
            InitializeComponent();
            urlTextBox.Text = "https://sports.bovada.lv/live-betting/event/2466820";
            NavigateMainWebBrowser();
            timer = new Timer(TIMER_DURATION);
            timer.Elapsed += new ElapsedEventHandler(timerElapsed);
            CreateSQLiteDatabase();
            textBoxTeam1.Text = "Australia";
            textBoxTeam2.Text = "West Indies";
        }

        private void CreateSQLiteDatabase()
        {
            sqlconn = new SQLiteConnection("Data Source=c:\\sports\\liveRates.sqlite;Version=3;New=True;Compress=True;");
            sqlconn.Open();

            SQLiteCommand createQ = new SQLiteCommand("CREATE TABLE IF NOT EXISTS sportsLiveRates( DateTime TEXT, Team TEXT, Rate INT)", sqlconn);
            createQ.ExecuteNonQuery();
            sqlconn.Close();
        }

        private void timerElapsed(object sender, ElapsedEventArgs e)
        {
            Dispatcher.Invoke(new Action(() =>
            {
                ReportRates();
                string currentRateToNotify = GetCurrentRate();
                if((currentRateToNotify.Length > 0) && (thresholdValueTextBox.Text.ToString().Length > 0))
                {
                    int iCurrentRate = int.Parse(currentRateToNotify);
                    if(thresholdComboBox.SelectedIndex == 0)
                    {
                        //Less than: 
                        if(iCurrentRate <= int.Parse(thresholdValueTextBox.Text.ToString()))
                        {
                            MessageBox.Show("Alert rate dropped to below " + thresholdValueTextBox + ", Current Rate = " + iCurrentRate.ToString());
                        }
                        
                    }
                    else if (thresholdComboBox.SelectedIndex == 1)
                    {
                        //Less than: 
                        if (iCurrentRate >= int.Parse(thresholdValueTextBox.Text.ToString()))
                        {
                            MessageBox.Show("Alert rate incresed more than " + thresholdValueTextBox + ", Current Rate = " + iCurrentRate.ToString());
                        }

                    }
                }

            }));
        }

        private void ReportRates()
        {
           
            List<string> processedWebSource = GetProcessedWebSource(readWebPageSource());

            if (processedWebSource.Contains(metricTextBox.Text.ToString()))
            {
                int index = processedWebSource.FindIndex(new Predicate<string>(item => item == metricTextBox.Text.ToString()));
                if ((processedWebSource[index + 1] == "Australia") || (processedWebSource[index + 1] == "West Indies"))
                {
                    int test = -1;
                    if (int.TryParse(processedWebSource[index + 2], out test))
                    {
                        CreateRow(processedWebSource[index + 1], int.Parse(processedWebSource[index + 2]));
                    }
                }
                if ((processedWebSource[index + 3] == "Australia") || (processedWebSource[index + 3] == "West Indies"))
                {
                    int test = -1;
                    if (int.TryParse(processedWebSource[index + 2], out test))
                    {
                        CreateRow(processedWebSource[index + 3], int.Parse(processedWebSource[index + 4]));
                    }
                }
            }
        }

        public void CreateRow(string team, int rate)
        {
            sqlconn.Open();
            
            string dateTime = DateTime.Now.ToString("MM/dd/yyyy h:mm tt");
            SQLiteCommand insertSQL = new SQLiteCommand(@"INSERT INTO sportsLiveRates (DateTime, Team, Rate) VALUES ("" " + dateTime + @" "","" " + team + @" ""," + rate + ");", sqlconn);
            try
            {
                insertSQL.ExecuteNonQuery();
                Console.WriteLine(insertSQL.CommandText.ToString());
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
            sqlconn.Close();
        }

        private string GetCurrentRate()
        {
            List<string> processedWebSource = GetProcessedWebSource(readWebPageSource());

            if (processedWebSource.Contains(metricTextBox.Text.ToString()))
            {
                int index = processedWebSource.FindIndex(new Predicate<string>(item => item == metricTextBox.Text.ToString()));
                if (processedWebSource[index + 1] == teamTextBox.Text.ToString())
                {
                    return processedWebSource[index + 2];
                }
                else if (processedWebSource[index + 3] == teamTextBox.Text.ToString())
                {
                    return processedWebSource[index + 4];
                }
            }
            return "";
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
                mainWebBrowser.Navigate(urlTextBox.Text.ToString());
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error in navigating URL " + Environment.NewLine + "Details: " + ex.Message);
            }
        }

        private void button_Click(object sender, RoutedEventArgs e)
        {
            string webPageSource = readWebPageSource();
            //MessageBox.Show(webPageSource);
            List<string> processedWebSource = GetProcessedWebSource(webPageSource);
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


//Testing GIT