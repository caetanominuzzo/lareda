using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace library
{
    class TorrentNameParser
    {
        static Dictionary<string, Regex> patterns = new Dictionary<string, Regex>();

        static void Start()
        {
            patterns.Add("season", new Regex("([Ss]?([0-9]{1,2}))[Eex]"));
            patterns.Add("episode", new Regex("([Eex]([0-9]{2})(?:[^0-9]|$))"));
            patterns.Add("year", new Regex("([\\[\\(]?((?:19[0-9]|20[01])[0-9])[\\]\\)]?)"));
            patterns.Add("resolution", new Regex("(([0-9]{3,4}p))[^M]?"));
            patterns.Add("quality", new Regex("(?:PPV\\.)?[HP]DTV|(?:HD)?CAM|B[rR]Rip|TS|(?:PPV )?WEB-?DL(?: DVDRip)?|H[dD]Rip|DVDRip|DVDRiP|DVDRIP|CamRip|W[EB]B[rR]ip|[Bb]lu[Rr]ay|DvDScr|hdtv"));
            patterns.Add("codec", new Regex("xvid|x264|h\\.?264", RegexOptions.IgnoreCase));
            patterns.Add("audio", new Regex("MP3|DD5\\.?1|Dual[\\- ]Audio|LiNE|DTS|AAC(?:\\.?2\\.0)?|AC3(?:\\.5\\.1)?"));
            patterns.Add("region", new Regex("R[0-9]"));
            patterns.Add("extended", new Regex("EXTENDED"));
            patterns.Add("hardcoded", new Regex("HC"));
            patterns.Add("proper", new Regex("PROPER"));
            patterns.Add("repack", new Regex("REPACK"));
            patterns.Add("container", new Regex("MKV|AVI"));
            patterns.Add("widescreen", new Regex("WS"));
            patterns.Add("language", new Regex("rus\\.eng"));
            patterns.Add("garbage", new Regex("1400Mb|3rd Nov| ((Rip))"));
        }

        public static Dictionary<string, string> ParseTitle(string title)
        {
            if (!patterns.Any())
                Start();

            Dictionary<string, string> values = new Dictionary<string, string>();

            var group = new Regex("(- ?([^-]+(?:-={[^-]+-?$)?))$");

            var v = RegexMatch(group, title);

            if (v != string.Empty)
            {
                title = group.Replace(title, string.Empty);

                values.Add("group", v);
            }

            var parts = SplitValues(values, title);

            var tmp = string.Join(" ", parts);

            var doublePipe = new Regex("(\\| ?){2,}");

            tmp = doublePipe.Replace(tmp, "| ");

            parts = tmp.Split(new string[] { "| " }, StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length == 1)
            {
                values.Add("title", parts[0].Trim());
            }
            else if (parts.Length == 2)
            {
                if (values.ContainsKey("episode"))
                {

                    values.Add("show", parts[0].Trim());

                    values.Add("title", parts[1].Trim());
                }
                else
                {
                    values.Add("title", parts[0].Trim());
                }
            }

            return values;
        }

        static string[] SplitValues(Dictionary<string, string> values, string title)
        {
            var space = " ";

            if (title.IndexOf(" ") == -1)
                if (title.IndexOf(".") != -1)
                    space = ".";
                else if (title.IndexOf("_") != -1)
                    space = "_";


            var parts = title.Split(new string[] { space }, StringSplitOptions.RemoveEmptyEntries);

            List<string> parts2 = new List<string>();


            for (var i = 0; i < parts.Length; i++)
            {
                var s = parts[i];

                foreach (var key in patterns.Keys)
                {
                    if (values.ContainsKey(key))
                        continue;

                    var v = RegexMatch(patterns[key], s);

                    if (v != string.Empty)
                    {
                        values.Add(key, v);

                        parts[i] = "|";
                    }

                }
            }

            return parts;
        }

        static string RegexMatch(Regex regex, string part)
        {
            var m = regex.Match(part);

            if (m.Groups.Count == 1)
                return m.Groups[0].Value;

            if (m.Groups.Count == 3)
                return m.Groups[2].Value;

            return string.Empty;
        }
    }
}
