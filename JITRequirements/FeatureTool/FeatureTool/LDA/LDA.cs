//#define blei_corpus
using System;
using System.Collections.Generic;
using System.Text;
using MicrosoftResearch.Infer.Distributions;
using MicrosoftResearch.Infer.Maths;
using MicrosoftResearch.Infer.Models;
using MicrosoftResearch.Infer;
using System.Diagnostics;
using System.Linq;

namespace FeatureTool
{
	class LDA
	{
        public static double[][] LDAmatrix;

        /// <summary>
        /// Run a single test for a single model
        /// </summary>
        /// <param name="sizeVocab">Size of the vocabulary</param>
        /// <param name="numTopics">Number of topics</param>
        /// <param name="alpha">Background pseudo-counts for distributions over topics</param>
        /// <param name="beta">Background pseudo-counts for distributions over words</param>
        /// <param name="shared">If true, uses shared variable version of the model</param>
        /// <param name="vocabulary">Vocabulary</param>
        public static void RunTest(
            int sizeVocab,
            int numTopics,
            Dictionary<int, int>[] allWords,
            double alpha,
            double beta,
            bool shared,
            Dictionary<int, string> vocabulary
            )
        {
            Stopwatch stopWatch = new Stopwatch();
            // Square root of number of documents is the optimal for memory
            int batchCount = (int)Math.Sqrt((double)allWords.Length);
            Rand.Restart(5);
            ILDA model;
            //LDAPredictionModel predictionModel;
            //LDATopicInferenceModel topicInfModel;
            if (shared)
            {
                model = new LDAShared(batchCount, sizeVocab, numTopics);
                ((LDAShared)model).IterationsPerPass = Enumerable.Repeat(10, 5).ToArray();
            }
            else
            {
                model = new LDAModel(sizeVocab, numTopics);
                model.Engine.NumberOfIterations = 50;
            }

            Utilities.LogMessageToFile(MainForm.logfile, "\n\n************************************");
            Utilities.LogMessageToFile(MainForm.logfile, 
                String.Format("\nTraining {0}LDA model...\n",
                shared ? "batched " : "non-batched "));

            // Train the model - we will also get rough estimates of execution time and memory
            Dirichlet[] postTheta, postPhi;
            GC.Collect();
            PerformanceCounter memCounter = new PerformanceCounter("Memory", "Available MBytes");
            float preMem = memCounter.NextValue();
            stopWatch.Reset();
            stopWatch.Start();
            double logEvidence = model.Infer(allWords, alpha, beta, out postTheta, out postPhi);
            stopWatch.Stop();
            float postMem = memCounter.NextValue();
            double approxMB = preMem - postMem;
            GC.KeepAlive(model); // Keep the model alive to this point (for the	memory counter)
            Utilities.LogMessageToFile(MainForm.logfile, String.Format("Approximate memory usage: {0:F2} MB", approxMB));
            Utilities.LogMessageToFile(MainForm.logfile, String.Format("Approximate execution time (including model compilation): {0} seconds", stopWatch.ElapsedMilliseconds / 1000));

            // Calculate average log evidence over total training words
            int totalWords = allWords.Sum(doc => doc.Sum(w => w.Value));
            Utilities.LogMessageToFile(MainForm.logfile,  String.Format("\nTotal number of training words = {0}", totalWords));
            Utilities.LogMessageToFile(MainForm.logfile, String.Format("Average log evidence of model: {0:F2}", logEvidence / (double)totalWords));

            //if (vocabulary != null)
            //{
            //    int numWordsToPrint = 20;
            //    // Print out the top n words for each topic
            //    for (int i = 0; i < postPhi.Length; i++)
            //    {
            //        double[] pc = postPhi[i].PseudoCount.ToArray();
            //        int[] wordIndices = new int[pc.Length];
            //        for (int j = 0; j < wordIndices.Length; j++)
            //            wordIndices[j] = j;
            //        Array.Sort(pc, wordIndices);
            //        Debug.WriteLine("Top {0} words in topic {1}:", numWordsToPrint, i);
            //        int idx = wordIndices.Length;
            //        for (int j = 0; j < numWordsToPrint; j++)
            //            Debug.Write("\t" + vocabulary[wordIndices[--idx]]);
            //        Debug.WriteLine("");
            //    }
            //}

            // nieuwe waardes
            LDAmatrix = new double[postTheta.Length][];
            for (int i = 0; i < postTheta.Length; i++)
            {
                LDAmatrix[i] = new double[postTheta[i].PseudoCount.Count];
                LDAmatrix[i] = postTheta[i].PseudoCount.ToArray();
            }
        }

		/// <summary>
		/// A topic pair
		/// </summary>
		public struct TopicPair
		{
			public int InferredTopic;
			public int TrueTopic;
		}

		/// <summary>
		/// Count the number of correct predictions of the best topic.
		/// This uses a simple greedy algorithm to determine the topic mapping (use with caution!)
		/// </summary>
		/// <param name="topicPairCounts">A dictionary mapping (inferred, true) pairs to counts</param>
		/// <param name="numTopics">The number of topics</param>
		/// <returns></returns>
		public static int CountCorrectTopicPredictions(Dictionary<TopicPair, int> topicPairCounts, int numTopics)
		{
			int[] topicMapping = new int[numTopics];
			for (int i=0; i < numTopics; i++) topicMapping[i] = -1;

			// Sort by count
			List<KeyValuePair<TopicPair, int>> kvps = new List<KeyValuePair<TopicPair, int>>(topicPairCounts); 
			kvps.Sort( 
				delegate(KeyValuePair<TopicPair, int> kvp1, KeyValuePair<TopicPair, int> kvp2)
				{ 
					return kvp2.Value.CompareTo(kvp1.Value); 
				} 
			);

			int correctCount = 0;
			while (kvps.Count > 0)
			{
				KeyValuePair<TopicPair, int> kvpHead = kvps[0];
				int inferredTopic = kvpHead.Key.InferredTopic;
				int trueTopic = kvpHead.Key.TrueTopic;
				topicMapping[inferredTopic] = trueTopic;
				correctCount += kvpHead.Value;
				kvps.Remove(kvpHead);
				// Now delete anything in the list that has either of these
				for (int i = kvps.Count-1; i >= 0; i--)
				{
					KeyValuePair<TopicPair, int> kvp = kvps[i];
					int infTop = kvp.Key.InferredTopic;
					int trueTop = kvp.Key.TrueTopic;
					if (infTop == inferredTopic || trueTop == trueTopic)
						kvps.Remove(kvp);
				}
			}

			return correctCount;
		}

        private static float[] GetTopicVector(int doc)
        {
            int _numTopics = LDAmatrix[0].Length;
            float[] w = new float[_numTopics];
            for (int i = 0; i < _numTopics; i++)
                w[i] = (float)LDAmatrix[doc][i];
            return w;
        }

        public static float GetSimilarity(int doc_i, int doc_j)
        {
            float[] vector1 = GetTopicVector(doc_i);
            float[] vector2 = GetTopicVector(doc_j);

            return TFIDFMeasure.TermVector.ComputeCosineSimilarity(vector1, vector2);

        }
	}
}
