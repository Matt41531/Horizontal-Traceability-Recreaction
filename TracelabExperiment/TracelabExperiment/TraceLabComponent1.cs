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
            // your component implementation
            try
            {
                Logger.Trace(this.Configuration.Artifacts.Absolute);
            }
            catch(Exception e)
            {
                Logger.Trace("Error: Please include an input artifact", e);
                return;
            }
            var inputFile = this.Configuration.Artifacts.Absolute;
            
            string strCmdText;
            string strStartingText;
            strStartingText = "/C ";
            strCmdText = "ipconfig/all";
            System.Diagnostics.Process.Start("CMD.exe", (strStartingText + inputFile));
            Logger.Trace(inputFile);
            Logger.Trace("Worked");
            
            //Workspace.Store("outputName", 5);
        }
    }
}