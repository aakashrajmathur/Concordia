using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GriphookTheGoblin
{
    class DBComm
    {
        public List<Record> GetAllRecords(string filePathOfDatabase)
        {
            List<Record> allRecords = new List<Record>();
            using (SQLiteConnection connection = new SQLiteConnection("Data Source= " + filePathOfDatabase))
            {
                connection.Open();
                using (SQLiteCommand command = new SQLiteCommand(connection))
                {
                    command.CommandText = "SELECT * FROM sportsLiveRates";
                    SQLiteDataReader sdr = command.ExecuteReader();

                    while (sdr.Read())
                    {
                        allRecords.Add(new Record(DateTime.Parse(sdr.GetString(0)), sdr.GetString(1), int.Parse(sdr.GetString(2))));
                    }
                    sdr.Close();
                }
                connection.Close();
            }
            return allRecords;
        }

    }
}