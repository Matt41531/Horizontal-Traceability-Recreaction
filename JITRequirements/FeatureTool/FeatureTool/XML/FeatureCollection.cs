using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FeatureTool
{
    public class FeatureCollection
    {
        public List<Feature> featureList = new List<Feature>();
        public string bugTag;

        public int TFIDF(string stem)
        {
            int documentCount=0;
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
