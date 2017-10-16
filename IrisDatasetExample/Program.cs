using HdbscanSharp.Distance;
using HdbscanSharp.Hdbscanstar;
using HdbscanSharp.Runner;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IrisDatasetExample
{
	class Program
	{
		static void Main(string[] args)
		{
			var dataset = LoadCsv("iris.csv", 5);

			var result = HdbscanRunner.Run(new HdbscanParameters
			{
				DataSet = dataset,
				MinPoints = 25,
				MinClusterSize = 25,
				DistanceFunction = new CosineSimilarity()
			});

			for (int specie = 1; specie <= 3; specie++)
			{
				int offset = (specie - 1) * 50;
				int size = 50;

				Console.Write("Specie #" + specie + " ");

				for (int i = 0; i < size; i++)
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
			List<double[]> myDataSet = new List<double[]>();

			var lines = File.ReadLines(fileName)
				.Skip(1) /* Skip header. */;

			foreach (var line in lines)
			{
				var values = line.Split(',')
					.Take(numberOfValues)
					.Select(m => double.Parse(m, CultureInfo.InvariantCulture))
					.ToArray();
				
				myDataSet.Add(values);
			}

			return myDataSet.ToArray();
		}
	}
}
