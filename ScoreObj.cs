using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClassicMusicTheoryForProgrammer
{
    struct Note
    {
        public readonly int num;
        public readonly bool isT;
        public readonly bool isR;

        public Note(int nn)
        {
            num = nn;
            isT = false;
            isR = false;
        }

        private Note(bool T)
        {
            num = 0;
            isT = T;
            isR = !T;
        }
        
        public static explicit operator Note(int nn) => new Note(nn);

        public static Note T
        {
            get => new Note(true);
        }

        public static Note R
        {
            get => new Note(false);
        }
    }

    class MusicTheoryException : Exception
    {

    }

    class ScoreObj
    {
    }

    class ChordObj : ScoreObj
    {
        public Note[] voice { get; private set; }
        private Chord? ch;
        public ChordObj pre { get; private set; } = null;

        public ChordObj(Note soprano, Note alto, Note tenor, Note bass)
        {
            voice = new Note[4];
            voice[0] = soprano;
            voice[1] = alto;
            voice[2] = tenor;
            voice[3] = bass;
            ch = null;
        }

        public void CheckMusicException(int line)
        {
            try
            {
                CheckNotes(true);
            }
            catch (MusicTheoryException e)
            {
                Console.WriteLine("(" + line + "-" + (line + 3) + "行)");
                throw e;
            }

            bool ex = false;
            for(int i = 0; i < 4; ++i)
            {
                for(int j = 0; j < 4; ++j)
                {
                    (bool c5, bool c8) = NoteContinuous5or8(i, j, true);
                    if (c5) Console.Error.WriteLine("音楽理論エラー：連続五度です(" + (line + i) + "行)");
                    if (c8) Console.Error.WriteLine("音楽理論エラー：連続八度です(" + (line + i) + "行)");
                    ex = ex || c5 || c8;
                }
            }

            for (int i = 0; i < 4; ++i)
            {
                bool isEx = CheckLimitedProgNoteException(i);
                if (isEx) Console.Error.WriteLine("音楽理論エラー：限定進行音に反しています(" + (line + i) + "行)");
                ex = ex || isEx;
            }

            for (int i = 0; i < 4; ++i)
            {
                bool isEx = CheckAbnormalChangeException(i);
                if (isEx) Console.Error.WriteLine("音楽理論エラー：不自然な音程の移動です(" + (line + i) + "行)");
                ex = ex || isEx;
            }

            bool chEx = CheckProgressionException();
            if(chEx) Console.Error.WriteLine("音楽理論エラー：chordの進行先が不正です(" + line + "-" + (line + 3) + "行)");
            ex = ex || chEx;

            if (ex) throw new MusicTheoryException();
        }

        private void CheckNotes(bool message = false)
        {
            int?[] nn = new int?[] { NoteDetect(0), NoteDetect(1), NoteDetect(2), NoteDetect(3) };
            int[] num = new int[] { nn[0] ?? -255, nn[1] ?? -255, nn[2] ?? -255, nn[3] ?? -255 };

            if (!((nn[0] is null || ((nn[1] is null || num[0] > num[1]) &&
                                     (nn[2] is null || num[0] > num[2]) &&
                                     (nn[3] is null || num[0] > num[3])   )) &&
                  (nn[1] is null || ((nn[2] is null || num[1] > num[2]) &&
                                     (nn[3] is null || num[1] > num[3])   )) &&
                  (nn[2] is null || ((nn[3] is null || num[2] > num[3])   ))    ))
            {
                if (message) Console.Error.Write("音楽理論エラー：上の声部より音高が高くなっています");
                throw new MusicTheoryException();
            }

            if(!((nn[0] is null || (num[0] <= 96 && num[0] >= 55)) &&
                 (nn[1] is null || (num[1] <= 96 && num[1] >= 55)) &&
                 (nn[2] is null || (num[2] <= 84 && num[2] >= 48)) &&
                 (nn[3] is null || (num[3] <= 72 && num[3] >= 36))   ))
            {
                if (message) Console.Error.Write("音楽理論エラー：声部の音域に収まっていません");
                throw new MusicTheoryException();
            }
        }

        private (bool, bool) NoteContinuous5or8(int vo1, int vo2, bool isThree)
        {
            if (vo1 == vo2) return (false, false);

            if (pre is null) return (false, false);
            int? preNn1 = pre.NoteDetect(vo1);
            if (preNn1 is null) return (false, false);
            if (voice[vo1].isR) return (false, false);
            if (voice[vo1].isT) return pre.NoteContinuous5or8(vo1, vo2, isThree);
            int preNum1 = preNn1 ?? 0;
            int num1 = voice[vo1].num;

            if (pre is null) return (false, false);
            int? preNn2 = pre.NoteDetect(vo2);
            if (preNn2 is null) return (false, false);
            if (voice[vo2].isR || voice[vo2].isT) return (false, false);
            int preNum2 = preNn2 ?? 0;
            int num2 = voice[vo2].num;

            bool c5 = false;
            bool c8 = false;

            int diff, preDiff;
            if (vo1 < vo2)
            {
                diff = (num1 - num2) % 12;
                preDiff = (preNum1 - preNum2) % 12;
            }
            else
            {
                diff = (num2 - num1) % 12;
                preDiff = (preNum2 - preNum1) % 12;
            }

            if (diff == 7 && preDiff == 7) c5 = true;
            if (diff == 0 && preDiff == 0) c8 = true;

            if (isThree)
            {
                (bool preC5, bool preC8) = pre.NoteContinuous5or8(vo1, vo2, false);
                c5 = c5 && preC5;
                c8 = c8 && preC8;
            }

            return (c5, c8);
        }

        private bool CheckLimitedProgNoteException(int vo)
        {
            if (voice[vo].isT || voice[vo].isR) return false;
            int nn = voice[vo].num;

            if (pre is null) return false;
            int? preNum = pre.NoteDetect(vo);
            if(preNum is int preNn)
            {
                Chord preCh = pre.ChordDetect();

                if (preCh == Chord.V || preCh == Chord.V7 || preCh == Chord.V7omit5 || preCh == Chord.Vx7)
                {
                    if(preNn % 12 == 11)
                    {
                        if (nn == preNn + 1) return false;
                        else return true;
                    }
                    else if(preNn % 12 == 5)
                    {
                        if (nn == preNn - 1) return false;
                        else return true;
                    }
                }
                else if (preCh == Chord.II7 || preCh == Chord.II7omit5 || preCh == Chord.IIx7)
                {
                    if (preNn % 12 == 6)
                    {
                        if (nn == preNn + 1) return false;
                        else return true;
                    }
                    else if (preNn % 12 == 0)
                    {
                        if (nn == preNn - 1) return false;
                        else return true;
                    }
                }

                return false;
            }
            else
            {
                return false;
            }
        }

        private bool CheckAbnormalChangeException(int vo)
        {
            if (voice[vo].isT || voice[vo].isR) return false;
            int nn = voice[vo].num;

            if (pre is null) return false;
            int? preNum = pre.NoteDetect(vo);
            if(preNum is int preNn)
            {
                if (Math.Abs(nn - preNn) > 9 && Math.Abs(nn - preNn) != 12) return true;
            }

            return false;
        }

        private bool CheckProgressionException()
        {
            if (pre is null) return false;
            Chord ch = ChordDetect();
            Chord preCh = pre.ChordDetect();
            if (ch == preCh) return false;

            switch (preCh)
            {
                case Chord.I:
                    return false;
                case Chord.II:
                    if (ch == Chord.V || ch == Chord.V7 || ch == Chord.V7omit5 || ch == Chord.Vx7) return false;
                    else return true;
                case Chord.II7:
                    goto case Chord.II;
                case Chord.II7omit5:
                    goto case Chord.II;
                case Chord.IIx7:
                    goto case Chord.II;
                case Chord.IV:
                    if (ch == Chord.VI) return true;
                    else return false;
                case Chord.V:
                    if (ch == Chord.I || ch == Chord.V7 || ch == Chord.V7omit5 || ch == Chord.Vx7 || ch == Chord.VI) return false;
                    else return true;
                case Chord.V7:
                    if (ch == Chord.I || ch == Chord.VI) return false;
                    else return true;
                case Chord.V7omit5:
                    goto case Chord.V7;
                case Chord.Vx7:
                    goto case Chord.V7;
                case Chord.VI:
                    if (ch == Chord.I) return true;
                    else return false;
                default:
                    return true;
            }
        }

        public void SetPreChordObj(ChordObj chord)
        {
            pre = chord;
            ch = null;
        }

        public Chord GetChord()
        {
            if(ch is null)
            {
                ch = ChordDetect();
            }

            return ch ?? Chord.I;
        }

        private Chord ChordDetect()
        {
            int[] names = NamesDetect();
            int num = names.Length;

            bool[] possible = new bool[]
            {
                true, true, true, true, true, true, true, true, true, true, true
            };

            int[] notes = new int[]
            {
                -3, -3, -4, -3, -3, -3, -3, -4, -3, -3, -3
            };

            //ローカル関数
            void Contains(params Chord[] chord)
            {
                for(int i = 0; i < 11; ++i)
                {
                    if (chord.Contains((Chord)i))
                    {
                        ++notes[i];
                    }
                    else
                    {
                        possible[i] = false;
                    }
                }
            }

            for (int i = 0; i < num; ++i)
            {
                bool contFlag = false;
                for(int j = 0; j < i; ++j)
                {
                    if (names[i] == names[j]) contFlag = true;
                }
                if (contFlag) continue;

                switch (names[i])
                {
                    case 0: //C
                        Contains(Chord.I, Chord.II7, Chord.II7omit5, Chord.IIx7, Chord.IV, Chord.VI);
                        break;
                    case 2: //D
                        Contains(Chord.II, Chord.II7, Chord.II7omit5, Chord.V, Chord.V7, Chord.Vx7);
                        break;
                    case 4: //E
                        Contains(Chord.I, Chord.VI);
                        break;
                    case 5: //F
                        Contains(Chord.II, Chord.IV, Chord.V7, Chord.V7omit5, Chord.Vx7);
                        break;
                    case 6: //F#
                        Contains(Chord.II7, Chord.II7omit5, Chord.IIx7);
                        break;
                    case 7: //G
                        Contains(Chord.I, Chord.V, Chord.V7, Chord.V7omit5);
                        break;
                    case 9: //A
                        Contains(Chord.II, Chord.II7, Chord.IIx7, Chord.IV, Chord.VI);
                        break;
                    case 11://B
                        Contains(Chord.V, Chord.V7, Chord.V7omit5, Chord.Vx7);
                        break;
                }
            }

            Chord output = Chord.I;
            bool isOne = false;

            for(int i = 0; i < 11; ++i)
            {
                if(possible[i] && notes[i] == 0)
                {
                    if (!isOne)
                    {
                        output = (Chord)i;
                        isOne = true;
                    }
                    else
                    {
                        Console.Error.WriteLine("エラー：chordに変換できません");
                        throw new Exception();
                    }
                }
            }

            if (!isOne)
            {
                Console.Error.WriteLine("エラー：chordに変換できません");
                throw new Exception();
            }

            return output;
        }

        private int[] NamesDetect()
        {
            List<int> names = new List<int>();
            for(int i = 0; i < 4; ++i)
            {
                Note iNote = NameDetect(i);
                if (!iNote.isR) names.Add(iNote.num);
            }

            return names.ToArray();
        }

        private Note NameDetect(int pos)
        {
            if (!(voice[pos].isT && pre is null) && !voice[pos].isR)
            {
                if (voice[pos].isT)
                {
                    return pre?.NameDetect(pos) ?? Note.R;
                }
                else
                {
                    int output = voice[pos].num % 12;
                    return (Note)((output < 0) ? output + 12 : output);
                }
            }
            else
            {
                return Note.R;
            }
        }

        public int? NoteDetect(int pos)
        {
            if (!(voice[pos].isT && pre is null) && !voice[pos].isR)
            {
                if (voice[pos].isT)
                {
                    return pre?.NoteDetect(pos) ?? null;
                }
                else
                {
                    return voice[pos].num;
                }
            }
            else
            {
                return null;
            }
        }
    }

    class BarObj : ScoreObj
    {
        public readonly BarType bar;
        private BarObj partner;

        public BarObj(BarType b)
        {
            bar = b;
        }

        public void SetPartner(BarObj b)
        {
            partner = b;
        }

        public BarObj GetPartner()
        {
            return partner ?? this;
        }
    }

    enum BarType
    {
        Line,
        RepStart,
        RepEnd
    }
}
