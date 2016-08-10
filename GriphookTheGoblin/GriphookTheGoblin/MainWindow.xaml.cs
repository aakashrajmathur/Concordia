using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

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
            
            labelSliderValue.Content = 0;
        }

        private void RunAnalysis()
        {
            int margin = int.Parse(labelSliderValue.Content.ToString());
            List<ResultStatistics> stats = new List<ResultStatistics>();
            foreach (string filePath in Directory.GetFiles(@"C:\Data\Ended"))
            {
                List<Record> allRecords = new DBComm().GetAllRecords(filePath);
                allRecords.Sort();

                if (RunCompliance(allRecords))
                {
                    stats.Add(new ResultStatistics(allRecords, filePath));
                }
                else
                {
                    //Data not valid to run analysis:
                }
            }
            int counterTotal = 0; int profitCounter = 0; int lossCounter = 0;
            foreach (ResultStatistics stat in stats)
            {
                if (stat.initialOddsDifference > 400)
                    continue;

                if (stat.teamAinitialOdds < 0 && stat.teamBinitialOdds < 0)
                    continue;

                if (stat.teamAinitialOdds < -200)
                    continue;

                if (stat.teamBinitialOdds < -200)
                    continue;


                string common = "Initial odds diff = " + stat.initialOddsDifference + ", # of Records = " + stat.totalNumberOfRecords + " Duration = " + stat.durationOfEvent;
                ListBoxItem item = new ListBoxItem { Content = ++counterTotal + ". " + common, Background = Brushes.Gray };
                listBoxResults.Items.Add(item);

                string teamAStats = stat.teamAName + "" + stat.teamAinitialOdds + " Max odds = " + stat.teamBMaxProfitOrClosestLossOdds; // + " Perc = " + stat.teamAMaxProfitOrClosestLossPerc;
                string teamBStats = stat.teamBName + "" + stat.teamBinitialOdds + " Max odds = " + stat.teamAMaxProfitOrClosestLossOdds; // + " Perc = " + stat.teamBMaxProfitOrClosestLossPerc;

                item = new ListBoxItem { Content = teamAStats };

                if (IsProfitable(stat.teamAinitialOdds, stat.teamBMaxProfitOrClosestLossOdds, margin) &&
                    IsProfitable(stat.teamBinitialOdds, stat.teamAMaxProfitOrClosestLossOdds, margin))
                //((stat.teamBinitialOdds * (1.0 + (margin / 100.0))) < stat.teamAMaxProfitOrClosestLossOdds))
                {
                    item.Background = Brushes.LightGreen;
                    profitCounter++;
                }
                else
                {
                    item.Background = Brushes.Salmon;
                    lossCounter++;
                }
                listBoxResults.Items.Add(item);

                item = new ListBoxItem { Content = teamBStats };

                listBoxResults.Items.Add(item);
            }

            labelInfo.Content = "Margin: " + margin + "\tTotal: " + counterTotal + "\tProfit: " + profitCounter + "\t\tLoss: " + lossCounter + "\tSuccess Rate: " + (profitCounter * 100.0 / counterTotal);
            //MessageBox.Show();
        }

        private bool IsProfitable(int initialOdds, int maxOdds, int margin)
        {
            //return maxOdds * (1.0 + (margin / 100.0)) > initialOdds;
            if (initialOdds < 0)
            {
                return maxOdds > (-1 * initialOdds) * (1.0 + (margin / 100.0));
            }
            else
            {
                return maxOdds > (-1 * (initialOdds * (1.0 + (margin / 100.0))));
            }
        }

        private bool RunCompliance(List<Record> allRecords)
        {
            List<string> teams = GetTeamNames(allRecords);
            if (teams.Count != 2)
            {
                return false;
                //throw new Exception("There must be exactly two teams for this fucntion to work!");
            }
            if (allRecords.Count <= 0)
            {
                return false;
                //Nothing to eval
            }
            //Duration must be an hour or more: 
            if (GetDurationOfEvent(allRecords) < (60 * 60))
            {
                return false;
            }

            return true;
        }

        private int GetDurationOfEvent(List<Record> allRecords)
        {
            if (allRecords.Count > 0)
            {
                DateTime start = allRecords[0].datetime;
                DateTime end = allRecords[allRecords.Count - 1].datetime;
                double diffInSeconds = (end - start).TotalSeconds;
                return (int)diffInSeconds;
            }
            return 0;
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

        private void buttonRunAnalysis_Click(object sender, RoutedEventArgs e)
        {
            RunAnalysis();
        }

        private void sliderProfitPerc_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            labelSliderValue.Content = sliderProfitPerc.Value;
        }
    }
}