using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Diagnostics;
using System.Collections;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;

namespace SortBigFile
{
    class Program
    {
        static void Main(string[] args)
        {
            var sw = Stopwatch.StartNew();

            var dataPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\Data\");
            var fileName = "BigFile.txt";
            var fullFileName = Path.Combine(dataPath, fileName);

            if (File.Exists(fullFileName))
            {
                var comparer = StringComparer.Ordinal;

                W("Splitting");
                var records = SplitAndSort(dataPath, fileName, comparer);
                GC.Collect();
                W("Splitting complete");

                MemoryUsage();

                W("Merging");
                MergeChunks(dataPath, comparer, records);
                W("Merging complete");

                MemoryUsage();

                Console.WriteLine("Operation completed in {0} sec", sw.Elapsed.TotalSeconds);
            }
            else
            {
                Console.WriteLine("File \"{0}\" not found", fullFileName);
            }

            Console.WriteLine("Press any key...");
            Console.ReadKey();
        }

        static long SplitAndSort(string dataPath, string fileName, IComparer<string> comparer)
        {
            var records = 0L;

            var maxLine = 2500000 * 3;

            var mergeSortHelper = new MergeSortHelper<string>(comparer);
            var split_num = 1;
            var line = string.Empty;
            var contents = new string[maxLine];
            var i = 0;
            var intLen = int.MaxValue.ToString().Length;
            var indx = 0;

            Action sortAndSave = () =>
            {
                mergeSortHelper.MergeSort(contents, 0, contents.Length - 1, true);
                File.WriteAllLines(Path.Combine(dataPath, string.Format("sorted{0:d5}.dat", split_num)), contents);
            };

            using (var sr = new StreamReader(Path.Combine(dataPath, fileName)))
            {
                while (sr.Peek() >= 0)
                {
                    if (++records % 5000 == 0)
                        Console.Write("{0:f2}%   \r",
                          100.0 * sr.BaseStream.Position / sr.BaseStream.Length);

                    line = sr.ReadLine();
                    indx = line.IndexOf('.');
                    contents[i] = string.Format("{0}.{1}", line.Substring(indx + 1), line.Substring(0, indx).PadLeft(intLen));
                    i++;

                    if (i == maxLine && sr.Peek() >= 0)
                    {
                        sortAndSave();
                        split_num++;

                        i = 0;
                    }
                }
            }

            if (i > 0)
            {
                Array.Resize(ref contents, i);
                sortAndSave();
            }

            return records;
        }

        class DataReader
        {
            public DataReader(string path)
            {
                this.path = path;
                reader = new StreamReader(path);
                Next();
            }

            private string path;
            private StreamReader reader;
            private bool isEnd;
            private string current;

            public void Next()
            {
                if (isEnd)
                    return;

                if (reader.Peek() < 0)
                {
                    isEnd = true;
                    return;
                }

                current = reader.ReadLine();
            }

            public void Close()
            {
                reader.Close();
                File.Delete(path);
            }

            public bool IsEnd
            {
                get
                {
                    return isEnd;
                }
            }

            public string Current
            {
                get
                {
                    return current;
                }
            }
        }

        static void MergeChunks(string dataPath, IComparer<string> comparer, long records)
        {
            var paths = Directory.GetFiles(dataPath, "sorted*.dat");
            var chunks = paths.Length;
            if (chunks == 0)
                return;

            var readers = new DataReader[chunks];
            try
            {
                for (int i = 0; i < chunks; i++)
                {
                    readers[i] = new DataReader(paths[i]);
                }

                var lowest_index = -1;
                var lowest_value = string.Empty;
                var curr_value = string.Empty;
                DataReader lowest_reader = null;
                DataReader curr_reader = null;
                var progress = 0L;
                var indx = 0;
                var j = 0;
                var progressBlock = records / 10000;

                using (var sw = new StreamWriter(Path.Combine(dataPath, "BigFileSorted.txt")))
                {
                    while (true)
                    {
                        lowest_index = -1;
                        lowest_value = string.Empty;
                        lowest_reader = null;
                        for (j = 0; j < chunks; j++)
                        {
                            curr_reader = readers[j];
                            if (!curr_reader.IsEnd)
                            {
                                curr_value = curr_reader.Current;
                                if (lowest_index < 0 || comparer.Compare(curr_value, lowest_value) < 0)
                                {
                                    lowest_index = j;
                                    lowest_value = curr_value;
                                    lowest_reader = curr_reader;
                                }
                            }
                        }

                        if (lowest_index == -1)
                            break;

                        var cnt = readers.AsParallel().Select((reader, index) =>
                        {
                            var res = 0;

                            if (reader == lowest_reader)
                            {
                                res++;
                                reader.Next();
                            }

                            while (true)
                            {
                                if (!reader.IsEnd && comparer.Compare(reader.Current, lowest_value) == 0)
                                {
                                    res++;
                                    reader.Next();
                                }
                                else
                                {
                                    break;
                                }
                            }

                            return res;
                        }).Sum();

                        if (cnt > 0)
                        {
                            indx = lowest_value.LastIndexOf('.');
                            curr_value = string.Format("{0}.{1}", lowest_value.Substring(indx + 1).TrimStart(), lowest_value.Substring(0, indx));

                            for (j = 0; j < cnt; j++)
                            {
                                sw.WriteLine(curr_value);
                                if (++progress % progressBlock == 0)
                                    Console.Write("{0:f2}%   \r", 100.0 * progress / records);
                            }
                        }
                    }
                }
            }
            finally
            {
                for (int i = 0; i < chunks; i++)
                {
                    readers[i].Close();
                }
            }
        }

        static void W(string s)
        {
            Console.WriteLine("{0}: {1}", DateTime.Now.ToLongTimeString(), s);
        }

        static void MemoryUsage()
        {
            W(string.Format("{0} MB peak working set | {1} MB private bytes",
              Process.GetCurrentProcess().PeakWorkingSet64 / 1024 / 1024,
              Process.GetCurrentProcess().PrivateMemorySize64 / 1024 / 1024
              ));
        }
    }
}
