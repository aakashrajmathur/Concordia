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
            if ((this.teamAName.CompareTo(other.teamAName) == 0) && 
                (this.teamBName.CompareTo(other.teamBName) == 0) && 
                (this.sport.CompareTo(other.sport) == 0))
            {
                return 0;
            }
            else {
                return -1;
            }               
        }

        public bool Equals(Game other)
        {
            return ((this.teamAName.CompareTo(other.teamAName) == 0) && 
                    (this.teamBName.CompareTo(other.teamBName) == 0) && 
                    (this.sport.CompareTo(other.sport) == 0));
        }
    }
}
