using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Net;
using System.Linq;
using System.Net.Security;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Json;
using System.Security.Cryptography.X509Certificates;
using System.Web.Helpers;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;
using Microsoft.SqlServer.Server;
using Newtonsoft.Json;
using SoundCloudGet;
using SoundCloudGet.Helpers;
using SoundCloudGet.IO;
using SoundCloudGet.DataModels;
using SoundCloudGet.Services;
using ops = SoundCloudGet.GeneralOperations;

using SoundCloudGet.Helpers;
using System.Text.RegularExpressions;

class Program
{

    /// <summary>
    /// To DO:
    /// 
    /// Add &limit= , &linked_partitioning= to request URL 
    /// to get next_href for next batch of results. 
    /// </summary>

    const string SNDCLD_SEARCH_WORD = "soundcloud.com";
    const string URI_STRINGS_SETS = "sets";
    public const int SNDCLD_BIT_RATE = 128;
    public const int PROGRAM_RETRIES = 20;

    //set key to a default.
    private static string _apiKeySndcldWeb = "client_id=02gUJC0hH2ct1EGOcYXQIzRFU91c72Ea&app_version=1458743429";
    
    static string _savePath = "";
    static SoundCloudAPICalls _api;
    static List<string> _downloadFrontier;
    static ConsoleColor _defaultConsoleCol;

    static bool _dumpJSONInfo = false;
    private static bool _splash = true;
    private static int _exceptionCount = 0;

    private static event EventHandler ExceptionThrown;

    protected virtual void OnExceptionThrown(EventArgs e)
    {
        EventHandler handler = ExceptionThrown;
        handler?.Invoke(this, e);
    }

    [STAThread]
    private static void Main(string[] args)
    {
        ExceptionThrown += (o, e) =>
        {
            if (o.GetType().Equals(typeof(Exception)))
                Console.WriteLine(((Exception)o).Message);

            if (_exceptionCount++ > PROGRAM_RETRIES)
                throw new Exception(String.Format("More than {0} errors have orccurred; Terminating Program!", _exceptionCount));
        };

        Configure();

        _defaultConsoleCol = Console.ForegroundColor;

        _downloadFrontier = new List<string>();

       _api = new SoundCloudAPICalls("https://api.soundcloud.com/", _apiKeySndcldWeb);

        string trackToGetId = "";
        string userId = "";
        string trackTitle = "";
        dynamic resolveURLJson = "";

        if(_splash)
            PrintHelpText();

        if (args.Length > 0)
        {
            ProcessInitialArgs(args);
        }
        else
        {
            string userResponse = AskForSoundCloudUrl(SNDCLD_SEARCH_WORD);

            if (userResponse.ToLower().Contains("user=") || userResponse.ToLower().Contains("track="))
                BeginUserSpecificDownload(userResponse);
            else if (userResponse.Contains(".txt"))
                BeginFileDownload(userResponse);
            else
                _downloadFrontier.Add(userResponse);
        }

        DownLoadURLSInList(ref trackToGetId, ref userId, ref trackTitle, ref resolveURLJson);
        
        ops.Log("done", ConsoleColor.Blue);
        
        Main(new string[0]);
    }

    private static void Configure()
    {
        string clientId = "", appVersion = "";
        try
        {
            ops.Log("Loading configuration...", ConsoleColor.Yellow);
            ops.Log("Looking for:", ConsoleColor.Yellow);
            ops.Log("SavePath       - Dir to save files  ", ConsoleColor.Yellow);
            ops.Log("ClientId       - Current Soundcloud web client Id", ConsoleColor.Yellow);
            ops.Log("AppVersionId   - Current Soundcloud web client version", ConsoleColor.Yellow);


            var settings = System.Configuration.ConfigurationSettings.AppSettings;

            if (String.IsNullOrEmpty(settings["SavePath"])) throw new Exception("Need to provide the SavePath!");
            _savePath = settings["SavePath"];

            if (String.IsNullOrEmpty(settings["ClientId"])) throw new Exception("Need to provide the Client Id!");
            clientId = settings["ClientId"];

            if (String.IsNullOrEmpty(settings["AppVersionId"])) throw new Exception("Need to provide the App Version ID!");
            appVersion = settings["AppVersionId"];

            _apiKeySndcldWeb = String.Format("client_id={0}&app_version={1}", clientId,  appVersion);
        }
        catch (Exception ex)
        {
            ops.Log(ex.Message, ConsoleColor.DarkYellow);
            ops.Log("Could not load config! Cannot continue", ConsoleColor.Red);

            Console.Read();
        }
    }


