using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;

namespace TuneSyncAPI
{

    public class Tunes
    {
        internal AppDB db { get; set; }

        public List<Tune> tunes { get; set; }

        public Tunes()
        {
            tunes = new List<Tune>();
        }

        public Tunes(List<Tune> tuneList)
        {
            tunes = tuneList;
        }

    }

    public class Tune
    {

        public Tune(DbDataReader reader)
        {
            Id = reader.GetInt32(0);
            Path = reader.GetString(1);
            Hash = reader.GetString(2);
            Artist = reader.GetValue(3) is DBNull ? string.Empty : reader.GetString(3);
            Title = reader.GetValue(4) is DBNull ? string.Empty : reader.GetString(4);
        }

        public int Id { get; }
        public string Path { get; }
        public string Hash { get; }
        public string Title { get; }
        public string Artist { get; }
    }
}
