using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace MaraudersMap
{
    class Game: IComparable <Game>, IEquatable<Game>
    {
        public string teamAName;
        public string teamBName;
        public Uri linkToLiveWebPage;
        public string sport;

        public Game(string teamAName, string teamBName, Uri linkToLiveWebPage, string sport)
        {
            this.teamAName = teamAName;
            this.teamBName = teamBName;
            this.linkToLiveWebPage = linkToLiveWebPage;
            this.sport = sport;
        }

        public int CompareTo(Game other)
        {
            
            int result =  this.teamAName.CompareTo(other.teamAName) + 
                this.teamAName.CompareTo(other.teamAName) + 
                this.linkToLiveWebPage.ToString().CompareTo(other.linkToLiveWebPage.ToString()) +
                this.sport.CompareTo(other.sport);
            //MessageBox.Show("from compare, result = " + result);
            return result;
        }

        public bool Equals(Game other)
        {
            int result = this.teamAName.CompareTo(other.teamAName) +
                this.teamAName.CompareTo(other.teamAName) +
                this.linkToLiveWebPage.ToString().CompareTo(other.linkToLiveWebPage.ToString()) +
                this.sport.CompareTo(other.sport);
            //MessageBox.Show("from equals, result = " + result);
            return result == 0;
        }
    }
}
