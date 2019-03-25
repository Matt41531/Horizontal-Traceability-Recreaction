using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Text.RegularExpressions;

using MicrosoftResearch.Infer.Distributions;
using MicrosoftResearch.Infer.Maths;
using MicrosoftResearch.Infer.Collections;

/* From PorterStreaming */
using System.Runtime.InteropServices;

/* From TFIDFMeasure */
using System.Collections;

/* From Utilities */
using System.IO;

/* From BugzillaFeatureCollection */
using System.Xml.Linq;

// Collection of classes used in all components
namespace InputXML
{
    public class setFiles
    {
        // global variables for input and ouput files
        string fileXML;
        string dirPath;
        public static string tokenizedFile;
        public static string logfile;
        // global values for booleans used in tokenization
        bool AC; bool SC; bool SM; bool SR; bool DW; bool BG; bool SY; bool LO; bool MU; bool DO;
        // globals I don't fully understand yet
        int type = 1; // this changes sometimes??? Used in processXML
        DataSet dsFeatures = new DataSet(); // *** I THINK *** this is what needs to be passed into other components
        public static FeatureCollection fc; // access to the Feature collection class??

        // hmmmm what do these do
        //DataTable dgFeatures_DataSource;
        //DataTable dgComments_DataSource;
        //DataTable bsFeatures_DataSource;
        //DataTable bsComments_DataSource;

        public setFiles(string inputFile, string outputFile, bool bAC, bool bSC, bool bDW)
        {
            // set global variables for input and output files
            fileXML = inputFile; // assign the XML file to be worked with
            dirPath = outputFile; // set location for log file

            // booleans used for the tokenization of a file
            AC = bAC;
            SC = bSC;
            DW = bDW;

            // set log file
            DateTime d = DateTime.Now;
            logfile = outputFile + '\\' + d.Year.ToString("D4") + d.Month.ToString("D2") + d.Day.ToString("D2") + "_" + d.Hour.ToString("D2") + d.Minute.ToString("D2") + d.Second.ToString("D2") + "_log";

        }

        // Function to input and read feature request file
        public FeatureCollection InputXML(string fileName)
        {
            dsFeatures = new DataSet();
            //dsFeatures.Clear();
            dsFeatures.ReadXml(fileName);
            //dgFeatures.DataSource = bsFeatures;
            //dgComments.DataSource = bsComments;
            fc = processXML();
            return fc;
        }

        private FeatureCollection processXML()
        {
            //read XML file into Feature Collection with or without 'All comments' and 'Source code'
            if (type == 0)
            {
                fc = new BugzillaFeatureCollection(fileXML, AC, SC, DW);
            }
            else
            {
                fc = new TigrisFeatureCollection(fileXML, AC, SC, DW);
            }

            // return FeatureCollection
            return fc;
        }

        // function to assign name to csv files
        public string getFileEnding(bool AC, bool SC, bool SM, bool SR, bool DW, bool BG, bool SY, bool LO, bool MU, bool DO)
        {
            string strEnd = "";

            // add boolean values to end
            if (AC)
            {
                strEnd += "_AC";
            }
            if (SC)
            {
                strEnd += "_SC";
            }
            if (SM)
            {
                strEnd += "_SM";
            }
            if (SR)
            {
                strEnd += "_SR";
            }
            if (DW)
            {
                strEnd += "_DW";
            }
            if (BG)
            {
                strEnd += "_BG";
            }
            if (SY)
            {
                strEnd += "_SY";
            }
            if (LO)
            {
                strEnd += "_LO";
            }
            if (MU)
            {
                strEnd += "_MU";
            }
            if (DO)
            {
                strEnd += "_DO";
            }
            return strEnd;
        }

        public int getFeatureIndex(string id)
        {
            foreach (Feature ft in fc.featureList)
            {
                if (ft.id == id)
                {
                    return fc.featureList.IndexOf(ft);
                }
            }
            return -1;
        }
    }

    public class Feature
    {
        public string id;
        public string title;
        public string description;
        public IEnumerable<string> comments;
        public Dictionary<string, int> wordCount = new Dictionary<string, int>();
        public string doc; //text of title and comments
        public string duplicate_id;
        public int dupRank;
        public float dupSim;

        //bAllComments = true means leave in all comments
        public Feature(string id, string title, string desc, IEnumerable<string> comm, bool bAllComments, bool bWTitle)
        {
            this.id = id;
            this.title = title;
            description = desc;
            comments = comm;
            doc += getText(title);
            if (bWTitle) doc += "\n " + getText(title);
            doc += "\n " + getText(desc);
            if (bAllComments == true)
            {
                foreach (string c in comm)
                {
                    doc += "\n " + getText(c);
                }
            }
            //createDictionary(true);
        }

        public Feature(string id, string dup)
        {
            this.id = id;
            this.duplicate_id = dup;
        }


        private void createDictionary(bool stemming)
        {
            MatchCollection matches = Regex.Matches(description, @"[\w\d_]+", RegexOptions.Singleline);
            foreach (Match match in matches)
            {
                if (match.Success)
                {
                    string stemStr;
                    if (stemming)
                    {
                        PorterStemming ps = new PorterStemming();
                        stemStr = ps.stemTerm(match.ToString().ToLower());
                    }
                    else
                    {
                        stemStr = match.ToString().ToLower();
                    }
                    if (wordCount.ContainsKey(stemStr))
                    {
                        wordCount[stemStr]++;
                    }
                    else
                    {
                        wordCount.Add(stemStr, 1);
                    }
                }
            }

        }

        private string getText(string strInput)
        {
            return strInput;
        }
    }

    public class FeatureCollection
    {
        public List<Feature> featureList = new List<Feature>();
        public string bugTag;

        public int TFIDF(string stem)
        {
            int documentCount = 0;
            foreach (Feature ft in featureList)
            {
                if (ft.wordCount.ContainsKey(stem))
                {
                    documentCount++;
                }
            }
            return documentCount;
        }
    }

    /* ****** ****** ****** ****** ******
    fills the featureList with Feature objects read from an XML file
    xmlFilePath: path of the XML file, e.g. "features.xml"
    bAllComments: true if all comments are loaded as a document, false if only first comment is loaded
    bCode: true if code is kept, false if code should be removed
    bWTitle: true if title is doubled in weight
    *  ****** ****** ****** ****** ****** */
    public class TigrisFeatureCollection : FeatureCollection
    {
        // paramaterless constructor *** Added by Lee in the recreation ***
        public TigrisFeatureCollection() { }

        public TigrisFeatureCollection(string xmlFilePath, bool bAllComments, bool bCode, bool bWTitle)
        {
            //read XML file
            XDocument xdoc = XDocument.Load(xmlFilePath);

            //get all <feature> elements in the file
            IEnumerable<XElement> xFeat = from xf in xdoc.Descendants("issue")
                                              //where (string)xf.Element("bug_severity") == "enhancement"
                                          select xf;

            //read through each <feature> element to create a Feature object
            foreach (XElement item in xFeat)
            {
                string iD = (string)(from xe in item.Descendants("issue_id") select xe).First();
                string title = (string)(from xe in item.Descendants("short_desc") select xe).First();
                IEnumerable<string> comm = from xe in item.Descendants("thetext") select (string)xe;
                string desc = comm.First(); //in Bugzilla XML the description is the first comment
                string dupId = (string)(from xe in item.Descendants("is_duplicate").Descendants("issue_id") select xe).FirstOrDefault();
                Feature ft = new Feature(iD, title, desc, comm.Skip(1), bAllComments, bWTitle);
                ft.duplicate_id = dupId;
                featureList.Add(ft);
            }

            bugTag = "issue";
        }
    }

    /* ****** ****** ****** ****** ******
    fills the featureList with Feature objects read from an XML file
    xmlFilePath: path of the XML file, e.g. "features.xml"
    bAllComments: true if all comments are loaded as a document, false if only first comment is loaded
    bCode: true if code is kept, false if code should be removed
    bWTitle: true if title is doubled in weight
    *  ****** ****** ****** ****** ****** */
    class BugzillaFeatureCollection : FeatureCollection
    {
        // paramaterless constructor *** Added by Lee in the recreation ***
        public BugzillaFeatureCollection() { }

        public BugzillaFeatureCollection(string xmlFilePath, bool bAllComments, bool bCode, bool bWTitle)
        {
            //read XML file
            XDocument xdoc = XDocument.Load(xmlFilePath);

            //get all <feature> elements in the file
            IEnumerable<XElement> xFeat = from xf in xdoc.Descendants("bug")
                                              //where (string)xf.Element("bug_severity") == "enhancement"
                                          select xf;

            //read through each <feature> element to create a Feature object
            foreach (XElement item in xFeat)
            {
                string iD = (string)(from xe in item.Descendants("bug_id") select xe).First();
                string title = (string)(from xe in item.Descendants("short_desc") select xe).First();
                IEnumerable<string> comm = from xe in item.Descendants("thetext") select (string)xe;
                string desc = comm.First(); //in Bugzilla XML the description is the first comment
                string dup_id = (string)(from xe in item.Descendants("dup_id") select xe).FirstOrDefault();

                //check if it is a valid feature request
                string resolution = (string)(from xe in item.Descendants("resolution") select xe).First();

                if (!resolution.EndsWith("INVALID"))
                {
                    Feature ft = new Feature(iD, title, desc, comm.Skip(1), bAllComments, bWTitle);
                    ft.duplicate_id = dup_id;
                    featureList.Add(ft);
                }
                else
                {
                    string s = "";
                }
            }

            bugTag = "bug";
        }
    }

