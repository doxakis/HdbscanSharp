using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HDBSCAN.Utils
{
	public class TextFileToExpressionVectorOption
	{
		public int MinWordCount { get; set; }
		public int MaxWordCount { get; set; }
		public string[] BadPatternList { get; set; }
		public bool InverseFreq { get; set; }
		public bool UsePresenceInsteadOfFreqSum { get; set; }
	}

	public class TextFileToExpressionVector
	{
		public static double[] GenerateExpressionVector(
			List<string> expressions,
			string filePath,
			TextFileToExpressionVectorOption option)
		{
			var vector = new double[expressions.Count];

			var content = File.ReadAllText(filePath);

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
						' ', '.', '?', '\n', '\r', ',', '!', '(', ')', ';', '"', ':', '-'
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

						var index = expressions.IndexOf(groupOfWords.Trim());
						if (index >= 0)
						{
							if (option.UsePresenceInsteadOfFreqSum)
							{
								vector[index] = 1;
							}
							else
							{
								vector[index]++;
							}
						}
					}
				}
			}

			if (option.InverseFreq)
			{
				for (int i = 0; i < vector.Length; i++)
				{
					if (vector[i] != 0)
					{
						vector[i] = 1.0 / vector[i];
					}
				}
			}

			return vector;
		}
	}
}
