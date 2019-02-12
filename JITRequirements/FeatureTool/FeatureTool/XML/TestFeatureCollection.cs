using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace FeatureTool
{
    class TestFeatureCollection: FeatureCollection
    {
        //fills the featureList with Feature objects read from an XML file
        //xmlFilePath: path of the XML file, e.g. "features.xml"
        public TestFeatureCollection(string xmlFilePath)
        {
            //read XML file
            XDocument xdoc = XDocument.Load(xmlFilePath);

            //get all <feature> elements in the file
            IEnumerable<XElement> xFeat = from xf in xdoc.Descendants("feature")
                                           select xf;

            //read through each <feature> element to create a Feature object
            foreach (XElement item in xFeat)
            {
                string iD = (string)(from xe in item.Descendants("ID") select xe).First();
                string title = (string)(from xe in item.Descendants("title") select xe).First();
                IEnumerable<string> comm = from xe in item.Descendants("cText") select (string)xe;
                string desc = comm.First(); //in XML the description is the first comment
                Feature ft = new Feature(iD, title, desc, comm.Skip(1), false, false);
                featureList.Add(ft);
            }

        }
    }
}
