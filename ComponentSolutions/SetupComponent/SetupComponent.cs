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
                Version = "2.1",
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
            try
            { 
                Logger.Trace(this.Configuration.Artifacts.Absolute);
            }
            catch (Exception e)
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
            catch (Exception e)
            { 
                Logger.Trace("Error: Invalid output directory", e);
            }
            var outputDirectory = this.Configuration.OutputDirectory.Absolute;
            string strCmdText;
            string strStartingText = "/C ";
            string[] directories = inputFile.Split();
            strCmdText = "/C ipconfig/all";
            System.Diagnostics.Process.Start("CMD.exe", (strStartingText + inputFile));
            //DEBUGGING prints        
            Logger.Trace(inputFile);
            Logger.Trace(directories);
            Logger.Trace("Worked");
            
            //Workspace.Store("outputName", 5);
        }
    }
}