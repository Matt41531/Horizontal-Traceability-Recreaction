using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;

// Located in c:\Program Files (x86)\COEST\TraceLab\lib\TraceLabSDK.dll
using TraceLabSDK;

namespace TFIDF
{

    [Component(Name = "TFIDF",
                Description = "Component to calculate the TFIDF of feature requests",
                Author = "Jen Lee at University of Kentucky: Based on contributions from Dr. Petra Heck and Dr. Andy Zaidman",
                Version = "1.0",
                ConfigurationType = typeof(TFIDF_configuration)
        )]

    // Get inputs from "Input XML" component
    [IOSpec(IOType = IOSpecType.Input, Name = "inputFile", DataType = typeof(string))]
    [IOSpec(IOType = IOSpecType.Input, Name = "outputDirectory", DataType = typeof(string))]
    [IOSpec(IOType = IOSpecType.Input, Name = "AC", DataType = typeof(bool))]
    [IOSpec(IOType = IOSpecType.Input, Name = "SC", DataType = typeof(bool))]
    [IOSpec(IOType = IOSpecType.Input, Name = "SM", DataType = typeof(bool))]
    [IOSpec(IOType = IOSpecType.Input, Name = "SR", DataType = typeof(bool))]
    [IOSpec(IOType = IOSpecType.Input, Name = "DW", DataType = typeof(bool))]
    [IOSpec(IOType = IOSpecType.Input, Name = "BG", DataType = typeof(bool))]
    [IOSpec(IOType = IOSpecType.Input, Name = "SY", DataType = typeof(bool))]
    [IOSpec(IOType = IOSpecType.Input, Name = "LO", DataType = typeof(bool))]
    [IOSpec(IOType = IOSpecType.Input, Name = "MU", DataType = typeof(bool))]
    [IOSpec(IOType = IOSpecType.Input, Name = "DO", DataType = typeof(bool))]
    [IOSpec(IOType = IOSpecType.Input, Name = "fc_type", DataType = typeof(int))]

    public class TFIDF : BaseComponent
    {
        // global variables
        Method_type selectedMethod;
        string inputFile;
        string outputDirectory;
        bool AC; bool SC; bool SM; bool SR; bool DW; bool BG; bool SY; bool LO; bool MU; bool DO;
        string TFIDF_Text;

        // access to classes
        TFIDFMeasure tf;
        FeatureCollection fc;
        setFiles sF;

        // connect to configuration file
        public TFIDF(ComponentLogger log) : base(log)
        {
            this.Configuration = new TFIDF_configuration();
        }
        public new TFIDF_configuration Configuration
        {
            get => base.Configuration as TFIDF_configuration;
            set => base.Configuration = value;
        }

        public override void Compute()
        {
            // load data from Worspace
            inputFile = (string)Workspace.Load("inputFile");
            outputDirectory = (string)Workspace.Load("outputDirectory");
            AC = (bool)Workspace.Load("AC");
            SC = (bool)Workspace.Load("SC");
            SM = (bool)Workspace.Load("SM");
            SR = (bool)Workspace.Load("SR");
            DW = (bool)Workspace.Load("DW");
            BG = (bool)Workspace.Load("BG");
            SY = (bool)Workspace.Load("SY");
            LO = (bool)Workspace.Load("LO");
            MU = (bool)Workspace.Load("MU"); 
            DO = (bool)Workspace.Load("DO");
            int fc_type = (int)Workspace.Load("fc_type");

            // Tokenize the XML File
            // *** Note: Ideallythis would happen in a seperate Tokenizing Component
            // *** Hurdle: FeatureCollection is not serilalizable to be stored in Workspace
            sF = new setFiles(inputFile, outputDirectory, AC, SC, DW);
            fc = sF.InputXML(inputFile, fc_type);

            // get Method from configuration file
            selectedMethod = (this.Configuration.Method);

            // determine action based on the selected Method
            if (selectedMethod == Method_type.TFIDF)
            {
                // calculate the TFIDF using method TFIDF
                calculateTFIDF();
            }
            else if (selectedMethod == Method_type.TitleVsDescription)
            {
                calculateTitleVsDesctiption();
            }
            Logger.Trace(TFIDF_Text); // output to TraceLab logger
        }

