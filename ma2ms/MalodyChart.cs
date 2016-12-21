using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ma2ms {
    public struct Song {
        public string titile { get; set; }
        public string artist { get; set; }
        public int id { get; set; }
    }
    public struct Mode_Ext {

    }
    public struct Time {
        public double bpm { get; set; }
        public List<int> beat { get; set; }
    }
    public struct Meta {
        public string creator { get; set; }
        public string background { get; set; }
        public string version { get; set; }
        public int id { get; set; }
        public int mode { get; set; }
        public int time { get; set; }
        public Song song { get; set; }
        public Mode_Ext mode_ext { get; set; }
    }
    public struct Note {
        public List<int> beat { get; set; }
        [DefaultValue(-99)]
        public int index { get; set; }
        public string sound { get; set; }
        public int vol { get; set; }
        public int type { get; set; }
    }
    public struct Test {
        public int divide { get; set; }
        public int speed { get; set; }
        public int save { get; set; }
        public int @lock { get; set; }
    }
    public struct Extra {
        public Test test { get; set; }
    }
    public class MalodyChart {
        public Meta meta { get; set; }
        public List<Time> time { get; set; }
        public List<Note> note { get; set; }
        public Extra extra { get; set; }
    }
}
