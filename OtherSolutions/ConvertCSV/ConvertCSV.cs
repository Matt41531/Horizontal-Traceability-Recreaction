using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace ConvertCSV
{
    class ConvertCSV
    {
        static void Main(string[] args)
        {
            try
            {
                // ****** Variable that changes *****
                // Options: mylyn, argouml, or netbeans
                var dataset = "netbeans";
                // **********************************
                var filename = "";

                // Assign filename
                if (dataset == "mylyn")
                    filename = "Mylyn_cossim_LO_SP_DW_SM_SC_AC";
                else if (dataset == "argouml")
                    filename = "ArgoUML_cossim_LO_SP_DW_SM_SC_AC";
                else if (dataset == "netbeans")
                    filename = "Netbeans_Top50_TFIDF_LO_SP_DW_SM_SC_AC";

                // open csv, replace all ',' with '.'
                using (StreamReader sr = new StreamReader(@"../../../../Databases/" + filename + ".csv"))
                {
                    String line = sr.ReadToEnd();

                    String newline = line.Replace(',', '.');

                    var newfile = new StringBuilder();

                    newfile.Append(newline);

                    File.WriteAllText(@"../../../../Databases/" + filename + "_formatted.csv", newfile.ToString());

                }
            }
            catch (Exception e)
            {
                Console.WriteLine("The File could not be read:");
                Console.WriteLine(e.Message);

                Console.ReadLine();
            }
        }
    }
}

