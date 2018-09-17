//  --------------------------------------------------------------------------------------------------------------------
//  <copyright file="Beat.cs" company="Microsoft">
//    Copyright (c) Microsoft Corporation.  All rights reserved.
//  </copyright>
//  --------------------------------------------------------------------------------------------------------------------

namespace Malody2Eve {
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;

    public static class NoteTypeString {
        private static Dictionary<EveNoteType, string> _name = new Dictionary<EveNoteType, string>
        {
            [EveNoteType.Measure] = Measure,
            [EveNoteType.Haku] = Haku,
            [EveNoteType.Tempo] = Tempo,
            [EveNoteType.Play] = Play,
            [EveNoteType.Hold] = Hold,
            [EveNoteType.End] = End
        };

        public const string Measure = "MEASURE";
        public const string Haku = "HAKU";
        public const string Tempo = "TEMPO";
        public const string Play = "PLAY";
        public const string Hold = "HOLD";
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
        Hold,
        End
    }

    public class EveChart {
        /// <summary>
        /// Gets or sets the beats per measure. e.g., for a 3/4 song, this is 3.
        /// The default value is 4 and this property 
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
                    return new EveChart(mc, seconds * 1000000, beatPerMeasure);
                }
            }
            catch (OverflowException) {
                Console.WriteLine("The song is too long.");
                return null;
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
                double bpm;
                bool isMeasure;
                bool isTimingpoint;

                // timing point
                if (this._timingPoints.ContainsKey(i)) {
                    bpm = this._timingPoints[i];
                    lastBpm = bpm;
                    isMeasure = true;
                    isTimingpoint = true;
                    j = 0;
                }

                // measures
                else {
                    bpm = lastBpm;
                    isTimingpoint = false;
                    if (j++ == this.BeatPerMeasure) {
                        isMeasure = true;
                        j = 0;
                    }
                    else {
                        isMeasure = false;
                    }
                }

                lastTime = lastTime + lastDim;
                lastDim = 60000000d / bpm;

                // Start first measure on 0 (last_time, not last_time+last_dim)
                var b = new EveBeat(lastTime, bpm, isMeasure, isTimingpoint);
                this._beats.Add(b);

                this._notes.Add(new EveNote(b.Time, EveNoteType.Haku, 0));
                if (b.IsTimingpoint) {
                    this._notes.Add(new EveNote(b.Time, EveNoteType.Tempo, (int)b.Dim));
                }

                if (b.IsMeasure) {
                    this._notes.Add(new EveNote(b.Time, EveNoteType.Measure, 0));
                }

                if (lastTime > microseconds) {
                    this._notes.Add(new EveNote(b.Time, EveNoteType.End, (int)b.Dim));
                    break;
                }

                i++;
            }

            foreach (var mcnote in mc.note) {
                if (mcnote.index == null) continue;

                var currentBeat = this._beats[mcnote.Beat];

                var time = currentBeat.Time + (mcnote.Numerator / (double)mcnote.Denominator) * currentBeat.Dim;

                this._notes.Add(new EveNote(time, EveNoteType.Play, mcnote.index.Value));
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

            public static EveNote CreateHold(int beginTimeEve, int endTimeEve, int holdIndex, int tailIndex)  {
                if (holdIndex > 15 || tailIndex > 15 || tailIndex == holdIndex || endTimeEve<=beginTimeEve) {
                    throw new ArgumentException("Invalid hold.");
                }

                var note = new EveNote
                {
                    _type = EveNoteType.Hold,
                    Time = beginTimeEve
                };

                sbyte len;
                byte h;
                if (tailIndex / 4 == holdIndex / 4) {
                    // the same row
                    len = (sbyte)(tailIndex - holdIndex);
                    // right: 10; left: 11
                    if (len > 0)
                        h = 0b10;
                    else
                        h = 0b11;
                }
                else if (tailIndex % 4 == holdIndex % 4) {
                    // same column
                    len = (sbyte)(tailIndex % 4 - holdIndex % 4);
                    // up: 01; down: 00;
                    if (len > 0)
                        h = 0b00;
                    else
                        h = 0b01;
                }
                else {
                    throw new ArgumentException("The begin and end of the hold is not at the same row or column");
                }
                h = (byte)(h | (Math.Abs(len) - 1) << 2);

                uint info;
                info = (uint)(endTimeEve - beginTimeEve);
                info <<= 16;
                info |= (ushort)(h << 8);
                info |= ((uint)holdIndex & 0xff);
                note.Index = (int)info;

                return note;
            }
        }

        private class EveTypeComparer : IComparer<EveNote> {
            private readonly Dictionary<string, int> _dict = new Dictionary<string, int> { { "PLAY", 0 }, { "TEMPO", 1 }, { "HAKU", 2 }, { "MEASURE", 3 }, { "END", 4 } };

            public int Compare(EveNote x, EveNote y) {
                var res = Comparer<int>.Default.Compare(x.Time, y.Time);
                if (res == 0) {
                    res = Comparer<int>.Default.Compare(this._dict[x.Type], this._dict[y.Type]);
                }

                return -res;
            }
        }
    }
}
