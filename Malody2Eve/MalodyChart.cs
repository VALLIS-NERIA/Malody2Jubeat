// ReSharper disable InconsistentNaming

namespace Malody2Eve {
    using System.Collections.Generic;
    using System.IO;
    using Newtonsoft.Json;

    public class McTime {
        public double bpm { get; set; }
        public List<int> beat { get; set; }
    }

    public class McNote {
        public List<int> beat { get; set; }
        public List<int> endbeat { get; set; }
        public int? index { get; set; }
        public int? endindex { get; set; }

        public int Beat => this.beat[0];
        public int Numerator => this.beat[1];
        public int Denominator => this.beat[2];
        public bool IsHold => this.endbeat != null;
        public bool IsPlayable => this.index != null;
    }

    public class MalodyChart {
        public List<McTime> time { get; set; }
        public IEnumerable<McNote> note {
            get => this.__note;
            set => this.__note = value;
        }

        protected virtual IEnumerable<McNote> __note { get; set; }

        protected MalodyChart() { }

        public static MalodyChart FromFile(string filename) {
            return MalodyChart.FromJson(File.ReadAllText(filename));
        }
        public static MalodyChart FromJson(string jsonString) {
            return JsonConvert.DeserializeObject<MalodyChart>(jsonString);
        }
    }
}

namespace Malody2Eve.Additional {
    using System.Collections.Generic;
    using System.IO;
    using Newtonsoft.Json;

    public class McNoteFull : McNote {
        public string sound { get; set; }
        public int vol { get; set; }
        public int type { get; set; }
    }

    public class MalodyChartFull : MalodyChart {
        protected override IEnumerable<McNote> __note {
            get => this.NoteFull;
            set => base.note = value;
        }

        [JsonProperty("note")]
        public IEnumerable<McNoteFull> NoteFull { get; set; }
        public Meta meta { get; set; }
        public Extra extra { get; set; }

        public new static MalodyChartFull FromFile(string filename) {
            return MalodyChartFull.FromJson(File.ReadAllText(filename));
        }

        public new static MalodyChartFull FromJson(string jsonString) {
            return JsonConvert.DeserializeObject<MalodyChartFull>(jsonString);
        }
    }

    public class Song {
        public string titile { get; set; }
        public string artist { get; set; }
        public int id { get; set; }
    }

    public class Meta {
        public string creator { get; set; }
        public string background { get; set; }
        public string version { get; set; }
        public int id { get; set; }
        public int mode { get; set; }
        public int time { get; set; }
        public Song song { get; set; }
        public object mode_ext { get; set; }
    }

    public class Test {
        public int divide { get; set; }
        public int speed { get; set; }
        public int save { get; set; }
        public int @lock { get; set; }
    }

    public class Extra {
        public Test test { get; set; }
    }
}
