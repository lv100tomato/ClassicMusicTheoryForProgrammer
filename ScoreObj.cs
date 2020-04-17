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

    class ScoreObj
    {
    }

    class ChordObj : ScoreObj
    {
        private Note[] voice;
        private Chord? ch;
        private ChordObj pre = null;

        public ChordObj(Note soprano, Note alto, Note tenor, Note bass)
        {
            voice = new Note[4];
            voice[0] = soprano;
            voice[1] = alto;
            voice[2] = tenor;
            voice[3] = bass;
            ch = null;
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
                        Console.WriteLine("エラー：chordに変換できません");
                        throw new Exception();
                    }
                }
            }

            if (!isOne)
            {
                Console.WriteLine("エラー：chordに変換できません");
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
            if(!(voice[pos].isT && pre is null) && !voice[pos].isR)
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
