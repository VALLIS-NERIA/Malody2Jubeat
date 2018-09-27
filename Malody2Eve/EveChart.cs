namespace Malody2Eve {
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;

    public static class EveNoteTypeExtension {
        private static Dictionary<EveNoteType, string> _name = new Dictionary<EveNoteType, string>
        {
            [EveNoteType.Measure] = Measure,
            [EveNoteType.Haku] = Haku,
            [EveNoteType.Tempo] = Tempo,
            [EveNoteType.Play] = Play,
            [EveNoteType.Long] = Long,
            [EveNoteType.End] = End
        };

        public const string Measure = "MEASURE";
        public const string Haku = "HAKU";
        public const string Tempo = "TEMPO";
        public const string Play = "PLAY";
        public const string Long = "LONG";
        public const string End = "END";

        public static string String(this EveNoteType type) => _name[type];
    }

    public enum EveNoteType {
        Measure,

        // empty beat
        Haku,

        // bpm point
        Tempo,

        // tap
        Play,

        // hold
        Long,
        End
    }

    public class EveChart {
        /// <summary>
        /// Gets or sets the beats per measure. e.g., for a 3/4 song, this is 3.
        /// The default value is 4 and this property can be set only once.
        /// </summary>
        public int BeatPerMeasure {
            get => this._beatPerMeasure;
            set {
                if (this._beatPerMeasureLocked) {
                    throw new InvalidOperationException("The BeatPerMeasure is locked. Maybe it is already assigned, or this chart has finished its parse.");
                }
                else {
                    this._beatPerMeasure = value;
                    this._beatPerMeasureLocked = true;
                }
            }
        }

        private int _beatPerMeasure = 4;
        private bool _beatPerMeasureLocked = false;
        private readonly List<EveBeat> _beats = new List<EveBeat>();
        private readonly List<EveNote> _notes = new List<EveNote>();
        private readonly Dictionary<int, double> _timingPoints = new Dictionary<int, double>();
        private string _string = null;

        public override string ToString() {
            // ....2580,HAKU....,.......0\r\n             : 28 characters per line
            var sb = new StringBuilder(28 * this._notes.Count);
            foreach (var note in this._notes) {
                sb.AppendLine($"{note.Time, 8},{note.Type, -8},{note.Index, 8}");
            }

            if (this._string == null) {
                this._string = sb.ToString();
            }

            return this._string;
        }

        public static EveChart FromMalodyMicroseconds(MalodyChart mc, int microseconds) {
            return new EveChart(mc, microseconds);
        }

        public static EveChart FromMalodyMicroseconds(MalodyChart mc, int microseconds, int beatPerMeasure) {
            return new EveChart(mc, microseconds, beatPerMeasure);
        }

        public static EveChart FromMalodySeconds(MalodyChart mc, int seconds) {
            try {
                checked {
                    return new EveChart(mc, seconds * 1000000);
                }
            }
            catch (OverflowException) {
                Console.WriteLine("The song is too long.");
                return null;
            }
        }

        public static EveChart FromMalodySeconds(MalodyChart mc, int seconds, int beatPerMeasure) {
            try {
                checked {
                    var us = seconds * 1000000;
                }

                return new EveChart(mc, seconds * 1000000, beatPerMeasure);
            }
            catch (OverflowException) {
                throw new ArgumentOutOfRangeException(nameof(seconds), "The song is too long.");
            }
        }

        public EveChart() { }

        public void ParseMalody(MalodyChart mc, int microseconds) {
            this._beatPerMeasureLocked = true;

            foreach (var timingpoint in mc.time) {
                this._timingPoints.Add(timingpoint.beat[0], timingpoint.bpm);
            }

            var i = 0; // 当前拍的编号
            var j = 0; // 当前拍在小节中的序号。
            double lastTime = 0;
            double lastDim = 0;
            double lastBpm = 0;

            while (true) {
                double currentBpm;
                bool isMeasure = false;
                bool isTimingpoint = false;

                // timing point
                if (this._timingPoints.ContainsKey(i)) {
                    currentBpm = this._timingPoints[i];
                    lastBpm = currentBpm;
                    isMeasure = true;
                    isTimingpoint = true;
                    j = 0;
                }

                // measures
                else {
                    currentBpm = lastBpm;
                    if (j++ == this.BeatPerMeasure) {
                        isMeasure = true;
                        j = 0;
                    }
                }

                lastTime = lastTime + lastDim;
                lastDim = 60000000d / currentBpm;

                var b = new EveBeat(lastTime, currentBpm, isMeasure, isTimingpoint);
                this._beats.Add(b);

                this._notes.Add(new EveNote(b.Time, EveNoteType.Haku, 0));
                if (b.IsTimingpoint) this._notes.Add(new EveNote(b.Time, EveNoteType.Tempo, (int)b.Dim));
                if (b.IsMeasure) this._notes.Add(new EveNote(b.Time, EveNoteType.Measure, 0));

                if (lastTime > microseconds) {
                    this._notes.Add(new EveNote(b.Time, EveNoteType.End, 0));
                    break;
                }

                i++;
            }

            foreach (var mcnote in mc.note) {
                if (mcnote.index == null) continue;

                var currentBeat = this._beats[mcnote.Beat];

                var time = currentBeat.Time + (mcnote.Numerator / (double)mcnote.Denominator) * currentBeat.Dim;

                if (mcnote.IsHold) {
                    var endBeat = this._beats[mcnote.endbeat[0]];
                    var endTime = endBeat.Time + (mcnote.endbeat[1] / (double)mcnote.endbeat[2]) * endBeat.Dim;
                    this._notes.Add(EveNote.CreateHold(time, endTime, (int)mcnote.index, (int)mcnote.endindex));
                }
                else {
                    this._notes.Add(new EveNote(time, EveNoteType.Play, mcnote.index.Value));
                }
            }

            this._notes.Sort(new EveTypeComparer());
        }

        private EveChart(MalodyChart mc, int microseconds) {
            this.ParseMalody(mc, microseconds);
        }

        private EveChart(MalodyChart mc, int microseconds, int beatPerMeasure) {
            this.BeatPerMeasure = beatPerMeasure;
            this.ParseMalody(mc, microseconds);
        }

        private struct EveBeat {
            // public string type = null;
            public EveBeat(double time, double bpm, bool isMeasure, bool isTimingPoint) {
                this.Time = time;
                this.Dim = 60000000 / bpm;
                this.IsMeasure = isMeasure;
                this.IsTimingpoint = isTimingPoint;
            }

            public double Dim { get; }

            public bool IsMeasure { get; }

            public bool IsTimingpoint { get; }

            public double Time { get; }
        }

        private struct EveNote {
            public int Index { get; private set; }

            public int Time { get; private set; }

            public string Type => this._type.String();

            private EveNoteType _type;

            /// <summary>
            ///     从微秒为单位的时间创建jbtNote
            /// </summary>
            /// <param name="timeUs">以微秒为单位的时间</param>
            /// <param name="s">MEASURE HAKU PLAY etc.</param>
            /// <param name="index"></param>
            public EveNote(double timeUs, EveNoteType s, int index) {
                this.Time = (int)(timeUs * 3 / 10000);
                this._type = s;
                this.Index = index;
            }

            public override string ToString() {
                return $"{this.Time, 8},{this.Type, -8},{this.Index, 8}";
            }

            public static EveNote CreateHold(double beginTimeUs, double endTimeUs, int holdIndex, int tailIndex) {
                return CreateHold((int)(beginTimeUs * 3 / 10000), (int)(endTimeUs * 3 / 10000), holdIndex, tailIndex);
            }

            public static EveNote CreateHold(int beginTimeEve, int endTimeEve, int holdIndex, int tailIndex) {
                /*
                 * Index of a hold note:
                 * 32               16        8  6  4  1
                 * -------- -------- -------- -- -- ----
                 * 00000000 00000000 00101101 01 11 0100
                 * \---    hold time     ---/ ^  ^  ^
                 *                            |  |  |= index of the hold panel
                 *               hold length =|  |= hold direction: down00, up01, right10, left11
                 *               \----    e.g. hold 4 and tail is 6, it is left, length 2   ----/
                 */
                if (holdIndex > 15 || tailIndex > 15 || tailIndex == holdIndex || endTimeEve <= beginTimeEve) {
                    throw new ArgumentException("Invalid hold.");
                }

                var note = new EveNote
                {
                    _type = EveNoteType.Long,
                    Time = beginTimeEve
                };

                sbyte len;
                byte h;
                if (tailIndex / 4 == holdIndex / 4) {
                    // same row
                    checked {
                        len = (sbyte)(tailIndex - holdIndex);
                    }

                    // right(tail<hold): 10; left: 11
                    if (len < 0)
                        h = 0b10;
                    else
                        h = 0b11;
                }
                else if (tailIndex % 4 == holdIndex % 4) {
                    // same column
                    checked {
                        len = (sbyte)(tailIndex % 4 - holdIndex % 4);
                    }

                    // up(tail>hold): 01; down: 00;
                    if (len > 0)
                        h = 0b01;
                    else
                        h = 0b00;
                }
                else {
                    throw new ArgumentException("The begin and end of the hold is not at the same row or column.");
                }

                checked {
                    // len
                    h = (byte)(h | (Math.Abs(len)) << 2);

                    uint info;
                    info = (uint)(endTimeEve - beginTimeEve);
                    info <<= 8;
                    info |= (ushort)(h << 4);
                    info |= ((uint)holdIndex & 0xff);
                    note.Index = (int)info;
                }

                return note;
            }
        }

        private class EveTypeComparer : IComparer<EveNote> {
            private readonly Dictionary<string, int> _dict = new Dictionary<string, int>
            {
                ["END"] = -4,
                ["MEASURE"] = -3,
                ["HAKU"] = -2,
                ["TEMPO"] = -1,
                ["PLAY"] = 0,
                ["HOLD"] = 0,
            };

            public int Compare(EveNote x, EveNote y) {
                var res = Comparer<int>.Default.Compare(x.Time, y.Time);
                if (res == 0) {
                    res = Comparer<int>.Default.Compare(this._dict[x.Type], this._dict[y.Type]);
                }

                return res;
            }
        }
    }
}
