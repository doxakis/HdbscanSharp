using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DocumentClusteringExample.Utils
{
	public class ExtractExpressionFromTextFilesOption
	{
		public int MinWordFrequency { get; set; }
		public int MinWordCount { get; set; }
		public int MaxWordCount { get; set; }
		public int MinGroupOfWordsLength { get; set; }
		public int MinWordLength { get; set; }
		public int FirstWordMinLength { get; set; }
		public int LastWordMinLength { get; set; }
		public int MaxExpressionComposition { get; set; }
		public string[] BadPatternList { get; set; }
		public string[] BadWords { get; set; }
	}

	public class ExtractExpressionFromTextFiles
	{
		public static List<string> ExtractExpressions(
			List<string> pathFiles,
			ExtractExpressionFromTextFilesOption option)
		{
			Dictionary<string, int> freq = new Dictionary<string, int>();
			var filesCount = pathFiles.Count();
			for (int iFile = 0; iFile < filesCount; iFile++)
			{
				var pathFile = pathFiles[iFile];

				// Get words from file.
				var content = File.ReadAllText(pathFile).ToLower();

				var sentences = content.Split(new char[] {
					'.', '?', '\n', '\r', '!', ';', ':', '/'
				}, StringSplitOptions.RemoveEmptyEntries);

				for (int iSentence = 0; iSentence < sentences.Length; iSentence++)
				{
					var sentence = sentences[iSentence];

					if (option.BadPatternList.Any(m => sentence.Contains(m)))
					{
						continue;
					}

					var words = sentence.Split(new char[] {
						' ', '.', '?', '\n', '\r', ',', '!', '(', ')', ';', '"', ':', '-', '\\', '/',
						'[', ']', '{', '}', '0', '1', '2', '3', '4', '5', '6', '7', '8', '9'
					}, StringSplitOptions.RemoveEmptyEntries);

					// Generate group(s)
					for (int wordCount = option.MinWordCount; wordCount <= option.MaxWordCount; wordCount++)
					{
						for (int i = wordCount; i < words.Length; i++)
						{
							var groupOfWords = "";
							for (int j = 0; j < wordCount; j++)
							{
								var currentWord = words[i - j];
								groupOfWords = currentWord + " " + groupOfWords;
							}

							var key = groupOfWords.Trim();

							if (option.BadWords.Contains(key))
							{
								continue;
							}

							if (freq.ContainsKey(key))
							{
								freq[key] = freq[key] + 1;
							}
							else
							{
								freq.Add(key, 1);
							}
						}
					}
				}
			};

			var topList = freq

				// Analysis based on freq.
				.Where(m => m.Value >= option.MinWordFrequency)

				// Exclude small words.
				.Where(m => m.Key.Length >= option.MinGroupOfWordsLength)

				// At least one long word
				.Where(m => !m.Key.Split(' ').All(mm => mm.Length < option.MinWordLength))

				// First word min length
				.Where(m => m.Key.Split(' ').First().Length >= option.FirstWordMinLength)

				// Last word min length
				.Where(m => m.Key.TrimEnd().Split(' ').Last().Length >= option.LastWordMinLength)

				.OrderByDescending(m => m.Key.Length)
				.ThenByDescending(m => m.Key.Count(mm => mm == ' '))
				.ThenByDescending(m => m.Value)
				.ToList();

			Dictionary<string, List<int>> exclusionList = new Dictionary<string, List<int>>();
			for (int i = 0; i < topList.Count; i++)
			{
				var item = topList[i];
				var words = item.Key.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

				foreach (var word in words)
				{
					if (!exclusionList.ContainsKey(word))
					{
						exclusionList.Add(word, new List<int>());
					}
					exclusionList[word].Add(i);
				}
			}
			IEnumerable<int> excludeRows = exclusionList
				.Where(m => m.Value.Count > option.MaxExpressionComposition)
				.SelectMany(m => m.Value)
				.Distinct()
				.OrderByDescending(m => m);
			foreach (var row in excludeRows)
			{
				topList.RemoveAt(row);
			}

			return topList.Select(m => m.Key).ToList();
		}
	}
}
