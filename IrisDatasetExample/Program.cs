using HdbscanSharp.Distance;
using HdbscanSharp.Runner;
using System;
using System.Globalization;
using System.IO;
using System.Linq;

namespace IrisDatasetExample
{
	internal static class Program
	{
		private static void Main()
		{
			var dataset = LoadCsv("iris.csv", 5);

			var result = HdbscanRunner.Run(dataset.Length, 25, 25, GenericCosineSimilarity.GetFunc(dataset));
			//var result = HdbscanRunner.Run(dataset.Length, 25, 25, GenericEuclideanDistance.GetFunc(dataset));
			//var result = HdbscanRunner.Run(dataset.Length, 25, 25, DistanceHelpers.GetFunc(new CosineSimilarity(true, true), dataset));

			for (var specie = 1; specie <= 3; specie++)
			{
				var offset = (specie - 1) * 50;
				const int size = 50;

				Console.Write("Specie #" + specie + " ");

				for (var i = 0; i < size; i++)
				{
					var label = result.Labels[offset + i];
					Console.Write(label);
				}
				Console.WriteLine();
			}
			Console.WriteLine();

			Console.WriteLine("Press any key to continue...");
			Console.ReadLine();
		}

		private static double[][] LoadCsv(
			string fileName,
			int numberOfValues)
		{
			var lines = File.ReadLines(fileName)
				.Skip(1) /* Skip header. */;

			return lines.Select(line => line.Split(',')
				.Take(numberOfValues)
				.Select(m => double.Parse(m, CultureInfo.InvariantCulture))
				.ToArray()).ToArray();
		}
	}
}
