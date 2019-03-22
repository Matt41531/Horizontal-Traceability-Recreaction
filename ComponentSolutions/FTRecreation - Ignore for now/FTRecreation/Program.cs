using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
// Located in c:\Program Files (x86)\COEST\TraceLab\lib\TraceLabSDK.dll
using TraceLabSDK;

namespace FTRecreation
{
    [Component(Name = "Program",
                Description = "The main entry point for the application.",
                Author = "Jen Lee",
                Version = "1.0")]
    //[IOSpec(IOType = IOSpecType.Output, Name = "outputName", DataType = typeof(int))]

    // ****** IS THIS NEEDED???? ********
    /*public class Program : BaseComponent
    {
        public Program(ComponentLogger log) : base(log) { }

        public override void Compute()
        {
            // your component implementation
            Logger.Trace("Hello World");

            //Workspace.Store("outputName", 5);
        }
    }
    */

    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }
}