    private static void PrintHelpText()
    {
        _splash = false;
        var green = ConsoleColor.DarkGreen;

        var lastCompile = System.IO.File.GetLastWriteTime(Assembly.GetExecutingAssembly().Location);

        ops.Log(String.Format("==============================================================="), green);
        ops.Log(String.Format("===================== +++ RIPAMAJIG +++ ======================="), green);
        ops.Log(String.Format("==========    For the aquisition and caching of      =========="), green);
        ops.Log(String.Format("==========            Soundcloud media.              =========="), green);
        ops.Log(String.Format("==========-------------------------------------------=========="), green);
        ops.Log(String.Format("==========   By derpest - [{0}]   ==========", lastCompile), green);
        ops.Log(String.Format("==========-------------------------------------------=========="), green);
        ops.Log(String.Format("==============================================================="), green);

        PrintSettings();
    }

    private static void PrintSettings()
    {
        var col2 = ConsoleColor.Cyan;
        var col1 = ConsoleColor.DarkCyan;

        ops.Log(String.Format("==============================================================="), col1);
        ops.Log(String.Format("==================== Application Settings ====================="), col1);
        ops.Log(String.Format("\t {0} :\n {1} ", "Soundcloud API Key", _apiKeySndcldWeb), col2);
        ops.Log(String.Format("\t {0} \t: {1} ", "Save Dir", _savePath), col2);
        ops.Log(String.Format("\t {0} : {1} ", "URL validation", SNDCLD_SEARCH_WORD), col2);
        ops.Log(String.Format("==============================================================="), col1);
        
    }

    private static void DownLoadURLSInList(ref string trackToGetId, ref string userId, ref string trackTitle, ref dynamic resolveURLJson)
    {
        foreach (string s in _downloadFrontier)
        {
            var urlToGet = RemoveQueryParameters(s);

            if (urlToGet.Contains(URI_STRINGS_SETS))
            {
                DownloadSet(urlToGet, ref trackToGetId, ref userId, ref trackTitle, ref resolveURLJson);
            }
            else
            {
                DownloadTrack(urlToGet, ref trackToGetId, ref userId, ref trackTitle, ref resolveURLJson);
            }
            //if (resolveURLJson == null)
            //    return;
            //// ops.Log(resolveURLJson.ToString(), Console.ForegroundColor);
        }
    }

    private static void PreviewsURLSInFrontier()
    {
        foreach (string s in _downloadFrontier)
        {
            var urlToGet = RemoveQueryParameters(s);
            GetSongInfo(urlToGet, _dumpJSONInfo);
            Console.WriteLine();
        }
    }

    private static void PreviewsURL(string url, ref string userId, ref string trackTitle, ref dynamic resolveURLJson)
    {
        
        var urlToGet = RemoveQueryParameters(url);

        if (urlToGet.Contains(URI_STRINGS_SETS))
        {
            PreviewSet(urlToGet);
        }
        else
        {
            GetSongInfo(urlToGet, _dumpJSONInfo);
        }

        ops.Log(resolveURLJson.ToString(), Console.ForegroundColor);
    
    }

    /// <summary>
    /// Assumes args[0] holds a value
    /// </summary>
    /// <param name="args"></param>
    /// <returns></returns>
    private static void ProcessInitialArgs(string[] args)
    {
        bool flag = false;

        for(int i = 0 ; i < args.Length; i++)
        {
            if (args[i].Contains(".txt"))
            {
                if (FileManager.CheckForValidFileName(args[i]))
                {
                    flag = true;
                    BeginFileDownload(args[0]);
                }
            }
            else if (args[i].Contains(SNDCLD_SEARCH_WORD))
            {
                flag = true;
                _downloadFrontier.Add(args[i]);
            }
        }   
    }