    public class Utilities
    {
        /// <summary>
        /// Randomly create true theta and phi arrays
        /// </summary>
        /// <param name="numVocab">Vocabulary size</param>
        /// <param name="numTopics">Number of topics</param>
        /// <param name="numDocs">Number of documents</param>
        /// <param name="averageDocLength">Avarage document length</param>
        /// <param name="trueTheta">Theta array (output)</param>
        /// <param name="truePhi">Phi array (output)</param>
        public static void CreateTrueThetaAndPhi(
            int numVocab, int numTopics, int numDocs, int averageDocLength, int averageWordsPerTopic,
            out Dirichlet[] trueTheta, out Dirichlet[] truePhi)
        {
            truePhi = new Dirichlet[numTopics];
            for (int i = 0; i < numTopics; i++)
            {
                truePhi[i] = Dirichlet.Uniform(numVocab);
                truePhi[i].PseudoCount.SetAllElementsTo(0.0);
                // Draw the number of unique words in the topic.
                int numUniqueWordsPerTopic = Poisson.Sample((double)averageWordsPerTopic);
                if (numUniqueWordsPerTopic >= numVocab) numUniqueWordsPerTopic = numVocab;
                if (numUniqueWordsPerTopic < 1) numUniqueWordsPerTopic = 1;
                double expectedRepeatOfWordInTopic =
                    ((double)numDocs) * averageDocLength / numUniqueWordsPerTopic;
                int[] shuffledWordIndices = Rand.Perm(numVocab);
                for (int j = 0; j < numUniqueWordsPerTopic; j++)
                {
                    int wordIndex = shuffledWordIndices[j];
                    // Draw the count for that word
                    int cnt = Poisson.Sample(expectedRepeatOfWordInTopic);
                    truePhi[i].PseudoCount[wordIndex] = cnt + 1.0;
                }
            }

            trueTheta = new Dirichlet[numDocs];
            for (int i = 0; i < numDocs; i++)
            {
                trueTheta[i] = Dirichlet.Uniform(numTopics);
                trueTheta[i].PseudoCount.SetAllElementsTo(0.0);
                // Draw the number of unique topics in the doc.
                int numUniqueTopicsPerDoc = Math.Min(1 + Poisson.Sample(1.0), numTopics);
                double expectedRepeatOfTopicInDoc =
                    averageDocLength / numUniqueTopicsPerDoc;
                int[] shuffledTopicIndices = Rand.Perm(numTopics);
                for (int j = 0; j < numUniqueTopicsPerDoc; j++)
                {
                    int topicIndex = shuffledTopicIndices[j];
                    // Draw the count for that topic
                    int cnt = Poisson.Sample(expectedRepeatOfTopicInDoc);
                    trueTheta[i].PseudoCount[topicIndex] = cnt + 1.0;
                }
            }
        }

        /// Generate LDA data - returns an array of dictionaries mapping unique word index
        /// to word count per document.
        /// <param name="trueTheta">Known Theta</param>
        /// <param name="truePhi">Known Phi</param>
        /// <param name="averageNumWords">Average number of words to sample per doc</param>
        /// <returns></returns>
        public static Dictionary<int, int>[] GenerateLDAData(Dirichlet[] trueTheta, Dirichlet[] truePhi, int averageNumWords)
        {
            int numVocab = truePhi[0].Dimension;
            int numTopics = truePhi.Length;
            int numDocs = trueTheta.Length;

            // Sample from the model
            Vector[] topicDist = new Vector[numDocs];
            Vector[] wordDist = new Vector[numTopics];
            for (int i = 0; i < numDocs; i++)
                topicDist[i] = trueTheta[i].Sample();
            for (int i = 0; i < numTopics; i++)
                wordDist[i] = truePhi[i].Sample();

            var wordCounts = new Dictionary<int, int>[numDocs];
            for (int i = 0; i < numDocs; i++)
            {
                int LengthOfDoc = Poisson.Sample((double)averageNumWords);

                var counts = new Dictionary<int, int>();
                for (int j = 0; j < LengthOfDoc; j++)
                {
                    int topic = Discrete.Sample(topicDist[i]);
                    int w = Discrete.Sample(wordDist[topic]);
                    if (!counts.ContainsKey(w))
                        counts.Add(w, 1);
                    else
                        counts[w] = counts[w] + 1;
                }
                wordCounts[i] = counts;
            }
            return wordCounts;
        }

        /// <summary>
        /// Calculate perplexity for test words
        /// </summary>
        /// <param name="predictiveDist">Predictive distribution for each document</param>
        /// <param name="testWordsPerDoc">Test words per document</param>
        /// <returns></returns>
        public static double Perplexity(Discrete[] predictiveDist, Dictionary<int, int>[] testWordsPerDoc)
        {
            double num = 0.0;
            double den = 0.0;
            int numDocs = predictiveDist.Length;
            for (int i = 0; i < numDocs; i++)
            {
                Discrete d = predictiveDist[i];
                var counts = testWordsPerDoc[i];
                foreach (KeyValuePair<int, int> kvp in counts)
                {
                    num += kvp.Value * d.GetLogProb(kvp.Key);
                    den += kvp.Value;
                }
            }
            return Math.Exp(-num / den);
        }

        /// Load data. Each line is of the form cnt,wrd1_index:count,wrd2_index:count,...
        /// </summary>
        /// <param name="fileName">The file name</param>
        /// <param name="vocabulary">Vocabulary (output)</param>
        /// <returns></returns>
        public static Dictionary<int, int>[] LoadWordCounts(string fileName)
        {
            List<Dictionary<int, int>> ld = new List<Dictionary<int, int>>();
            using (StreamReader sr = new StreamReader(fileName))
            {
                string str = null;
                while ((str = sr.ReadLine()) != null)
                {
                    string[] split = str.Split(' ', ':');
                    int numUniqueTerms = int.Parse(split[0]);
                    var dict = new Dictionary<int, int>();
                    for (int i = 0; i < (split.Length - 1) / 2; i++)
                        dict.Add(int.Parse(split[2 * i + 1]), int.Parse(split[2 * i + 2]));
                    ld.Add(dict);
                }
            }
            return ld.ToArray();
        }

        /// <summary>
        /// Load the vocabulary
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public static Dictionary<int, string> LoadVocabulary(string fileName)
        {
            Dictionary<int, string> vocab = new Dictionary<int, string>();
            using (StreamReader sr = new StreamReader(fileName))
            {
                string str = null;
                int idx = 0;
                while ((str = sr.ReadLine()) != null)
                    vocab.Add(idx++, str);
            }
            return vocab;
        }

        /// <summary>
        /// Get the vocabulary size for the data (max index + 1)
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static int GetVocabularySize(Dictionary<int, int>[] data)
        {
            int max = int.MinValue;
            foreach (Dictionary<int, int> dict in data)
            {
                foreach (int key in dict.Keys)
                {
                    if (key > max)
                        max = key;
                }
            }
            return max + 1;

        }


        public static void LogMessageToFile(string fileName, string message)
        {
            System.IO.StreamWriter sw =
               System.IO.File.AppendText(fileName); // Change filename
            try
            {
                string logLine =
                   System.String.Format(
                      "{0:G}: {1}.", System.DateTime.Now, message);
                sw.WriteLine(logLine);
            }
            finally
            {
                sw.Close();
            }
        }
    }

    /**
  * Stemmer, implementing the Porter Stemming Algorithm
  *
  * The Stemmer class transforms a word into its root form.  The input
  * word can be provided a character at time (by calling add()), or at once
  * by calling one of the various stem(something) methods.
  */
    public interface StemmerInterface
    {
        string stemTerm(string s);
    }

    /*

   Porter stemmer in CSharp, based on the Java port. The original paper is in

       Porter, 1980, An algorithm for suffix stripping, Program, Vol. 14,
       no. 3, pp 130-137,

   See also http://www.tartarus.org/~martin/PorterStemmer

   History:

   Release 1

   Bug 1 (reported by Gonzalo Parra 16/10/99) fixed as marked below.
   The words 'aed', 'eed', 'oed' leave k at 'a' for step 3, and b[k-1]
   is then out outside the bounds of b.

   Release 2

   Similarly,

   Bug 2 (reported by Steve Dyrdahl 22/2/00) fixed as marked below.
   'ion' by itself leaves j = -1 in the test for 'ion' in step 5, and
   b[j] is then outside the bounds of b.

   Release 3

   Considerably revised 4/9/00 in the light of many helpful suggestions
   from Brian Goetz of Quiotix Corporation (brian@quiotix.com).

   Release 4

   This revision allows the Porter Stemmer Algorithm to be exported via the
   .NET Framework. To facilate its use via .NET, the following commands need to be
   issued to the operating system to register the component so that it can be
   imported into .Net compatible languages, such as Delphi.NET, Visual Basic.NET,
   Visual C++.NET, etc. 

   1. Create a stong name: 		
        sn -k Keyfile.snk  
   2. Compile the C# class, which creates an assembly PorterStemmerAlgorithm.dll
        csc /t:library PorterStemmerAlgorithm.cs
   3. Register the dll with the Windows Registry 
      and so expose the interface to COM Clients via the type library 
      ( PorterStemmerAlgorithm.tlb will be created)
        regasm /tlb PorterStemmerAlgorithm.dll
   4. Load the component in the Global Assembly Cache
        gacutil -i PorterStemmerAlgorithm.dll

   Note: You must have the .Net Studio installed.

   Once this process is performed you should be able to import the class 
   via the appropiate mechanism in the language that you are using.

   i.e in Delphi 7 .NET this is simply a matter of selecting: 
        Project | Import Type Libary
   And then selecting Porter stemmer in CSharp Version 1.4"!

   Cheers Leif

*/
    [ClassInterface(ClassInterfaceType.None)]
    public class PorterStemming : StemmerInterface
    {
        private char[] b;
        private int i,     /* offset into b */
            i_end, /* offset to end of stemmed word */
            j, k;
        private static int INC = 200;
        /* unit of size whereby b is increased */

