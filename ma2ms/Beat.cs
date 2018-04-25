using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mc2eve {

    public struct Beat {
        public double dim;
        public bool is_measure;
        public bool is_timingpoint;
        public double time;
        //public string type = null;
        public Beat(double time, double bpm, bool isMeasure, bool isTimingPoint) {
            this.time = time;
            this.dim = 60000000 / bpm;
            this.is_measure = isMeasure;
            this.is_timingpoint = isTimingPoint;
        }
    }


    public static class NoteType {
        public const string MEASURE = "MEASURE";
        public const string HAKU = "HAKU";
        public const string TEMPO = "TEMPO";
        public const string PLAY = "PLAY";
        public const string END = "END";
    }
    public class eveChart {

        List<Beat> beats = new List<Beat>();
        List<Note_jbt> notes = new List<Note_jbt>();
        Dictionary<int, double> timing_points = new Dictionary<int, double>();
        int beat_per_measure = 4;
        int index_null = -99;
        List<Note_jbt> lines = new List<Note_jbt>();


        /// <summary>
        /// 
        /// </summary>
        /// <param name="mc"></param>
        /// <param name="length">以秒计时的长度</param>
        public eveChart(MalodyChart mc, int length)
            : this(mc, ((double)length) * 1000000) { }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="mc"></param>
        /// <param name="length">以微秒计时的长度</param>
        public eveChart(MalodyChart mc, double length) {
            foreach (var timingpoint in mc.time) {
                timing_points.Add(timingpoint.beat[0], timingpoint.bpm);
            }

            int i = 0;//当前拍的编号
            int j = 0;//当前拍在小节中的序号。
            double last_time = 0;
            double last_dim = 0;
            double last_bpm = 0;
            while (true) {
                double time;
                double bpm;
                bool is_measure;
                bool is_timingpoint;
                //红线
                if (timing_points.ContainsKey(i)) {

                    bpm = timing_points[i];
                    last_bpm = bpm;
                    is_measure = true;
                    is_timingpoint = true;
                    j = 0;

                }
                //小节
                else {
                    bpm = last_bpm;
                    is_timingpoint = false;
                    if (j++ == beat_per_measure) {
                        is_measure = true;
                        j = 0;
                    }
                    else {
                        is_measure = false;
                    }
                }

                time = last_time + last_dim;
                last_time = time;
                last_dim = 60000000 / bpm;

                // Start first measure on 0 (last_time, not last_time+last_dim)
                Beat b = new Beat(last_time, bpm, is_measure, is_timingpoint);
                beats.Add(b);

                lines.Add(b);

                if (time > length)
                {
                    lines.Add(new Note_jbt(b.time, NoteType.END, (int)b.dim));
                    break;
                }
                i++;
            }

            foreach (Note mcnote in mc.note) {
                
                if (mcnote.index == index_null) continue;

                Beat current_beat = beats[mcnote.beat[0]];
                
                double time = current_beat.time + ((double)mcnote.beat[1] / (double)mcnote.beat[2]) * current_beat.dim;
                Note_jbt note = new Note_jbt(time, NoteType.PLAY, mcnote.index);

                notes.Add(note);

                lines.Add(note);
            }
            Sort();
        }

        private void Sort() {
            var query = lines.OrderBy(o => o.time).ThenByDescending(o => o.type, new typeComparer()).Select(o => new Note_jbt(o.time, o.type, o.index)).ToList();
            lines = query;
        }

        public void Write(FileStream fs) {
            StreamWriter sw = new StreamWriter(fs);
            foreach (var note in lines) {
                string s = string.Format("{0,8},{1,-8},{2,8}", note.time, note.type, note.index);
                sw.WriteLine(s);
            }
            sw.Close();
        }
    }




    public static class eveLane {
        /// <summary>
        /// 添加一个Beat，根据Beat类型不同可能会有MEASURE/TEMPO/HAKU
        /// </summary>
        /// <param name="b"></param>
        /// <returns>添加的项目的个数</returns>
        public static int Add(this List<Note_jbt> list, Beat b) {
            int i = 1;
            list.Add(new Note_jbt(b.time, NoteType.HAKU, 0));
            if (b.is_timingpoint) {
                list.Add(new Note_jbt(b.time, NoteType.TEMPO, (int)b.dim));
                i++;
            }
            if (b.is_measure) {
                list.Add(new Note_jbt(b.time, NoteType.MEASURE, 0));
                i++;
            }
            return i;
        }
    }


    public struct Note_us {
        public double time;
        public int index;
        public Note_us(double time, int index) {
            this.time = time;
            this.index = index;
        }
    }

    public struct Note_jbt {
        /// <summary>
        /// 以3又1/3毫秒为单位的时间。这是eve格式使用的计时单位。
        /// </summary>
        public int time;
        public string type;
        public int index;
        /// <summary>
        /// 从微秒为单位的时间创建jbtNote
        /// </summary>
        /// <param name="time_us">以微秒为单位的时间</param>
        /// <param name="s">MEASURE HAKU PLAY etc.</param>
        /// <param name="index"></param>
        public Note_jbt(double time_us, string s, int index) {
            this.time = (int)(time_us * 3 / 10000);
            this.type = s;
            this.index = index;
        }
        /// <summary>
        /// 以3又1/3毫秒为单位创建jbtNote
        /// </summary>
        /// <param name="time_eve">以3又1/3毫秒为单位的时间。这是eve格式使用的计时单位。</param>
        /// <param name="s"></param>
        /// <param name="index"></param>
        public Note_jbt(int time_eve, string s, int index) {
            this.time = time_eve;
            this.type = s;
            this.index = index;
        }

        public override string ToString() {
            return string.Format("{0,8},{1,-8},{2,8}", time, type, index);
        }

    }

    class typeComparer : IComparer<string> {
        Dictionary<string, int> dict = new Dictionary<string, int> { { "PLAY", 0 }, { "TEMPO", 1 }, { "HAKU", 2 }, { "MEASURE", 3 }, { "END", 4 } };
        public int Compare(string x, string y) {
            return Comparer<int>.Default.Compare(dict[x], dict[y]);
        }
    }
}
