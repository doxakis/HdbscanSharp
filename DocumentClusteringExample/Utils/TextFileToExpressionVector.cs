using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DocumentClusteringExample.Utils
{
	public class TextFileToExpressionVectorOption
	{
		public int MinWordCount { get; set; }
		public int MaxWordCount { get; set; }
		public string[] BadPatternList { get; set; }
		public ValueStrategy Strategy { get; set; }
		public int MinVectorElements { get; set; }
		public bool ReplaceMissingValueWithRandomValue { get; set; }
	}
	
	public enum ValueStrategy
	{
		Presence,
		Freq,
		PositionInText
	}

	public class TextFileToExpressionVector
	{
		public static double[] GenerateExpressionVector(
			List<string> expressions,
			string filePath,
			TextFileToExpressionVectorOption option)
		{
			var vector = new double[expressions.Count];

			var content = File.ReadAllText(filePath).ToLower();

			var sentences = content.Split(new char[] {
					'.', '?', '\n', '\r', '!', ';', ':', '/'
				}, StringSplitOptions.RemoveEmptyEntries);

			int beforeSentence = 0;

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

						var index = expressions.IndexOf(groupOfWords.Trim());
						if (index >= 0)
						{
							if (option.Strategy == ValueStrategy.Presence)
							{
								vector[index] = 1;
							}
							else if(option.Strategy == ValueStrategy.Freq)
							{
								vector[index]++;
							}
							else if (option.Strategy == ValueStrategy.PositionInText)
							{
								if (vector[index] == 0)
								{
									vector[index] = beforeSentence + i;
								}
							}
						}
					}
				}

				beforeSentence += words.Length;
			}

			if (vector.Sum() < option.MinVectorElements)
			{
				return vector;
			}

			if (option.Strategy == ValueStrategy.PositionInText)
			{
				for (int i = 0; i < vector.Length; i++)
				{
					if (vector[i] == 0)
					{
						vector[i] = 0;
					}
					else
					{
						vector[i] = (10 + 1.0 * vector[i] / beforeSentence);
					}
				}
			}
			
			if (option.ReplaceMissingValueWithRandomValue)
			{
				Random r = new Random();
				for (int i = 0; i < vector.Length; i++)
				{
					if (vector[i] == 0)
					{
						vector[i] = r.Next(1000, 10000);
					}
				}
			}

			return vector;
		}
	}
}