        public PorterStemming()
        {
            b = new char[INC];
            i = 0;
            i_end = 0;
        }

        /* Implementation of the .NET interface - added as part of realease 4 (Leif) */
        public string stemTerm(string s)
        {
            setTerm(s);
            stem();
            return getTerm();
        }

        /*
			SetTerm and GetTerm have been simply added to ease the 
			interface with other lanaguages. They replace the add functions 
			and toString function. This was done because the original functions stored
			all stemmed words (and each time a new woprd was added, the buffer would be
			re-copied each time, making it quite slow). Now, The class interface 
			that is provided simply accepts a term and returns its stem, 
			instead of storing all stemmed words.
			(Leif)
		*/



        void setTerm(string s)
        {
            i = s.Length;
            char[] new_b = new char[i];
            for (int c = 0; c < i; c++)
                new_b[c] = s[c];

            b = new_b;

        }

        public string getTerm()
        {
            return new String(b, 0, i_end);
        }


        /* Old interface to the class - left for posterity. However, it is not
		 * used when accessing the class via .NET (Leif)*/

        /**
		 * Add a character to the word being stemmed.  When you are finished
		 * adding characters, you can call stem(void) to stem the word.
		 */

        public void add(char ch)
        {
            if (i == b.Length)
            {
                char[] new_b = new char[i + INC];
                for (int c = 0; c < i; c++)
                    new_b[c] = b[c];
                b = new_b;
            }
            b[i++] = ch;
        }


        /** Adds wLen characters to the word being stemmed contained in a portion
		 * of a char[] array. This is like repeated calls of add(char ch), but
		 * faster.
		 */

        public void add(char[] w, int wLen)
        {
            if (i + wLen >= b.Length)
            {
                char[] new_b = new char[i + wLen + INC];
                for (int c = 0; c < i; c++)
                    new_b[c] = b[c];
                b = new_b;
            }
            for (int c = 0; c < wLen; c++)
                b[i++] = w[c];
        }

        /**
		 * After a word has been stemmed, it can be retrieved by toString(),
		 * or a reference to the internal buffer can be retrieved by getResultBuffer
		 * and getResultLength (which is generally more efficient.)
		 */
        public override string ToString()
        {
            return new String(b, 0, i_end);
        }

        /**
		 * Returns the length of the word resulting from the stemming process.
		 */
        public int getResultLength()
        {
            return i_end;
        }

        /**
		 * Returns a reference to a character buffer containing the results of
		 * the stemming process.  You also need to consult getResultLength()
		 * to determine the length of the result.
		 */
        public char[] getResultBuffer()
        {
            return b;
        }

        /* cons(i) is true <=> b[i] is a consonant. */
        private bool cons(int i)
        {
            switch (b[i])
            {
                case 'a': case 'e': case 'i': case 'o': case 'u': return false;
                case 'y': return (i == 0) ? true : !cons(i - 1);
                default: return true;
            }
        }

        /* m() measures the number of consonant sequences between 0 and j. if c is
		   a consonant sequence and v a vowel sequence, and <..> indicates arbitrary
		   presence,

			  <c><v>       gives 0
			  <c>vc<v>     gives 1
			  <c>vcvc<v>   gives 2
			  <c>vcvcvc<v> gives 3
			  ....
		*/
        private int m()
        {
            int n = 0;
            int i = 0;
            while (true)
            {
                if (i > j) return n;
                if (!cons(i)) break; i++;
            }
            i++;
            while (true)
            {
                while (true)
                {
                    if (i > j) return n;
                    if (cons(i)) break;
                    i++;
                }
                i++;
                n++;
                while (true)
                {
                    if (i > j) return n;
                    if (!cons(i)) break;
                    i++;
                }
                i++;
            }
        }

        /* vowelinstem() is true <=> 0,...j contains a vowel */
        private bool vowelinstem()
        {
            int i;
            for (i = 0; i <= j; i++)
                if (!cons(i))
                    return true;
            return false;
        }

        /* doublec(j) is true <=> j,(j-1) contain a double consonant. */
        private bool doublec(int j)
        {
            if (j < 1)
                return false;
            if (b[j] != b[j - 1])
                return false;
            return cons(j);
        }

        /* cvc(i) is true <=> i-2,i-1,i has the form consonant - vowel - consonant
		   and also if the second c is not w,x or y. this is used when trying to
		   restore an e at the end of a short word. e.g.

			  cav(e), lov(e), hop(e), crim(e), but
			  snow, box, tray.

		*/
        private bool cvc(int i)
        {
            if (i < 2 || !cons(i) || cons(i - 1) || !cons(i - 2))
                return false;
            int ch = b[i];
            if (ch == 'w' || ch == 'x' || ch == 'y')
                return false;
            return true;
        }

        private bool ends(String s)
        {
            int l = s.Length;
            int o = k - l + 1;
            if (o < 0)
                return false;
            char[] sc = s.ToCharArray();
            for (int i = 0; i < l; i++)
                if (b[o + i] != sc[i])
                    return false;
            j = k - l;
            return true;
        }

        /* setto(s) sets (j+1),...k to the characters in the string s, readjusting
		   k. */
        private void setto(String s)
        {
            int l = s.Length;
            int o = j + 1;
            char[] sc = s.ToCharArray();
            for (int i = 0; i < l; i++)
                b[o + i] = sc[i];
            k = j + l;
        }

        /* r(s) is used further down. */
        private void r(String s)
        {
            if (m() > 0)
                setto(s);
        }

        /* step1() gets rid of plurals and -ed or -ing. e.g.
			   caresses  ->  caress
			   ponies    ->  poni
			   ties      ->  ti
			   caress    ->  caress
			   cats      ->  cat

			   feed      ->  feed
			   agreed    ->  agree
			   disabled  ->  disable

			   matting   ->  mat
			   mating    ->  mate
			   meeting   ->  meet
			   milling   ->  mill
			   messing   ->  mess

			   meetings  ->  meet

		*/

        private void step1()
        {
            if (b[k] == 's')
            {
                if (ends("sses"))
                    k -= 2;
                else if (ends("ies"))
                    setto("i");
                else if (b[k - 1] != 's')
                    k--;
            }
            if (ends("eed"))
            {
                if (m() > 0)
                    k--;
            }
            else if ((ends("ed") || ends("ing")) && vowelinstem())
            {
                k = j;
                if (ends("at"))
                    setto("ate");
                else if (ends("bl"))
                    setto("ble");
                else if (ends("iz"))
                    setto("ize");
                else if (doublec(k))
                {
                    k--;
                    int ch = b[k];
                    if (ch == 'l' || ch == 's' || ch == 'z')
                        k++;
                }
                else if (m() == 1 && cvc(k)) setto("e");
            }
        }

        /* step2() turns terminal y to i when there is another vowel in the stem. */
        private void step2()
        {
            if (ends("y") && vowelinstem())
                b[k] = 'i';
        }

        /* step3() maps double suffices to single ones. so -ization ( = -ize plus
		   -ation) maps to -ize etc. note that the string before the suffix must give
		   m() > 0. */
        private void step3()
        {
            if (k == 0)
                return;

            /* For Bug 1 */
            switch (b[k - 1])
            {
                case 'a':
                    if (ends("ational")) { r("ate"); break; }
                    if (ends("tional")) { r("tion"); break; }
                    break;
                case 'c':
                    if (ends("enci")) { r("ence"); break; }
                    if (ends("anci")) { r("ance"); break; }
                    break;
                case 'e':
                    if (ends("izer")) { r("ize"); break; }
                    break;
                case 'l':
                    if (ends("bli")) { r("ble"); break; }
                    if (ends("alli")) { r("al"); break; }
                    if (ends("entli")) { r("ent"); break; }
                    if (ends("eli")) { r("e"); break; }
                    if (ends("ousli")) { r("ous"); break; }
                    break;
                case 'o':
                    if (ends("ization")) { r("ize"); break; }
                    if (ends("ation")) { r("ate"); break; }
                    if (ends("ator")) { r("ate"); break; }
                    break;
                case 's':
                    if (ends("alism")) { r("al"); break; }
                    if (ends("iveness")) { r("ive"); break; }
                    if (ends("fulness")) { r("ful"); break; }
                    if (ends("ousness")) { r("ous"); break; }
                    break;
                case 't':
                    if (ends("aliti")) { r("al"); break; }
                    if (ends("iviti")) { r("ive"); break; }
                    if (ends("biliti")) { r("ble"); break; }
                    break;
                case 'g':
                    if (ends("logi")) { r("log"); break; }
                    break;
                default:
                    break;
            }
        }

