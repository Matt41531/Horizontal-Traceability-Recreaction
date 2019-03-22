using System;
using System.Collections;
using System.Collections.Generic;
// Located in c:\Program Files (x86)\COEST\TraceLab\lib\TraceLabSDK.dll
using TraceLabSDK;

namespace TFIDF
{
    [Component(Name = "TFIDF",
                Description = "Used to convert a collection of documents into a collection of vectors (our Vector Space Model, VSM) by calculating the relative importance of each word in each document",
                Author = "Jen Lee - based on work by Thanh Dao",
                Version = "1.0")]

    //[IOSpec(IOType = IOSpecType.Output, Name = "outputName", DataType = typeof(int))]
    public class TFIDF /*: BaseComponent */
    {
        /* public TFIDF(ComponentLogger log) : base(log) { }

        public override void Compute()
        {
            // your component implementation
            Logger.Trace("Hello World");

            //Workspace.Store("outputName", 5);
        } */

        private string[] _docs;
        //private string[][] _ngramDoc;
        private int _numDocs = 0;
        private int _numTerms = 0;
        public ArrayList _terms;
        public int[][] _termFreq;
        public float[][] _termWeight;
        private int[] _maxTermFreq;
        private int[] _docFreq;
        //private List<string[]> _parsedDocs = new List<string[]>();

        public class TermVector
        {
            public static float ComputeCosineSimilarity(float[] vector1, float[] vector2)
            {
                if (vector1.Length != vector2.Length)
                    throw new Exception("DIFER LENGTH");


                float denom = (VectorLength(vector1) * VectorLength(vector2));
                if (denom == 0F)
                    return 0F;
                else
                    return (InnerProduct(vector1, vector2) / denom);

            }

            public static float InnerProduct(float[] vector1, float[] vector2)
            {

                if (vector1.Length != vector2.Length)
                    throw new Exception("DIFFER LENGTH ARE NOT ALLOWED");


                float result = 0F;
                for (int i = 0; i < vector1.Length; i++)
                    result += vector1[i] * vector2[i];

                return result;
            }

            public static float VectorLength(float[] vector)
            {
                float sum = 0.0F;
                for (int i = 0; i < vector.Length; i++)
                    sum = sum + (vector[i] * vector[i]);

                return (float)Math.Sqrt(sum);
            }

        }

        private IDictionary _wordsIndex = new Hashtable();

        public TFIDF(string[] documents, bool bStem, bool bRemoveStop, bool bLower, bool bBi, bool bSyn, bool bCode, bool bMulti, bool bDoc)
        {
            _docs = documents;
            _numDocs = documents.Length;
            MyInit(bStem, bRemoveStop, bLower, bBi, bSyn, bCode, bMulti, bDoc);
        }

        public TFIDF(FeatureCollection fc, bool bStem, bool bRemoveStop, bool bWTitle, bool bBi, bool bSyn, bool bLower, bool bCode, bool bMulti, bool bDoc)
        {
            _numDocs = fc.featureList.Count;
            _docs = new string[fc.featureList.Count];
            int i = 0;
            foreach (Feature f in fc.featureList)
            {
                _docs[i] = f.doc;
                i++;
            }
            MyInit(bStem, bRemoveStop, bLower, bBi, bSyn, bCode, bMulti, bDoc);
        }

        //Cross-measure TFIDF with title en summary of feature iFt vs all comments of all others
        public TFIDF(FeatureCollection fc, int iFt, bool bStem, bool bRemoveStop, bool bWTitle, bool bBi, bool bSyn, bool bLower, bool bCode, bool bMulti, bool bDoc)
        {
            _numDocs = fc.featureList.Count;
            _docs = new string[fc.featureList.Count];
            int i = 0;
            foreach (Feature f in fc.featureList)
            {
                if (fc.featureList.IndexOf(f) != iFt)
                {
                    _docs[i] = f.doc;
                }
                else
                {
                    _docs[i] = f.title + " \n " + f.description;
                }
                i++;
            }
            MyInit(bStem, bRemoveStop, bLower, bBi, bSyn, bCode, bMulti, bDoc);
        }

        private void GeneratNgramText()
        {

        }

