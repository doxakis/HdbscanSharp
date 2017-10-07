﻿using Accord.Statistics.Analysis;
using HDBSCAN.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
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
			// Specify which files to use.
			var projectDir = Directory.GetParent(Directory.GetCurrentDirectory()).Parent.FullName;
			var pathFiles = Directory.EnumerateFiles(projectDir + @"\Samples")
				.ToList();

			/*for (int i = 0; i < pathFiles.Count; i++)
			{
				if (Path.GetFileNameWithoutExtension(pathFiles[i]).Contains("_u_")
					&&
					!Path.GetFileNameWithoutExtension(pathFiles[i]).Contains("comment -"))
				{
					var parts = pathFiles[i].Split('-');

					var newFileName = parts[0].TrimEnd() + " comment - " + parts[1].Trim();

					try
					{
						File.Move(pathFiles[i], newFileName);
					}
					catch (Exception)
					{
					}
					
				}
			}
			return;*/

			// Hyper parameters.
			var minVectorElements = 20;
			var freqMultiplyByVectorSum = false;
			var freqMin = 10;
			var minWordCount = 1;
			var maxWordCount = 1;
			var minGroupOfWordsLength = 6;
			var minWordLength = 1;
			var firstWordMinLength = 1;
			var lastWordMinLength = 1;
			var maxComposition = 50;
			var badPatternList = new string[]
			{
			};

			// Files -> List of expressions (Our dictionary based on files)
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

			// Files -­> Vectors
			var expressionVectorOption = new TextFileToExpressionVectorOption
			{
				MinVectorElements = minVectorElements,
				BadPatternList = badPatternList,
				MaxWordCount = maxWordCount,
				MinWordCount = minWordCount,
				FreqMultiplyByVectorSum = freqMultiplyByVectorSum,
				Strategy = ValueStrategy.PositionInText
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
			Console.WriteLine("Reducing vector size with PCA");
			Stopwatch stopwatch = new Stopwatch();
			stopwatch.Start();
			var vectorArray = vectors.ToArray();
			PrincipalComponentAnalysis pca = new PrincipalComponentAnalysis();
			pca.NumberOfOutputs = 3;
			var trainingVector = vectors.ToArray();
			Shuffle(trainingVector);
			trainingVector = trainingVector.Take(300).ToArray();
			var pcaResult = pca.Learn(trainingVector);
			var reducedVectorsWithPCA = pcaResult.Transform(vectorArray);
			stopwatch.Stop();
			Console.WriteLine("PCA duration: " + stopwatch.Elapsed.ToString());

			// Write vectors on temporary files for HDBSCAN algo.
			StringBuilder vectorsToCsv = new StringBuilder();
			foreach (var vector in reducedVectorsWithPCA)
			{
				vectorsToCsv.Append(string.Join(",",
					vector.Select(m => m.ToString(CultureInfo.InvariantCulture))));
				vectorsToCsv.Append("\n");
			}
			File.WriteAllText("vectors.csv", vectorsToCsv.ToString());

			// HDBSCAN parameters.
			Console.WriteLine("HDBSCAN starting...");
			args = new string[] {
				"file=vectors.csv",
				"minPts=40",
				"minClSize=40",
				"compact=true",
				"dist_function=cosine",
			};
			// Run HDBSCAN algo.
			Hdbscanstar.HDBSCANStarRunner.Run(args);
			Console.WriteLine("HDBSCAN done.");

			// Read results.
			var line = File.ReadAllText("vectors_partition.csv");
			var labels = line
				.Split(',')
				.Select(m => int.Parse(m))
				.ToArray();
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
			Console.ReadLine();
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