        /* step4() deals with -ic-, -full, -ness etc. similar strategy to step3. */
        private void step4()
        {
            switch (b[k])
            {
                case 'e':
                    if (ends("icate")) { r("ic"); break; }
                    if (ends("ative")) { r(""); break; }
                    if (ends("alize")) { r("al"); break; }
                    break;
                case 'i':
                    if (ends("iciti")) { r("ic"); break; }
                    break;
                case 'l':
                    if (ends("ical")) { r("ic"); break; }
                    if (ends("ful")) { r(""); break; }
                    break;
                case 's':
                    if (ends("ness")) { r(""); break; }
                    break;
            }
        }

        /* step5() takes off -ant, -ence etc., in context <c>vcvc<v>. */
        private void step5()
        {
            if (k == 0)
                return;

            /* for Bug 1 */
            switch (b[k - 1])
            {
                case 'a':
                    if (ends("al")) break; return;
                case 'c':
                    if (ends("ance")) break;
                    if (ends("ence")) break; return;
                case 'e':
                    if (ends("er")) break; return;
                case 'i':
                    if (ends("ic")) break; return;
                case 'l':
                    if (ends("able")) break;
                    if (ends("ible")) break; return;
                case 'n':
                    if (ends("ant")) break;
                    if (ends("ement")) break;
                    if (ends("ment")) break;
                    /* element etc. not stripped before the m */
                    if (ends("ent")) break; return;
                case 'o':
                    if (ends("ion") && j >= 0 && (b[j] == 's' || b[j] == 't')) break;
                    /* j >= 0 fixes Bug 2 */
                    if (ends("ou")) break; return;
                /* takes care of -ous */
                case 's':
                    if (ends("ism")) break; return;
                case 't':
                    if (ends("ate")) break;
                    if (ends("iti")) break; return;
                case 'u':
                    if (ends("ous")) break; return;
                case 'v':
                    if (ends("ive")) break; return;
                case 'z':
                    if (ends("ize")) break; return;
                default:
                    return;
            }
            if (m() > 1)
                k = j;
        }

        /* step6() removes a final -e if m() > 1. */
        private void step6()
        {
            j = k;

            if (b[k] == 'e')
            {
                int a = m();
                if (a > 1 || a == 1 && !cvc(k - 1))
                    k--;
            }
            if (b[k] == 'l' && doublec(k) && m() > 1)
                k--;
        }

        /** Stem the word placed into the Stemmer buffer through calls to add().
		 * Returns true if the stemming process resulted in a word different
		 * from the input.  You can retrieve the result with
		 * getResultLength()/getResultBuffer() or toString().
		 */
        public void stem()
        {
            k = i - 1;
            if (k > 1)
            {
                step1();
                step2();
                step3();
                step4();
                step5();
                step6();
            }
            i_end = k + 1;
            i = 0;
        }


    }

