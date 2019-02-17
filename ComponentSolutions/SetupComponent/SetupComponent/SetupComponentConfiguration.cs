using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using TraceLabSDK.Component.Config;

namespace ComponentSolutions
{
    public class SetupComponentConfiguration
    {
        [DisplayName("Artifacts location")]
        [Description("Location of artifacts file")]
        public FilePath Artifacts { get; set; }

        [DisplayName("Output Directory")]
        [Description("Directory for output files")]
        public DirectoryPath OutputDirectory { get; set; }

    }
}
