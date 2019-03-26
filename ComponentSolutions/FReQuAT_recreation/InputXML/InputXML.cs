using System;
using System.ComponentModel;

// Located in c:\Program Files (x86)\COEST\TraceLab\lib\TraceLabSDK.dll
using TraceLabSDK;

namespace InputXML
{
    [Component(Name = "Input XML",
                Description = "Component to input feature request file",
                Author = "Jen Lee & Matthew Rife at University of Kentucky: Based on contributions from Dr. Petra Heck and Dr. Andy Zaidman",
                Version = "1.0",
                ConfigurationType = typeof(InputXML_configuration)
        )]

    // Workspace items to store directory paths for needed files
    [IOSpec(IOType = IOSpecType.Output, Name = "outputDirectory", DataType = typeof(String))]
    [IOSpec(IOType = IOSpecType.Output, Name = "inputFile", DataType = typeof(String))]

    // Workplace items to store boolean values to the workspace used in Tokenization
    [IOSpec(IOType = IOSpecType.Output, Name = "AC", DataType = typeof(bool))]
    [IOSpec(IOType = IOSpecType.Output, Name = "SC", DataType = typeof(bool))]
    [IOSpec(IOType = IOSpecType.Output, Name = "SM", DataType = typeof(bool))]
    [IOSpec(IOType = IOSpecType.Output, Name = "SR", DataType = typeof(bool))]
    [IOSpec(IOType = IOSpecType.Output, Name = "DW", DataType = typeof(bool))]
    [IOSpec(IOType = IOSpecType.Output, Name = "BG", DataType = typeof(bool))]
    [IOSpec(IOType = IOSpecType.Output, Name = "SY", DataType = typeof(bool))]
    [IOSpec(IOType = IOSpecType.Output, Name = "LO", DataType = typeof(bool))]
    [IOSpec(IOType = IOSpecType.Output, Name = "MU", DataType = typeof(bool))]
    [IOSpec(IOType = IOSpecType.Output, Name = "DO", DataType = typeof(bool))]

    // Workpace item to store the type fo Feature Collection to process the file as
    [IOSpec(IOType = IOSpecType.Output, Name = "fc_type", DataType = typeof(int))]

    public class InputXML : BaseComponent
    {
        // connect to configuration file
        public InputXML(ComponentLogger log) : base(log)
        {
            this.Configuration = new InputXML_configuration();
        }

        public new InputXML_configuration Configuration
        {
            get => base.Configuration as InputXML_configuration;
            set => base.Configuration = value;
        }

        public override void Compute()
        {
            // try importing XML input file
            try
            {
                Logger.Trace(this.Configuration.XML.Absolute);
            }
            catch (Exception e)
            {
                Logger.Trace("Error: Missing input artifact", e);
                return;
            }
            // set and store inputFile
            string inputFile = this.Configuration.XML.Absolute; // feature requests file
            Workspace.Store("inputFile", inputFile);

            try
            {
                Logger.Trace(this.Configuration.OutputDirectory.Absolute);
            }
            catch (Exception e)
            {
                Logger.Trace("Error: Invalid output directory", e);
            }

            // set and store outputDirectory
            var outputDirectory = this.Configuration.OutputDirectory.Absolute; // get output directory from configuration
            Workspace.Store("outputDirectory", outputDirectory);

            // set and story boolean values used for tokenization
            bool AC = get_bool(this.Configuration.AC); // get boolean value for "AC: All Comments"
            Workspace.Store("AC", AC);
            bool SC = get_bool(this.Configuration.SC); // get boolean value for "SC: Source code removal"
            Workspace.Store("SC", SC);
            bool SM = get_bool(this.Configuration.SM); // get boolean value for "SM: Stremming"
            Workspace.Store("SM", SM);
            bool SR = get_bool(this.Configuration.SR); // get boolean value for "SR: Stop word removal"
            Workspace.Store("SR", SR);
            bool DW = get_bool(this.Configuration.DW); // get boolean value for "DW: Double weight title"
            Workspace.Store("DW", DW);
            bool BG = get_bool(this.Configuration.BG); // get boolean value for "BG: Bi-gram"
            Workspace.Store("BG", BG);
            bool SY = get_bool(this.Configuration.SY); // get boolean value for "SY: Remove synonyms, spelling mistakes"
            Workspace.Store("SY", SY);
            bool LO = get_bool(this.Configuration.LO); // get boolean value for "LO: toLower"
            Workspace.Store("LO", LO);
            bool MU = get_bool(this.Configuration.MU); // get boolean value for "MU: Remove words with count 1"
            Workspace.Store("MU", MU);
            bool DO = get_bool(this.Configuration.DO); // get boolean value for "DO: Remove words with only 1 document"
            Workspace.Store("DO", DO);

            int fc_type = get_fctype(this.Configuration.fc_type); // get int value to represent the feature collection type
            Workspace.Store("fc_type", fc_type);
        }

        // Function to take dropbox values, and return the resulting boolean
        public bool get_bool(Boolean_type dropbox)
        {
            bool truth_val;
            if (dropbox == Boolean_type.On)
            {
                truth_val = true;
            }
            else
            {
                truth_val = false;
            }
            return truth_val;
        }

        // Function to return an integer to represent FeatureCollection type
        public int get_fctype(FC_type type)
        {
            int return_int;
            if (type == FC_type.Bugzilla)
            {
                return_int = 0; // 0 = Bugzilla (Mylyn/Netbeans)
            } else
            {
                return_int = 1; // 1 = Tigris (ArgoUML)
            }
            return return_int;
        }
    }
}