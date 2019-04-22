using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Text;

namespace SoundCloudGet.IO
{
    public class FileManager
    {
        public FileStream GetFile(string filename)
        {
            if (!File.Exists(filename))
            {
                try
                {
                    return File.Create(filename);

                }
                catch (Exception ex)
                {
                    
                    throw ex;
                }

            }
            else
            {
                return File.Open(filename, FileMode.Truncate);
            }
        }

        /// <summary>
        /// Returns a list of filenames that are found within a given directory
        /// </summary>
        /// <param name="dir">the directory to search</param>
        /// <param name="fileFilter">The filterstring to apply to the search</param>
        /// <returns>a List of filepaths</returns>
        public static List<string> GetFilesInDirectory(string dir, string fileFilter)
        {
            if (!Directory.Exists(dir))
            {
                throw new Exception(String.Format("Directory '{0}' does not exist", dir));
                return null;
            }
            try
            {
                return Directory.GetFiles(dir, fileFilter).ToList();
            }
            catch (Exception ex)
            {
                throw ex;
                return null;
            }
        }

        public static string ReadFile(string filename)
        {
            StringBuilder sb = new StringBuilder();

            using (StreamReader sr = new StreamReader(filename))
            {
                String line;
                // Read and display lines from the file until the end of 
                // the file is reached.
                while ((line = sr.ReadLine()) != null)
                {
                    sb.AppendLine(line);
                }
            }

            string allines = sb.ToString();

            return allines;
        }

        public static List<string> ReadFileLines(string filename)
        {
            List<string> ret = new List<string>();

            using (StreamReader sr = new StreamReader(filename))
            {
                string line;
                // Read and display lines from the file until the end of 
                // the file is reached.
                while ((line = sr.ReadLine()) != null)
                {
                    ret.Add(line);
                }
            }

            return ret;
        }

        public static bool CheckForValidFileName(string p)
        {
            var col = Console.ForegroundColor;

            try
            {
                if (File.Exists(p))
                    return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("A Exception was thrown while trying to open the file provided: ");
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(ex.Message);
                Console.ForegroundColor = col;
            }
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("The specified file '{0}' was not found");
            Console.ForegroundColor = col;
                
            return false;            
        }

        public static byte[] SaveDownloadedStream(string theSavePath, Stream responseStream, MemoryStream memoryStream, int retries)
        {
            byte[] result = null;
            try
            {
                FileStream fs = new FileStream(theSavePath,
                                       FileMode.CreateNew,
                                       FileAccess.Write);

                result = memoryStream.ToArray();
                fs.Write(result, 0, result.Length);

                fs.Close();
                memoryStream.Close();
            }
            catch (Exception ex)
            {
                Console.Write("!");
                System.Threading.Thread.Sleep(2000);
                if (++retries < 5)
                    SaveDownloadedStream(theSavePath, responseStream, memoryStream, retries);
                else
                    throw ex;
            }

            return result;
        }
    }
}