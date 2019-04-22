using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SoundCloudGet
{
    class SoundCloudAPICalls
    {

        private string _baseUrl = "https://api.soundcloud.com/";
        private string _key = "";

        public SoundCloudAPICalls(string urlBase, string key)
        {
            _baseUrl = urlBase;
            _key = key;
        }

        public string MakeCallSearchByGenre(string genre)
        {
            return String.Format("{0}tracks?genres={1}&{2}", _baseUrl, genre, _key);
        }

        public string MakeCallResolveURL(string url)
        {
            return String.Format("{0}resolve?url={1}&{2}", _baseUrl, url, _key);
        }
        public string MakeCallStream(string trackNumber)
        {
            return String.Format("{0}tracks/{1}?allow_redirects=true&{2}", _baseUrl, trackNumber, _key);
        }
        public string MakeCallStreamForSong(string trackNumber)
        {
            return String.Format("{0}i1/tracks/{1}/streams?{2}", _baseUrl, trackNumber, _key);
        }
        public string MakeCallInfoForSong(string trackNumber)
        {
            return String.Format("{0}tracks/{1}?{2}", _baseUrl, trackNumber, _key);
        }
        public string MakeCallInfoForUser(string userNo)
        {
            return String.Format("{0}users/{1}?{2}", _baseUrl, userNo, _key); //, "&limit=200");
        }

        public string MakeCallInfoForUserTracks(string userNo)
        {
            return String.Format("{0}users/{1}/tracks?{2}", _baseUrl, userNo, _key);
        }

        public string MakeCallInfoForUserSets(string userNo)
        {
            return String.Format("{0}users/{1}/playlists?{2}", _baseUrl, userNo, _key);
        }

        public string MakeCallSearchByArtist(string artist)
        {
            return String.Format("{0}tracks/?artist={1}&{2}", _baseUrl, artist, _key);
        }

        public string MakeCallGetSetInfo(string artist, string setName)
        {
            return String.Format("{0}/{1}/sets/{2}?{3}", _baseUrl, artist, setName, _key);
        }

        public string MakePagedCall(string url, int limit = 200, int linked_partitioning = 1, int offset = 0)
        {

            return String.Format("{0}&limit={1}{2}{3}", 
                url, 
                limit, 
                linked_partitioning == 1 ? "&linked_partitioning=1" : "",
                offset != 0 ? "&offset="+offset : "");
        }

    }
}