        private ArrayList GenerateTerms(string[] docs, bool bStem, bool bRemoveStop, bool bLower, bool bBi, bool bSyn, bool bCode, bool bMulti, bool bDoc)
        {
            ArrayList uniques = new ArrayList();
            List<int> counts = new List<int>();
            List<int> docCounts = new List<int>();
            //_ngramDoc=new string[_numDocs][] ;
            int totalMatches = 0;
            for (int i = 0; i < docs.Length; i++)
            {
                Tokeniser tokenizer = new Tokeniser(bStem, bRemoveStop, bLower);
                int nrMatches = 0;
                string[] words = tokenizer.Partition(docs[i], bBi, bSyn, bCode, out nrMatches);
                totalMatches += nrMatches;
                //Utilities.LogMessageToFile(MainForm.logfile, i + ": " + nrMatches + " matches removed");
                //_parsedDocs.Add(words);
                //words = tokenizer.Partition(docs[i], bBi, bSyn, bCode);

                for (int j = 0; j < words.Length; j++)
                {
                    if (!uniques.Contains(words[j]))
                    {
                        uniques.Add(words[j]);
                        counts.Add(1);
                        if (bDoc) docCounts.Add(i);
                    }
                    else
                    {
                        int wordIndex = uniques.IndexOf(words[j]);
                        counts[wordIndex] += 1;
                        if (bDoc)
                        {
                            if (docCounts[wordIndex] < i)
                            {
                                docCounts[wordIndex] = -1;
                            }
                        }
                    }
                }
            }
            //Utilities.LogMessageToFile(MainForm.logfile, "TOTAL: " + totalMatches + " matches removed");

            if (bMulti)
            {
                for (int i = 0; i < counts.Count; i++)
                {
                    if (bDoc)
                    {
                        if (counts[i] == 1 || docCounts[i] > -1)
                        {
                            uniques.RemoveAt(i);
                            counts.RemoveAt(i);
                            docCounts.RemoveAt(i);
                            i--;
                        }
                    }
                    else
                    {
                        if (counts[i] == 1)
                        {
                            uniques.RemoveAt(i);
                            counts.RemoveAt(i);
                            i--;
                        }
                    }
                }
            }

            return uniques;
        }



        private static object AddElement(IDictionary collection, object key, object newValue)
        {
            object element = collection[key];
            collection[key] = newValue;
            return element;
        }

        private int GetTermIndex(string term)
        {
            object index = _wordsIndex[term];
            if (index == null) return -1;
            return (int)index;
        }

        private void MyInit(bool bStem, bool bRemoveStop, bool bLower, bool bBi, bool bSyn, bool bCode, bool bMulti, bool bDoc)
        {
            _terms = GenerateTerms(_docs, bStem, bRemoveStop, bLower, bBi, bSyn, bCode, bMulti, bDoc);
            _numTerms = _terms.Count;

            _maxTermFreq = new int[_numDocs];
            _docFreq = new int[_numTerms];
            _termFreq = new int[_numTerms][];
            _termWeight = new float[_numTerms][];

            for (int i = 0; i < _terms.Count; i++)
            {
                _termWeight[i] = new float[_numDocs];
                _termFreq[i] = new int[_numDocs];

                AddElement(_wordsIndex, _terms[i], i);
            }

            GenerateTermFrequency(bStem, bRemoveStop, bLower, bBi, bSyn, bCode);
            // hierna is er een matrix met termen per doc
            //if (bMulti)
            //{

            //    //remove words with total count = 1
            //    for (int i = 0; i < _terms.Count; i++)
            //    {
            //        int totalCount = 0;
            //        int index = 0;
            //        for (int j = 0; j < _numDocs; j++)
            //        {
            //            totalCount = totalCount + _termFreq[i][j];
            //            if (_termFreq[i][j] == 1)
            //            {
            //                index = j;
            //            }
            //        }
            //        if (totalCount == 1)
            //        {
            //            _termFreq[i][index] = 0;
            //        }
            //    }
            //}
            GenerateTermWeight();

        }

        private float Log(float num)
        {
            return (float)Math.Log(num);//log2
        }

