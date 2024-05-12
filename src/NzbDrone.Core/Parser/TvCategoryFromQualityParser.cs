using System.Text.RegularExpressions;
using NzbDrone.Core.Indexers;

namespace NzbDrone.Core.Parser
{
    public static class TvCategoryFromQualityParser
    {
        private static readonly Regex SourceRegex = new (@"\b(?:
                                                                (?<bluray>BluRay|Blu-Ray|HDDVD|BD)|
                                                                (?<webdl>WEB[-_. ]DL|WEBDL|WebRip|iTunesHD|WebHD)|
                                                                (?<hdtv>HDTV)|
                                                                (?<bdrip>BDRip)|
                                                                (?<brrip>BRRip)|
                                                                (?<dvd>DVD|DVDRip|NTSC|PAL|xvidvd)|
                                                                (?<dsr>WS[-_. ]DSR|DSR)|
                                                                (?<pdtv>PDTV)|
                                                                (?<sdtv>SDTV)|
                                                                (?<tvrip>TVRip)
                                                                )\b",
                                                RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace);

        private static readonly Regex RawHdRegex = new (@"\b(?<rawhd>TrollHD|RawHD|1080i[-_. ]HDTV|Raw[-_. ]HD|MPEG[-_. ]?2)\b",
                                                                RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex ResolutionRegex = new (@"\b(?:(?<q480p>480p|640x480|848x480)|(?<q576p>576p)|(?<q720p>720p|1280x720)|(?<q1080p>1080p|1920x1080)|(?<q2160p>2160p))\b",
                                                                RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex CodecRegex = new (@"\b(?:(?<x264>x264)|(?<h264>h264)|(?<xvidhd>XvidHD)|(?<xvid>Xvid)|(?<divx>divx))\b",
                                                                RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex HighDefPdtvRegex = new (@"hr[-_. ]ws", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public static IndexerCategory ParseTvShowQuality(string tvShowFileName)
        {
            return NewznabStandardCategory.Other;
        }
    }
}
