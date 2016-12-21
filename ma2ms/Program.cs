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


    class Program {

        //TODO:添加自动加上END标志的功能
        static void Main(string[] args) {
            string fileinput = @"1.mc";
            MalodyChart mc = JsonConvert.DeserializeObject<MalodyChart>(File.ReadAllText(fileinput), new JsonSerializerSettings { DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate });
            eveChart ec = new eveChart(mc, 132);
            FileStream fs = new FileStream("1.eve", FileMode.Create, FileAccess.ReadWrite);
            ec.Write(fs);
        }




        /////////////////////////////
        //以下是旧版本
        /////////////////////////////
        const string MEASURE = "MEASURE";
        const string HAKU = "HAKU";
        const string TEMPO = "TEMPO";
        const string PLAY = "PLAY";
        const string END = "END";
        const int MAX_BEAT = 420;
        const int bpmeasure = 4;

        static void OldMain(string[] args) {
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
            List<Note_us> note_ms = new List<Note_us>();
            foreach (var note in mc.note) {
                double time;
                time = beats[note.beat[0]] + ((double)note.beat[1] / (double)note.beat[2]) * beat_dims[note.beat[0]];
                if (note.index != -99) note_ms.Add(new Note_us(time, note.index));
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
            FileStream fs = new FileStream("1.eve", FileMode.Create, FileAccess.ReadWrite);
            StreamWriter sw = new StreamWriter(fs);
            foreach (var note in query) {
                string s = string.Format("{0,8},{1,-8},{2,8}", note.time, note.type, note.index);
                sw.WriteLine(s);
            }

            Console.ReadKey();
        }
    }
}