    /// <summary>
	/// Stop words are frequently occurring, insignificant words words 
	/// that appear in a database record, article or web page. 
	/// Common stop words include
	/// </summary>
	public class StopWordsHandler
    {
        public static string[] stopWordsList = new string[] {
            //completed stop word list from ftp://ftp.cs.cornell.edu/pub/smart/english.stop
            //PH 5 apr 2013
                                                            "a",
                                                            "able",
                                                            "about",
                                                            "above",
                                                            "according",
                                                            "accordingly",
                                                            "across",
                                                            "actually",
                                                            "afore",
                                                            "aforesaid",
                                                            "after",
                                                            "afterwards",
                                                            "again",
                                                            "against",
                                                            "agin",
                                                            "ago",
                                                            "aint",
                                                            "ain't",
                                                            "albeit",
                                                            "all",
                                                            "allow",
                                                            "allows",
                                                            "almost",
                                                            "alone",
                                                            "along",
                                                            "alongside",
                                                            "already",
                                                            "also",
                                                            "although",
                                                            "always",
                                                            "am",
                                                            "american",
                                                            "amid",
                                                            "amidst",
                                                            "among",
                                                            "amongst",
                                                            "an",
                                                            "and",
                                                            "anent",
                                                            "another",
                                                            "any",
                                                            "anybody",
                                                            "anyhow",
                                                            "anyone",
                                                            "anything",
                                                            "anyway",
                                                            "anyways",
                                                            "anywhere",
                                                            "apart",
                                                            "appear",
                                                            "appreciate",
                                                            "appropriate",
                                                            "are",
                                                            "aren't",
                                                            "around",
                                                            "as",
                                                            "a's",
                                                            "aside",
                                                            "ask",
                                                            "asking",
                                                            "aslant",
                                                            "associated",
                                                            "astride",
                                                            "at",
                                                            "athwart",
                                                            "available",
                                                            "away",
                                                            "awfully",
                                                            "b",
                                                            "back",
                                                            "bar",
                                                            "barring",
                                                            "be",
                                                            "became",
                                                            "because",
                                                            "become",
                                                            "becomes",
                                                            "becoming",
                                                            "been",
                                                            "before",
                                                            "beforehand",
                                                            "behind",
                                                            "being",
                                                            "believe",
                                                            "below",
                                                            "beneath",
                                                            "beside",
                                                            "besides",
                                                            "best",
                                                            "better",
                                                            "between",
                                                            "betwixt",
                                                            "beyond",
                                                            "both",
                                                            "brief",
                                                            "but",
                                                            "by",
                                                            "c",
                                                            "came",
                                                            "can",
                                                            "cannot",
                                                            "cant",
                                                            "can't",
                                                            "cause",
                                                            "causes",
                                                            "certain",
                                                            "certainly",
                                                            "changes",
                                                            "circa",
                                                            "clearly",
                                                            "close",
                                                            "c'mon",
                                                            "co",
                                                            "com",
                                                            "come",
                                                            "comes",
                                                            "concerning",
                                                            "consequently",
                                                            "consider",
                                                            "considering",
                                                            "contain",
                                                            "containing",
                                                            "contains",
                                                            "corresponding",
                                                            "cos",
                                                            "could",
                                                            "couldn't",
                                                            "couldst",
                                                            "course",
                                                            "c's",
                                                            "currently",
                                                            "d",
                                                            "dare",
                                                            "dared",
                                                            "daren't",
                                                            "dares",
                                                            "daring",
                                                            "definitely",
                                                            "described",
                                                            "despite",
                                                            "did",
                                                            "didn't",
                                                            "different",
                                                            "directly",
                                                            "do",
                                                            "does",
                                                            "doesn't",
                                                            "doing",
                                                            "done",
                                                            "don't",
                                                            "dost",
                                                            "doth",
                                                            "down",
                                                            "downwards",
                                                            "during",
                                                            "durst",
                                                            "e",
                                                            "each",
                                                            "early",
                                                            "edu",
                                                            "eg",
                                                            "eight",
                                                            "either",
                                                            "else",
                                                            "elsewhere",
                                                            "em",
                                                            "english",
                                                            "enough",
                                                            "entirely",
                                                            "ere",
                                                            "especially",
                                                            "et",
                                                            "etc",
                                                            "even",
                                                            "ever",
                                                            "every",
                                                            "everybody",
                                                            "everyone",
                                                            "everything",
                                                            "everywhere",
                                                            "ex",
                                                            "exactly",
                                                            "example",
                                                            "except",
                                                            "excepting",
                                                            "f",
                                                            "failing",
                                                            "far",
                                                            "few",
                                                            "fifth",
                                                            "first",
                                                            "five",
                                                            "followed",
                                                            "following",
                                                            "follows",
                                                            "for",
                                                            "former",
                                                            "formerly",
                                                            "forth",
                                                            "four",
                                                            "from",
                                                            "further",
                                                            "furthermore",
                                                            "g",
                                                            "get",
                                                            "gets",
                                                            "getting",
                                                            "given",
                                                            "gives",
                                                            "go",
                                                            "goes",
                                                            "going",
                                                            "gone",
                                                            "gonna",
                                                            "got",
                                                            "gotta",
                                                            "gotten",
                                                            "greetings",
                                                            "h",
                                                            "had",
                                                            "hadn't",
                                                            "happens",
                                                            "hard",
                                                            "hardly",
                                                            "has",
                                                            "hasn't",
                                                            "hast",
                                                            "hath",
                                                            "have",
                                                            "haven't",
                                                            "having",
                                                            "he",
                                                            "he'd",
                                                            "he'll",
                                                            "hello",
                                                            "help",
                                                            "hence",
                                                            "her",
                                                            "here",
                                                            "hereafter",
                                                            "hereby",
                                                            "herein",
                                                            "here's",
                                                            "hereupon",
                                                            "hers",
                                                            "herself",
                                                            "he's",
                                                            "hi",
                                                            "high",
                                                            "him",
                                                            "himself",
                                                            "his",
                                                            "hither",
                                                            "home",
                                                            "hopefully",
                                                            "how",
                                                            "howbeit",
                                                            "however",
                                                            "how's",
                                                            "i",
                                                            "id",
                                                            "i'd",
                                                            "ie",
                                                            "if",
                                                            "ignored",
                                                            "ill",
                                                            "i'll",
                                                            "i'm",
                                                            "immediate",
                                                            "immediately",
                                                            "important",
                                                            "in",
                                                            "inasmuch",
                                                            "inc",
                                                            "indeed",
                                                            "indicate",
                                                            "indicated",
                                                            "indicates",
                                                            "inner",
                                                            "inside",
                                                            "insofar",
                                                            "instantly",
                                                            "instead",
                                                            "into",
                                                            "inward",
                                                            "is",
                                                            "isn't",
                                                            "it",
                                                            "it'd",
                                                            "it'll",
                                                            "its",
                                                            "it's",
                                                            "itself",
                                                            "i've",
                                                            "j",
                                                            "just",
                                                            "k",
                                                            "keep",
                                                            "keeps",
                                                            "kept",
                                                            "know",
                                                            "known",
                                                            "knows",
                                                            "l",
                                                            "large",
                                                            "last",
                                                            "lately",
                                                            "later",
                                                            "latter",
                                                            "latterly",
                                                            "least",
                                                            "left",
                                                            "less",
                                                            "lest",
                                                            "let",
                                                            "let's",
                                                            "like",
                                                            "liked",
                                                            "likely",
                                                            "likewise",
                                                            "little",
                                                            "living",
                                                            "long",
                                                            "look",
                                                            "looking",
                                                            "looks",
                                                            "ltd",
                                                            "m",
                                                            "mainly",
                                                            "many",
                                                            "may",
                                                            "maybe",
                                                            "mayn't",
                                                            "me",
                                                            "mean",
                                                            "meanwhile",
                                                            "merely",
                                                            "mid",
                                                            "midst",
                                                            "might",
                                                            "mightn't",
                                                            "mine",
                                                            "minus",
                                                            "more",
                                                            "moreover",
                                                            "most",
                                                            "mostly",
                                                            "much",
                                                            "must",
                                                            "mustn't",
                                                            "my",
                                                            "myself",
                                                            "n",
                                                            "name",
                                                            "namely",
                                                            "nd",
                                                            "near",
                                                            "nearly",
                                                            "neath",
                                                            "necessary",
                                                            "need",
                                                            "needed",
                                                            "needing",
                                                            "needn't",
                                                            "needs",
                                                            "neither",
                                                            "never",
                                                            "nevertheless",
                                                            "new",
                                                            "next",
                                                            "nigh",
                                                            "nigher",
                                                            "nighest",
                                                            "nine",
                                                            "nisi",
                                                            "no",
                                                            "nobody",
                                                            "non",
                                                            "none",
                                                            "noone",
                                                            "no-one",
                                                            "nor",
                                                            "normally",
                                                            "not",
                                                            "nothing",
                                                            "notwithstanding",
                                                            "novel",
                                                            "now",
                                                            "nowhere",
                                                            "o",
                                                            "obviously",
                                                            "o'er",
                                                            "of",
                                                            "off",
                                                            "often",
                                                            "oh",
                                                            "ok",
                                                            "okay",
                                                            "old",
                                                            "on",
                                                            "once",
                                                            "one",
                                                            "ones",
                                                            "oneself",
                                                            "only",
                                                            "onto",
                                                            "open",
                                                            "or",
                                                            "other",
                                                            "others",
                                                            "otherwise",
                                                            "ought",
                                                            "oughtn't",
                                                            "our",
                                                            "ours",
                                                            "ourselves",
                                                            "out",
                                                            "outside",
                                                            "over",
                                                            "overall",
                                                            "own",
                                                            "p",
                                                            "particular",
                                                            "particularly",
                                                            "past",
                                                            "pending",
                                                            "per",
                                                            "perhaps",
                                                            "placed",
                                                            "please",
                                                            "plus",
                                                            "possible",
                                                            "present",
                                                            "presumably",
                                                            "probably",
                                                            "provided",
                                                            "provides",
                                                            "providing",
                                                            "public",
                                                            "q",
                                                            "qua",
                                                            "que",
                                                            "quite",
                                                            "qv",
                                                            "r",
                                                            "rather",
                                                            "rd",
                                                            "re",
                                                            "real",
                                                            "really",
                                                            "reasonably",
                                                            "regarding",
                                                            "regardless",
                                                            "regards",
                                                            "relatively",
                                                            "respecting",
                                                            "respectively",
                                                            "right",
                                                            "round",
                                                            "s",
                                                            "said",
                                                            "same",
                                                            "sans",
                                                            "save",
                                                            "saving",
                                                            "saw",
                                                            "say",
                                                            "saying",
                                                            "says",
                                                            "second",
                                                            "secondly",
                                                            "see",
                                                            "seeing",
                                                            "seem",
                                                            "seemed",
                                                            "seeming",
                                                            "seems",
                                                            "seen",
                                                            "self",
                                                            "selves",
                                                            "sensible",
                                                            "sent",
                                                            "serious",
                                                            "seriously",
                                                            "seven",
                                                            "several",
                                                            "shall",
                                                            "shalt",
                                                            "shan't",
                                                            "she",
                                                            "shed",
                                                            "shell",
                                                            "she's",
                                                            "short",
                                                            "should",
                                                            "shouldn't",
                                                            "since",
                                                            "six",
                                                            "small",
                                                            "so",
                                                            "some",
                                                            "somebody",
                                                            "somehow",
                                                            "someone",
                                                            "something",
                                                            "sometime",
                                                            "sometimes",
                                                            "somewhat",
                                                            "somewhere",
                                                            "soon",
                                                            "sorry",
                                                            "special",
                                                            "specified",
                                                            "specify",
                                                            "specifying",
                                                            "still",
                                                            "sub",
                                                            "such",
                                                            "summat",
                                                            "sup",
                                                            "supposing",
                                                            "sure",
                                                            "t",
                                                            "take",
                                                            "taken",
                                                            "tell",
                                                            "tends",
                                                            "th",
                                                            "than",
                                                            "thank",
                                                            "thanks",
                                                            "thanx",
                                                            "that",
                                                            "that'd",
                                                            "that'll",
                                                            "thats",
                                                            "that's",
                                                            "the",
                                                            "thee",
                                                            "their",
                                                            "theirs",
                                                            "their's",
                                                            "them",
                                                            "themselves",
                                                            "then",
                                                            "thence",
                                                            "there",
                                                            "thereafter",
                                                            "thereby",
                                                            "therefore",
                                                            "therein",
                                                            "theres",
                                                            "there's",
                                                            "thereupon",
                                                            "these",
                                                            "they",
                                                            "they'd",
                                                            "they'll",
                                                            "they're",
                                                            "they've",
                                                            "thine",
                                                            "think",
                                                            "third",
                                                            "this",
                                                            "tho",
                                                            "thorough",
                                                            "thoroughly",
                                                            "those",
                                                            "thou",
                                                            "though",
                                                            "three",
                                                            "thro'",
                                                            "through",
                                                            "throughout",
                                                            "thru",
                                                            "thus",
                                                            "thyself",
                                                            "till",
                                                            "to",
                                                            "today",
                                                            "together",
                                                            "too",
                                                            "took",
                                                            "touching",
                                                            "toward",
                                                            "towards",
                                                            "tried",
                                                            "tries",
                                                            "truly",
                                                            "try",
                                                            "trying",
                                                            "t's",
                                                            "twas",
                                                            "tween",
                                                            "twere",
                                                            "twice",
                                                            "twill",
                                                            "twixt",
                                                            "two",
                                                            "twould",
                                                            "u",
                                                            "un",
                                                            "under",
                                                            "underneath",
                                                            "unfortunately",
                                                            "unless",
                                                            "unlike",
                                                            "unlikely",
                                                            "until",
                                                            "unto",
                                                            "up",
                                                            "upon",
                                                            "us",
                                                            "use",
                                                            "used",
                                                            "useful",
                                                            "uses",
                                                            "using",
                                                            "usually",
                                                            "uucp",
                                                            "v",
                                                            "value",
                                                            "various",
                                                            "versus",
                                                            "very",
                                                            "via",
                                                            "vice",
                                                            "vis-a-vis",
                                                            "viz",
                                                            "vs",
                                                            "w",
                                                            "wanna",
                                                            "want",
                                                            "wanting",
                                                            "wants",
                                                            "was",
                                                            "wasn't",
                                                            "way",
                                                            "we",
                                                            "we'd",
                                                            "welcome",
                                                            "well",
                                                            "we'll",
                                                            "went",
                                                            "were",
                                                            "we're",
                                                            "weren't",
                                                            "wert",
                                                            "we've",
                                                            "what",
                                                            "whatever",
                                                            "what'll",
                                                            "what's",
                                                            "when",
                                                            "whence",
                                                            "whencesoever",
                                                            "whenever",
                                                            "when's",
                                                            "where",
                                                            "whereafter",
                                                            "whereas",
                                                            "whereby",
                                                            "wherein",
                                                            "where's",
                                                            "whereupon",
                                                            "wherever",
                                                            "whether",
                                                            "which",
                                                            "whichever",
                                                            "whichsoever",
                                                            "while",
                                                            "whilst",
                                                            "whither",
                                                            "who",
                                                            "who'd",
                                                            "whoever",
                                                            "whole",
                                                            "who'll",
                                                            "whom",
                                                            "whore",
                                                            "who's",
                                                            "whose",
                                                            "whoso",
                                                            "whosoever",
                                                            "why",
                                                            "will",
                                                            "willing",
                                                            "wish",
                                                            "with",
                                                            "within",
                                                            "without",
                                                            "wonder",
                                                            "wont",
                                                            "won't",
                                                            "would",
                                                            "wouldn't",
                                                            "wouldst",
                                                            "x",
                                                            "y",
                                                            "ye",
                                                            "yes",
                                                            "yet",
                                                            "you",
                                                            "you'd",
                                                            "you'll",
                                                            "your",
                                                            "you're",
                                                            "yours",
                                                            "yourself",
                                                            "yourselves",
                                                            "you've",
                                                            "z",
                                                            "zero" 
                                                            //"a", 
                                                            //"about", 
                                                            //"above", 
                                                            //"across", 
                                                            //"afore", 
                                                            //"aforesaid", 
                                                            //"after", 
                                                            //"again", 
                                                            //"against", 
                                                            //"agin", 
                                                            //"ago", 
                                                            //"aint", 
                                                            //"albeit", 
                                                            //"all", 
                                                            //"almost", 
                                                            //"alone", 
                                                            //"along", 
                                                            //"alongside", 
                                                            //"already", 
                                                            //"also", 
                                                            //"although", 
                                                            //"always", 
                                                            //"am", 
                                                            //"american", 
                                                            //"amid", 
                                                            //"amidst", 
                                                            //"among", 
                                                            //"amongst", 
                                                            //"an", 
                                                            //"and", 
                                                            //"anent", 
                                                            //"another", 
                                                            //"any", 
                                                            //"anybody", 
                                                            //"anyone", 
                                                            //"anything", 
                                                            //"are", 
                                                            //"aren't", 
                                                            //"around", 
                                                            //"as", 
                                                            //"aslant", 
                                                            //"astride", 
                                                            //"at", 
                                                            //"athwart", 
                                                            //"away", 
                                                            //"b", 
                                                            //"back", 
                                                            //"bar", 
                                                            //"barring", 
                                                            //"be", 
                                                            //"because", 
                                                            //"been", 
                                                            //"before", 
                                                            //"behind", 
                                                            //"being", 
                                                            //"below", 
                                                            //"beneath", 
                                                            //"beside", 
                                                            //"besides", 
                                                            //"best", 
                                                            //"better", 
                                                            //"between", 
                                                            //"betwixt", 
                                                            //"beyond", 
                                                            //"both", 
                                                            //"but", 
                                                            //"by", 
                                                            //"c", 
                                                            //"can", 
                                                            //"cannot", 
                                                            //"can't", 
                                                            //"certain", 
                                                            //"circa", 
                                                            //"close", 
                                                            //"concerning", 
                                                            //"considering", 
                                                            //"cos", 
                                                            //"could", 
                                                            //"couldn't", 
                                                            //"couldst", 
                                                            //"d", 
                                                            //"dare", 
                                                            //"dared", 
                                                            //"daren't", 
                                                            //"dares", 
                                                            //"daring", 
                                                            //"despite", 
                                                            //"did", 
                                                            //"didn't", 
                                                            //"different", 
                                                            //"directly", 
                                                            //"do", 
                                                            //"does", 
                                                            //"doesn't", 
                                                            //"doing", 
                                                            //"done", 
                                                            //"don't", 
                                                            //"dost", 
                                                            //"doth", 
                                                            //"down", 
                                                            //"during", 
                                                            //"durst", 
                                                            //"e", 
                                                            //"each", 
                                                            //"early", 
                                                            //"either", 
                                                            //"em", 
                                                            //"english", 
                                                            //"enough", 
                                                            //"ere", 
                                                            //"even", 
                                                            //"ever", 
                                                            //"every", 
                                                            //"everybody", 
                                                            //"everyone", 
                                                            //"everything", 
                                                            //"except", 
                                                            //"excepting", 
                                                            //"f", 
                                                            //"failing", 
                                                            //"far", 
                                                            //"few", 
                                                            //"first", 
                                                            //"five", 
                                                            //"following", 
                                                            //"for", 
                                                            //"four", 
                                                            //"from", 
                                                            //"g", 
                                                            //"gonna", 
                                                            //"gotta", 
                                                            //"h", 
                                                            //"had", 
                                                            //"hadn't", 
                                                            //"hard", 
                                                            //"has", 
                                                            //"hasn't", 
                                                            //"hast", 
                                                            //"hath", 
                                                            //"have", 
                                                            //"haven't", 
                                                            //"having", 
                                                            //"he", 
                                                            //"he'd", 
                                                            //"he'll", 
                                                            //"her", 
                                                            //"here", 
                                                            //"here's", 
                                                            //"hers", 
                                                            //"herself", 
                                                            //"he's", 
                                                            //"high", 
                                                            //"him", 
                                                            //"himself", 
                                                            //"his", 
                                                            //"home", 
                                                            //"how", 
                                                            //"howbeit", 
                                                            //"however", 
                                                            //"how's", 
                                                            //"i", 
                                                            //"id", 
                                                            //"if", 
                                                            //"ill", 
                                                            //"i'm", 
                                                            //"immediately", 
                                                            //"important", 
                                                            //"in", 
                                                            //"inside", 
                                                            //"instantly", 
                                                            //"into", 
                                                            //"is", 
                                                            //"isn't", 
                                                            //"it", 
                                                            //"it'll", 
                                                            //"its", 
                                                            //"it's", 
                                                            //"itself", 
                                                            //"i've", 
                                                            //"j", 
                                                            //"just", 
                                                            //"k", 
                                                            //"l", 
                                                            //"large", 
                                                            //"last", 
                                                            //"later", 
                                                            //"least", 
                                                            //"left", 
                                                            //"less", 
                                                            //"lest", 
                                                            //"let's", 
                                                            //"like", 
                                                            //"likewise", 
                                                            //"little", 
                                                            //"living", 
                                                            //"long", 
                                                            //"m", 
                                                            //"many", 
                                                            //"may", 
                                                            //"mayn't", 
                                                            //"me", 
                                                            //"mid", 
                                                            //"midst", 
                                                            //"might", 
                                                            //"mightn't", 
                                                            //"mine", 
                                                            //"minus", 
                                                            //"more", 
                                                            //"most", 
                                                            //"much", 
                                                            //"must", 
                                                            //"mustn't", 
                                                            //"my", 
                                                            //"myself", 
                                                            //"n", 
                                                            //"near", 
                                                            //"neath", 
                                                            //"need", 
                                                            //"needed", 
                                                            //"needing", 
                                                            //"needn't", 
                                                            //"needs", 
                                                            //"neither", 
                                                            //"never", 
                                                            //"nevertheless", 
                                                            //"new", 
                                                            //"next", 
                                                            //"nigh", 
                                                            //"nigher", 
                                                            //"nighest", 
                                                            //"nisi", 
                                                            //"no", 
                                                            //"nobody", 
                                                            //"none", 
                                                            //"no-one", 
                                                            //"nor", 
                                                            //"not", 
                                                            //"nothing", 
                                                            //"notwithstanding", 
                                                            //"now", 
                                                            //"o", 
                                                            //"o'er", 
                                                            //"of", 
                                                            //"off", 
                                                            //"often", 
                                                            //"on", 
                                                            //"once", 
                                                            //"one", 
                                                            //"oneself", 
                                                            //"only", 
                                                            //"onto", 
                                                            //"open", 
                                                            //"or", 
                                                            //"other", 
                                                            //"otherwise", 
                                                            //"ought", 
                                                            //"oughtn't", 
                                                            //"our", 
                                                            //"ours", 
                                                            //"ourselves", 
                                                            //"out", 
                                                            //"outside", 
                                                            //"over", 
                                                            //"own", 
                                                            //"p", 
                                                            //"past", 
                                                            //"pending", 
                                                            //"per", 
                                                            //"perhaps", 
                                                            //"plus", 
                                                            //"possible", 
                                                            //"present", 
                                                            //"probably", 
                                                            //"provided", 
                                                            //"providing", 
                                                            //"public", 
                                                            //"q", 
                                                            //"qua", 
                                                            //"quite", 
                                                            //"r", 
                                                            //"rather", 
                                                            //"re", 
                                                            //"real", 
                                                            //"really", 
                                                            //"respecting", 
                                                            //"right", 
                                                            //"round", 
                                                            //"s", 
                                                            //"same", 
                                                            //"sans", 
                                                            //"save", 
                                                            //"saving", 
                                                            //"second", 
                                                            //"several", 
                                                            //"shall", 
                                                            //"shalt", 
                                                            //"shan't", 
                                                            //"she", 
                                                            //"shed", 
                                                            //"shell", 
                                                            //"she's", 
                                                            //"short", 
                                                            //"should", 
                                                            //"shouldn't", 
                                                            //"since", 
                                                            //"six", 
                                                            //"small", 
                                                            //"so", 
                                                            //"some", 
                                                            //"somebody", 
                                                            //"someone", 
                                                            //"something", 
                                                            //"sometimes", 
                                                            //"soon", 
                                                            //"special", 
                                                            //"still", 
                                                            //"such", 
                                                            //"summat", 
                                                            //"supposing", 
                                                            //"sure", 
                                                            //"t", 
                                                            //"than", 
                                                            //"that", 
                                                            //"that'd", 
                                                            //"that'll", 
                                                            //"that's", 
                                                            //"the", 
                                                            //"thee", 
                                                            //"their", 
                                                            //"theirs", 
                                                            //"their's", 
                                                            //"them", 
                                                            //"themselves", 
                                                            //"then", 
                                                            //"there", 
                                                            //"there's", 
                                                            //"these", 
                                                            //"they", 
                                                            //"they'd", 
                                                            //"they'll", 
                                                            //"they're", 
                                                            //"they've", 
                                                            //"thine", 
                                                            //"this", 
                                                            //"tho", 
                                                            //"those", 
                                                            //"thou", 
                                                            //"though", 
                                                            //"three", 
                                                            //"thro'", 
                                                            //"through", 
                                                            //"throughout", 
                                                            //"thru", 
                                                            //"thyself", 
                                                            //"till", 
                                                            //"to", 
                                                            //"today", 
                                                            //"together", 
                                                            //"too", 
                                                            //"touching", 
                                                            //"toward", 
                                                            //"towards", 
                                                            //"twas", 
                                                            //"tween", 
                                                            //"twere", 
                                                            //"twill", 
                                                            //"twixt", 
                                                            //"two", 
                                                            //"twould", 
                                                            //"u", 
                                                            //"under", 
                                                            //"underneath", 
                                                            //"unless", 
                                                            //"unlike", 
                                                            //"until", 
                                                            //"unto", 
                                                            //"up", 
                                                            //"upon", 
                                                            //"us", 
                                                            //"used", 
                                                            //"usually", 
                                                            //"v", 
                                                            //"versus", 
                                                            //"very", 
                                                            //"via", 
                                                            //"vice", 
                                                            //"vis-a-vis", 
                                                            //"w", 
                                                            //"wanna", 
                                                            //"wanting", 
                                                            //"was", 
                                                            //"wasn't", 
                                                            //"way", 
                                                            //"we", 
                                                            //"we'd", 
                                                            //"well", 
                                                            //"were", 
                                                            //"weren't", 
                                                            //"wert", 
                                                            //"we've", 
                                                            //"what", 
                                                            //"whatever", 
                                                            //"what'll", 
                                                            //"what's", 
                                                            //"when", 
                                                            //"whencesoever", 
                                                            //"whenever", 
                                                            //"when's", 
                                                            //"whereas", 
                                                            //"where's", 
                                                            //"whether", 
                                                            //"which", 
                                                            //"whichever", 
                                                            //"whichsoever", 
                                                            //"while", 
                                                            //"whilst", 
                                                            //"who", 
                                                            //"who'd", 
                                                            //"whoever", 
                                                            //"whole", 
                                                            //"who'll", 
                                                            //"whom", 
                                                            //"whore", 
                                                            //"who's", 
                                                            //"whose", 
                                                            //"whoso", 
                                                            //"whosoever", 
                                                            //"will", 
                                                            //"with", 
                                                            //"within", 
                                                            //"without", 
                                                            //"wont", 
                                                            //"would", 
                                                            //"wouldn't", 
                                                            //"wouldst", 
                                                            //"x", 
                                                            //"y", 
                                                            //"ye", 
                                                            //"yet", 
                                                            //"you", 
                                                            //"you'd", 
                                                            //"you'll", 
                                                            //"your", 
                                                            //"you're", 
                                                            //"yours", 
                                                            //"yourself", 
                                                            //"yourselves", 
                                                            //"you've", 
                                                            //"z"

		};

