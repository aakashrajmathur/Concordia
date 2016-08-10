using System;


namespace GriphookTheGoblin
{
    public class Record : IComparable<Record>
    {
        public DateTime datetime;
        public string teamName;
        public int americanRate;
        
        public Record(DateTime dateTime, string teamName, int americanRate)
        {
            this.datetime = dateTime;
            this.teamName = teamName;
            this.americanRate = americanRate;
        }
        
        public int CompareTo(Record other)
        {
            return DateTime.Compare(datetime, other.datetime);
        }

        public override string ToString()
        {
            return datetime.ToString("u") + " , Team = " + teamName + " , American odds = " + americanRate.ToString();
        }
    }
}
