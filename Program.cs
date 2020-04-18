using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace ClassicMusicTheoryForProgrammer
{
    class Program
    {
        private static ScoreObj[] source = new ScoreObj[0];
        private static int[] sourceLine = new int[0];
        private static uint bpm = 96;
        private static bool ignore = false;

        static void Main(string[] args)
        {
            if(args.Length < 1)
            {
                Console.WriteLine("ファイルを指定してください");
                throw new Exception();
            }

            string prop = "";

            if(args.Length > 1)
            {
                prop = args[1];
            }

            try
            {
                using (StreamReader file = new StreamReader(args[0]))
                {
                    if (prop == "-IgnoreTheoryEx" || prop == "-MakeMidi") ignore = true;
                    ReadAndInterpret(file);
                }
            }
            catch (Exception e) when (e is ArgumentException ||
                                      e is ArgumentNullException ||
                                      e is FileNotFoundException ||
                                      e is DirectoryNotFoundException ||
                                      e is IOException)
            {
                Console.WriteLine("ファイルを開けません : " + args[0]);
                throw e;
            }

            if(prop == "-MakeMidi")
            {
                MakeMidi(Path.ChangeExtension(args[0], ".mid"));
            }
            else
            {
                RunSource();
            }
        }

        private static string ReadAndCheck(StreamReader st)
        {
            string read = st.ReadLine();
            if(read == null)
            {
                Console.WriteLine("ソースコードが不正です");
                throw new Exception();
            }
            else
            {
                return read;
            }
        }

        private static void ReadAndInterpret(StreamReader st)
        {
            string first = st.ReadLine();
            string line;
            List<ScoreObj> sourceList = new List<ScoreObj>();
            List<int> lineList = new List<int>();
            int index = 1;

            if(first is null)
            {
                Console.WriteLine("ファイルが空です");
                throw new Exception();
            }

            first = RemoveComments(first);

            if (first.Substring(0,1) == "|" || first.Substring(0, 1) == ":")
            {
                line = first;
            }
            else
            {
                DetectBPM(first);
                line = st.ReadLine();
                ++index;
            }


            //ローカル関数
            void addSource(ScoreObj obj, int ind)
            {
                sourceList.Add(obj);
                lineList.Add(ind);
            }

            Stack<(BarObj, int)> startBars = new Stack<(BarObj, int)>();
            ChordObj lastChord = null;
            bool isPreBar = false;
            bool isPreRep = false;
            bool isMusicEx = false;

            //ソースコード本体
            while (!(line is null))
            {
                line = RemoveComments(line);

                if (line.Substring(0, 1) == "|" || line.Substring(0, 1) == ":")
                {
                    //barを読み取り
                    isPreRep = false;

                    if (isPreBar)
                    {
                        Console.WriteLine("文法エラー：不明なシンボルです(" + index + "行)");
                        throw new Exception();
                    }

                    BarObj bar;

                    if (line == "|:")
                    {
                        bar = new BarObj(BarType.RepStart);
                        startBars.Push((bar, index));

                        isPreRep = true;
                    }
                    else if (line == ":|")
                    {
                        if(startBars.Count == 0)
                        {
                            Console.WriteLine("文法エラー：対応するbarが存在しません(" + index + "行)");
                            throw new Exception();
                        }

                        bar = new BarObj(BarType.RepEnd);
                        (BarObj pair, _) = startBars.Pop();
                        bar.SetPartner(pair);
                        pair.SetPartner(bar);

                        isPreRep = true;
                    }
                    else if (line == "|")
                    {
                        bar = new BarObj(BarType.Line);
                    }
                    else
                    {
                        Console.WriteLine("文法エラー：不明なシンボルです(" + index + "行)");
                        throw new Exception();
                    }

                    addSource(bar, index);
                    isPreBar = true;
                }
                else
                {
                    //toneを読み取り

                    string[] voice = new string[4];
                    voice[0] = line;
                    for (int i = 1; i < 4; ++i)
                    {
                        voice[i] = st.ReadLine();
                        ++index;

                        if(voice[i] is null)
                        {
                            Console.WriteLine("文法エラー：ソースコードの最後が不正です(" + index + "行)");
                            throw new Exception();
                        }

                        voice[i].Trim();
                    }

                    Note[,] notes = new Note[4, 4];

                    for (int i = 0; i < 4; ++i)
                    {
                        string[] tones = voice[i].Split(new char[] {' '}, StringSplitOptions.RemoveEmptyEntries);

                        if(tones.Length == 0 || tones.Length == 3 || tones.Length > 4)
                        {
                            Console.WriteLine("文法エラー：toneの個数が不正です(" + (index + i - 3) + "行)");
                            throw new Exception();
                        }

                        try
                        {
                            if(tones.Length == 1)
                            {
                                notes[i, 0] = StringToNote(tones[0]);
                                notes[i, 1] = Note.T;
                                notes[i, 2] = Note.T;
                                notes[i, 3] = Note.T;
                            }
                            else if (tones.Length == 2)
                            {
                                notes[i, 0] = StringToNote(tones[0]);
                                notes[i, 1] = Note.T;
                                notes[i, 2] = StringToNote(tones[1]);
                                notes[i, 3] = Note.T;
                            }
                            else if (tones.Length == 4)
                            {
                                notes[i, 0] = StringToNote(tones[0]);
                                notes[i, 1] = StringToNote(tones[1]);
                                notes[i, 2] = StringToNote(tones[2]);
                                notes[i, 3] = StringToNote(tones[3]);
                            }
                        }
                        catch
                        {
                            Console.WriteLine("文法エラー：不明なシンボルです(" + (index + i - 3) + "行)");
                            throw new Exception();
                        }
                    }

                    for (int i = 0; i < 4; ++i)
                    {
                        ChordObj nowChord = new ChordObj(notes[0, i], notes[1, i], notes[2, i], notes[3, i]);
                        addSource(nowChord, index - 3);
                        nowChord.SetPreChordObj(lastChord);
                        try
                        {
                            nowChord.CheckMusicException(index - 3);
                        }
                        catch (MusicTheoryException e)
                        {
                            isMusicEx = true;
                        }
                        lastChord = nowChord;
                    }

                    isPreBar = false;
                    isPreRep = false;
                }

                line = st.ReadLine();
                ++index;
            }

            if (!isPreBar)
            {
                Console.WriteLine("文法エラー：ソースコードの最後が不正です(" + index + "行)");
                throw new Exception();
            }

            if(startBars.Count > 0)
            {
                (_, int barInd) = startBars.Pop();
                Console.WriteLine("文法エラー：対応するbarが存在しません(" + barInd + "行)");
                throw new Exception();
            }

            if (isMusicEx && !ignore)
            {
                throw new MusicTheoryException();
            }

            source = sourceList.ToArray();
            sourceLine = lineList.ToArray();
        }

        private static void RunSource()
        {
            int index = 0;
            ChordObj preChord = null;
            bool lastBar = false;
            int lastCommand = 0;
            List<SimpleChord> progression = new List<SimpleChord>();
            List<(SimpleChord[], int)> progs = new List<(SimpleChord[], int)>();
            Dictionary<string, int> env = new Dictionary<string, int>();

            while(index < source.Length)
            {
                if(source[index] is ChordObj ch)
                {
                    ch.SetPreChordObj(preChord);
                    if (!ignore)
                    {
                        ch.CheckMusicException(sourceLine[Array.IndexOf(source, preChord ?? ch)]);
                    }
                    SimpleChord sChord = ChordToSimpleChord(ch.GetChord());

                    //Console.Write(sChord.ToString() + " ");

                    if(sChord == SimpleChord.I && lastBar)
                    {
                        if(progression.Count > 0) progs.Add((progression.ToArray(), sourceLine[Array.IndexOf(source, preChord ?? ch)]));

                        progression.Clear();
                        lastCommand = DoCommand(progs, env) ?? lastCommand;
                    }

                    if (progression.Count == 0) progression.Add(sChord);
                    else if (progression.Last() != sChord) progression.Add(sChord);

                    preChord = ch;
                }

                lastBar = false;

                if(source[index] is BarObj b)
                {
                    switch (b.bar)
                    {
                        case BarType.RepStart:
                            //Console.Write("\n■: ");
                            if (lastCommand == 0)
                            {
                                index = Array.IndexOf(source, b.GetPartner());
                                //Console.Write(":■ ");
                            }
                            break;
                        case BarType.RepEnd:
                            //Console.Write("\n:■ ");
                            if (lastCommand != 0)
                            {
                                index = Array.IndexOf(source, b.GetPartner());
                                //Console.Write("■: ");
                            }
                            break;
                        case BarType.Line:
                            //Console.Write("\n■ ");
                            break;
                    }

                    lastBar = true;
                }

                ++index;
            }

            if(preChord.GetChord() != Chord.I)
            {
                Console.WriteLine("音楽理論エラー：最後のコードは I である必要があります");
                throw new MusicTheoryException();
            }
        }

        private static void MakeMidi(string filename)
        {
            using(FileStream midi = new FileStream(filename, FileMode.Create, FileAccess.Write))
            {
                byte[] header = new byte[] { 0x4D, 0x54, 0x68, 0x64, 0x00, 0x00, 0x00, 6,
                                             0x00, 0x01,
                                             0x00, 0x05,
                                             0x00, 0x01 };
                int usec = (int)((long)60 * 1000000 / bpm);
                byte[] byteInt = new byte[3];
                for(int i = 2; i >= 0; --i)
                {
                    byteInt[i] = unchecked((byte)usec);
                    usec /= 0x100;
                }
                byte[] conduct = new byte[] { 0x4D, 0x54, 0x72, 0x6B, 0x00, 0x00, 0x00, 11,
                                              0x00, 0xFF, 0x51, 0x03, byteInt[0], byteInt[1], byteInt[2],
                                              0x00, 0xFF, 0x2F, 0x00};

                List<byte>[] tracks = new List<byte>[] { new List<byte>(), new List<byte>(), new List<byte>(), new List<byte>() };
                Stack<BarObj> loopMgr = new Stack<BarObj>();
                ChordObj lastChord = null;
                int index = 0;
                int[] interval = new int[] { 0, 0, 0, 0 };

                tracks[0].Add(0x00);
                tracks[0].Add(0xC0);
                tracks[0].Add(40);

                tracks[1].Add(0x00);
                tracks[1].Add(0xC1);
                tracks[1].Add(40);

                tracks[2].Add(0x00);
                tracks[2].Add(0xC2);
                tracks[2].Add(41);

                tracks[3].Add(0x00);
                tracks[3].Add(0xC3);
                tracks[3].Add(42);

                while (index < source.Length)
                {
                    if(source[index] is BarObj b)
                    {
                        if(b.bar == BarType.RepStart)
                        {
                            loopMgr.Push(b);
                        }
                        else if(b.bar == BarType.RepEnd)
                        {
                            if(loopMgr.Count > 0)
                            {
                                if (loopMgr.Peek() == b.GetPartner())
                                {
                                    BarObj st = loopMgr.Pop();
                                    index = Array.IndexOf(source, st);
                                }
                            }
                        }
                    }
                    else if(source[index] is ChordObj c)
                    {
                        lastChord = c;
                        for(int i = 0; i < 4; ++i)
                        {
                            if (c.voice[i].isR)
                            {
                                if(c.pre.NoteDetect(i) is int num)
                                {
                                    byte[] value = IntToChangeableBytes(interval[i]);

                                    for(int j = 0; j < value.Length; ++j)
                                    {
                                        tracks[i].Add(value[j]);
                                    }
                                    tracks[i].Add((byte)(0x80 + i));
                                    tracks[i].Add((byte)num);
                                    tracks[i].Add(0);

                                    interval[i] = 0;
                                }
                            }
                            else if (c.voice[i].isT)
                            {
                            }
                            else
                            {
                                if (c.pre?.NoteDetect(i) is int num)
                                {
                                    byte[] value = IntToChangeableBytes(interval[i]);

                                    for (int j = 0; j < value.Length; ++j)
                                    {
                                        tracks[i].Add(value[j]);
                                    }
                                    tracks[i].Add((byte)(0x80 + i));
                                    tracks[i].Add((byte)num);
                                    tracks[i].Add(0);

                                    tracks[i].Add(0);
                                }
                                else
                                {
                                    byte[] value = IntToChangeableBytes(interval[i]);

                                    for (int j = 0; j < value.Length; ++j)
                                    {
                                        tracks[i].Add(value[j]);
                                    }
                                }
                                tracks[i].Add((byte)(0x90 + i));
                                tracks[i].Add((byte)c.voice[i].num);
                                tracks[i].Add((byte)0x7F);

                                interval[i] = 0;
                            }

                            ++interval[i];
                        }
                    }

                    ++index;
                }

                for(int i = 0; i < 4; ++i)
                {
                    if (lastChord.NoteDetect(i) is int num)
                    {
                        byte[] value = IntToChangeableBytes(interval[i]);

                        for (int j = 0; j < value.Length; ++j)
                        {
                            tracks[i].Add(value[j]);
                        }
                        tracks[i].Add((byte)(0x80 + i));
                        tracks[i].Add((byte)num);
                        tracks[i].Add(0);

                        interval[i] = 0;
                    }

                    tracks[i].Add(0x01);
                    tracks[i].Add(0xFF);
                    tracks[i].Add(0x2F);
                    tracks[i].Add(0x00);
                }

                byte[][] tracksByte = new byte[][]
                {
                    new byte[] { 0x4D, 0x54, 0x72, 0x6B, 0x00, 0x00, 0x00, 0x00},
                    new byte[] { 0x4D, 0x54, 0x72, 0x6B, 0x00, 0x00, 0x00, 0x00},
                    new byte[] { 0x4D, 0x54, 0x72, 0x6B, 0x00, 0x00, 0x00, 0x00},
                    new byte[] { 0x4D, 0x54, 0x72, 0x6B, 0x00, 0x00, 0x00, 0x00}
                };

                int[] length = new int[] { tracks[0].Count, tracks[1].Count, tracks[2].Count, tracks[3].Count };

                for(int i = 0; i < 4; ++i)
                {
                    for (int j = 3; j >= 0; --j)
                    {
                        tracksByte[i][4 + j] = (byte)(length[i] % 0x100);
                        length[i] /= 0x100;
                    }

                    tracksByte[i] = tracksByte[i].Concat(tracks[i]).ToArray();
                }

                var output = (new byte[0]).Concat(header);
                output = output.Concat(conduct);
                output = output.Concat(tracksByte[0]);
                output = output.Concat(tracksByte[1]);
                output = output.Concat(tracksByte[2]);
                output = output.Concat(tracksByte[3]);

                midi.Write(output.ToArray(), 0, output.Count());
            }
        }

        private static byte[] IntToChangeableBytes(int x)
        {
            if (x < 0) return new byte[0];
            if (x == 0) return new byte[] { 0 };

            List<byte> outList = new List<byte>();
            while(x > 0)
            {
                outList.Add(unchecked((byte)((x % 0x80) + (outList.Count == 0 ? 0 : 0x80))));
                x /= 0x80;
            }
            outList.Reverse();

            return outList.ToArray();

        }

        private static SimpleChord[][] commands = new SimpleChord[][]
            {
                new SimpleChord[]{ SimpleChord.I , SimpleChord.V  },
                new SimpleChord[]{ SimpleChord.I , SimpleChord.IV },
                new SimpleChord[]{ SimpleChord.I , SimpleChord.V , SimpleChord.I , SimpleChord.V  },
                new SimpleChord[]{ SimpleChord.I , SimpleChord.IV, SimpleChord.I , SimpleChord.V  },
                new SimpleChord[]{ SimpleChord.I , SimpleChord.IV, SimpleChord.V  },
                new SimpleChord[]{ SimpleChord.I , SimpleChord.IV, SimpleChord.II, SimpleChord.V  },
                new SimpleChord[]{ SimpleChord.I , SimpleChord.VI, SimpleChord.V  },
                new SimpleChord[]{ SimpleChord.I , SimpleChord.VI, SimpleChord.IV, SimpleChord.V  },
                new SimpleChord[]{ SimpleChord.I , SimpleChord.VI, SimpleChord.II, SimpleChord.V  },
            };

        private static int? DoCommand(List<(SimpleChord[], int)> progs, Dictionary<string, int> env)
        {
            if (progs.Count < 2) return null;

            int comInd = 0;
            bool isCommand = false;

            for(;comInd < 9; ++comInd)
            {
                if (progs[0].Item1.SequenceEqual(commands[comInd]))
                {
                    isCommand = true;
                    break;
                }
            }

            if (!isCommand)
            {
                goto error;
            }

            try
            {
                if (progs.Count == 2)
                {
                    if(comInd > 3) return null;

                    string name = ProgressionToString(progs[1].Item1);

                    switch (comInd)
                    {
                        case 0:
                            if (env.ContainsKey(name))
                            {
                                env[name] = 0;
                            }
                            else
                            {
                                env.Add(name, 0);
                            }
                            progs.Clear();
                            return 0;
                        case 1:
                            ++env[name];
                            progs.Clear();
                            return env[name];
                        case 2:
                            env[name] = Console.Read();
                            progs.Clear();
                            return env[name];
                        case 3:
                            Console.Write(unchecked((char)env[name]));
                            progs.Clear();
                            return env[name];
                    }

                    goto error;
                }
                else if (progs.Count == 3)
                {
                    string name1 = ProgressionToString(progs[1].Item1);
                    string name2 = ProgressionToString(progs[2].Item1);

                    switch (comInd)
                    {
                        case 4:
                            env[name1] += env[name2];
                            progs.Clear();
                            return env[name1];
                        case 5:
                            env[name1] -= env[name2];
                            progs.Clear();
                            return env[name1];
                        case 6:
                            env[name1] *= env[name2];
                            progs.Clear();
                            return env[name1];
                        case 7:
                            env[name1] /= env[name2];
                            progs.Clear();
                            return env[name1];
                        case 8:
                            env[name1] %= env[name2];
                            progs.Clear();
                            return env[name1];
                    }

                    goto error;
                }
            }
            catch(KeyNotFoundException)
            {
                Console.WriteLine("実行時エラー：宣言されていない変数が呼ばれました(" + progs[0].Item2 + "行)");
                throw new Exception();
            }

            error:
            Console.WriteLine("実行時エラー：progressionに対応する命令が存在しません(" + progs[0].Item2 + "行)");
            throw new Exception();
        }

        private static void DetectBPM(string st)
        {
            if(st.Contains("BPM") || st.Contains("bpm"))
            {
                st = st.Substring(4, st.Length - 4);
                try
                {
                    bpm = uint.Parse(st);
                }
                catch(Exception e)
                {
                    Console.WriteLine("BPMの値が不正です");
                    throw e;
                }
            }
            else
            {
                switch (st)
                {
                    case "Grave":
                        bpm = 42;
                        break;
                    case "Largo":
                        bpm = 46;
                        break;
                    case "Lento":
                        bpm = 52;
                        break;
                    case "Adagio":
                        bpm = 56;
                        break;
                    case "Larghetto":
                        bpm = 60;
                        break;
                    case "Adagietto":
                        bpm = 66;
                        break;
                    case "Andante":
                        bpm = 72;
                        break;
                    case "Maestoso":
                        bpm = 80;
                        break;
                    case "Moderate":
                        bpm = 96;
                        break;
                    case "Allegletto":
                        bpm = 108;
                        break;
                    case "Animato":
                        bpm = 120;
                        break;
                    case "Allegro":
                        bpm = 132;
                        break;
                    case "Assai":
                        bpm = 144;
                        break;
                    case "Vivace":
                        bpm = 160;
                        break;
                    case "Presto":
                        bpm = 184;
                        break;
                    case "Prestissimo":
                        bpm = 208;
                        break;
                    default:
                        Console.WriteLine("テンポの指定が不正です");
                        throw new Exception();
                }
            }
        }

        private static string RemoveComments(string origin)
        {
            int st = origin.IndexOf("//");
            if (st < 0) return origin;
            return origin.Substring(0, st).Trim();
        }

        private static Note StringToNote(string st)
        {
            //匿名関数
            int CharToNoteName(char c)
            {
                switch (c)
                {
                    case 'C':
                        return 0;
                    case 'D':
                        return 2;
                    case 'E':
                        return 4;
                    case 'F':
                        return 5;
                    case 'G':
                        return 7;
                    case 'A':
                        return 9;
                    case 'B':
                        return 11;
                    default:
                        throw new Exception();
                }
            }

            if(st == "T")
            {
                return Note.T;
            }
            else if(st == "R")
            {
                return Note.R;
            }
            else if(int.TryParse(st, out int nn))
            {
                return (Note)nn;
            }
            else if (int.TryParse(st.Substring(1, 1), out int oct))
            {
                char[] ch = st.ToCharArray();
                int chnn = CharToNoteName(ch[0]) + (oct + 1) * 12;

                if (ch.Length == 3)
                {
                    if (ch[2] == '#')
                    {
                        ++chnn;
                    }
                    else if (ch[2] == 'b')
                    {
                        --chnn;
                    }
                    else
                    {
                        throw new Exception();
                    }
                }
                else if (ch.Length > 3) throw new Exception();

                return (Note)chnn;
            }
            else if (int.TryParse(st.Substring(1, 2), out int noct))
            {
                char[] ch = st.ToCharArray();
                int chnn = CharToNoteName(ch[0]) + (noct + 1) * 12;

                if (ch.Length == 4)
                {
                    if (ch[3] == '#')
                    {
                        ++chnn;
                    }
                    else if (ch[3] == 'b')
                    {
                        --chnn;
                    }
                    else
                    {
                        throw new Exception();
                    }
                }
                else if (ch.Length > 4) throw new Exception();

                return (Note)chnn;
            }

            throw new Exception();
        }

        private static string ProgressionToString(SimpleChord[] prog)
        {
            StringBuilder build = new StringBuilder();
            for (int i = 0; i < prog.Length; ++i)
            {
                build.Append(prog[i].ToString());
                if (i < prog.Length - 1)
                {
                    build.Append("-");
                }
            }
            return build.ToString();
        }

        private static SimpleChord ChordToSimpleChord(Chord c)
        {
            switch (c)
            {
                case Chord.I:
                    return SimpleChord.I;
                case Chord.II:
                    return SimpleChord.II;
                case Chord.II7:
                    goto case Chord.II;
                case Chord.II7omit5:
                    goto case Chord.II;
                case Chord.IIx7:
                    goto case Chord.II;
                case Chord.IV:
                    return SimpleChord.IV;
                case Chord.V:
                    return SimpleChord.V;
                case Chord.V7:
                    goto case Chord.V;
                case Chord.V7omit5:
                    goto case Chord.V;
                case Chord.Vx7:
                    goto case Chord.V;
                case Chord.VI:
                    return SimpleChord.VI;
                default:
                    return default;
            }
        }
    }

    enum Chord
    {
        I,
        II,
        II7,
        II7omit5,
        IIx7,
        IV,
        V,
        V7,
        V7omit5,
        Vx7,
        VI
    }

    enum SimpleChord
    {
        I,
        II,
        IV,
        V,
        VI
    }
}