        public static string[] extraStopWordsList = new string[] {  "e.g",
                                                                    "i.e",
                                                                    "abc0003",
                                                                    "abc-t123",
                                                                    "achmetow",
                                                                    "afaict",
                                                                    "afaik",
                                                                    "asap",
                                                                    "aug",
                                                                    "b04",
                                                                    "clr",
                                                                    "cq",
                                                                    "cq1803",
                                                                    "cqnnn",
                                                                    "def123",
                                                                    "denis",
                                                                    "e.g",
                                                                    "e4",
                                                                    "ed",
                                                                    "edt",
                                                                    "eugene",
                                                                    "fyi",
                                                                    "give",
                                                                    "h1",
                                                                    "h2",
                                                                    "i.e",
                                                                    "i20070621-1340",
                                                                    "i20080617-2000",
                                                                    "i20091126-2200-e3x",
                                                                    "iv",
                                                                    "jan",
                                                                    "jong",
                                                                    "m20070212-1330",
                                                                    "m20071023-1652",
                                                                    "m20080221-1800",
                                                                    "m20080911-1700",
                                                                    "m20100909-0800",
                                                                    "m20110909-1335",
                                                                    "m20120208-0800",
                                                                    "p1-p5",
                                                                    "p2",
                                                                    "p3",
                                                                    "pb",
                                                                    "pebkac",
                                                                    "pingel",
                                                                    "pm",
                                                                    "pov",
                                                                    "px",
                                                                    "r4e",
                                                                    "rc1",
                                                                    "roy",
                                                                    "rv",
                                                                    "sp2",
                                                                    "sr1",
                                                                    "steffen",
                                                                    "tehrnhoefer",
                                                                    "thomas",
                                                                    "timur",
                                                                    "v20090912-0400-e3x",
                                                                    "v20091015-0500-e3x",
                                                                    "v20100608-0100-e3x",
                                                                    "v20110422-0200",
                                                                    "v201202080800",
                                                                    "wed",
                                                                    "ws",
                                                                    "xxx",
                                                                    "xxxx",
                                                                    "zoe", 
                                                                    /*//extra for 394920
                                                                    "bug", 
                                                                    "mark",
                                                                    "duplicate", 
                                                                    "helpful",
                                                                    "dialog",
                                                                    "editor",
                                                                    "ui",
                                                                    "user",
                                                                    "nice",
                                                                    "great",
                                                                    "made",
                                                                    "value",
                                                                    "happy",
                                                                    "dirty",
                                                                    "eyesight",
                                                                    "side-by-side",
                                                                    "exist",
                                                                    "over-weight"
                                                                    //extra for 394920*/
        };


