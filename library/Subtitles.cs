using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace library
{
    class Subtitles
    {
        /// <summary>
        /// from https://github.com/woollybogger/srt-to-vtt-converter
        /// </summary>
        /// <param name="sFilePath"></param>
        internal static string ConvertSrtToVtt(string sFilePath)
        {
            var result = sFilePath.Replace(".srt", ".vtt");

            using (var strReader = new StreamReader(sFilePath))
            using (var strWriter = new StreamWriter(result))
            {
                var rgxDialogNumber = new Regex(@"^\d+$");
                var rgxTimeFrame = new Regex(@"(\d\d:\d\d:\d\d,\d\d\d) --> (\d\d:\d\d:\d\d,\d\d\d)");

                // Write starting line for the WebVTT file
                strWriter.WriteLine("WEBVTT");
                strWriter.WriteLine("");

                // Handle each line of the SRT file
                string sLine;
                while ((sLine = strReader.ReadLine()) != null)
                {
                    // We only care about lines that aren't just an integer (aka ignore dialog id number lines)
                    if (rgxDialogNumber.IsMatch(sLine))
                        continue;

                    // If the line is a time frame line, reformat and output the time frame
                    Match match = rgxTimeFrame.Match(sLine);
                    if (match.Success)
                    {
                        //if (_offsetMs > 0)
                        //{
                        //    // Extract the times from the matched time frame line
                        //    var tsStartTime = TimeSpan.Parse(match.Groups[1].Value.Replace(',', '.'));
                        //    var tsEndTime = TimeSpan.Parse(match.Groups[2].Value.Replace(',', '.'));

                        //    // Modify the time with the offset
                        //    long startTimeMs = _nOffsetDirection * _offsetMs + (uint)tsStartTime.TotalMilliseconds;
                        //    long endTimeMs = _nOffsetDirection * _offsetMs + (uint)tsEndTime.TotalMilliseconds;
                        //    tsStartTime = TimeSpan.FromMilliseconds(startTimeMs < 0 ? 0 : startTimeMs);
                        //    tsEndTime = TimeSpan.FromMilliseconds(endTimeMs < 0 ? 0 : endTimeMs);

                        //    // Construct the new time frame line
                        //    sLine = tsStartTime.ToString(@"hh\:mm\:ss\.fff") +
                        //            " --> " +
                        //            tsEndTime.ToString(@"hh\:mm\:ss\.fff");
                        //}
                        //else
                        {
                            sLine = sLine.Replace(',', '.'); // Simply replace the comma in the time with a period
                        }
                    }

                    strWriter.WriteLine(sLine); // Write out the line
                }
            }

            return result;
        }
    }
}
