package sumslice.tests;

import sumslice.DocumentPlanner;
import sumslice.MicroPlanner;
import sumslice.messages.*;
import simplenlg.framework.*;
import simplenlg.realiser.english.*;
import simplenlg.phrasespec.*;
import simplenlg.features.*;
import simplenlg.lexicon.*;

import java.util.Set;
import java.util.Vector;
import java.util.Iterator;

public class FullTest
{
	public static void main(String args[])
	{
		String mname = "none";

		String configFilename = "conf/nanoXML-example.xml";
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

		System.out.println();

		String lexiconFilename = "conf/default-lexicon.xml";
		MicroPlanner mp = new MicroPlanner(lexiconFilename);

		mp.lexicalize(documentPlan);
		mp.aggregate();

		String summary = mp.realizeAll();
		System.out.println(summary);
	}
}