        private static Hashtable _stopwords = null;

        public static object AddElement(IDictionary collection, Object key, object newValue)
        {
            object element = collection[key];
            collection[key] = newValue;
            return element;
        }

        public static bool IsStopword(string str)
        {

            //int index=Array.BinarySearch(stopWordsList, str)
            return _stopwords.ContainsKey(str.ToLower());
        }


        public StopWordsHandler(bool bSyn)
        {
            //if (_stopwords == null)
            //{
            _stopwords = new Hashtable();
            double dummy = 0;
            foreach (string word in stopWordsList)
            {
                AddElement(_stopwords, word, dummy);
            }
            if (bSyn == true)
            {
                foreach (string word in extraStopWordsList)
                {
                    AddElement(_stopwords, word, dummy);
                }

            }
            //}
        }
    }

    /* ****** ****** ****** ******
   Class used to partition strings into SUBwords
   *  ****** ****** ****** ****** */
    internal class Tokeniser
    {
        //PH 11 apr
        bool bStem; //true if stemming is done
        bool bRemoveStop; //true if stop words are removed
        bool bLower; //true if lower casing is done
                     //System.IO.StreamWriter sw = new System.IO.StreamWriter("Removed.txt", true);

        // function to conver an ArrayList into a string[] array
        public static string[] ArrayListToArray(ArrayList arraylist)
        {
            string[] array = new string[arraylist.Count];
            for (int i = 0; i < arraylist.Count; i++) array[i] = (string)arraylist[i];
            return array;
        }

