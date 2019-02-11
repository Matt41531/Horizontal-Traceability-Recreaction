using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
// Located in c:\Program Files (x86)\COEST\TraceLab\lib\TraceLabSDK.dll
using TraceLabSDK;
using TraceLabSDK.Types;

namespace TracelabExperiment
{
    [Component(Name = "FeatureRequestsTest",
                Description = "",
                Author = "Matthew Rife & Jen Lee",
                Version = "2.1",
                ConfigurationType = typeof(TraceLabComponent1Configuration)
        )]
    [IOSpec(IOType = IOSpecType.Output, Name = "outputName", DataType = typeof(TLArtifactsCollection))]
    public class TraceLabComponent1 : BaseComponent
    {
        public TraceLabComponent1(ComponentLogger log) : base(log)
        {
            this.Configuration = new TraceLabComponent1Configuration();
        }

        public new TraceLabComponent1Configuration Configuration
        {
            get => base.Configuration as TraceLabComponent1Configuration;
            set => base.Configuration = value;
        }
        public override void Compute()
        {
            // Check for input artifact
            try
            {
                Logger.Trace(this.Configuration.Artifacts.Absolute);
            }
            catch(Exception e)
            {
                Logger.Trace("Error: Missing input artifact", e);
                return;
            }
            //Run command line tool with parameters
            var inputFile = this.Configuration.Artifacts.Absolute;
            try
            {
                Logger.Trace(this.Configuration.OutputDirectory.Absolute);
            }
            catch(Exception e)
            {
                Logger.Trace("Error: Invalid output directory", e);
            }

            var outputDirectory = this.Configuration.OutputDirectory.Absolute;

            string strCmdText;
            string strStartingText;
            strStartingText = "/C ";
            strCmdText = "ipconfig/all";
            System.Diagnostics.Process.Start("CMD.exe", (strStartingText + inputFile));
            //DEBUGGING prints
            Logger.Trace(inputFile);
            Logger.Trace("Worked");
            //Store values?
            Workspace.Store("outputName", 5);
        }
    }
}