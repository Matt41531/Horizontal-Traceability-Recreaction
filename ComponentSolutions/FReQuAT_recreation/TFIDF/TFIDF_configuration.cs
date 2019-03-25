using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
using System.ComponentModel;
//using TraceLabSDK.Component.Config;

namespace TFIDF
{
    // Enum to determine the method in which to calculate TFIDF
    public enum Method_type
    {
        TFIDF, // Needs renaming
        TitleVsDescription,
    }

    // All of the things in the configuration file
    public class TFIDF_configuration
    {
        [DisplayName("Method")]
        [Description("Set Method to 'TFIDF' or 'Title vs. Description'")]
        public Method_type Method { get; set; }

    }
}