﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MaraudersMap
{
    class Game: IComparable <Game>
    {
        string teamAName;
        string teamBName;
        Uri linkToLiveWebPage;
        string sport;
        DateTime startDateTime; 

        public Game(string teamAName, string teamBName, Uri linkToLiveWebPage, string sport, DateTime startDateTime)
        {
            this.teamAName = teamAName;
            this.teamBName = teamBName;
            this.linkToLiveWebPage = linkToLiveWebPage;
            this.sport = sport;
            this.startDateTime = startDateTime;  
        }

        public int CompareTo(Game other)
        {
            return this.teamAName.CompareTo(other.teamAName) + 
                this.teamAName.CompareTo(other.teamAName) + 
                this.linkToLiveWebPage.ToString().CompareTo(other.linkToLiveWebPage.ToString());
        }
    }
}
