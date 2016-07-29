using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace MaraudersMap
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        const string filepathToDobby = @"C:\src\Concordia\DobbyTheOddsElf\DobbyTheOddsElf\bin\Debug\DobbyTheOddsElf.exe";                                                                                                    
        const int TIMER_DURATION = 6000;
        System.Timers.Timer timer;

        List<Game> currentLiveEvents;
        HashSet<string> sportsCategories; 
        
        public MainWindow()
        {
            timer = new System.Timers.Timer(TIMER_DURATION);
            timer.Elapsed += new ElapsedEventHandler(TimerElapsed);
            currentLiveEvents = new List<Game>();
            string[] sportsList = { "Baseball", "Soccer", "Tennis", "Basketball", "UFC/MMA", "Golf", "Football", "Hockey", "Cricket", "Horse Racing ", "Boxing", "Motor Sports", "E-Sports", "Olympic Games", "Politics", "Rugby League", "Darts", "Snooker", "Volleyball", "Beach Volleyball", "Handball", "Winter Sports" };
            sportsCategories = new HashSet<string>(sportsList);

            InitializeComponent();
        }

        private void TimerElapsed(object sender, ElapsedEventArgs e)
        {
            MessageBox.Show(currentLiveEvents.Count + " original games");
            //timer.Stop();

            Dispatcher.Invoke(new Action(() =>
            {
                //TODO: scrap the webpage for current live events, compare with existing -> create list of new events, list of ended events, foreach new event -> create a dobby, foreach eneded event -> do analysis & add to records.
                List<Game> updatedLiveEvents = GetLiveEvents();
                MessageBox.Show(updatedLiveEvents.Count + " updated games");

                List<Game> newGames = GetDiff(updatedLiveEvents, currentLiveEvents);
                MessageBox.Show(newGames.Count + " games just started.");
                List<Game> endedGames = GetDiff(currentLiveEvents, updatedLiveEvents);
                MessageBox.Show(endedGames.Count + " games ended.");
                
                foreach(Game game in newGames)
                {
                    string databaseName = GetDatabaseName();
                    //Start Dobby:
                    ProcessStartInfo startInfo = new ProcessStartInfo
                    {
                        Arguments = @"""" + game.teamAName + @"""" + @" """ + game.teamBName + @"""" + @" """ + game.linkToLiveWebPage.ToString() + @"""" + @" """ + databaseName + @"""",
                        FileName = filepathToDobby
                    };
                    Process process = Process.Start(startInfo);
                    TrackedEvent trackedEvent = new TrackedEvent(game, process, databaseName);                     
                }
                currentLiveEvents = new List<Game>(updatedLiveEvents);
            }));
        }

        private string GetDatabaseName()
        {
            return DateTime.Now.ToString("yyyy_MM_dd_HH_mm_ss");
        }

        private List<Game> GetDiff(List<Game> listSource, List<Game> listToCompare)
        {
            List<Game> result = new List<Game>();
            foreach(Game game in listSource)
            {
                if (!listToCompare.Contains(game))
                {
                    result.Add(game);
                }
            }
            return result;
        }

        private List<Game> GetLiveEvents()
        {
            List<Game> liveEvents = new List<Game>();
            string webPageSource = ReadWebPageSource();

            //Remove all new line characters: 
            webPageSource = webPageSource.Replace("\n", "");

            int startingIndex = webPageSource.IndexOf("Live Now") - 1;
            int endingIndex = webPageSource.IndexOf("Upcoming") + 9;

            webPageSource = webPageSource.Substring(startingIndex, (endingIndex - startingIndex));
            
            Dictionary<int, string> textDisplayed = GetTextDisplayedWithIndex(webPageSource);
            Dictionary<int, string> linksEmbeded = GetLinksEmbeddedWithIndex(webPageSource);
            //foreach (int i in linksEmbeded.Keys)
            //{
            //    listBoxLiveEvents.Items.Add(i + " = " + linksEmbeded[i]);
            //}

            string[] supportedSports = { "Baseball", "Tennis", "Basketball", "Football", "Hockey", "Cricket" };

            string currentSport = "";

            for (int i = 0; i < textDisplayed.Keys.Count; i++)  // in textDisplayed.Keys)
            {
                int currentKey = textDisplayed.Keys.ElementAt(i);
                if (sportsCategories.Contains(textDisplayed[currentKey]))
                {
                    currentSport = textDisplayed[currentKey];
                }
                //listBoxFinishedEvents.Items.Add(currentKey + " = " + textDisplayed[currentKey] + " , current sport = " + currentSport);

                if (supportedSports.Contains(currentSport) && ((textDisplayed[currentKey].CompareTo("@") == 0) || (textDisplayed[currentKey].ToUpper().CompareTo("VS") == 0)))
                {
                    string teamA = textDisplayed[textDisplayed.Keys.ElementAt(i - 1)];
                    string teamB = textDisplayed[textDisplayed.Keys.ElementAt(i + 1)];
                    Uri link = GetLinkToGame(currentKey, linksEmbeded);
                    Game game = new MaraudersMap.Game(teamA, teamB, link, currentSport);
                    liveEvents.Add(game);
                }
            }
            return liveEvents;
        }

        private Uri GetLinkToGame(int currentIndex, Dictionary<int, string> linksEmbeded)
        {

            string firstHalf = @"https://sports.bovada.lv/";
            //Format of the link = live-betting/event/2486513

            string secondHalf = ""; 
            foreach(int currentKey in linksEmbeded.Keys)
            {
                secondHalf = linksEmbeded[currentKey];
                if (currentKey > currentIndex)
                    break; 
            }
            //MessageBox.Show(firstHalf + secondHalf);
            return new Uri(firstHalf + secondHalf);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            mainPageWebBrowser.Navigate(new Uri("https://sports.bovada.lv/live-betting")); 
            timer.Start();
        }

        private string ReadWebPageSource()
        {
            mshtml.HTMLDocumentClass dom = (mshtml.HTMLDocumentClass)mainPageWebBrowser.Document;
            return dom.body.innerHTML;
        }

        private Dictionary<int, string> GetLinksEmbeddedWithIndex(string webPageSource)
        {
            Dictionary<int, string> dict = new Dictionary<int, string>();

            int startIndex = 0; 
            while (true)
            {
                startIndex = webPageSource.IndexOf("live-betting/event", startIndex+9);
                if (startIndex == -1)
                    break;
                int endIndex = webPageSource.IndexOf(@"""", startIndex);
                string toBeAdded = webPageSource.Substring(startIndex, (endIndex - startIndex));
                dict.Add(startIndex, toBeAdded);
            }
            return dict;
        }

        private Dictionary<int, string> GetTextDisplayedWithIndex(string webPageSource)
        {
            bool isInTag = false;
            string tempBuffer = "";
            Dictionary<int, string> dict = new Dictionary<int, string>();
            //DateTime startTime = DateTime.Now;
            //MessageBox.Show("starting loop");
            int updatedIndex = 0;
            for (int i = 0; i < webPageSource.ToCharArray().Length - 1; i++)
            {
                char c = webPageSource.ToCharArray()[i];

                if (c == '<')
                {
                    tempBuffer = tempBuffer.TrimStart();
                    tempBuffer = tempBuffer.TrimEnd();
                    if (tempBuffer.Length > 0)
                    {
                        dict.Add(updatedIndex, tempBuffer);
                        tempBuffer = "";
                    }
                    isInTag = true;
                }
                else if (c == '>')
                {
                    updatedIndex = i;
                    isInTag = false;
                    continue;
                }

                if (!isInTag)
                {
                    tempBuffer += c;
                }
            }
            //DateTime endTime = DateTime.Now;
            //MessageBox.Show("Done with loop" + dict.Count + "executed in "+ (endTime-startTime)  );
            tempBuffer = tempBuffer.TrimStart();
            tempBuffer = tempBuffer.TrimEnd();
            if (tempBuffer.Length > 0)
            {
                dict.Add(updatedIndex, tempBuffer);
            }
            return dict;
        }
    }
}