    /// <summary>
    /// Assumes the file exists.
    /// </summary>
    /// <param name="filename"></param>
    private static void BeginFileDownload(string filename)
    {
        List<string> urls = FileManager.ReadFileLines(filename);

        if (urls.Count > 0)
        {
            //lets add them to the frontier, if valid
            foreach (string s in urls.Where(x => x != ""))
                ProcessStringForSoundcloudURL(s);
        }
        else
            ops.Log(String.Format("The file '{0}' contained no lines!", filename), ConsoleColor.Red);

        PreviewsURLSInFrontier();
        
        string filenameonly = new FileInfo(filename).Name;

        filenameonly = filenameonly.Substring(0, filenameonly.IndexOf(".txt"));

        string foldername = String.Format("{0}_{1}", DateTime.Now.ToString("yyyyMMdd_hhmm"), filenameonly);

        string proposedFoldername = _savePath + @"\" + foldername + @"\";

        ops.Log("A folder will be created for this download collection...", ConsoleColor.White);
        ops.Log(String.Format("HIT ENTER TO DOWNLOAD TRACKS TO {0}\\{1}", _savePath, foldername), ConsoleColor.Yellow);

        Console.Read();

        if (!Directory.Exists(proposedFoldername))
            Directory.CreateDirectory(proposedFoldername);

        _savePath = proposedFoldername;
    }


    /// <summary>
    /// Adds a potential URL to the Frontier
    /// </summary>
    /// <param name="s"></param>
    private static void ProcessStringForSoundcloudURL(string s)
    {
        if (s.Contains(SNDCLD_SEARCH_WORD))
            _downloadFrontier.Add(s);
        else
        {
            ops.Log("The string ", ConsoleColor.Red);
            ops.Log(s, ConsoleColor.Cyan);
            ops.Log(" did not seem like a SoundCloud URL!", ConsoleColor.Red);
        }

        ops.Log(
            String.Format("Found {0} potential URLs", _downloadFrontier.Count), 
            _downloadFrontier.Count > 0 ? ConsoleColor.Green : ConsoleColor.Red
            );
    }

    

    private static string AskForSoundCloudUrl(string baseUrl)
    {
        string urlToGet = GetStringAnswer("Please Enter a SounCloud URL to resolve or a filename containing a list of URLs - OR, provide user= / track=...");
        //if (!urlToGet.Contains(baseUrl))
         //   urlToGet = baseUrl + urlToGet;
        return urlToGet;
    }


    
    private static string RemoveQueryParameters(string urlToGet)
    {
        if (urlToGet.Contains("?in"))
        {
            urlToGet = urlToGet.Substring(0, urlToGet.IndexOf("?in", 0));
        }
        if (urlToGet.Contains("?"))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("WARNING! \n The current URL contains a '?' - this may indicate query parameters which need to be stripped.");
            Console.ForegroundColor = _defaultConsoleCol;
        }

