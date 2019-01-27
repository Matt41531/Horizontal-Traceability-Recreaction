using System;
// Located in c:\Program Files (x86)\COEST\TraceLab\lib\TraceLabSDK.dll
using TraceLabSDK;

namespace TracelabExperiment
{
    [Component(Name = "FeatureRequestsTest",
                Description = "",
                Author = "Matthew Rife & Jen Lee",
                Version = "1.0")]
    //[IOSpec(IOType = IOSpecType.Output, Name = "outputName", DataType = typeof(int))]
    public class TraceLabComponent1 : BaseComponent
    {
        public TraceLabComponent1(ComponentLogger log) : base(log) { }

        public override void Compute()
        {
            // your component implementation
            Logger.Trace("Hello World");

            //Workspace.Store("outputName", 5);
        }
    }
}