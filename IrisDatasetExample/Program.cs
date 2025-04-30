using HdbscanSharp.Distance;
using HdbscanSharp.Runner;
using System;
using System.Globalization;
using System.IO;
using System.Linq;
using CsvHelper;
using IrisDatasetExample;

// Load csv.
using var sr = new StreamReader("iris.csv");
using var csv = new CsvReader(sr, CultureInfo.InvariantCulture);
var dataset = csv.GetRecords<Flower>().ToList();

// Run the algo:
var result = HdbscanRunner.Run(dataset, flower => flower.Vector, 25, 25, GenericCosineSimilarity.GetFunc);

// Show results:
foreach (var group in result.Groups)
{
	Console.Write("Group #" + group.Key);

	foreach (var flower in group.Value)
		Console.Write(" " + flower.Species);

	Console.WriteLine();
}

Console.WriteLine();
Console.WriteLine("Press any key to continue...");
Console.ReadLine();