using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
// Located in c:\Program Files (x86)\COEST\TraceLab\lib\TraceLabSDK.dll
using TraceLabSDK;
using TraceLabSDK.Types;

namespace ComponentSolutions
{
    [Component(Name = "Setup",
                Description = "",
                Author = "Matthew Rife & Jen Lee",
                Version = "2.0",
                ConfigurationType = typeof(SetupComponentConfiguration)
        )]
    [IOSpec(IOType = IOSpecType.Output, Name = "outputName", DataType = typeof(TLArtifactsCollection))]
    public class SetupComponent : BaseComponent
    {
        public SetupComponent(ComponentLogger log) : base(log)
        {
            this.Configuration = new SetupComponentConfiguration();
        }

        public new SetupComponentConfiguration Configuration
        {
            get => base.Configuration as SetupComponentConfiguration;
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