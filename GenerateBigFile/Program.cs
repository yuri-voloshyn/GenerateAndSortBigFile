using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GenerateBigFile
{
    class Program
    {
        static void Main(string[] args)
        {
            var sw = Stopwatch.StartNew();

            var dataPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\Data\");
            var fileName = "BigFile.txt";

            var strings = new[] { "Apple", "Something something something", "Cherry is the best", "Banana is yellow" };
            var stringLen = strings.Length;

            var maxNumber = 10000;
            var maxSize = 1024 * 1024 * 1024L;// * 10L;

            var progressBlock = maxSize / 10000;
            var rnd = new Random(maxNumber);
            var progress = 0L;
            var streamLen = 0L;
            var line = string.Empty;

            W("Start");

            if (!Directory.Exists(dataPath))
                Directory.CreateDirectory(dataPath);

            using (var outfile = new StreamWriter(Path.Combine(dataPath, fileName)))
            {
                while (true)
                {
                    if (streamLen >= maxSize)
                        break;

                    if (streamLen > progress + progressBlock)
                    {
                        Console.Write("{0:f2}%   \r", 100.0 * streamLen / maxSize);
                        progress += progressBlock;
                    }

                    line = string.Format("{0}. {1}", rnd.Next(1, maxNumber), strings[rnd.Next(stringLen)]);
                    outfile.WriteLine(line);
                    streamLen += line.Length;
                }
            }

            W("Complete");

            Console.WriteLine("Operation completed in {0} sec", sw.Elapsed.TotalSeconds);

            Console.WriteLine("Press any key...");
            Console.ReadKey();
        }

        static void W(string s)
        {
            Console.WriteLine("{0}: {1}", DateTime.Now.ToLongTimeString(), s);
        }
    }
}
