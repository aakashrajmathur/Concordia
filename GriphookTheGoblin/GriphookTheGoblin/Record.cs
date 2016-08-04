using System;


namespace GriphookTheGoblin
{
    class Record : IComparable<Record>
    {
        public DateTime datetime;
        public string teamName;
        public int americanRate;
        public double decimalRate;

        public Record(DateTime dateTime, string teamName, int americanRate)
        {
            this.datetime = dateTime;
            this.teamName = teamName;
            this.americanRate = americanRate;
            this.decimalRate = GetDecimalRate(americanRate);
        }

        public double GetDecimalRate(int americanRate)
        {
            if (americanRate > 0)
                return (americanRate / 100.0) + 1.0;
            else
                return (100.0 / Math.Abs(americanRate)) + 1.0;
        }

        public int CompareTo(Record other)
        {
            return DateTime.Compare(datetime, other.datetime);
        }

        public override string ToString()
        {
            return datetime.ToString("u") + " , Team = " + teamName + " , American odds = " + americanRate.ToString() + " , Decimal odds = " + decimalRate.ToString();
        }
    }
}