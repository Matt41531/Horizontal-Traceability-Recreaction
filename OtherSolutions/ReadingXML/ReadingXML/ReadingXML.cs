using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

// C# Program to try to confrim data from 3 .xml files

namespace ReadingXML
{

    // Class to hold globals
    // This is the only way I can figure out how to do this
    class Globals
    {
        public int requests;
        public int dups;
        public string id;

    }

    // Class to read through XML files and find data
    class ReadingXML
    {

    static void Main(string[] args)
        {

            // Allocate global values
            Globals g = new Globals();
            g.requests = 0;
            g.dups = 0;
            g.id = "";

            // ********** ONLY VALUE THAT CHANGES **********
            // Options: "netbeans", "argo", "mylyn"
            string file = "mylyn";
            // ********** ONLY VALUE THAT CHANGES **********

            // Allocate local variables
            string filename = ""; // create filename
            string root = ""; // create root

            if (file == "netbeans") {
                filename = "20130514_netbeans.xml";
                root = "bug";
                g.id = "bug_id";
            } else if (file == "argo")
            {
                filename = "20130514_argouml.xml";
                root = "issue";
                g.id = "issue_id";
            } else if (file == "mylyn")
            {
                filename = "20130408_mylyntasks.xml";
                root = "bug";
                g.id = "bug_id";
            } else
            {
                //Exit
            }


        // Loading from a file, you can also load from a stream
        // Note: Needs to be edited to open local file
             var xml = XDocument.Load(@"..\..\..\..\Databases\" + filename);

            // Parse every bug in the file to find total
            var requests = from r in xml.Root.Descendants(root)
                           // select all id values
                       select r.Element(g.id).Value;

            // Parse every bug in the file to find duplicates
            var dups = from d in xml.Root.Descendants(root)
                       // select all resolution values
                       select d.Element("resolution").Value;

            // Count id values to find number of reature requests
            // Note: Maybe some id's are repeated?
            foreach (string id in requests)
            {
                //Console.WriteLine("Bug ID: {0}", id);
                g.requests++;
            }

            // Count duplicates
            // Note: Maybe some duplicates are duplicated mulitple times
            foreach (string res in dups)
            {   
                if (res == "DUPLICATE")
                {
                    g.dups++;
                }
            } 

            // Output results
            Console.WriteLine("# Feature Requests: {0}", g.requests);
            Console.WriteLine("# Duplicates: {0}, ", g .dups);
            Console.ReadKey();
        }
    }
}
