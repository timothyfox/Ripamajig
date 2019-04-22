using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SoundCloudGet.Helpers
{
    public static class SizeHelper
    {
        public static string MegaBytesToString(double size)
        {
            return String.Format("{0:N1}MB", size);
        }

        public static double GetMegaBytesFromDuration(long duration)
        {
            return (
                (double)((
                    (double)(duration) / (double)1000)
                         * Program.SNDCLD_BIT_RATE * 1024) / (double)8) / (double)1000000;
        }
    }
}
