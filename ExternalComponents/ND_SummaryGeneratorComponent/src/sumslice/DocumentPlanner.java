package sumslice;

import sumslice.messages.*;
import sumslice.util.Xml;

import java.util.Set;
import java.util.HashSet;
import java.util.Vector;
import java.util.Iterator;

/**
 * @author Collin McMillan <cmc@nd.edu>
 * @since 2013-03-12
 */
public class DocumentPlanner
{
	private String configFilename;
	private Set<Integer> method_list;
	private Set<Message> messages;
	private Vector<Message> documentPlan;

	public DocumentPlanner(String configFilename)
	{
		this.configFilename = configFilename;
		method_list = new HashSet<Integer>();
		messages = new HashSet<Message>();
		documentPlan = new Vector<Message>();
	}

	/**
	 * Parses the input XML file to create a set of Message objects.
	 *
	 * This method acts as the "Content Determination" step of NLG.  It takes
	 * the basic facts from a config XML file and generates the high-level
	 * Message objects as a set.  At this point the messages are completely
	 * unordered.
	 *
	 * @return The number of messages generated.
	 */
	public int generateMessages()
	{
		Xml config = new Xml(configFilename, "method_list");

		int avg_called = 0;
		int avg_calls = 0;
		double avg_pagerank = 0;

		// start by reading the averages, to give us perspective
		for(Xml averages: config.children("averages"))
		{
			avg_called = Integer.parseInt(averages.child("called").content());
			avg_calls = Integer.parseInt(averages.child("calls").content());
			avg_pagerank = Double.parseDouble(averages.child("pagerank").content());
		}

		// now read in values from methods and create the messages
		for(Xml method: config.children("method"))
		{
			int mid = Integer.parseInt(method.child("id").content());
			String mname = method.child("name").content();

			// keep a list of all methods we are reading in
			if(!method_list.contains(mid))
			{
				method_list.add(mid);
			}

			// create a message about a method's return type
			if(method.optChild("returntype") != null)
			{
				String mreturntype = method.child("returntype").content();

				if(!mreturntype.equals("void"))
				{
					ReturnMessage rm = new ReturnMessage();
					rm.setId(mid);
					rm.setMethod(mname);
					rm.setReturnType(mreturntype);
					messages.add(rm);
				}
			}

			// we measure importance using pagerank
			if(method.optChild("pagerank") != null)
			{
				double pagerank = Double.parseDouble(method.child("pagerank").content());

				ImportanceMessage im = new ImportanceMessage();
				im.setId(mid);
				im.setMethod(mname);
				im.setPagerank(pagerank);
				im.setAvgPagerank(avg_pagerank);
				messages.add(im);
			}

			if(method.optChild("swum") != null)
			{
				Xml swum = method.optChild("swum");

				String verb = swum.child("verb").content();
				String object = swum.child("object").content();

				QuickSummaryMessage sm = new QuickSummaryMessage();
				sm.setId(mid);
				sm.setMethod(mname);
				sm.setVerb(verb);
				sm.setObject(object);
				messages.add(sm);
			}
			
			if(method.optChild("use") != null)
			{
				Xml use = method.optChild("use");

				String type = use.child("type").content();
				String example = use.child("example").content();

				UseMessage sm = new UseMessage();
				sm.setId(mid);
				sm.setMethod(mname);
				sm.setType(type);
				sm.setExample(example);
				messages.add(sm);
			}
		}

		// need another pass because we need to read all methods before
		// some messages can be created
		for(Xml method: config.children("method"))
		{
			int mid = Integer.parseInt(method.child("id").content());
			String mname = method.child("name").content();

			// message regarding how many times method is called
			Xml called = method.child("called");
			int calledcount = 0;
			Set callerset = new HashSet();

			for(Xml id: called.children("id"))
			{
				calledcount++;
				callerset.add(Integer.parseInt(id.content()));
			}

			int callerone = getCallerOne(callerset);
			int callertwo = getCallerTwo(callerset);

			CalledMessage cm = new CalledMessage();
			cm.setId(mid);
			cm.setMethod(mname);
			cm.setCalledCount(calledcount);
			cm.setCallerSet(callerset);
			cm.setCallerOne(callerone);
			cm.setCallerTwo(callertwo);
			messages.add(cm);

			// OUM for callertwo
			QuickSummaryMessage sm = getQuickSummaryMessage(callerone);

			if(sm != null)
			{
				OutputUsedMessage oum = new OutputUsedMessage();
				oum.setId(mid);
				oum.setMethod(mname);
				oum.setVP(sm.getVerb());
				oum.setNP(sm.getObject());
				messages.add(oum);
			}

			// ... and OUM for callertwo
			sm = getQuickSummaryMessage(callertwo);

                        if(sm != null)
                        {
                                OutputUsedMessage oum = new OutputUsedMessage();
                                oum.setId(mid);
                                oum.setMethod(mname);
                                oum.setVP(sm.getVerb());
                                oum.setNP(sm.getObject());
                                messages.add(oum);
                        }
		}

		return messages.size();
	}

