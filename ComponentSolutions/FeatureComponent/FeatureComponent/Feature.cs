using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
// Located in c:\Program Files (x86)\COEST\TraceLab\lib\TraceLabSDK.dll
using TraceLabSDK;

namespace FeatureComponent
{
    [Component(Name = "Feature",
                Description = "",
                Author = "Jen Lee",
                Version = "1.0")]
    //[IOSpec(IOType = IOSpecType.Output, Name = "outputName", DataType = typeof(int))]
    public class Feature /*: BaseComponent */
    {
        /*public Feature(ComponentLogger log) : base(log) { }

        public override void Compute()
        {
            // your component implementation
            Logger.Trace("Hello World");

            //Workspace.Store("outputName", 5);
        } */

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
}