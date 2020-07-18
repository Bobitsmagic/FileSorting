using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileCreator
{
	class Program
	{
		const int FileSize =	20_000_000;
		const int SampleSize =	4_000_000;
		const int FileCount = ((FileSize - 1) / SampleSize + 1);

		static void Main(string[] args)
		{
			string filePath = Environment.CurrentDirectory + "\\data.txt";
			string resPath = Environment.CurrentDirectory + "\\result";
			string bufferPath = Environment.CurrentDirectory + "\\buffer";
			Directory.CreateDirectory(resPath);
			Directory.CreateDirectory(bufferPath);

			Console.WriteLine("Main File Entry Count: " + FileSize.ToString("000 000 000"));
			Console.WriteLine("Sub File Entry Count: " + SampleSize.ToString("000 000 000"));
			Console.WriteLine("Sub File Count: " + ((FileSize - 1) / SampleSize + 1).ToString("000 000"));


			long start = Environment.TickCount;
			CreateTestFile(FileSize, filePath);
			Console.WriteLine("Created file in: " + (Environment.TickCount - start).ToString("000 000 000"));

			start = Environment.TickCount;
			SplitFiles(filePath, resPath, SampleSize);
			Console.WriteLine("Split files in: " + (Environment.TickCount - start).ToString("000 000 000"));

			start = Environment.TickCount;
			string resDir = SortFiles(resPath, bufferPath, SampleSize,  (FileSize - 1) / SampleSize + 1);
			Console.WriteLine("Sorted files in: " + (Environment.TickCount - start).ToString("000 000 000"));

			start = Environment.TickCount;
			CheckFiles(resDir, (FileSize - 1) / SampleSize + 1);
			Console.WriteLine("Checked files in: " + (Environment.TickCount - start).ToString("000 000 000"));

			Console.ReadLine();
		}

		public static void CreateTestFile(int n, string path)
		{
			Random rnd = new Random(0);
			string alphabet = "abcdefghijklmnopqrstuvwxyz";

			StreamWriter sw = File.CreateText(path);
			int k = 1_000_000;
			StringBuilder s = new StringBuilder(k * 40 * 2);
			for(int i = 0; i < n; i++)
			{
				for(int j = 0; j < 10; j++)
				{
					s.Append(alphabet[rnd.Next(alphabet.Length)]);
				}

				s.Append("#");

				for (int j = 0; j < 30; j++)
				{
					s.Append(alphabet[rnd.Next(alphabet.Length)]);
				}

				s.Append("\n");

				if((i + 1) % k == 0)
				{
					sw.Write(s);
					s.Clear();

					Console.WriteLine("TestFile: " + ((double)i + 1) * 100 / n + " %");
				}
			}

			sw.Close();
		}
		public static void SplitFiles(string ogFile, string result, int sampleSize)
		{
			StreamReader sr =  new StreamReader(File.OpenRead(ogFile));

			string[][] list = new string[3][];
			for (int i = 0; i < list.Length; i++) list[i] = new string[sampleSize];

			int fileCounter = 0;
			int lineCounter = 0;
			int index = 0;
			long start = Environment.TickCount;
			string s;
			while ((s = sr.ReadLine()) != null)
			{
				list[index][lineCounter++] = s;

				if(lineCounter == sampleSize)
				{
					lineCounter = 0;
					index++;

					if (index == list.Length)
					{
						//Console.WriteLine("ReadTime: " + (Environment.TickCount - start).ToString("000 000"));
						start = Environment.TickCount;

						Parallel.For(0, list.Length, i => Array.Sort(list[i]));

						//Console.WriteLine("SortTime: " + (Environment.TickCount - start).ToString("000 000"));
						start = Environment.TickCount;

						Parallel.For(0, list.Length, i => File.WriteAllLines(result + "\\" + fileCounter++.ToString() + ".txt", list[i]));
						
						//Console.WriteLine("WriteTime: " + (Environment.TickCount - start).ToString("000 000"));
						start = Environment.TickCount;


						lineCounter = 0;
						index = 0;

						Console.WriteLine("SplitFiles: " + ((double)fileCounter * 100 / FileCount + " %"));
					}
				}
			}

			if (index == 0) return;

			//Console.WriteLine("ReadTime: " + (Environment.TickCount - start).ToString("000 000"));
			start = Environment.TickCount;

			Parallel.For(0, index, i => Array.Sort(list[i]));

			//Console.WriteLine("SortTime: " + (Environment.TickCount - start).ToString("000 000"));
			start = Environment.TickCount;

			Parallel.For(0, index, i => File.WriteAllLines(result + "\\" + fileCounter++.ToString() + ".txt", list[i]));

			//Console.WriteLine("WriteTime: " + (Environment.TickCount - start).ToString("000 000"));
		}

		public static string SortFiles(string readPath, string writePath, int sampleSize, int fileCount)
		{	
			string[] a = null, b = null;
			List<string> res = new List<string>(sampleSize);

			for(int stepSize = 2; stepSize < fileCount * 2; stepSize *= 2)
			{
				Console.WriteLine("Current stepSize: " + stepSize);
				//Console.WriteLine(stepSize);
				int resFile = 0;
				for (int i = 0; i < fileCount; i += stepSize)
				{
					Console.WriteLine("Index: " + i);

					int fileA = i;
					int fileB = i + stepSize / 2;

					int indexA = 0, indexB = 0;
					if (fileA < i + stepSize / 2) 
						a = File.ReadAllLines(readPath + "\\" + fileA.ToString() + ".txt");
					if (fileB < Math.Min(fileCount, i + stepSize)) 
						b = File.ReadAllLines(readPath + "\\" + fileB.ToString() + ".txt");
					while(fileA < Math.Min(fileCount, i + stepSize / 2) && fileB < Math.Min(fileCount, i + stepSize))
					{
						while (indexA < a.Length && indexB < b.Length)
						{
							if (a[indexA].CompareTo(b[indexB]) <= 0)
								res.Add(a[indexA++]);
							else
								res.Add(b[indexB++]);

							if(res.Count == sampleSize)
							{
								File.WriteAllLines(writePath + "\\" + resFile++.ToString() + ".txt", res);

								res.Clear();
							}
						}

						if (indexA < a.Length)
						{				
							fileB++;
							indexB = 0;
							if(fileB < Math.Min(fileCount, i + stepSize))
								b = File.ReadAllLines(readPath + "\\" + fileB.ToString() + ".txt");
						}
						else
						{
							fileA++;
							indexA = 0;

							if(fileA < Math.Min(fileCount, i + stepSize / 2))
								a = File.ReadAllLines(readPath + "\\" + fileA.ToString() + ".txt");
						}
					}

					
					while(fileA < Math.Min(fileCount, i + stepSize / 2))
					{
						while(indexA < a.Length)
						{
							res.Add(a[indexA++]);
						}
						File.WriteAllLines(writePath + "\\" + resFile++.ToString() + ".txt", res);
						res.Clear();

						indexA = 0;
						fileA++;

						while(fileA < Math.Min(fileCount, i + stepSize / 2))
							File.Copy(readPath + "\\" + fileA++.ToString() + ".txt", writePath + "\\" + resFile++.ToString() + ".txt", true);
					}
					while (fileB < Math.Min(fileCount, i + stepSize))
					{
						while (indexB < b.Length)
						{
							res.Add(b[indexB++]);
						}
						File.WriteAllLines(writePath + "\\" + resFile++.ToString() + ".txt", res);
						res.Clear();

						indexB = 0;
						fileB++;

						while (fileB < Math.Min(fileCount, i + stepSize))
							File.Copy(readPath + "\\" + fileB++.ToString() + ".txt", writePath + "\\" + resFile++.ToString() + ".txt", true);
					}
				}

				string buffer = writePath;
				writePath = readPath;
				readPath = buffer;
			}

			Console.WriteLine("Results in: " + readPath);
			return readPath;
		}

		public static void CheckFiles(string result, int fileCount)
		{
			string s = "";

			for(int i = 0; i < fileCount; i++)
			{
				string[] lines = File.ReadAllLines(result + "\\" + i.ToString() + ".txt");

				for(int j = 0; j < lines.Length; j++)
				{
					if (s.CompareTo(lines[j]) > 0) Console.WriteLine("Error at file " + i + " line " + j);

					s = lines[j];
				}
			}
		}
	}
}
