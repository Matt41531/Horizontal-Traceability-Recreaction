using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using TraceLabSDK.Component.Config;

namespace FReQuAT_compiled_component
{
    public class FReQuAT_configuration
    {
        [DisplayName("XML location")]
        [Description("Location of log file.xml")]
        public FilePath XML { get; set; }

        [DisplayName("Output Directory")]
        [Description("Directory for output files")]
        public DirectoryPath OutputDirectory { get; set; }

    }
}