        private void calculateTFIDF()
        {
            // Log beginning of process
            Utilities.LogMessageToFile(setFiles.logfile + "_TFIDF.txt", DateTime.Now.ToShortTimeString() + " Start TF-IDF");

            Utilities.LogMessageToFile(setFiles.logfile + "_TFIDF.txt", DateTime.Now.ToShortTimeString()
                + " Configuration: " + sF.getFileEnding(AC, SC, SM, SR, DW, BG, SY, LO, MU, DO));

            StopWordsHandler stopword = new StopWordsHandler(SY);

            tf = new TFIDFMeasure(fc, SM, SR, DW, BG, SY, LO, SC, MU, DO);
            Utilities.LogMessageToFile(setFiles.logfile + "_TFIDF.txt", DateTime.Now.ToShortTimeString() + " End TF-IDF");
            TFIDF_Text = "cosim(0,1) = " + tf.GetSimilarity(17, 259).ToString();
            TFIDF_Text += "; cosim(0,2) = " + tf.GetSimilarity(0, 2).ToString();
            Utilities.LogMessageToFile(setFiles.logfile + "_TFIDF.txt", DateTime.Now.ToShortTimeString() + " " + TFIDF_Text);

            string outputFile = outputDirectory + "\\duplicatesTFIDF" + sF.getFileEnding(AC, SC, SM, SR, DW, BG, SY, LO, MU, DO);
            outputFile += ".csv";
            System.IO.File.Delete(outputFile);
            System.IO.StreamWriter sw2 = new System.IO.StreamWriter(outputFile);
            sw2.WriteLine("feature_id, dup_id, rank, sim");

            //voor 1 duplicate top 10/20 vinden
            int cTotal = 0;
            int cTen = 0;
            int cTwenty = 0;
            foreach (Feature f in fc.featureList)
            {
                if (f.duplicate_id != "" && f.duplicate_id != null)
                {

                    //simlist wordt lijst met alle similarities
                    List<KeyValuePair<string, float>> simlist = new List<KeyValuePair<string, float>>();
                    if (f.id == "319295" && f.duplicate_id == "341829")
                    {
                    }
                    else
                    {
                        int v1 = sF.getFeatureIndex(f.id);
                        int d = sF.getFeatureIndex(f.duplicate_id);
                        if (v1 != -1 && d != -1)
                        {
                            cTotal++;
                            foreach (Feature f2 in fc.featureList)
                            {
                                if (f.id != f2.id)
                                {
                                    int v2 = sF.getFeatureIndex(f2.id);
                                    if (v2 != -1)
                                    {
                                        float sim = tf.GetSimilarity(v1, v2);
                                        KeyValuePair<string, float> kvp = new KeyValuePair<string, float>(f2.id, sim);
                                        simlist.Add(kvp);
                                    }
                                }
                            }

                            //sorteer op similarity
                            simlist = simlist.OrderByDescending(x => x.Value).ToList();
                            //vind ranking
                            f.dupRank = 0;
                            while (simlist.ElementAt(f.dupRank).Key != f.duplicate_id)
                            {
                                f.dupRank++;
                            }
                            f.dupSim = simlist.ElementAt(f.dupRank).Value;
                            f.dupRank += 1;
                            if (f.dupRank <= 10)
                            {
                                cTen++;
                            }

                            if (f.dupRank <= 20)
                            {
                                cTwenty++;
                            }

                            sw2.WriteLine(f.id + "," + f.duplicate_id + "," + f.dupRank + "," + f.dupSim.ToString("F4"));
                            Debug.WriteLine(DateTime.Now.ToShortTimeString() + " " + f.id + "," + f.duplicate_id + "," + f.dupRank + "," + f.dupSim.ToString("F4"));
                        }
                    }
                }
            }
            sw2.Close();
            //System.Diagnostics.Process.Start(outputFile);

            //calculate percentage in top 10 or top 20
            if (cTotal > 0)
            {
                double pTen = (double)cTen / cTotal;
                double pTwenty = (double)cTwenty / cTotal;
                DateTime end = DateTime.Now;
                //TimeSpan duration = end - start;
                //double time = duration.Days * 24 * 3600.0 + duration.Hours * 3600.0 + duration.Minutes * 60.0 + duration.Seconds + duration.Milliseconds / 1000.0;
                ////MessageBox.Show("top 10: " + pTen.ToString("F2") + "% out of " + cTotal + " \n top 20:" + pTwenty.ToString("F2") + "% out of " + cTotal);
                ////MessageBox.Show(String.Format("Total processing time {0:########.00} seconds", time));
                Utilities.LogMessageToFile(setFiles.logfile + "_TFIDF.txt", DateTime.Now.ToShortTimeString() + " top 10: " + pTen.ToString("F2") + "% out of " + cTotal + " \n top 20:" + pTwenty.ToString("F2") + "% out of " + cTotal);
                //Utilities.LogMessageToFile(setFiles.logfile + "_LSA.txt", DateTime.Now.ToShortTimeString() + String.Format(" Total processing time {0:########.00} seconds", time));

            }
            else
            {
                ////MessageBox.Show("No valid duplicates found");
                Utilities.LogMessageToFile(setFiles.logfile + "_TFIDF.txt", DateTime.Now.ToShortTimeString() + " No valid duplicates found");
            }
        }