	/**
	 * Returns the Quick Summary Message for a method with a given ID.
	 */
	private QuickSummaryMessage getQuickSummaryMessage(int id)
	{ // TODO clean this up as done with getMessage in DocumentPlanner
		for(Message m: messages)
		{
			if(m instanceof QuickSummaryMessage && m.getId() == id)
			{
				return (QuickSummaryMessage)m;
			}
		}

		return null;
	}

	/**
	 * Returns the most-important method in the set.
	 */
	private int getCallerOne(Set callerset)
	{
		double high_pr = 0;
		int high_id = 0;

		for(Message m: messages)
		{
			if(m instanceof ImportanceMessage)
			{
				ImportanceMessage im = (ImportanceMessage)m;
				if(callerset.contains(im.getId()))
				{
					if(high_pr < im.getPagerank())
					{
						high_pr = im.getPagerank();
						high_id = im.getId();
					}
				}
			}
		}

		return high_id;
	}

	/**
         * Returns the second-most-important method in the set.
         */
	private int getCallerTwo(Set callerset)
	{
		int callerone = getCallerOne(callerset);

                double high_pr = 0;
                int high_id = 0;

                for(Message m: messages)
                {
                        if(m instanceof ImportanceMessage)
                        {       
                                ImportanceMessage im = (ImportanceMessage)m;
                                if(callerset.contains(im.getId()))
                                {
                                        if(high_pr < im.getPagerank() & im.getId() != callerone)
                                        {
                                                high_pr = im.getPagerank();
                                                high_id = im.getId();
                                        }
                                }
                        }
                }
        
                return high_id;
	}

	/**
	 * Returns the set of messages created by generateMessages().
	 *
	 * @return A java.util.Set object of all messages.
	 */
	public Set<Message> getMessages()
	{
		return messages;
	}

	/**
	 * Reads the messages from the set, orders them, and puts them into a vector.
	 *
	 * This method acts as the "Document Structuring" step of NLG.  It creates a
	 * "document plan."  A document plan is an ordered list (i.e., a vector) of
	 * messages.
	 */
	public void createDocumentPlan()
	{
		for(Integer id: method_list)
		{
			Message sm = getMessage(id, QuickSummaryMessage.class);
			Message rm = getMessage(id, ReturnMessage.class);
			Vector<Message> om = getMessages(id, OutputUsedMessage.class);
			Message cm = getMessage(id, CalledMessage.class);
			Message im = getMessage(id, ImportanceMessage.class);
			Message um = getMessage(id, UseMessage.class);

			if(sm != null) documentPlan.add(sm);
			if(rm != null) documentPlan.add(rm);
			if(om != null) documentPlan.addAll(om);
			if(cm != null) documentPlan.add(cm);
			if(im != null) documentPlan.add(im);
			if(um != null) documentPlan.add(um);
		}
	}

        /**
         * Gets all Messages of a given type for a method with a given ID.
         */
        private Vector<Message> getMessages(int id, Class type)
        {
		Vector<Message> vec = new Vector<Message>();

                for(Message message: messages)
                {
                        if(id == message.getId() && type.isInstance(message))
                        {
                                vec.add(message);
                        }
                }

                return vec;
        }

	/**
	 * Gets one Message of a given type for a method with a given ID.
	 */
	private Message getMessage(int id, Class type)
	{
		for(Message message: messages)
		{
			if(id == message.getId() && type.isInstance(message))
			{
				return message;
			}
		}

		return null;
	}

	/**
	 * Returns the document plan.
	 *
	 * @return A java.util.Vector object of all messages in order.
	 */
	public Vector<Message> getDocumentPlan()
	{
		return documentPlan;
	}
}

