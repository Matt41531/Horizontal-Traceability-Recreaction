using System;
// Located in c:\Program Files (x86)\COEST\TraceLab\lib\TraceLabSDK.dll
using TraceLabSDK;

namespace TracelabExperiment
{
    [Component(Name = "FeatureRequestsTest",
                Description = "",
                Author = "Matthew Rife & Jen Lee",
                Version = "2.0")]
    //[IOSpec(IOType = IOSpecType.Output, Name = "outputName", DataType = typeof(int))]
    public class TraceLabComponent1 : BaseComponent
    {
        public TraceLabComponent1(ComponentLogger log) : base(log) { }

        public override void Compute()
        {
            // your component implementation
            /*
             This generates input file, just need to find the dependencies to run it first
            var inputFile = this.Configuration.Artifacts.Absolute;
            if (!this.fileSystem.File.Exists(inputFile))
            {
                throw new ComponentException("File path does not exist.");
            }
            */
            string strCmdText;
            strCmdText = "/C ipconfig/all";
            System.Diagnostics.Process.Start("CMD.exe", strCmdText);
            Logger.Trace("Worked");
            
            //Workspace.Store("outputName", 5);
        }
    }
}