        private void GenerateTermFrequency(bool bStem, bool bRemoveStop, bool bLower, bool bBi, bool bSyn, bool bCode)
        {
            for (int i = 0; i < _numDocs; i++)
            {
                string curDoc = _docs[i];
                IDictionary freq = GetWordFrequency(curDoc, bStem, bRemoveStop, bLower, bBi, bSyn, bCode);
                //IDictionary freq = GetWordFrequency(i, bStem, bRemoveStop, bLower, bBi, bSyn, bCode);
                IDictionaryEnumerator enums = freq.GetEnumerator();
                _maxTermFreq[i] = int.MinValue;
                while (enums.MoveNext())
                {
                    string word = (string)enums.Key;
                    int wordFreq = (int)enums.Value;
                    int termIndex = GetTermIndex(word);
                    if (termIndex != -1)
                    {
                        _termFreq[termIndex][i] = wordFreq;
                        _docFreq[termIndex]++;
                        if (wordFreq > _maxTermFreq[i]) _maxTermFreq[i] = wordFreq;
                    }
                    else
                    {
                    }
                }
            }
        }


        private void GenerateTermWeight()
        {
            for (int i = 0; i < _numTerms; i++)
            {
                for (int j = 0; j < _numDocs; j++)
                    _termWeight[i][j] = ComputeTermWeight(i, j);
            }
        }

        private float GetTermFrequency(int term, int doc)
        {
            int freq = _termFreq[term][doc];
            int maxfreq = _maxTermFreq[doc];

            return ((float)freq / (float)maxfreq);
        }

        private float GetInverseDocumentFrequency(int term)
        {
            int df = _docFreq[term];
            return Log((float)(_numDocs) / (float)df);
        }

        private float ComputeTermWeight(int term, int doc)
        {
            float tf = GetTermFrequency(term, doc);
            float idf = GetInverseDocumentFrequency(term);
            return tf * idf;
        }

        public float[] GetTermVector(int doc)
        {
            float[] w = new float[_numTerms];
            for (int i = 0; i < _numTerms; i++)
                w[i] = _termWeight[i][doc];


            return w;
        }

        public float GetSimilarity(int doc_i, int doc_j)
        {
            float[] vector1 = GetTermVector(doc_i);
            float[] vector2 = GetTermVector(doc_j);

            return TermVector.ComputeCosineSimilarity(vector1, vector2);

        }

        //private IDictionary GetWordFrequency(int docNr, bool bStem, bool bRemoveStop, bool bLower, bool bBi, bool bSyn, bool bCode)
        //{
        //    //PH 11 apr string convertedInput=input.ToLower();

        //    //Tokeniser tokenizer = new Tokeniser(bStem, bRemoveStop, bLower);
        //    //String[] words = tokenizer.Partition(input, bBi, bSyn, bCode);
        //    String[] words = _parsedDocs[docNr];
        //    Array.Sort(words);

        //    String[] distinctWords = GetDistinctWords(words);

        //    IDictionary result = new Hashtable();
        //    for (int i = 0; i < distinctWords.Length; i++)
        //    {
        //        object tmp;
        //        tmp = CountWords(distinctWords[i], words);
        //        result[distinctWords[i]] = tmp;

        //    }

        //    return result;
        //}				

        private IDictionary GetWordFrequency(string input, bool bStem, bool bRemoveStop, bool bLower, bool bBi, bool bSyn, bool bCode)
        {
            //PH 11 apr string convertedInput=input.ToLower();

            Tokeniser tokenizer = new Tokeniser(bStem, bRemoveStop, bLower);
            int nrMatches = 0;
            String[] words = tokenizer.Partition(input, bBi, bSyn, bCode, out nrMatches);
            Array.Sort(words);

            String[] distinctWords = GetDistinctWords(words);

            IDictionary result = new Hashtable();
            for (int i = 0; i < distinctWords.Length; i++)
            {
                object tmp;
                tmp = CountWords(distinctWords[i], words);
                result[distinctWords[i]] = tmp;

            }

            return result;
        }

        private string[] GetDistinctWords(String[] input)
        {
            if (input == null)
                return new string[0];
            else
            {
                ArrayList list = new ArrayList();

                for (int i = 0; i < input.Length; i++)
                    if (!list.Contains(input[i])) // N-GRAM SIMILARITY?				
                        list.Add(input[i]);

                return Tokeniser.ArrayListToArray(list);
            }
        }



        private int CountWords(string word, string[] words)
        {
            int itemIdx = Array.BinarySearch(words, word);

            if (itemIdx > 0)
                while (itemIdx > 0 && words[itemIdx].Equals(word))
                    itemIdx--;

            int count = 0;
            while (itemIdx < words.Length && itemIdx >= 0)
            {
                if (words[itemIdx].Equals(word)) count++;

                itemIdx++;
                if (itemIdx < words.Length)
                    if (!words[itemIdx].Equals(word)) break;

            }

            return count;
        }

    }
}