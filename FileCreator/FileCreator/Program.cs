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

			CreateTestFile(FileSize, filePath);

			SplitFiles(filePath, resPath, SampleSize);

			string resDir = SortFiles(resPath, bufferPath, SampleSize,  (FileSize - 1) / SampleSize + 1);

			CheckFiles(resDir, (FileSize - 1) / SampleSize + 1);
		}

		public static void CreateTestFile(int n, string path)
		{
			Random rnd = new Random();
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
						Parallel.For(0, list.Length, i => Array.Sort(list[i]));

						Parallel.For(0, list.Length, i => File.WriteAllLines(result + "\\" + (fileCounter + i).ToString() + ".txt", list[i]));
						fileCounter += list.Length;

						lineCounter = 0;
						index = 0;

						Console.WriteLine("SplitFiles: " + ((double)fileCounter * 100 / FileCount + " %"));
					}
				}
			}

			if (index == 0) return;

			Parallel.For(0, index, i => Array.Sort(list[i]));
			Parallel.For(0, index, i => File.WriteAllLines(result + "\\" + fileCounter++.ToString() + ".txt", list[i]));
		}

		public static string SortFiles(string readPath, string writePath, int sampleSize, int fileCount)
		{	
			string[] a = null, b = null;
			List<string> res = new List<string>(sampleSize);

			for(int stepSize = 2; stepSize < fileCount * 2; stepSize *= 2)
			{
				int resFile = 0;
				for (int i = 0; i < fileCount; i += stepSize)
				{
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
