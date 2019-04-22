using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json.Linq;
using SoundCloudGet.DataModels;

namespace SoundCloudGet.Services
{
    public static class JsonPopulatorService
    {
        public static Track GetTrackFromJToken(JToken input)
        {
            Track ret = null;
            try
            {
               ret = new Track
                (
                    input["id"].ToString(),
                    DateTime.Parse(input["created_at"].ToString()),
                    DateTime.Now,
                    long.Parse(input["original_content_size"].ToString()),
                    input["description"].ToString(),
                    long.Parse(input["duration"].ToString()),
                    input["title"].ToString(),
                    input["uri"].ToString(),
                    input["user"]["username"].ToString()
                );
            }catch(Exception ex)
            {
            }
            
            return ret;
        }

    }
}