        private void calculateTitleVsDesctiption()
        {
            Utilities.LogMessageToFile(setFiles.logfile + "_TFIDF.txt", DateTime.Now.ToShortTimeString() + " Start TF-IDF Title Vs. Description");

            StopWordsHandler stopword = new StopWordsHandler(SY);

            string outputFile = outputDirectory + "\\cossimTvsD" + sF.getFileEnding(AC, SC, SM, SR, DW, BG, SY, LO, MU, DO);
            Utilities.LogMessageToFile(setFiles.logfile + "_TFIDF.txt", DateTime.Now.ToShortTimeString() 
                + " T vs. D Output File: " + "cossim" + sF.getFileEnding(AC, SC, SM, SR, DW, BG, SY, LO, MU, DO) + "_xref.csv");

            outputFile += "_xref.csv";
            System.IO.File.Delete(outputFile);
            System.IO.StreamWriter sw = new System.IO.StreamWriter(outputFile, true);
            sw.WriteLine("Title_i, ID_i, Doc_j, ID_j, Cosine Similarity"); // updated deliminators to be commas instead of semi-colens
            for (int i = 0; i < fc.featureList.Count; i++)
            {
                Feature f1 = fc.featureList[i];
                //if (f1.id == "224119" || f1.id == "238186" || f1.id == "343755" || f1.id == "344748" ||
                //   f1.id == "353263" || f1.id == "363984" || f1.id == "364870" || f1.id == "376807" ||
                //   f1.id == "378528" || f1.id == "394920")
                //{

                tf = new TFIDFMeasure(fc, i, SM, SR, DW, BG, SY, LO, SC, MU, DO);
                for (int j = i + 1; j < fc.featureList.Count; j++) // updated from j = 0 to improve speed
                {
                    if (j != i) // *** pretty sure this if can be removed, just don't want to mess with for now
                    {
                        Feature f2 = fc.featureList[j];
                        sw.WriteLine(i.ToString() + "," + f1.id + "," + j.ToString() + "," + f2.id + "," + tf.GetSimilarity(i, j).ToString());
                    }
                }
                TFIDF_Text = i.ToString() + "done!";
                ///// lblTDF.Refresh();
                //}
            }
            sw.Close();

            string outputFile2 = outputDirectory + "\\duplicatesTvsD" + sF.getFileEnding(AC, SC, SM, SR, DW, BG, SY, LO, MU, DO);
            outputFile += ".csv";
            System.IO.File.Delete(outputFile);
            System.IO.StreamWriter sw2 = new System.IO.StreamWriter(outputFile);
            sw2.WriteLine("feature_id, dup_id, rank, sim");

            //voor 1 duplicate top 10/20 vinden
            int cTotal = 0;
            int cTen = 0;
            int cTwenty = 0;
            foreach (Feature f in fc.featureList)
            {
                if (f.duplicate_id != "" && f.duplicate_id != null)
                {

                    //simlist wordt lijst met alle similarities
                    List<KeyValuePair<string, float>> simlist = new List<KeyValuePair<string, float>>();
                    if (f.id == "319295" && f.duplicate_id == "341829")
                    {
                    }
                    else
                    {
                        int v1 = sF.getFeatureIndex(f.id);
                        int d = sF.getFeatureIndex(f.duplicate_id);
                        if (v1 != -1 && d != -1)
                        {
                            cTotal++;
                            foreach (Feature f2 in fc.featureList)
                            {
                                if (f.id != f2.id)
                                {
                                    int v2 = sF.getFeatureIndex(f2.id);
                                    if (v2 != -1)
                                    {
                                        float sim = tf.GetSimilarity(v1, v2);
                                        KeyValuePair<string, float> kvp = new KeyValuePair<string, float>(f2.id, sim);
                                        simlist.Add(kvp);
                                    }
                                }
                            }

                            //sorteer op similarity
                            simlist = simlist.OrderByDescending(x => x.Value).ToList();
                            //vind ranking
                            f.dupRank = 0;
                            while (simlist.ElementAt(f.dupRank).Key != f.duplicate_id)
                            {
                                f.dupRank++;
                            }
                            f.dupSim = simlist.ElementAt(f.dupRank).Value;
                            f.dupRank += 1;
                            if (f.dupRank <= 10)
                            {
                                cTen++;
                            }

                            if (f.dupRank <= 20)
                            {
                                cTwenty++;
                            }

                            sw2.WriteLine(f.id + "," + f.duplicate_id + "," + f.dupRank + "," + f.dupSim.ToString("F4"));
                            Debug.WriteLine(DateTime.Now.ToShortTimeString() + " " + f.id + "," + f.duplicate_id + "," + f.dupRank + "," + f.dupSim.ToString("F4"));
                        }
                    }
                }
            }
            sw2.Close();
            //System.Diagnostics.Process.Start(outputFile);

            //calculate percentage in top 10 or top 20
            if (cTotal > 0)
            {
                double pTen = (double)cTen / cTotal;
                double pTwenty = (double)cTwenty / cTotal;
                DateTime end = DateTime.Now;
                //TimeSpan duration = end - start;
                //double time = duration.Days * 24 * 3600.0 + duration.Hours * 3600.0 + duration.Minutes * 60.0 + duration.Seconds + duration.Milliseconds / 1000.0;
                ////MessageBox.Show("top 10: " + pTen.ToString("F2") + "% out of " + cTotal + " \n top 20:" + pTwenty.ToString("F2") + "% out of " + cTotal);
                ////MessageBox.Show(String.Format("Total processing time {0:########.00} seconds", time));
                Utilities.LogMessageToFile(setFiles.logfile + "_TFIDF.txt", DateTime.Now.ToShortTimeString() + " top 10: " + pTen.ToString("F2") + "% out of " + cTotal + " \n top 20:" + pTwenty.ToString("F2") + "% out of " + cTotal);
                //Utilities.LogMessageToFile(setFiles.logfile + "_LSA.txt", DateTime.Now.ToShortTimeString() + String.Format(" Total processing time {0:########.00} seconds", time));

            }
            else
            {
                ////MessageBox.Show("No valid duplicates found");
                Utilities.LogMessageToFile(setFiles.logfile + "_TFIDF.txt", DateTime.Now.ToShortTimeString() + " No valid duplicates found");
            }

        }
    }

}