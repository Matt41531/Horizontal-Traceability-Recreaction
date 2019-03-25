using System;
using System.ComponentModel;

// Located in c:\Program Files (x86)\COEST\TraceLab\lib\TraceLabSDK.dll
using TraceLabSDK;

namespace TFIDF
{

    [Component(Name = "TFIDF",
                Description = "*Insert*",
                Author = "*Insert*",
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

            // Tokenize the XML File
            // *** Note: Ideallythis would happen in a seperate Tokenizing Component
            // *** Hurdle: FeatureCollection is not serilalizable to be stored in Workspace
            sF = new setFiles(inputFile, outputDirectory, AC, SC, DW);
            fc = sF.InputXML(inputFile);

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

            StopWordsHandler stopword = new StopWordsHandler(SY);

            tf = new TFIDFMeasure(fc, SM, SR, DW, BG, SY, LO, SC, MU, DO);
            Utilities.LogMessageToFile(setFiles.logfile + "_TFIDF.txt", DateTime.Now.ToShortTimeString() + " End TF-IDF");
            TFIDF_Text = "cosim(0,1) = " + tf.GetSimilarity(17, 259).ToString();
            TFIDF_Text += "; cosim(0,2) = " + tf.GetSimilarity(0, 2).ToString();

        }

        private void calculateTitleVsDesctiption()
        {
            Utilities.LogMessageToFile(setFiles.logfile + "_TFIDF.txt", DateTime.Now.ToShortTimeString() + " Start TF-IDF");

            StopWordsHandler stopword = new StopWordsHandler(SY);

            string outputFile = outputDirectory + "\\cossim" + sF.getFileEnding(AC, SC, SM, SR, DW, BG, SY, LO, MU, DO);
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
        }
    }

}