using System;
using System.IO;

namespace mszProg1_szorg9_csharp
{
    class LZWBinFa
    {
        private class Csomopont
        {
            private char betu;
            private Csomopont balNulla;
            private Csomopont jobbEgy;

            public Csomopont(char b = '/')
            {
                betu = b;
                balNulla = null;
                jobbEgy = null;
            }

            public Csomopont NullasGyermek()
            {
                return balNulla;
            }

            public Csomopont EgyesGyermek()
            {
                return jobbEgy;
            }

            public void UjNullasGyermek(Csomopont gy)
            {
                balNulla = gy;
            }

            public void UjEgyesGyermek(Csomopont gy)
            {
                jobbEgy = gy;
            }

            public char GetBetu()
            {
                return betu;
            }
        }

        private Csomopont gyoker = new Csomopont('/');
        private Csomopont fa;

        private int melyseg;
        private int atlagosszeg;
        private int atlagdb;
        private int maxMelyseg;

        private double atlag;
        private double szoras;
        private double szorasosszeg;

        public LZWBinFa()
        {
            fa = gyoker;
        }

        // Operátor helyett metódus
        public void AddBit(char b)
        {
            if (b == '0')
            {
                if (fa.NullasGyermek() == null)
                {
                    Csomopont uj = new Csomopont('0');
                    fa.UjNullasGyermek(uj);
                    fa = gyoker;
                }
                else
                {
                    fa = fa.NullasGyermek();
                }
            }
            else
            {
                if (fa.EgyesGyermek() == null)
                {
                    Csomopont uj = new Csomopont('1');
                    fa.UjEgyesGyermek(uj);
                    fa = gyoker;
                }
                else
                {
                    fa = fa.EgyesGyermek();
                }
            }
        }

        public void Kiir(TextWriter os)
        {
            melyseg = 0;
            Kiir(gyoker, os);
        }

        private void Kiir(Csomopont elem, TextWriter os)
        {
            if (elem != null)
            {
                ++melyseg;

                Kiir(elem.EgyesGyermek(), os);

                for (int i = 0; i < melyseg; ++i)
                    os.Write("---");

                os.WriteLine(elem.GetBetu() + "(" + (melyseg - 1) + ")");

                Kiir(elem.NullasGyermek(), os);

                --melyseg;
            }
        }

        public int GetMelyseg()
        {
            melyseg = 0;
            maxMelyseg = 0;

            RMelyseg(gyoker);

            return maxMelyseg - 1;
        }

        private void RMelyseg(Csomopont elem)
        {
            if (elem != null)
            {
                ++melyseg;

                if (melyseg > maxMelyseg)
                    maxMelyseg = melyseg;

                RMelyseg(elem.EgyesGyermek());
                RMelyseg(elem.NullasGyermek());

                --melyseg;
            }
        }

        public double GetAtlag()
        {
            melyseg = 0;
            atlagosszeg = 0;
            atlagdb = 0;

            RAtlag(gyoker);

            atlag = (double)atlagosszeg / atlagdb;

            return atlag;
        }

        private void RAtlag(Csomopont elem)
        {
            if (elem != null)
            {
                ++melyseg;

                RAtlag(elem.EgyesGyermek());
                RAtlag(elem.NullasGyermek());

                --melyseg;

                if (elem.EgyesGyermek() == null &&
                    elem.NullasGyermek() == null)
                {
                    ++atlagdb;
                    atlagosszeg += melyseg;
                }
            }
        }

        public double GetSzoras()
        {
            atlag = GetAtlag();

            szorasosszeg = 0.0;
            melyseg = 0;
            atlagdb = 0;

            RSzoras(gyoker);

            if (atlagdb - 1 > 0)
                szoras = Math.Sqrt(szorasosszeg / (atlagdb - 1));
            else
                szoras = Math.Sqrt(szorasosszeg);

            return szoras;
        }

        private void RSzoras(Csomopont elem)
        {
            if (elem != null)
            {
                ++melyseg;

                RSzoras(elem.EgyesGyermek());
                RSzoras(elem.NullasGyermek());

                --melyseg;

                if (elem.EgyesGyermek() == null &&
                    elem.NullasGyermek() == null)
                {
                    ++atlagdb;

                    szorasosszeg +=
                        ((melyseg - atlag) * (melyseg - atlag));
                }
            }
        }
    }

    class Program
    {
        static void Usage()
        {
            Console.WriteLine("Usage: lzwtree in_file -o out_file");
        }

        static int Main(string[] args)
        {
            if (args.Length != 3)
            {
                Usage();
                return -1;
            }

            string inFile = args[0];

            if (args[1] != "-o")
            {
                Usage();
                return -2;
            }

            string outFile = args[2];

            if (!File.Exists(inFile))
            {
                Console.WriteLine(inFile + " nem letezik...");
                Usage();
                return -3;
            }

            LZWBinFa binFa = new LZWBinFa();

            using (FileStream beFile = new FileStream(inFile, FileMode.Open, FileAccess.Read))
            using (BinaryReader reader = new BinaryReader(beFile))
            {
                byte b;

                // első sor átugrása
                while (reader.BaseStream.Position < reader.BaseStream.Length)
                {
                    b = reader.ReadByte();

                    if (b == 0x0A)
                        break;
                }

                bool kommentben = false;

                while (reader.BaseStream.Position < reader.BaseStream.Length)
                {
                    b = reader.ReadByte();

                    if (b == 0x3E) // >
                    {
                        kommentben = true;
                        continue;
                    }

                    if (b == 0x0A) // \n
                    {
                        kommentben = false;
                        continue;
                    }

                    if (kommentben)
                        continue;

                    if (b == 0x4E) // N
                        continue;

                    for (int i = 0; i < 8; ++i)
                    {
                        if ((b & 0x80) != 0)
                            binFa.AddBit('1');
                        else
                            binFa.AddBit('0');

                        b <<= 1;
                    }
                }
            }

            using (StreamWriter kiFile = new StreamWriter(outFile))
            {
                binFa.Kiir(kiFile);

                kiFile.WriteLine("depth = " + binFa.GetMelyseg());
                kiFile.WriteLine("mean = " + binFa.GetAtlag());
                kiFile.WriteLine("var = " + binFa.GetSzoras());
            }

            return 0;
        }
    }
}