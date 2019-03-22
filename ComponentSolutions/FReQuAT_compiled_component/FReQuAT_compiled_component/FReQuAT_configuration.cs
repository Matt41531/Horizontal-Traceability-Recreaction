using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using TraceLabSDK.Component.Config;

namespace FReQuAT_compiled_component
{
    // Enum to set boolean values to On or Off
    public enum Boolean_type
    {
        On,
        Off
    }

    public enum Method_type
    {
        TFIDF,
        TitleVsDescription, 
        LSA
    }

    // All of the things in the configuration file
    public class FReQuAT_configuration
    {
        [DisplayName("XML location")]
        [Description("Location of log file.xml")]
        public FilePath XML { get; set; }

        [DisplayName("Output Directory")]
        [Description("Directory for output files")]
        public DirectoryPath OutputDirectory { get; set; }

        [DisplayName("AC: All Comments")]
        [Description("Set AC: All Comments format as 'True', or 'False'")]
        public Boolean_type AC { get; set; }

        [DisplayName("SC: Source code removal")]
        [Description("Set SC: Cource code removal format as 'True', or 'False'")]
        public Boolean_type SC { get; set; }

        [DisplayName("SM: Stremming")]
        [Description("Set SM: Stremming format as 'True', or 'False'")]
        public Boolean_type SM { get; set; }

        [DisplayName("SR: Stop word removal")]
        [Description("Set SR: Stop word removal format as 'True', or 'False'")]
        public Boolean_type SR { get; set; }

        [DisplayName("DW: Double weight title")]
        [Description("Set DW: Double weight title format as 'True', or 'False'")]
        public Boolean_type DW { get; set; }

        [DisplayName("BG: Bi-gram")]
        [Description("Set BG: Bi-gram format as 'True', or 'False'")]
        public Boolean_type BG { get; set; }

        [DisplayName("SY: Remove synonyms, spelling mistakes")]
        [Description("Set SY: Remove synonyms, spelling mistakes format as 'True', or 'False'")]
        public Boolean_type SY { get; set; }

        [DisplayName("LO: toLower")]
        [Description("Set LO: toLoweer format as 'True', or 'False'")]
        public Boolean_type LO { get; set; }

        [DisplayName("MU: Remove words with count 1")]
        [Description("Set MU: Remove words with count 1 as 'True', or 'False'")]
        public Boolean_type MU { get; set; }

        [DisplayName("DO: Remove words with only 1 document")]
        [Description("Set DO: Remove words with only 1 document format as 'True', or 'False'")]
        public Boolean_type DO { get; set; }

        [DisplayName("Method")]
        [Description("Set Method to 'TFIDF', 'Title vs. Description', or 'LSA'")]
        public Method_type Method { get; set; }
    }
}