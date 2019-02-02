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
                Version = "2.0",
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
    
            var inputFile = this.Configuration.Artifacts.Absolute;
            
            string strCmdText;
            strCmdText = "/C ipconfig/all";
            System.Diagnostics.Process.Start("CMD.exe", strCmdText);
            Logger.Trace(inputFile);
            Logger.Trace("Worked");
            
            //Workspace.Store("outputName", 5);
        }
    }
}