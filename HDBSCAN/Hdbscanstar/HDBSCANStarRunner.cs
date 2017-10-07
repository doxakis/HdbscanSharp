using HDBSCAN.Distance;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HDBSCAN.Hdbscanstar
{
	/**
	 * Entry point for the HDBSCAN* algorithm.
	 */
	public class HDBSCANStarRunner
	{
		private const string FILE_FLAG = "file=";
		private const string CONSTRAINTS_FLAG = "constraints=";
		private const string MIN_PTS_FLAG = "minPts=";
		private const string MIN_CL_SIZE_FLAG = "minClSize=";
		private const string COMPACT_FLAG = "compact=";
		private const string DISTANCE_FUNCTION_FLAG = "dist_function=";
		private const string EUCLIDEAN_DISTANCE = "euclidean";
		private const string COSINE_SIMILARITY = "cosine";
		private const string PEARSON_CORRELATION = "pearson";
		private const string MANHATTAN_DISTANCE = "manhattan";
		private const string SUPREMUM_DISTANCE = "supremum";

		/**
		 * Runs the HDBSCAN* algorithm given an input data set file and a value for minPoints and
		 * minClusterSize.  Note that the input file must be a comma-separated value (CSV) file, and
		 * that all of the output files will be CSV files as well.  The flags "file=", "minPts=",
		 * "minClSize=", "constraints=", and "distance_function=" should be used to specify the input 
		 * data set file, value for minPoints, value for minClusterSize, input constraints file, and 
		 * the distance function to use, respectively.
		 * @param args The input arguments for the algorithm
		 */
		public static void Run(string[] args)
		{
			long overallStartTime = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;

			//Parse input parameters from program arguments:
			HDBSCANStarParameters parameters = CheckInputParameters(args);

			Console.WriteLine("Running HDBSCAN* on " + parameters.inputFile + " with minPts=" + parameters.minPoints +
					", minClSize=" + parameters.minClusterSize + ", constraints=" + parameters.constraintsFile +
					", compact=" + parameters.compactHierarchy + ", dist_function=" + parameters.distanceFunction.GetName());

			//Read in input file:
			double[][] dataSet = null;

			try
			{
				dataSet = HDBSCANStar.ReadInDataSet(parameters.inputFile, ',');
			}
			catch (IOException)
			{
				Console.WriteLine("Error reading input data set file.");
				Console.ReadLine();
				Environment.Exit(-1);
			}

			int numPoints = dataSet.Length;

			//Read in constraints:
			List<Constraint> constraints = null;

			if (parameters.constraintsFile != null)
			{
				try
				{
					constraints = HDBSCANStar.ReadInConstraints(parameters.constraintsFile, ',');
				}
				catch (IOException)
				{
					Console.WriteLine("Error reading constraints file.");
					Console.ReadLine();
					Environment.Exit(-1);
				}
			}

			//Compute core distances:
			long startTime = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
			double[] coreDistances = HDBSCANStar.CalculateCoreDistances(dataSet, parameters.minPoints, parameters.distanceFunction);
			Console.WriteLine("Time to compute core distances (ms): " + (DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond - startTime));

			//Calculate minimum spanning tree:
			startTime = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
			UndirectedGraph mst = HDBSCANStar.ConstructMST(dataSet, coreDistances, true, parameters.distanceFunction);
			mst.QuicksortByEdgeWeight();
			Console.WriteLine("Time to calculate MST (ms): " + (DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond - startTime));
			
			//Remove references to unneeded objects:
			dataSet = null;
			
			double[] pointNoiseLevels = new double[numPoints];
			int[] pointLastClusters = new int[numPoints];
			
			//Compute hierarchy and cluster tree:
			List<Cluster> clusters = null;

			try
			{
				startTime = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
				clusters = HDBSCANStar.ComputeHierarchyAndClusterTree(mst, parameters.minClusterSize,
						parameters.compactHierarchy, constraints, parameters.hierarchyFile,
						parameters.clusterTreeFile, ',', pointNoiseLevels, pointLastClusters, parameters.visualizationFile);
				Console.WriteLine("Time to compute hierarchy and cluster tree (ms): " + (DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond - startTime));
			}
			catch (IOException)
			{
				Console.WriteLine("Error writing to hierarchy file or cluster tree file.");
				Console.ReadLine();
				Environment.Exit(-1);
			}
			
			//Remove references to unneeded objects:
			mst = null;
			
			//Propagate clusters:
			bool infiniteStability = HDBSCANStar.PropagateTree(clusters);
			
			//Compute final flat partitioning:
			try
			{
				startTime = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
				HDBSCANStar.FindProminentClusters(clusters, parameters.hierarchyFile, parameters.partitionFile,
						',', numPoints, infiniteStability);
				Console.WriteLine("Time to find flat result (ms): " + (DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond - startTime));
			}
			catch (IOException)
			{
				Console.WriteLine("Error writing to partitioning file.");
				Console.ReadLine();
				Environment.Exit(-1);
			}
			
			//Compute outlier scores for each point:
			try
			{
				startTime = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
				HDBSCANStar.CalculateOutlierScores(clusters, pointNoiseLevels, pointLastClusters,
						coreDistances, parameters.outlierScoreFile, ',', infiniteStability);
				Console.WriteLine("Time to compute outlier scores (ms): " + (DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond - startTime));
			}
			catch (IOException)
			{
				Console.WriteLine("Error writing to outlier score file.");
				Console.ReadLine();
				Environment.Exit(-1);
			}

			Console.WriteLine("Overall runtime (ms): " + (DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond - overallStartTime));
		}
		
		/**
		 * Parses out the input parameters from the program arguments.  Prints out a help message and
		 * exits the program if the parameters are incorrect.
		 * @param args The input arguments for the program
		 * @return Input parameters for HDBSCAN*
		 */
		private static HDBSCANStarParameters CheckInputParameters(string[] args)
		{
			HDBSCANStarParameters parameters = new HDBSCANStarParameters();
			parameters.distanceFunction = new EuclideanDistance();
			parameters.compactHierarchy = false;

			//Read in the input arguments and assign them to variables:
			foreach (string argument in args)
			{
				//Assign input file:
				if (argument.StartsWith(FILE_FLAG) && argument.Length > FILE_FLAG.Length)
					parameters.inputFile = argument.Substring(FILE_FLAG.Length);

				//Assign constraints file:
				if (argument.StartsWith(CONSTRAINTS_FLAG) && argument.Length > CONSTRAINTS_FLAG.Length)
					parameters.constraintsFile = argument.Substring(CONSTRAINTS_FLAG.Length);
				//Assign minPoints:
				else if (argument.StartsWith(MIN_PTS_FLAG) && argument.Length > MIN_PTS_FLAG.Length)
				{
					try
					{
						parameters.minPoints = int.Parse(argument.Substring(MIN_PTS_FLAG.Length));
					}
					catch (FormatException)
					{
						Console.WriteLine("Illegal value for minPts.");
					}
				}
				//Assign minClusterSize:
				else if (argument.StartsWith(MIN_CL_SIZE_FLAG) && argument.Length > MIN_CL_SIZE_FLAG.Length)
				{
					try
					{
						parameters.minClusterSize = int.Parse(argument.Substring(MIN_CL_SIZE_FLAG.Length));
					}
					catch (FormatException)
					{
						Console.WriteLine("Illegal value for minClSize.");
					}
				}
				//Assign compact hierarchy:
				else if (argument.StartsWith(COMPACT_FLAG) && argument.Length > COMPACT_FLAG.Length)
				{
					parameters.compactHierarchy = bool.Parse(argument.Substring(COMPACT_FLAG.Length));
				}
				//Assign distance function:
				else if (argument.StartsWith(DISTANCE_FUNCTION_FLAG) && argument.Length > DISTANCE_FUNCTION_FLAG.Length)
				{
					string functionName = argument.Substring(DISTANCE_FUNCTION_FLAG.Length);

					if (functionName.Equals(EUCLIDEAN_DISTANCE))
						parameters.distanceFunction = new EuclideanDistance();
					else if (functionName.Equals(COSINE_SIMILARITY))
						parameters.distanceFunction = new CosineSimilarity();
					else if (functionName.Equals(PEARSON_CORRELATION))
						parameters.distanceFunction = new PearsonCorrelation();
					else if (functionName.Equals(MANHATTAN_DISTANCE))
						parameters.distanceFunction = new ManhattanDistance();
					else if (functionName.Equals(SUPREMUM_DISTANCE))
						parameters.distanceFunction = new SupremumDistance();
					else
						parameters.distanceFunction = null;
				}
			}

			//Check that each input parameter has been assigned:
			if (parameters.inputFile == null)
			{
				Console.WriteLine("Missing input file name.");
				PrintHelpMessageAndExit();
			}
			else if (parameters.minPoints == 0)
			{
				Console.WriteLine("Missing value for minPts.");
				PrintHelpMessageAndExit();
			}
			else if (parameters.minClusterSize == 0)
			{
				Console.WriteLine("Missing value for minClSize");
				PrintHelpMessageAndExit();
			}
			else if (parameters.distanceFunction == null)
			{
				Console.WriteLine("Missing distance function.");
				PrintHelpMessageAndExit();
			}
			
			//Generate names for output files:

			string inputName = parameters.inputFile;
			if (parameters.inputFile.Contains("."))
				inputName = parameters.inputFile.Substring(0, parameters.inputFile.LastIndexOf("."));

			if (parameters.compactHierarchy)
				parameters.hierarchyFile = inputName + "_compact_hierarchy.csv";
			else
				parameters.hierarchyFile = inputName + "_hierarchy.csv";

			parameters.clusterTreeFile = inputName + "_tree.csv";
			parameters.partitionFile = inputName + "_partition.csv";
			parameters.outlierScoreFile = inputName + "_outlier_scores.csv";
			parameters.visualizationFile = inputName + "_visualization.vis";

			return parameters;
		}

		/**
		 * Prints a help message that explains the usage of HDBSCANStarRunner, and then exits the program.
		 */
		private static void PrintHelpMessageAndExit()
		{
			Console.WriteLine();

			Console.WriteLine("Executes the HDBSCAN* algorithm, which produces a hierarchy, cluster tree, " +
					"flat partitioning, and outlier scores for an input data set.");
			Console.WriteLine("Usage: java -jar HDBSCANStar.jar file=<input file> minPts=<minPts value> " +
					"minClSize=<minClSize value> [constraints=<constraints file>] [compact={true,false}] " +
					"[dist_function=<distance function>]");
			Console.WriteLine("By default the hierarchy produced is non-compact (full), and euclidean distance is used.");
			Console.WriteLine("Example usage: \"java -jar HDBSCANStar.jar file=input.csv minPts=4 minClSize=4\"");
			Console.WriteLine("Example usage: \"java -jar HDBSCANStar.jar file=collection.csv minPts=6 minClSize=1 " +
					"constraints=collection_constraints.csv dist_function=manhattan\"");
			Console.WriteLine("Example usage: \"java -jar HDBSCANStar.jar file=data_set.csv minPts=8 minClSize=8 " +
					"compact=true\"");
			Console.WriteLine("In cases where the source is compiled, use the following: \"java HDBSCANStarRunner " +
					"file=data_set.csv minPts=8 minClSize=8 compact=true\"");
			Console.WriteLine();
			Console.WriteLine("The input data set file must be a comma-separated value (CSV) file, where each line " +
					"represents an object, with attributes separated by commas.");
			Console.WriteLine("The algorithm will produce five files: the hierarchy, cluster tree, final flat partitioning, outlier scores, and an auxiliary file for visualization.");
			Console.WriteLine();

			Console.WriteLine("The hierarchy file will be named <input>_hierarchy.csv for a non-compact " +
					"(full) hierarchy, and <input>_compact_hierarchy.csv for a compact hierarchy.");
			Console.WriteLine("The hierarchy file will have the following format on each line:");
			Console.WriteLine("<hierarchy scale (epsilon radius)>,<label for object 1>,<label for object 2>,...,<label for object n>");
			Console.WriteLine("Noise objects are labelled zero.");
			Console.WriteLine();
			
			Console.WriteLine("The cluster tree file will be named <input>_tree.csv");
			Console.WriteLine("The cluster tree file will have the following format on each line:");
			Console.WriteLine("<cluster label>,<birth level>,<death level>,<stability>,<gamma>," +
					"<virtual child cluster gamma>,<character_offset>,<parent>");
			Console.WriteLine("<character_offset> is the character offset of the line in the hierarchy " +
					"file at which the cluster first appears.");
			Console.WriteLine();

			Console.WriteLine("The final flat partitioning file will be named <input>_partition.csv");
			Console.WriteLine("The final flat partitioning file will have the following format on a single line:");
			Console.WriteLine("<label for object 1>,<label for object 2>,...,<label for object n>");
			Console.WriteLine();

			Console.WriteLine("The outlier scores file will be named <input>_outlier_scores.csv");
			Console.WriteLine("The outlier scores file will be sorted from 'most inlier' to 'most outlier', " +
					"and will have the following format on each line:");
			Console.WriteLine("<outlier score>,<object id>");
			Console.WriteLine("<object id> is the zero-indexed line on which the object appeared in the input file.");
			Console.WriteLine();

			Console.WriteLine("The auxiliary visualization file will be named <input>_visulization.vis");
			Console.WriteLine("This file is only used by the visualization module.");
			Console.WriteLine();

			Console.WriteLine("The optional input constraints file can be used to provide constraints for " +
					"the algorithm (semi-supervised flat partitioning extraction).");
			Console.WriteLine("If this file is not given, only stability will be used to selected the " +
					"most prominent clusters (unsupervised flat partitioning extraction).");
			Console.WriteLine("This file must be a comma-separated value (CSV) file, where each line " +
					"represents a constraint, with the two zero-indexed objects and type of constaint " +
					"separated by commas.");
			Console.WriteLine("Use 'ml' to specify a must-link constraint, and 'cl' to specify a cannot-link constraint.");
			Console.WriteLine();

			Console.WriteLine("The optional compact flag can be used to specify if the hierarchy saved to file " +
					"should be the full or the compact one (this does not affect the final partitioning or cluster tree).");
			Console.WriteLine("The full hierarchy includes all levels where objects change clusters or " +
					"become noise, while the compact hierarchy only includes levels where clusters are born or die.");
			Console.WriteLine();

			Console.WriteLine("Possible values for the optional dist_function flag are:");
			Console.WriteLine("euclidean: Euclidean Distance, d = sqrt((x1-y1)^2 + (x2-y2)^2 + ... + (xn-yn)^2)");
			Console.WriteLine("cosine: Cosine Similarity, d = 1 - ((X*Y) / (||X||*||Y||))");
			Console.WriteLine("pearson: Pearson Correlation, d = 1 - (cov(X,Y) / (std_dev(X) * std_dev(Y)))");
			Console.WriteLine("manhattan: Manhattan Distance, d = |x1-y1| + |x2-y2| + ... + |xn-yn|");
			Console.WriteLine("supremum: Supremum Distance, d = max[(x1-y1), (x2-y2), ... ,(xn-yn)]");
			Console.WriteLine();

			Console.ReadLine();
			Environment.Exit(0);
		}

		/**
		 * Simple class for storing input parameters.
		 */
		private class HDBSCANStarParameters
		{
			public string inputFile;
			public string constraintsFile;
			public int minPoints;
			public int minClusterSize;
			public bool compactHierarchy;
			public IDistanceCalculator distanceFunction;
			public string hierarchyFile;
			public string clusterTreeFile;
			public string partitionFile;
			public string outlierScoreFile;
			public string visualizationFile;
		}
	}
}
