using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using Newtonsoft.Json;
using System.IO;
using System.ComponentModel;
using ma2ms;

namespace ma2ms {
    public struct Note_ms {
        public double time;
        public int index;
        public Note_ms(double time, int index) {
            this.time = time;
            this.index = index;
        }
    }
    public struct Note_jbt {
        public int time;
        public string type;
        public int index;
        public Note_jbt(double time, string s, int index) {
            this.time = (int)(time * 3  / 10000);
            this.type = s;
            this.index = index;
        }
        public Note_jbt(int time, string s, int index) {
            this.time = time ;
            this.type = s;
            this.index = index;
        }
    }

    class typeComparer : IComparer<string> {
        Dictionary<string, int> dict = new Dictionary<string, int> {{"PLAY",0},{"TEMPO",1},{"HAKU",2},{"MEASURE",3} };
        public int Compare(string x, string y) {
            return Comparer<int>.Default.Compare(dict[x], dict[y]);
        }
    }

    class Program {
        const string MEASURE = "MEASURE";
        const string HAKU = "HAKU";
        const string TEMPO = "TEMPO";
        const string PLAY = "PLAY";
        const int MAX_BEAT = 420;
        const int bpmeasure = 4;
        static void Main(string[] args) {
            string fileinput = @"1.mc";
            MalodyChart mc = JsonConvert.DeserializeObject<MalodyChart>(File.ReadAllText(fileinput), new JsonSerializerSettings { DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate });
            double[] beat_dims = new double[MAX_BEAT];
            foreach (var timingpoint in mc.time) {
                beat_dims[timingpoint.beat[0]] = 60000000 / timingpoint.bpm;
            }

            bool[] is_measure = new bool[MAX_BEAT];
            int j = 0;
            for (int i = 0; i < MAX_BEAT; i++) {
                if (beat_dims[i] != 0.0) {
                    j = 0;
                    is_measure[i] = true;
                }
                else {

                    j++;
                    if (j == 4) {
                        is_measure[i] = true;
                        j = 0;
                    }
                    else {
                        is_measure[i] = false;
                    }
                }

            }

            double lasttiming = 0.0;
            for (int i = 0; i < MAX_BEAT; i++) {
                if (beat_dims[i] != 0.0) {
                    lasttiming = beat_dims[i];
                }
                else {
                    beat_dims[i] = lasttiming;
                }
            }


            double[] beats = new double[MAX_BEAT];
            for (int i = 1; i < MAX_BEAT; i++) {
                beats[i] += beats[i - 1] + beat_dims[i - 1];
            }
            List<Note_ms> note_ms = new List<Note_ms>();
            foreach (var note in mc.note) {
                double time;
                time = beats[note.beat[0]] + ((double)note.beat[1] / (double)note.beat[2]) * beat_dims[note.beat[0]];
                if (note.index != -99) note_ms.Add(new Note_ms(time, note.index));
            }


            List<Note_jbt> note_jbt = new List<Note_jbt>();
            foreach (var timingpoint in mc.time) {
                var note = new Note_jbt(beats[timingpoint.beat[0]], TEMPO, (int)(60000000 / timingpoint.bpm));
                note_jbt.Add(note);
            }

            foreach (var beat in beats) {
                var note = new Note_jbt(beat, HAKU, 0);
                note_jbt.Add(note);
            }
            for (int i = 0; i < MAX_BEAT; i++) {
                if (is_measure[i]) {
                    var note = new Note_jbt(beats[i], MEASURE, 0);
                    note_jbt.Add(note);
                }
            }
            foreach (var note in note_ms) {
                var jbnote = new Note_jbt(note.time, PLAY, note.index);
                note_jbt.Add(jbnote);
            }

            var query = note_jbt.OrderBy(o => o.time).ThenByDescending(o => o.type, new typeComparer()).Select(o => new { o.time, o.type, o.index });
            //var note_ordered = from o in note_jbt orderby o.time,o.type  select o;
            FileStream fs = new FileStream("1.eve",FileMode.Create,FileAccess.ReadWrite);
            StreamWriter sw = new StreamWriter(fs);
            foreach (var note in query) {
                string s=string.Format("{0,8},{1,-8},{2,8}",note.time,note.type,note.index);
                sw.WriteLine(s);
            }

            Console.ReadKey();
        }
    }
}
