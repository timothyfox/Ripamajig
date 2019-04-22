using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace SoundCloudGet.DataModels
{
    public class Track
    {
        public string Id;

        public DateTime CreatedAt;

        public DateTime RippedAt;

        public long Size;

        public string Description;

        public long Playtime;

        public string Title;

        public string Uri;

        public string Artist;

        public Track()
        {
        }

        public Track(string i, DateTime c, DateTime r, long s, string d, long p, string t, string u, string a)
        {
            Id = i;
            CreatedAt = c;
            RippedAt = r;
            Size = s;
            Description = d;
            Playtime = p;
            Title = t;
            Uri = u;
            Artist = a;
        }

        public string ToString(bool description = false)
        {
            return String.Format("{0} - {1}, [{5} MB] \n {2} - {3}\n{4}", Artist, Title, CreatedAt.ToString("yyy/MM/dd"), Uri, description ? Description : "", GetSizeInMegabytes());
        }

        public double GetSizeInMegabytes()
        {
            return Helpers.SizeHelper.GetMegaBytesFromDuration(Playtime);
        }
    }
}
