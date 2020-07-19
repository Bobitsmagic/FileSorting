using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace FileCreator
{
	class Program
	{
		const int FileSize = 200_000_000;

		//Choose this number so that 3 arrays of size SamplesSize can fit into your ram
		const int SampleSize = 4_000_000;

		static void Main(string[] args)
		{
			string filePath = Environment.CurrentDirectory + "\\data.txt";
			string resPath = Environment.CurrentDirectory + "\\result";
			string bufferPath = Environment.CurrentDirectory + "\\buffer";

			Directory.CreateDirectory(resPath);
			Directory.CreateDirectory(bufferPath);

			//Creating a test file with #FileSize entries
			CreateTestFile(FileSize, filePath);

			Console.WriteLine("Created file");
			//Split the main file into #FileSize/#SampleSize  
			long start = Environment.TickCount;
			SplitFiles(filePath, resPath, SampleSize);
			Console.WriteLine("Split: " + (Environment.TickCount - start));

			start = Environment.TickCount;
			//Merge sort all subfiles                                   #number if sub files needed (round up)
			string resDir = SortFiles(resPath, bufferPath, SampleSize, (FileSize - 1) / SampleSize + 1);
			Console.WriteLine("Sort: " + (Environment.TickCount - start));

			//Check if everything went correctly
			CheckFiles(resDir, (FileSize - 1) / SampleSize + 1);

			Console.WriteLine("Done");
			Console.ReadLine();
		}

		public static void CreateTestFile(int n, string path)
		{
			Random rnd = new Random(0);
			string alphabet = "abcdefghijklmnopqrstuvwxyz";

			StreamWriter sw = File.CreateText(path);
			int k = 1_000_000;
			StringBuilder s = new StringBuilder(k * 40 * 2);
			for (int i = 0; i < n; i++)
			{
				for (int j = 0; j < 10; j++)
				{
					s.Append(alphabet[rnd.Next(alphabet.Length)]);
				}

				s.Append("#");

				for (int j = 0; j < 30; j++)
				{
					s.Append(alphabet[rnd.Next(alphabet.Length)]);
				}

				s.Append("\n");

				if ((i + 1) % k == 0)
				{
					sw.Write(s);
					s.Clear();
				}
			}

			if (s.Length != 0)
			{
				sw.WriteLine(s);
			}

			sw.Close();
		}
		public static void SplitFiles(string ogFile, string result, int sampleSize)
		{
			//BinaryReader br = new BinaryReader();
			FileStream fs = File.OpenRead(ogFile);
			byte[] list = new byte[sampleSize * 42];
			byte[] buffer = new byte[sampleSize * 42];

			int[] indices = new int[sampleSize];

			int fileCounter = 0;
			int n;

			//[TODO] n
			while ((n = fs.Read(list, 0, list.Length)) != 0)
			{
				for (int i = 0; i < sampleSize; i++) indices[i] = i;

				long start = Environment.TickCount;
				Array.Sort(indices, (x, y) =>
				{
					for (int i = 0; i < 42; i++)
					{
						if (list[x * 42 + i] != list[y * 42 + i]) return list[x * 42 + i].CompareTo(list[y * 42 + i]);
					}

					return 0;
				});
				Console.WriteLine("Time sorting: " + (Environment.TickCount - start));


				for (int i = 0; i < sampleSize; i++)
					Buffer.BlockCopy(list, indices[i] * 42, buffer, i * 42, 42);

				File.WriteAllBytes(result + "\\" + fileCounter++.ToString() + ".txt", buffer);
			}
		}

		public static string SortFiles(string readPath, string writePath, int sampleSize, int fileCount)
		{
			byte[] a = new byte[sampleSize * 42], b = new byte[sampleSize * 42];
			byte[] res = new byte[sampleSize * 42];
			int resIndex = 0;

			//Merge 2 lists of size stepsize / 2
			for (int stepSize = 2; stepSize < fileCount * 2; stepSize *= 2)
			{
				int resFile = 0;
				for (int i = 0; i < fileCount; i += stepSize)
				{
					int fileA = i;
					int fileB = i + stepSize / 2;

					int indexA = 0, indexB = 0;

					
					
					if (fileA < Math.Min(fileCount, i + stepSize / 2))
					{
						FileStream fs = File.OpenRead(readPath + "\\" + fileA.ToString() + ".txt");
						fs.Read(a, 0, sampleSize * 42);
						fs.Close();
					}
				
					if (fileB < Math.Min(fileCount, i + stepSize))
					{
						FileStream fs = File.OpenRead(readPath + "\\" + fileB.ToString() + ".txt");
						fs.Read(b, 0, sampleSize * 42);
						fs.Close();
					}
					


					//While both lists are not exceeded
					while (fileA < Math.Min(fileCount, i + stepSize / 2) && fileB < Math.Min(fileCount, i + stepSize))
					{
						//while no buffer exceeds
						while (indexA < sampleSize && indexB < sampleSize)
						{
							int j;
							for (j = 0; j < 42; j++)
							{
								if (a[indexA * 42 + j] != b[indexB * 42 + j])
								{
									if (a[indexA * 42 + j] < b[indexB * 42 + j])
										Buffer.BlockCopy(a, indexA++ * 42, res, resIndex++ * 42, 42);
									else
										Buffer.BlockCopy(b, indexB++ * 42, res, resIndex++ * 42, 42);
									break;
								}
							}

							if(j == 42)
							{
								Buffer.BlockCopy(a, indexA++ * 42, res, resIndex++ * 42, 42);
							}

							//reached [sampleSize] elements
							if (resIndex == sampleSize)
							{
								File.WriteAllBytes(writePath + "\\" + resFile++.ToString() + ".txt", res);

								resIndex = 0;
							}
						}

						//load new buffer
						if (indexB == sampleSize)
						{
							fileB++;
							indexB = 0;
							if (fileB < Math.Min(fileCount, i + stepSize))
							{
								FileStream fs = File.OpenRead(readPath + "\\" + fileB.ToString() + ".txt");
								fs.Read(b, 0, sampleSize * 42);
								fs.Close();
							}
						}

						if (indexA == sampleSize)
						{
							fileA++;
							indexA = 0;

							if(fileA < Math.Min(fileCount, i + stepSize / 2))
							{
								FileStream fs = File.OpenRead(readPath + "\\" + fileA.ToString() + ".txt");
								fs.Read(a, 0, sampleSize * 42);
								fs.Close();

							}
						}
					}

					//Copy the remaining elements of the non exceeded buffer
					while (fileA < Math.Min(fileCount, i + stepSize / 2))
					{
						Buffer.BlockCopy(a, indexA * 42, res, resIndex * 42, (sampleSize - indexA) * 42);
						
						File.WriteAllBytes(writePath + "\\" + resFile++.ToString() + ".txt", res);
						resIndex = 0;

						indexA = 0;
						fileA++;

						//the rest of the files dont need to be processesd, just copied
						while (fileA < Math.Min(fileCount, i + stepSize / 2))
							File.Copy(readPath + "\\" + fileA++.ToString() + ".txt", writePath + "\\" + resFile++.ToString() + ".txt", true);
					}
					while (fileB < Math.Min(fileCount, i + stepSize))
					{
						Buffer.BlockCopy(b, indexB * 42, res, resIndex * 42, (sampleSize - indexB) * 42);
						File.WriteAllBytes(writePath + "\\" + resFile++.ToString() + ".txt", res);
						resIndex = 0;

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

			for (int i = 0; i < fileCount; i++)
			{
				string[] lines = File.ReadAllLines(result + "\\" + i.ToString() + ".txt");

				for (int j = 0; j < lines.Length; j++)
				{
					if (s.CompareTo(lines[j]) > 0) Console.WriteLine("Error at file " + i + " line " + j);

					s = lines[j];
				}
			}
		}
	}
}
