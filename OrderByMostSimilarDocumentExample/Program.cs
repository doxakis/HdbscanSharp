using DocumentClusteringExample;
using DocumentClusteringExample.Utils;
using HdbscanSharp.Distance;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OrderByMostSimilarDocumentExample
{
	public class FileAndVector
	{
		public string Path { get; set; }
		public double[] Vector { get; set; }
	}

	public class Program
	{
		static void Main(string[] args)
		{
			// Specify which files to use.
			var projectDir = Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.FullName;
			var pathFiles = Directory.EnumerateFiles(projectDir + @"\OrderByMostSimilarDocumentExample\Samples").ToList();

			// Hyper parameters.

			// This option prevent overfitting on missing words.
			var replaceMissingValueWithRandomValue = false;
			
			var strategy = ValueStrategy.Presence;
			var minVectorElements = 25;
			var freqMin = 5;
			var minWordCount = 1;
			var maxWordCount = 3;
			var minGroupOfWordsLength = 1;
			var minWordLength = 1;
			var firstWordMinLength = 1;
			var lastWordMinLength = 1;
			var maxComposition = 50;
			var badWords = File.ReadLines(projectDir + @"\DocumentClusteringExample\stop-words-english.txt")
				.Where(m => !string.IsNullOrWhiteSpace(m))
				.ToArray();
			var badPatternList = new string[]
			{
			};

			// Files -> List of expressions (Our dictionary based on files)
			var expressions = ExtractExpressionFromTextFiles.ExtractExpressions(
				pathFiles,
				new ExtractExpressionFromTextFilesOption
				{
					BadPatternList = badPatternList,
					BadWords = badWords,
					FirstWordMinLength = firstWordMinLength,
					LastWordMinLength = lastWordMinLength,
					MaxExpressionComposition = maxComposition,
					MaxWordCount = maxWordCount,
					MinGroupOfWordsLength = minGroupOfWordsLength,
					MinWordCount = minWordCount,
					MinWordFrequency = freqMin,
					MinWordLength = minWordLength
				});
			Console.WriteLine("Expressions: " + expressions.Count);

			// Files -­> Vectors
			var expressionVectorOption = new TextFileToExpressionVectorOption
			{
				MinVectorElements = minVectorElements,
				BadPatternList = badPatternList,
				MaxWordCount = maxWordCount,
				MinWordCount = minWordCount,
				Strategy = strategy,
				ReplaceMissingValueWithRandomValue = replaceMissingValueWithRandomValue
			};
			List<Tuple<string, double[]>> filesToVector = new List<Tuple<string, double[]>>();
			foreach (var pathFile in pathFiles)
			{
				filesToVector.Add(
					new Tuple<string, double[]>(
						pathFile,
						TextFileToExpressionVector.GenerateExpressionVector(
							expressions,
							pathFile,
							expressionVectorOption)
						)
					);
			}
			var vectors = filesToVector
				.Select(m => m.Item2)
				.ToList();
			Console.WriteLine("vectors count: " + vectors.Count);

			// Remove non-representative vectors
			for (int i = 0; i < vectors.Count; i++)
			{
				var vector = vectors[i];
				if (vector.Sum() < minVectorElements)
				{
					vectors.RemoveAt(i);
					pathFiles.RemoveAt(i);
					i--;
				}
			}
			Console.WriteLine("vectors count (after removing non-representative vectors): " + vectors.Count);

			var listFileAndVector = new List<FileAndVector>();
			for (int i = 0; i < vectors.Count; i++)
			{
				var path = pathFiles[i];
				var vector = vectors[i];
				listFileAndVector.Add(new FileAndVector { Path = path, Vector = vector });
			}

			var distanceFunc = new CustomDistance();

			Shuffle(listFileAndVector);

			for (int i = 0; i < listFileAndVector.Count; i++)
			{
				var element = listFileAndVector[i];
				var orderedList = listFileAndVector.OrderByDescending(m => distanceFunc.ComputeDistance(element.Vector, m.Vector));

				var pathA = Path.GetFileNameWithoutExtension(element.Path);
				pathA = string.Join("", pathA.Take(70));
				var catA = pathA.Split('-')[0].Trim();

				int countSameCat = 0;

				Console.WriteLine("\n\n\n# " + pathA + "\n");
				foreach (var item in orderedList.Skip(1).Take(5))
				{
					var pathB = Path.GetFileNameWithoutExtension(item.Path);
					pathB = string.Join("", pathB.Take(70));
					var catB = pathB.Split('-')[0].Trim();

					double score = distanceFunc.ComputeDistance(element.Vector, item.Vector);

					if (catA == catB)
					{
						countSameCat++;
					}

					Console.WriteLine(" - " + pathB + " " + string.Format("{0:#.##}", score));
				}
				Console.WriteLine("\nSame category: " + countSameCat);
				Console.ReadLine();
			}

			Console.WriteLine("Press any key to continue...");
			Console.ReadLine();
		}

		private static Random rng = new Random();

		public static void Shuffle<T>(IList<T> list)
		{
			int n = list.Count;
			while (n > 1)
			{
				n--;
				int k = rng.Next(n + 1);
				T value = list[k];
				list[k] = list[n];
				list[n] = value;
			}
		}
	}
}
