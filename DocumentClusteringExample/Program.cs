using Accord.Statistics.Analysis;
using DocumentClusteringExample.Utils;
using HdbscanSharp.Distance;
using HdbscanSharp.Hdbscanstar;
using HdbscanSharp.Runner;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DocumentClusteringExample
{
	class Program
	{
		static void Main(string[] args)
		{
			// Specify which files to use.
			var projectDir = Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.FullName;
			var pathFiles = Directory.EnumerateFiles(projectDir + @"\DocumentClusteringExample\Samples").ToList();

			// Hyper parameters.
			
			// This option prevent overfitting on missing words.
			var replaceMissingValueWithRandomValue = false;

			var usePCA = false;
			var numberOfOutputPCA = 100;
			var distanceFunction = new PearsonCorrelation();

			var strategy = ValueStrategy.Freq;
			var minVectorElements = 2;
			var freqMin = 2;
			var minWordCount = 1;
			var maxWordCount = 3;
			var minGroupOfWordsLength = 3;
			var minWordLength = 1;
			var firstWordMinLength = 1;
			var lastWordMinLength = 1;
			var maxComposition = int.MaxValue;
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

			// Reduce the vector size with PCA.
			if (usePCA)
			{
				Console.WriteLine("Reducing vector size with PCA");
				Stopwatch stopwatch = new Stopwatch();
				stopwatch.Start();
				PrincipalComponentAnalysis pca = new PrincipalComponentAnalysis();
				pca.NumberOfOutputs = numberOfOutputPCA;
				var trainingVector = vectors.ToArray();
				Shuffle(trainingVector);
				trainingVector = trainingVector.Take(600).ToArray();
				var pcaResult = pca.Learn(trainingVector);
				var reducedVectorsWithPCA = pcaResult.Transform(vectors.ToArray());
				stopwatch.Stop();
				Console.WriteLine("PCA duration: " + stopwatch.Elapsed.ToString());

				vectors = reducedVectorsWithPCA.ToList();
			}
			

			// Run HDBSCAN algo.
			Console.WriteLine("HDBSCAN starting...");

			var contraintsList = new List<HdbscanConstraint>();
			if (usePCA)
			{
				for (int i = 1; i < numberOfOutputPCA; i++)
				{
					contraintsList.Add(new HdbscanConstraint(i - 1, i, HdbscanConstraintType.CannotLink));
				}
			}

            var watch = Stopwatch.StartNew();
			var result = HdbscanRunner.Run(new HdbscanParameters
			{
				DataSet = vectors.ToArray(),
				MinPoints = 5,
				MinClusterSize = 5,
				DistanceFunction = distanceFunction,
				Constraints = contraintsList,
                UseMultipleThread = true
			});
            watch.Stop();
			Console.WriteLine("HDBSCAN done " + watch.Elapsed);

			// Read results.
			var labels = result.Labels;
			int n = labels.Max();

			Console.WriteLine("\n\n");

			int clusterId = 0;
			for (int iCluster = 1; iCluster <= n; iCluster++)
			{
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

						anyFound = true;
					}
				}
				if (anyFound)
				{
					clusterId++;
					Console.WriteLine("Cluster #" + clusterId);

					Console.WriteLine();
					foreach (var category in categories)
					{
						Console.WriteLine(category.Key + ": " + category.Value);
					}
					Console.ReadLine();
				}
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
