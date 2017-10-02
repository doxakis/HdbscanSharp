using HDBSCAN.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HDBSCAN
{
	class Program
	{
		static void Main(string[] args)
		{
			/*args = new string[] {
				"file=dataset.csv",
				"minPts=2",
				"minClSize=30",
				"compact=true",
				"dist_function=pearson",
			};
			Hdbscanstar.HDBSCANStarRunner.Run(args);*/

			var projectDir = Directory.GetParent(Directory.GetCurrentDirectory()).Parent.FullName;
			var pathFiles = Directory.EnumerateFiles(projectDir + @"\Samples")
				.ToList();

			var usePresenceInsteadOfFreqSum = true;
			var inverseFreq = false;
			var freqMin = 5;
			var minWordCount = 1;
			var maxWordCount = 2;
			var minGroupOfWordsLength = 1;
			var minWordLength = 1;
			var firstWordMinLength = 1;
			var lastWordMinLength = 1;
			var maxComposition = 5;
			var badPatternList = new string[]
			{
				"[link]",
				"[comments]",
				"/u/",
				"/r/",
				"0"
			};

			var expressions = ExtractExpressionFromTextFiles.ExtractExpressions(
				pathFiles,
				new ExtractExpressionFromTextFilesOption
				{
					BadPatternList = badPatternList,
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

			var expressionVectorOption = new TextFileToExpressionVectorOption
			{
				BadPatternList = badPatternList,
				MaxWordCount = maxWordCount,
				MinWordCount = minWordCount,
				InverseFreq = inverseFreq,
				UsePresenceInsteadOfFreqSum = usePresenceInsteadOfFreqSum
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
			var vectors = filesToVector.Select(m => m.Item2);

			StringBuilder vectorsToCsv = new StringBuilder();
			foreach (var vector in vectors)
			{
				vectorsToCsv.Append(string.Join(",", vector));
				vectorsToCsv.Append("\n");
			}
			File.WriteAllText("vectors.csv", vectorsToCsv.ToString());

			args = new string[] {
				"file=vectors.csv",
				"minPts=5",
				"minClSize=10",
				"compact=true",
				"dist_function=pearson",
			};
			Hdbscanstar.HDBSCANStarRunner.Run(args);

			var lines = File.ReadLines("vectors_compact_hierarchy.csv")
				.Where(m => !string.IsNullOrWhiteSpace(m))
				.ToList();
			var line = lines[lines.Count - 2];
			var labels = line
				.Split(',')
				.Skip(1)
				.Select(m => int.Parse(m))
				.ToArray();
			int n = labels.Max();

			Console.WriteLine("# Clusters: " + n);
			Console.ReadLine();

			for (int iCluster = 1; iCluster <= n; iCluster++)
			{
				Console.WriteLine("Cluster #" + iCluster);

				Dictionary<string, int> categories = new Dictionary<string, int>();
				bool anyFound = false;
				for (int i = 0; i < labels.Length; i++)
				{
					if (labels[i] == iCluster)
					{
						var fileName = Path.GetFileNameWithoutExtension(pathFiles[i]);
						var category = fileName.Split('-')[0].Trim();

						if (categories.ContainsKey(category))
						{
							var count = categories[category];
							categories.Remove(category);
							categories.Add(category, count + 1);
						}
						else
						{
							categories.Add(category, 1);
						}

						Console.WriteLine("  " + fileName);
						anyFound = true;
					}
				}
				if (anyFound)
				{
					Console.WriteLine();
					foreach (var category in categories)
					{
						Console.WriteLine(category.Key + ": " + category.Value);
					}

					Console.ReadLine();
				}
				Console.WriteLine();
			}

			Console.WriteLine("Press any key to continue...");
			Console.ReadLine();
		}
	}
}
