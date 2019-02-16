import java.io.BufferedReader;
import java.io.BufferedWriter;
import java.io.File;
import java.io.FileNotFoundException;
import java.io.FileOutputStream;
import java.io.FileReader;
import java.io.IOException;
import java.io.InputStreamReader;
import java.io.OutputStreamWriter;
import java.io.PrintStream;
import java.util.ArrayList;
import java.util.Iterator;
import java.util.Set;
import java.util.Vector;

import sumslice.DocumentPlanner;
import sumslice.MicroPlanner;
import sumslice.messages.Message;

import cli.TraceLabSDK.*;
import cli.TraceLabSDK.Types.TLArtifact;
import cli.TraceLabSDK.Types.TLArtifactsCollection;

@ComponentAttribute.Annotation(
        Name = "ND Summary Generator Component",
        DefaultLabel = "ND Summary Generator Component",
        Description = "Generates summaries for sumslice given a correctly formatted XML file. See Readme",
        Author = "Notre Dame TraceLab Team",
        Version = "0.5",
ConfigurationType = Config.class)

public class ND_SummaryGeneratorComponent extends BaseComponent{
	
	public ND_SummaryGeneratorComponent(ComponentLogger log){
		super(log);
		config = new Config();
		super.set_Configuration(config);
	}
	private Config config;
	
	@Override
	public void Compute(){
		//super.get_Logger().Trace("loading inputs"); //debug message
		String outputFilePath = config.getOutputFilePath();
		String configFilename = "/tmp/nd_xmlsumgen.xml";
		String lexiconFilename = config.getLexiconFilePath();
		//super.get_Logger().Trace("inputs loaded");//debug message
		File outputFile = new File(outputFilePath);
		FileOutputStream fis;
		PrintStream out = null;
		//set output stream to write to file
		try {
			fis = new FileOutputStream(outputFile);
			out = new PrintStream(fis);
		} catch (FileNotFoundException e) {
			// TODO Auto-generated catch block
			e.printStackTrace();
		}
		//super.get_Logger().Trace("Output stream set");//debug message
		String mname = "none";

		//String configFilename = inputFilePath;
		DocumentPlanner dp = new DocumentPlanner(configFilename);
		int numMessages = dp.generateMessages();

		//super.get_Logger().Trace("dp.generateMessages");////debug message
		Set<Message> s = dp.getMessages();
		dp.createDocumentPlan();
		Vector<Message> documentPlan = dp.getDocumentPlan();
		//super.get_Logger().Trace("GotDocumentPlan");//debug message
		Iterator it = s.iterator();
		//super.get_Logger().Trace("Iterating through messages");//debug message
		while(it.hasNext())
		{
			Message m = (Message)it.next();
			mname = m.getMethod();
		}
		//super.get_Logger().Trace("Iterated through messages");////debug message
		out.println("Messages:\t" + numMessages);
		out.println("Method Name:\t" + mname);

		it = documentPlan.iterator();

		while(it.hasNext())
		{
			Message m = (Message)it.next();
			String messageType = m.getClass().getSimpleName();
			out.print("-> " + messageType);
		}

		out.println();

		MicroPlanner mp = new MicroPlanner(lexiconFilename);

		mp.lexicalize(documentPlan);
		mp.aggregate();

		String summary = mp.realizeAll();
		out.println(summary);
        out.close();//clean up
	}

}
