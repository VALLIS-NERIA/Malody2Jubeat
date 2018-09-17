namespace Malody2Eve {
    using System.Collections.Generic;

    public struct McTime {
        public double bpm { get; set; }
        public List<int> beat { get; set; }
    }

    public struct McNote {
        public List<int> beat { get; set; }
        public List<int> endbeat { get; set; }
        public uint? index { get; set; }
        public uint? endindex { get; set; }
        //public string sound { get; set; }
        //public int vol { get; set; }
        //public int type { get; set; }

        public int Beat => this.beat[0];
        public int Numerator => this.beat[1];
        public int Denominator => this.beat[2];
        public bool IsHold => this.endbeat != null;
        public bool IsPlayable => this.index != null;
    }

    public class MalodyChart {
        public List<McTime> time { get; set; }
        public List<McNote> note { get; set; }
    }
}