        return urlToGet;
    }

    /// <summary>
    /// NEEDS PROPER REFACTORING
    /// </summary>
    /// <param name="urlToGet"></param>
    /// <param name="trackToGetId"></param>
    /// <param name="userId"></param>
    /// <param name="trackTitle"></param>
    /// <param name="resolveURLJson"></param>
    private static void DownloadSet(string urlToGet, ref string trackToGetId, ref string userId, ref string trackTitle, ref dynamic resolveURLJson)
    {

        resolveURLJson = SendAPIRequest(_api.MakeCallResolveURL(urlToGet));
        trackToGetId = resolveURLJson["id"];
        string setName = resolveURLJson["user"]["username"] + " - " + resolveURLJson["title"];
        userId = resolveURLJson["user_id"];
        //trackTitle = resolveURLJson["title"];

        Newtonsoft.Json.Linq.JContainer tracklist = resolveURLJson["tracks"];

        List<Track> tracks = new List<Track>();

        foreach (var t in tracklist.Children())
        {
            if (t["kind"].ToString() != "track")
            {
                ops.Log(String.Format("{0} : \n", "Found something other than a track!"), ConsoleColor.Red);
                ops.Log(String.Format("{0}", t.ToString()), ConsoleColor.Magenta);
                continue;
            }
            Track newTrack = JsonPopulatorService.GetTrackFromJToken(t);
            tracks.Add(newTrack);
        }
        PreviewSet(urlToGet);

        string theResult = "";

        foreach (Track t in tracks)
        {
            try
            {
                theResult += GrabTrackById(t.Id, setName) + "\n\n";
            }
            catch (Exception ex)
            {
                ExceptionThrown(ex, new EventArgs());
            }
            
        }

        Console.WriteLine(theResult);

        //try
        //{
            

        //}
        //catch (Exception ex)
        //{

        //    Console.WriteLine(trackToGetId.ToString() + " " + ex.Message);
        //}
        //finally
        //{
        //    Console.WriteLine(trackToGetId.ToString() + " finished.");
        //}
    }


    /// <summary>
    /// NEEDS PROPER REFACTORING
    /// </summary>
    /// <param name="urlToGet"></param>
    /// <param name="trackToGetId"></param>
    /// <param name="userId"></param>
    /// <param name="trackTitle"></param>
    /// <param name="resolveURLJson"></param>
    private static void DownloadAllTracksByAtist(dynamic userInfo, dynamic tracksInfo, dynamic setsInfo)
    {

        ////Newtonsoft.Json.Linq.JContainer tracklist = resolveURLJson["tracks"];

        //List<Track> tracks = new List<Track>();

        //foreach (var t in tracklist.Children())
        //{
        //    if (t["kind"].ToString() != "track")
        //    {
        //        ops.Log(String.Format("{0} : \n", "Found something other than a track!"), ConsoleColor.Red);
        //        ops.Log(String.Format("{0}", t.ToString()), ConsoleColor.Magenta);
        //        continue;
        //    }
        //    Track newTrack = JsonPopulatorService.GetTrackFromJToken(t);
        //    tracks.Add(newTrack);
        //}
        //PreviewSet(urlToGet);

        //string theResult = "";

        //foreach (Track t in tracks)
        //{
        //    theResult += GrabTrackById(t.Id, setName) + "\n\n";
        //}

        //Console.WriteLine(theResult);

    }

    /// <summary>
    /// NEEDS PROPER REFACTORING
    /// returns the list of track URIs
    /// </summary>
    /// <param name="urlToGet"></param>
    /// <param name="trackToGetId"></param>
    /// <param name="userId"></param>
    /// <param name="trackTitle"></param>
    /// <param name="resolveURLJson"></param>
    private static List<string> PreviewSet(string urlToGet)
    {
        dynamic resolveURLJson = SendAPIRequest(_api.MakeCallResolveURL(urlToGet));
        string trackToGetId = resolveURLJson["id"];
        string setName = resolveURLJson["user"]["username"] + " - " + resolveURLJson["title"];
        string userId = resolveURLJson["user_id"];
        //trackTitle = resolveURLJson["title"];

        Newtonsoft.Json.Linq.JContainer tracklist = resolveURLJson["tracks"];

        List<Track> tracks = new List<Track>();

        double playlistSize = 0;

        foreach (var t in tracklist.Children())
        {
            if (t["kind"].ToString() != "track")
            {
                ops.Log(String.Format("{0} : \n", "Found something other than a track!"), ConsoleColor.Red);
                ops.Log(String.Format("{0}", t.ToString()), ConsoleColor.Magenta);
                continue;
            }
            Track newTrack = JsonPopulatorService.GetTrackFromJToken(t);
            tracks.Add(newTrack);
        }

        playlistSize = tracks.Sum(x => x.GetSizeInMegabytes());

        ops.Log(String.Format("{0} ", setName), ConsoleColor.Green);
        ops.Log(String.Format("In this set listing there are {0} track(s):", tracks.Count), ConsoleColor.White);
        ops.Log(String.Format("Est. Total playlist size: "), ConsoleColor.White);
        ops.Log(String.Format("{0:N1}MB", playlistSize), ConsoleColor.Cyan);

        ops.LogLines(tracks.Select(t => t.ToString()).ToArray());
        //tracks.ForEach(x => GetSongInfo(x.Uri));
        return tracks.Select(x => x.Uri).ToList();
    }

    private static void DownloadTrack(string urlToGet, ref string trackToGetId, ref string userId, ref string trackTitle, ref dynamic resolveURLJson)
    {
        try
        {
            resolveURLJson = SendAPIRequest(_api.MakeCallResolveURL(urlToGet));
            trackToGetId = resolveURLJson["id"];
            
            Console.WriteLine("Starting the download...");

            string theResult = GrabTrackById(trackToGetId.ToString());

            //Console.WriteLine(theResult);

        }
        catch (Exception ex)
        {

            Console.WriteLine(trackToGetId.ToString() + " " + ex.Message);
        }
        finally
        {
            Console.WriteLine(trackToGetId.ToString() + " finished.");
        }
    }

    private static string GetSongInfo(string urlToGet, bool dumpJSON = false)
    {
        dynamic resolveURLJson = null;
        try
        {
            resolveURLJson = SendAPIRequest(_api.MakeCallResolveURL(urlToGet));

            PrintTrackDetails(resolveURLJson);
        }
        catch (Exception ex)
        {
            ops.Log(ex.Message, ConsoleColor.Red);
            return "";
        }
        return resolveURLJson.ToString();
    }


    /// <summary>
    /// returns the track URI
    /// </summary>
    /// <param name="resolveURLJson"></param>
    /// <returns></returns>
    private static string PrintTrackDetails(dynamic resolveURLJson)
    {
        string trackToGetId = resolveURLJson["id"];
        string userId = resolveURLJson["user_id"];
        string trackTitle = resolveURLJson["title"];

        string artistName = resolveURLJson["user"]["username"];
        string kind = resolveURLJson["kind"];

        //var bgCol = Console.BackgroundColor;
        Console.WriteLine();
        //Console.BackgroundColor = ConsoleColor.White;
        ops.Log("==============" + kind.ToUpper() + "============", kind == "track" ? ConsoleColor.Blue : ConsoleColor.White);
        ops.Log(trackTitle, ConsoleColor.Green);
        ops.Log(String.Format("{0}, \t {1}", artistName, kind == "track" ? SizeHelper.MegaBytesToString(SizeHelper.GetMegaBytesFromDuration((long)resolveURLJson["duration"])) : "Tracks: " + resolveURLJson["track_count"].ToString()), ConsoleColor.Yellow);
        ops.Log("================================", kind == "track" ? ConsoleColor.Blue : ConsoleColor.White);
        //Console.BackgroundColor = bgCol;


        return resolveURLJson["uri"];
    }





    private static int GetIntAnswer(string p, int max = 50, int min = 0)
    {
        ops.Log(p + ": ", ConsoleColor.Yellow);
        var keys = Console.ReadLine();
        try
        {
            if(keys == "")
                throw new Exception();

            int answer = int.Parse(keys);

            if (answer >= min && answer <= max)
                return answer;
            else
                throw new Exception();
        }
        catch (Exception)
        {
            ops.Log("Invalid response, try again!", ConsoleColor.Red);
            return GetIntAnswer(p);
        }
    }

    private static string  GetStringAnswer(string p)
    {
        ops.Log(p + ": ", ConsoleColor.Yellow);
        var keys = Console.ReadLine();
        try
        {
            if(true)
                return keys.ToString();
        }
        catch (Exception)
        {
            ops.Log("Invalid response, try again!", ConsoleColor.Red);
            return GetStringAnswer(p);
        }
    }

    private static void BeginUserSpecificDownload(string userResponse)
    {
        string search = "";

        userResponse = System.Uri.EscapeUriString(userResponse);

        if (userResponse.ToLower().StartsWith("user="))
        {
            search = userResponse.Substring(userResponse.IndexOf('=') + 1);

            dynamic responseJson = SendAPIRequest(_api.MakeCallInfoForUser(search));

            if(responseJson == null)
               return;
             
            dynamic infoJson = "", tracksResponse = "", playlistResponse = "";

            if (responseJson.Count != null)
            {
                int choice = 0;
                foreach (var userResult in responseJson)
                {
                    ops.Log("Option " + choice++, ConsoleColor.Yellow);
                    ops.Log(
                        String.Format(
                            "Artist : '{0}' \t\t Tracks: {1} \t\tPlaylists: {2}",
                            userResult["username"],
                            userResult["track_count"],
                            userResult["playlist_count"]), ConsoleColor.Green
                        );
                }

                var userChoice = GetIntAnswer("Please enter Choice: ");
                infoJson = responseJson[userChoice];
            }
            else
                infoJson = responseJson;

            ops.Log(
                    String.Format(
                        "Artist : '{0}' \t\t Tracks: {1} \t\tPlaylists: {2}",
                        infoJson["username"],
                        infoJson["track_count"],
                        infoJson["playlist_count"]), ConsoleColor.Green);


            string uri = "";

            if (infoJson["track_count"] > 0)
            {
                tracksResponse = SendAPIRequest(_api.MakePagedCall(_api.MakeCallInfoForUserTracks(infoJson["id"].ToString())));

                while (tracksResponse != null)
                {
                    var count = tracksResponse["collection"].Count;
                    foreach (var t in tracksResponse["collection"])
                    {
                        uri = PrintTrackDetails(t);
                        ProcessStringForSoundcloudURL(uri);
                    }

                    if (tracksResponse["next_href"] != null)
                        tracksResponse = SendAPIRequest(tracksResponse["next_href"].ToString());
                    else
                        tracksResponse = null;
                }
                

            }

            if (infoJson["playlist_count"] > 0)
            {
                playlistResponse = SendAPIRequest(_api.MakeCallInfoForUserSets(infoJson["id"].ToString()));

                foreach (var s in playlistResponse)
                {
                    PreviewSet(s["uri"].ToString());

                    ProcessStringForSoundcloudURL(s["uri"].ToString());
                }
            }

            string downloadAnswer = GetStringAnswer("DOWNLOAD ALL STUFF?");

            if (downloadAnswer.ToLower()[0] == 'y')
            {
                _savePath += RemoveBadCharacters(infoJson["username"].ToString()) + "\\";
                DownloadAllTracksByAtist(infoJson, tracksResponse, playlistResponse);
            }
            else
            {
                Main(new string[0]);
            }

        }
        else //string wasnt 'user='. could be track=, not implemented
        {
            
        }
    }


    private static string GetArtistInfo(string artistId, bool includeRealname)
    {
        var resolveURLJson = SendAPIRequest(_api.MakeCallInfoForUser(artistId));

        if(includeRealname)
            return String.Format("{0} ({1})", resolveURLJson["username"], resolveURLJson["full_name"]);
        return String.Format("{0}", resolveURLJson["username"]);
    }

    private static void SearchByArtist(string appDeets, string artistName)
    {
        var response = SendAPIRequest(_api.MakeCallSearchByArtist(artistName));
        Console.WriteLine(response.ToString());
        
        Console.ReadKey();
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="appDeets">The Web Client's key to use for auth</param>
    /// <param name="trackId">the track ID to get</param>
    /// <returns>Track info + User Info for song (JSON, probably not properly formatted)</returns>
    private static string GrabTrackById(string trackId, string setName = null)
    {
        string outputInfo = "Song not found";
        string track = trackId;
        HttpWebRequest request;
        HttpWebResponse response;
        var info = SendAPIRequest(_api.MakeCallInfoForSong(track));
        var output = SendAPIRequest(_api.MakeCallStreamForSong(track));
        var userInfo = SendAPIRequest(_api.MakeCallInfoForUser(info["user_id"].ToString()));
         
        string theMoney = output["http_mp3_128_url"];

        PrintTrackDetails(info);

        Exception exceptionResult = SaveRippedSongToFile(theMoney, userInfo, info, setName);

        if (exceptionResult == null)
            outputInfo = trackId;
        else
        {
            outputInfo = exceptionResult.Message;
            System.Threading.Thread.Sleep(new TimeSpan((long)(new Random().NextDouble() * 5)));

            //ops.Log("JSON Keys available: ", ConsoleColor.Cyan, false);
            //ops.Log(output.ToString(), ConsoleColor.Cyan);
            //ops.RestoreColour();
        }
         
        return outputInfo;
    }

    private static Exception SaveRippedSongToFile(string theMoney, dynamic userInfo, dynamic info, string folder = null)
    {
        HttpWebRequest request;
        HttpWebResponse response;
        Exception ret = null;
        string theSavePath = "";

        try
        {
            string songName = info["title"];
            songName = RemoveBadCharacters(songName);

            if (folder != null)
                folder = RemoveBadCharacters(folder);

            if (!Directory.Exists(String.Format(@"{0}", _savePath)))
            {
                Directory.CreateDirectory(String.Format(@"{0}", _savePath));
            }

            if (folder == null)
                theSavePath = String.Format("{0}{1} - {2}.mp3", _savePath, userInfo["username"], songName);
            else
            {
                folder = RemoveBadCharacters(folder);
                if (!Directory.Exists(String.Format(@"{0}\{1}", _savePath, folder)))
                    Directory.CreateDirectory(String.Format(@"{0}\{1}", _savePath, folder));
                theSavePath = String.Format(@"{0}\{3}\{1} - {2}.mp3", _savePath, userInfo["username"], songName, folder);
            }

            if (File.Exists(theSavePath))
            {
                throw new Exception("Song already downloaded! Work done. \n");
            }

            request = (HttpWebRequest)WebRequest.Create(theMoney);
            request.Timeout = 5000;
            response = (HttpWebResponse)request.GetResponse();
        }
        catch (Exception ex)
        {
            ret = ex;
            ops.Log(ret.Message, ConsoleColor.Red);
            return ret;
        }

        Stream responseStream = response.GetResponseStream();
        MemoryStream memoryStream = new MemoryStream();

        byte[] result = null;
        byte[] buffer = new byte[4096];
        long thisChunk = 0, nextChunk = 0;
        float size = 0;
        int count = 0;
        long counter = 0;
        do
        {
            count = responseStream.Read(buffer, 0, buffer.Length);
            memoryStream.Write(buffer, 0, count);
            counter += count;
            nextChunk = counter / 256;
            if (nextChunk != thisChunk)
            {
                Console.SetCursorPosition(0, Console.CursorTop);
                size = (float)counter / (float)1024 / (float)1024;
                Console.Write(String.Format("{0}Mb...\t", size));
            }

            if (count == 0)
            {
                break;
            }
        } while (true);

        try
        {
            FileManager.SaveDownloadedStream(theSavePath, responseStream, memoryStream, 0);
        }
        catch (Exception ex)
        {
            ExceptionThrown(ex, new EventArgs());
            ret = ex;
        }
        

        responseStream.Close();
        return null;
    }

    

    private static string RemoveBadCharacters(string name)
    {
        //name = name.Replace('\\', '-');
        name = name.Replace('/', '-');
        name = name.Replace('|', '\0');
        name = name.Replace('Δ', 'A');
        name = name.Replace('[', '(');
        name = name.Replace(']', ')');
        name = name.Replace(':', '-');
        name = name.Replace("?", "");

        var invalid = System.IO.Path.GetInvalidFileNameChars();
        name = new String(name.Select(x => { if (invalid.Contains(x)) return '#'; else return x; }).ToArray());
        
        return name;
    }

    private static dynamic SendAPIRequest(string address)
    {
        Uri theAddress = new Uri(address);
        HttpWebRequest request = (HttpWebRequest) WebRequest.Create(theAddress);

        HttpWebResponse response = null;

        try
        {
            request.Timeout = 4000;
            response = (HttpWebResponse)request.GetResponse();
        }
        catch (Exception ex)
        {
            Console.Write(ex.Message);
            return null;
        }

        return DeserializeWithNewtonsoft(response);

    }

    private static dynamic DeserializeWithNewtonsoft(HttpWebResponse response)
    {
        Stream resStream = response.GetResponseStream();

        StreamReader sr = new StreamReader(resStream);
        JsonReader jr = new JsonTextReader(sr);

        Newtonsoft.Json.JsonSerializer jSer = new JsonSerializer();

        dynamic output = jSer.Deserialize(jr);
        return output;
    }

    private static dynamic DeserializeWithMicrosoft(HttpWebResponse response)
    {
        Stream resStream = response.GetResponseStream();
        StreamReader sr = new StreamReader(resStream);

        DataContractJsonSerializer dcjs = new DataContractJsonSerializer(typeof(Dictionary<string, string>));

        MemoryStream jsonMemoryStream = new MemoryStream();
        //dynamic output = jSer.Deserialize(jr);

        dcjs.WriteObject(jsonMemoryStream, sr.ReadToEnd());
        sr.Close();

        StreamReader final = new StreamReader(jsonMemoryStream);

        //Dictionary<string, string> output = final.ReadToEnd();
        //FIX THIS
        return null;
    }


    /// <summary>
    /// Certificate validation callback.
    /// </summary>
    private static bool ValidateRemoteCertificate(object sender, X509Certificate cert, X509Chain chain, SslPolicyErrors error)
    {
        // If the certificate is a valid, signed certificate, return true.
        if (error == System.Net.Security.SslPolicyErrors.None)
        {
            return true;
        }

        Console.WriteLine("X509Certificate [{0}] Policy Error: '{1}'",
            cert.Subject,
            error.ToString());

        return false;
    }
}