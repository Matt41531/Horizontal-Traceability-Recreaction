//N-gram text generation
//Author: Thanh Dao, Thanh.dao@gmx.net
using System;
using System.Collections;

namespace FeatureTool
{
	/// <summary>
	/// Summary description for NGram.
	/// </summary>
	public class NGram
	{
		public NGram()
		{
			//
			// TODO: Add constructor logic here
			//
		}

		
		internal static string[] GenerateNGrams(string text, int gramLength)
		{
			if ( text == null || text.Length == 0)
				return null;
			
			ArrayList grams=new ArrayList();
			int length=text.Length;
			if (length < gramLength)
			{
				string gram;
				for (int i=1; i <= length; i++)
				{
					gram=text.Substring(0, (i) - (0));
					if (grams.IndexOf(gram) == - 1)											
						grams.Add(gram);					
				}
				
				gram=text.Substring(length - 1, (length) - (length - 1));
				if (grams.IndexOf(gram) == - 1)								
					grams.Add(gram);
				
			}
			else
			{
				for (int i=1; i <= gramLength - 1; i++)
				{
					string gram=text.Substring(0, (i) - (0));
					if (grams.IndexOf(gram) == - 1)
						grams.Add(gram);
					
				}
				
				for (int i=0; i < (length - gramLength) + 1; i++)
				{
					string gram=text.Substring(i, (i + gramLength) - (i));
					if (grams.IndexOf(gram) == - 1)								
						grams.Add(gram);					
				}
				
				for (int i=(length - gramLength) + 1; i < length; i++)
				{
					string gram=text.Substring(i, (length) - (i));
					if (grams.IndexOf(gram) == - 1)										
						grams.Add(gram);					
				}
			}
			return Tokeniser.ArrayListToArray(grams);
		}
		
		public static float ComputeNGramSimilarity(string text1, string text2, int gramlength)
		{
			if ((object) text1 == null || (object) text2 == null || text1.Length == 0 || text2.Length == 0)
				return 0.0F;
			string[] grams1=GenerateNGrams(text1, gramlength);
			string[] grams2=GenerateNGrams(text2, gramlength);
			int count=0;
			for (int i=0; i < grams1.Length; i++)
			{
				for (int j=0; j < grams2.Length; j++)
				{
					if (!grams1[i].Equals(grams2[j]))
						continue;
					count++;
					break;
				}
			}
						
			float sim=(2.0F * (float) count) / (float) (grams1.Length + grams2.Length);
			return sim;
		}
		
		public static float GetBigramSimilarity(string text1, string text2)
		{
			return ComputeNGramSimilarity(text1, text2, 2);
		}
		
		public static float GetTrigramSimilarity(string text1, string text2)
		{
			return ComputeNGramSimilarity(text1, text2, 3);
		}
		
		public static float GetQuadGramSimilarity(string text1, string text2)
		{
			return ComputeNGramSimilarity(text1, text2, 4);
		}
		
	}
}
