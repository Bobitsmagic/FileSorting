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
		
		//Choose this number so that 3 arrays of size SamplesSize can fit into your ram
		const int SampleSize =	4_000_000;

		static void Main(string[] args)
		{
			string filePath = Environment.CurrentDirectory + "\\data.txt";
			string resPath = Environment.CurrentDirectory + "\\result";
			string bufferPath = Environment.CurrentDirectory + "\\buffer";

			Directory.CreateDirectory(resPath);
			Directory.CreateDirectory(bufferPath);

			//Creating a test file with #FileSize entries
			CreateTestFile(FileSize, filePath);

			//Split the main file into #FileSize/#SampleSize  
			SplitFiles(filePath, resPath, SampleSize);

			//Merge sort all subfiles                                   #number if sub files needed (round up)
			string resDir = SortFiles(resPath, bufferPath, SampleSize,  (FileSize - 1) / SampleSize + 1);

			//Check if everything went correctly
			CheckFiles(resDir, (FileSize - 1) / SampleSize + 1);

			Console.WriteLine("Done");
			Console.ReadLine();
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
			string s;

			while ((s = sr.ReadLine()) != null)
			{
				//read lines until you fill 3 buffers
				list[index][lineCounter++] = s;

				if(lineCounter == sampleSize)
				{
					lineCounter = 0;
					index++;

					if (index == list.Length)
					{
						//Sort all 3 buffers in parallel
						Parallel.For(0, list.Length, i => Array.Sort(list[i]));

						//Write all buffers to files
						Parallel.For(0, list.Length, i => File.WriteAllLines(result + "\\" + (fileCounter + i).ToString() + ".txt", list[i]));
						fileCounter += list.Length;

						lineCounter = 0;
						index = 0;
					}
				}
			}

			
			if (index == 0) return;

			//same thing for the remainder (FileSize / SampleSize)
			Parallel.For(0, index, i => Array.Sort(list[i]));
			Parallel.For(0, index, i => File.WriteAllLines(result + "\\" + fileCounter++.ToString() + ".txt", list[i]));
		}

		public static string SortFiles(string readPath, string writePath, int sampleSize, int fileCount)
		{	
			string[] a = null, b = null;
			List<string> res = new List<string>(sampleSize);

			//Merge 2 lists of size stepsize / 2
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


					//While both lists are not exceeded
					while(fileA < Math.Min(fileCount, i + stepSize / 2) && fileB < Math.Min(fileCount, i + stepSize))
					{
						//while no buffer exceeds
						while (indexA < a.Length && indexB < b.Length)
						{
							if (a[indexA].CompareTo(b[indexB]) <= 0)
								res.Add(a[indexA++]);
							else
								res.Add(b[indexB++]);

							//reached [sampleSize] elements
							if(res.Count == sampleSize)
							{
								File.WriteAllLines(writePath + "\\" + resFile++.ToString() + ".txt", res);

								res.Clear();
							}
						}

						//load new buffer
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

					//Copy the remaining elements of the non exceeded buffer
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

						//the rest of the files dont need to be processesd, just copied
						while (fileA < Math.Min(fileCount, i + stepSize / 2))
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

						//the rest of the files dont need to be processesd, just copied
						while (fileB < Math.Min(fileCount, i + stepSize))
							File.Copy(readPath + "\\" + fileB++.ToString() + ".txt", writePath + "\\" + resFile++.ToString() + ".txt", true);
					}
				}

				//swap read and write directory
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