        public string[] Partition(string input, bool bBi, bool bSyn, bool bCode, out int nrMatches)
        {
            //PH 5 apr
            //Regex r=new Regex("([ \\t{}():;. \n])");		
            //PH 15 apr
            //Regex r = new Regex(@"[a-z]");			

            if (bLower) input = input.ToLower();

            nrMatches = 0;

            if (bCode)
            {
                //remove cvs logs
                Regex r14 = new Regex(@"[C|c]hecking\sin(.|\n)*done");
                nrMatches += r14.Matches(input).Count;
                input = r14.Replace(input, "\n");
                Regex r15 = new Regex(@"[C|c]hecking\sin(.|\n)*revision.*\n");
                nrMatches += r15.Matches(input).Count;
                input = r15.Replace(input, "\n");
                Regex r16 = new Regex(@"/cvs/(.|\n)*revision.*\n");
                nrMatches += r16.Matches(input).Count;
                //Regex r16 = new Regex(@"/cvs");
                //test
                MatchCollection testMatches = Regex.Matches(input, r16.ToString());
                //foreach (Match m in testMatches)
                //{
                //    Debug.WriteLine(m.ToString());
                //}
                //do it
                input = r16.Replace(input, "\n");
            }

            //PH 15 apr
            // Remove URLs 
            Regex r1 = new Regex(@"\w+\://\S+");
            nrMatches += r1.Matches(input).Count;
            //test
            //MatchCollection matches1 = Regex.Matches(input, r1.ToString());
            //foreach (Match m in matches1)
            //{
            //   sw.WriteLine(m.ToString());
            //}
            //do it
            input = r1.Replace(input, " ");

            // Remove file paths
            Regex r2 = new Regex(@"((\w+/\w+(/\w+)+)|(\w+(\\\w+)+))");
            nrMatches += r2.Matches(input).Count;
            //test
            //MatchCollection matches2 = Regex.Matches(input, r2.ToString());
            //foreach (Match m in matches2)
            //{
            //    sw.WriteLine(m.ToString());
            //}
            //sw.Close();
            //do it
            input = r2.Replace(input, " ");

            // Remove quoted text
            Regex r3 = new Regex(@"\n>.*\n");
            nrMatches += r3.Matches(input).Count;
            //test
            //MatchCollection matches3 = Regex.Matches(input, r3.ToString());
            //foreach (Match m in matches3)
            //{
            //    sw.WriteLine(m.ToString());
            //}
            //sw.Close();
            //do it
            input = r3.Replace(input, "\n ");

            if (bCode)
            {
                //remove source code
                //remove stack traces
                Regex r4 = new Regex(@"\n\s*at\s*\S+(\.\S)*\(.*\)");
                nrMatches += r4.Matches(input).Count;
                input = r4.Replace(input, "\n ");
                Regex r5 = new Regex(@"\n[\w\.]+[e|E]xception.*(?=\n)");
                nrMatches += r5.Matches(input).Count;
                input = r5.Replace(input, "\n ");
                Regex r6 = new Regex(@"\-\-\s[E|e]rror\s[D|d]etails(.|\n)*[E|e]xception\s[S|s]tack\s[t|T]race\:");
                nrMatches += r6.Matches(input).Count;
                input = r6.Replace(input, "\n ");

                //remove build ID and other related comments
                Regex r7 = new Regex(@"[B|b]uild\s[I|i][d|D].*\n");
                nrMatches += r7.Matches(input).Count;
                input = r7.Replace(input, "\n ");
                Regex r8 = new Regex(@"[U|u]ser\-[A|a]gent.*\n");
                nrMatches += r8.Matches(input).Count;
                input = r8.Replace(input, "\n ");
                Regex r9 = new Regex(@"[S|s]teps.*[R|r]eproduce\:");
                nrMatches += r9.Matches(input).Count;
                input = r9.Replace(input, " ");
                Regex r10 = new Regex(@"[M|m]ore\s[I|i]nformation\:");
                nrMatches += r10.Matches(input).Count;
                input = r10.Replace(input, " ");
                Regex r11 = new Regex(@"[R|r]eproducible\:.*(?=\n)");
                nrMatches += r11.Matches(input).Count;
                input = r11.Replace(input, " ");
                Regex r13 = new Regex(@"\n[T|t]hread\s\[.*\n(.*line.*\n)+");
                nrMatches += r13.Matches(input).Count;
                input = r13.Replace(input, "\n ");

                //remove code lines
                Regex r12 = new Regex(@"\n.*[\;|\}|\{](?=\n)");
                nrMatches += r12.Matches(input).Count;
                input = r12.Replace(input, "\n ");


            } //end if bCode

            // Get words
            MatchCollection matches = Regex.Matches(input, @"\b[a-z|A-Z]\w*((\.|\-|_|')*(\w+))*");
            String[] tokens = new String[matches.Count];
            int j = 0;
            foreach (Match m in matches)
            {
                tokens[j] = m.Value;
                j++;
            }

            ArrayList filter = new ArrayList();
            string strLast = "";

            //added stemming PH 27 march
            PorterStemming ps = new PorterStemming();

            for (int i = 0; i < tokens.Length; i++)
            {
                string s;
                if (bSyn)
                {
                    s = CorrectTerm(tokens[i]);
                }
                else
                {
                    s = tokens[i];
                }
                if (bRemoveStop)
                {
                    if (!StopWordsHandler.IsStopword(tokens[i]))
                    {
                        if (bStem)
                        {
                            s = ps.stemTerm(s.Replace("'", ""));
                        }
                        else
                        {
                            s = s.Replace("'", "");
                        }
                        if (!bBi) filter.Add(s);
                        if (strLast != "" && bBi) filter.Add(strLast + " " + s);
                        strLast = s;
                    }
                    else
                    {
                        if (strLast != "" && bBi) filter.Add(strLast + " " + s);
                        strLast = "";
                    }
                }
                else
                {
                    if (bStem)
                    {
                        s = ps.stemTerm(s.Replace("'", ""));
                    }
                    else
                    {
                        s = s.Replace("'", "");
                    }
                    if (!bBi) filter.Add(s);
                    if (strLast != "" && bBi) filter.Add(strLast + " " + s);
                    strLast = s;
                }
            }
            return ArrayListToArray(filter);
        }


        public Tokeniser(bool bStem, bool bRemoveStop, bool bLower)
        {
            this.bStem = bStem;
            this.bRemoveStop = bRemoveStop;
            this.bLower = bLower;
            lstWrong.Clear();
            lstWrong.AddRange(wrongWords);
        }

        public static string[] wrongWords = new string[] {
            "ablity",
            "accidently",
            "achive",
            "appered",
            "assuption",
            "attachements",
            "auth",
            "avaliability",
            "behaviour",
            "bolded",
            "bugzill",
            "challening",
            "ci",
            "ctlr",
            "currenlty",
            "custonm",
            "defautl",
            "defs",
            "delgators",
            "depdencies",
            "dev",
            "diabled",
            "dialogue",
            "didnt",
            "diff",
            "dnd",
            "drop-down",
            "e.g",
            "elses",
            "e-mails",
            "enterting",
            "exisiting",
            "focussed",
            "impl",
            "incase",
            "int",
            "isse",
            "junit3",
            "junit4",
            "key-bindings",
            "maintainance",
            "meta-data",
            "peformquery",
            "plug-in",
            "presync",
            "read-only",
            "recognise",
            "reopen",
            "repos",
            "reuse",
            "rev",
            "sec",
            "short-cut",
            "similiar",
            "simliar",
            "someting",
            "specifiy",
            "sub-menu",
            "submited",
            "submition",
            "sub-section",
            "sub-task",
            "sub-tasks",
            "successfull",
            "sufficent",
            "swe",
            "sync",
            "synchronisation",
            "synchronising",
            "synchronization",
            "syncing",
            "synctaskjob",
            "taks",
            "taskes",
            "task-repositories",
            "timeout",
            "validatesetttings",
            "wil",
            "work-arounds",
            "wouldt",
            "xml_rpc",
            "xmlrpc",
            "unsubmitted",
            "dup"
        };

        public static string[] correctWords = new string[] {
            "ability",
            "accidentally",
            "achieve",
            "appeared",
            "assumption",
            "attachments",
            "authentication",
            "availability",
            "behavior",
            "bold",
            "bugzilla",
            "challenging",
            "checkin",
            "ctrl",
            "currently",
            "custom",
            "default",
            "definitions",
            "delegators",
            "dependencies",
            "development",
            "disabled",
            "dialog",
            "didn't",
            "difference",
            "drag and drop",
            "dropdown",
            "abbr",
            "else's",
            "email",
            "entering",
            "existing",
            "focused",
            "implementation",
            "in case",
            "integer",
            "issue",
            "junit",
            "junit",
            "keybindings",
            "maintenance",
            "metadata",
            "performquery",
            "plugin",
            "presynchronization",
            "readonly",
            "recognize",
            "re-open",
            "repositories",
            "re-use",
            "revision",
            "seconds",
            "shortcut",
            "similar",
            "similar",
            "something",
            "specify",
            "submenu",
            "submitted",
            "submission",
            "subsection",
            "subtask",
            "subtasks",
            "successful",
            "sufficient",
            "we",
            "synchronization",
            "synchronization",
            "synchronizing",
            "synchronization",
            "synchronizing",
            "synchronizetasksjob",
            "task",
            "tasks",
            "taskrepositories",
            "time-out",
            "validatesettings",
            "will",
            "workarounds",
            "would",
            "xml-rpc",
            "xml-rpc",
            "submit",
            "duplicate"
        };

        List<string> lstWrong = new List<string>();

        private string CorrectTerm(string input)
        {
            if (lstWrong.Contains(input))
            {
                return correctWords[lstWrong.IndexOf(input)];
            }
            else
            {
                return input;
            }
        }

    }

    /* *** *** *** *** *** ***
     * Classes used for TFIDF
     * *** *** *** *** *** *** */
    /// <summary>
    /// Summary description for TF_IDFLib.
    /// </summary>
    public class TFIDFMeasure
    {
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

        public TFIDFMeasure(string[] documents, bool bStem, bool bRemoveStop, bool bLower, bool bBi, bool bSyn, bool bCode, bool bMulti, bool bDoc)
        {
            _docs = documents;
            _numDocs = documents.Length;
            MyInit(bStem, bRemoveStop, bLower, bBi, bSyn, bCode, bMulti, bDoc);
        }

        public TFIDFMeasure(FeatureCollection fc, bool bStem, bool bRemoveStop, bool bWTitle, bool bBi, bool bSyn, bool bLower, bool bCode, bool bMulti, bool bDoc)
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
        public TFIDFMeasure(FeatureCollection fc, int iFt, bool bStem, bool bRemoveStop, bool bWTitle, bool bBi, bool bSyn, bool bLower, bool bCode, bool bMulti, bool bDoc)
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