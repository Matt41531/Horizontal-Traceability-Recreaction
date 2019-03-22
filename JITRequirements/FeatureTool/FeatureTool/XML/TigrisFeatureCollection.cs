using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace FeatureTool
{
    class TigrisFeatureCollection: FeatureCollection
    {
        //fills the featureList with Feature objects read from an XML file
        //xmlFilePath: path of the XML file, e.g. "features.xml"
        //bAllComments: true if all comments are loaded as a document, false if only first comment is loaded
        //bCode: true if code is kept, false if code should be removed
        //bWTitle: true if title is doubled in weight
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
}
