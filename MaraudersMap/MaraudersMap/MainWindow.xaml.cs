using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Timers;
using System.Windows;

namespace MaraudersMap
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        const string filepathToDobby = @"C:\src\Concordia\DobbyTheOddsElf\DobbyTheOddsElf\bin\Debug\DobbyTheOddsElf.exe";                                                                                                    
        const int TIMER_DURATION = 12000;
        System.Timers.Timer timer;

        List<Game> currentLiveEvents;
        List<TrackedEvent> currentTrackedEvents;
        List<TrackedEvent> completedEvents;
        HashSet<string> sportsCategories;
        int counter;

        public MainWindow()
        {
            timer = new System.Timers.Timer(TIMER_DURATION);
            timer.Elapsed += new ElapsedEventHandler(TimerElapsed);
            currentLiveEvents = new List<Game>();
            currentTrackedEvents = new List<TrackedEvent>();
            completedEvents = new List<TrackedEvent>();
            string[] sportsList = { "Baseball", "Soccer", "Tennis", "Basketball", "UFC/MMA", "Golf", "Football", "Hockey", "Cricket", "Horse Racing ", "Boxing", "Motor Sports", "E-Sports", "Olympic Games", "Politics", "Rugby League", "Darts", "Snooker", "Volleyball", "Beach Volleyball", "Handball", "Winter Sports" };
            sportsCategories = new HashSet<string>(sportsList);
            counter = 1;
            InitializeComponent();
        }

        private void TimerElapsed(object sender, ElapsedEventArgs e)
        {
            //MessageBox.Show(currentLiveEvents.Count + " original games");
            //timer.Stop();

            Dispatcher.Invoke(new Action(() =>
            {
                //TODO: scrap the webpage for current live events, compare with existing -> create list of new events, list of ended events, foreach new event -> create a dobby, foreach eneded event -> do analysis & add to records.
                List<Game> updatedLiveEvents = GetLiveEvents();
                
                //MessageBox.Show(updatedLiveEvents.Count + " updated games");
                listBoxLiveEvents.Items.Clear();
                foreach(Game game in updatedLiveEvents)
                {
                    listBoxLiveEvents.Items.Add(game.sport + " - " + game.teamAName + " vs " + game.teamBName);
                } 

                List<Game> newGames = GetNewGames(updatedLiveEvents, currentLiveEvents);
                //MessageBox.Show(newGames.Count + " games just started.");
                List<Game> endedGames = GetEndedGames(updatedLiveEvents, currentLiveEvents);
                //MessageBox.Show(endedGames.Count + " games ended.");

                currentLiveEvents = new List<Game>(updatedLiveEvents);

                foreach (Game game in newGames)
                {
                    string databaseName = GetDatabaseName(game);
                    //Start Dobby:
                    ProcessStartInfo startInfo = new ProcessStartInfo
                    {
                        Arguments = @"""" + game.teamAName + @"""" + @" """ + game.teamBName + @"""" + @" """ + game.linkToLiveWebPage.ToString() + @"""" + @" """ + databaseName + @"""",
                        FileName = filepathToDobby
                    };
                    Process process = Process.Start(startInfo);
                    TrackedEvent trackedEvent = new TrackedEvent(game, process, databaseName);
                    currentTrackedEvents.Add(trackedEvent);               
                }
                foreach(Game game in endedGames)
                {
                    MessageBox.Show("number of ended games = " + endedGames.Count);
                    listBoxFinishedEvents.Items.Add(game.sport + " - " + game.teamAName + " vs " + game.teamBName);
                    TrackedEvent trackedEvent = GetTrackedEventGivenTheGame(game);
                    
                    //Run Analysis: 
                    completedEvents.Add(trackedEvent);
                    
                    trackedEvent.processHandle.Kill();
                    currentTrackedEvents.Remove(trackedEvent);
                }
                counterLabel.Content = counter++; 
            }));
        }

        private TrackedEvent GetTrackedEventGivenTheGame(Game game)
        {
            foreach (TrackedEvent t in currentTrackedEvents)
            {
                if (t.game.CompareTo(game) == 0)
                {
                    return t;
                }
            }
            return null;
        }

        private string GetClensedString(string input, string replacementText = "")
        {
            return System.Text.RegularExpressions.Regex.Replace(input, @"[^0-9a-zA-Z]+", replacementText);
        }

        private string GetQuotedString(string input)
        {
            return @"""" + input + @"""";
        }

        private string GetDatabaseName(Game game)
        {
            string databaseName = DateTime.Now.ToString("yyyy_MM_dd_HH_mm_ss_");

            databaseName += GetClensedString(game.sport) + "_" + GetClensedString(game.teamAName) + "_vs_" + GetClensedString(game.teamBName);
            databaseName = databaseName.Replace(" ", "");
            return GetQuotedString(databaseName);
        }

        private List<Game> GetNewGames(List<Game> newList, List<Game> existingList)
        {
            List<Game> result = new List<Game>(newList); 
            foreach(Game game in existingList)
            {
                if (result.Contains(game))
                {
                    result.Remove(game);
                }
            }
            return result;
        }

        private List<Game> GetEndedGames(List<Game> newList, List<Game> existingList)
        {
            //Games present in current but not in updated:

            List<Game> result = new List<Game>(existingList);
            foreach(Game game in newList)
            {
                if (result.Contains(game))
                {
                    result.Remove(game);
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
                if (currentKey > currentIndex)
                    return new Uri(firstHalf + secondHalf);


                secondHalf = linksEmbeded[currentKey];
            }
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
