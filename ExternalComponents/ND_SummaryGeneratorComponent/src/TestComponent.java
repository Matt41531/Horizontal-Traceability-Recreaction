import java.io.File;
import java.io.FileNotFoundException;
import java.io.FileOutputStream;
import java.io.PrintStream;
import java.util.Iterator;
import java.util.Set;
import java.util.Vector;

import sumslice.DocumentPlanner;
import sumslice.MicroPlanner;
import sumslice.messages.Message;


public class TestComponent {

	/**
	 * @param args
	 */
	public static void main(String[] args) {
			String outputFilePath = "test.txt";
			String configFilename = "example/example.xml";
			String lexiconFilename = "conf/default-lexicon.xml";
			
			File outputFile = new File(outputFilePath);
			FileOutputStream fis;
			PrintStream out = null;
			//set output stream to write to file
			try {
				fis = new FileOutputStream(outputFile);
				out = new PrintStream(fis);
				//System.setOut(out);
			} catch (FileNotFoundException e) {
				// TODO Auto-generated catch block
				e.printStackTrace();
			}
			
			String mname = "none";

			//String configFilename = inputFilePath;
			DocumentPlanner dp = new DocumentPlanner(configFilename);

			int numMessages = dp.generateMessages();
			Set<Message> s = dp.getMessages();
			dp.createDocumentPlan();
			Vector<Message> documentPlan = dp.getDocumentPlan();

			Iterator it = s.iterator();

			while(it.hasNext())
			{
				Message m = (Message)it.next();
				mname = m.getMethod();
			}

			System.out.println("Messages:\t" + numMessages);
			System.out.println("Method Name:\t" + mname);

			it = documentPlan.iterator();

			while(it.hasNext())
			{
				Message m = (Message)it.next();
				String messageType = m.getClass().getSimpleName();
				System.out.print("-> " + messageType);
			}

			

			MicroPlanner mp = new MicroPlanner(lexiconFilename);

			mp.lexicalize(documentPlan);
			mp.aggregate();

			String summary = mp.realizeAll();
	        out.close();//clean up
	}

}
