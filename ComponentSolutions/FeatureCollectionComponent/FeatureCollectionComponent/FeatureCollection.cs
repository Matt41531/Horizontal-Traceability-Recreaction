using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
// Located in c:\Program Files (x86)\COEST\TraceLab\lib\TraceLabSDK.dll
using TraceLabSDK;

namespace FeatureCollectionComponent
{
    [Component(Name = "FeatureCollection",
                Description = "",
                Author = "Jen Lee",
                Version = "1.0")]
    //[IOSpec(IOType = IOSpecType.Output, Name = "outputName", DataType = typeof(int))]
    public class FeatureCollection /*: BaseComponent */
    {
        /*public FeatureCollection(ComponentLogger log) : base(log) { }

        public override void Compute()
        {
            // your component implementation
            Logger.Trace("Hello World");

            //Workspace.Store("outputName", 5);
        } */

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
}