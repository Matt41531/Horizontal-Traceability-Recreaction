package sumslice;

import sumslice.messages.*;

//import simplenlg.*;

import simplenlg.framework.*;
import simplenlg.realiser.english.*;
import simplenlg.phrasespec.*;
import simplenlg.features.*;
import simplenlg.lexicon.*;

import java.util.Vector;
import java.util.Iterator;
import java.util.HashMap;

/**
 * @author Collin McMillan <cmc@nd.edu>
 * @since 2013-03-12
 */
public class MicroPlanner
{
	private NLGFactory nlgFactory;
	private HashMap raw_paragraphs;
	private HashMap agg_paragraphs;
	private Realiser realizer;

	public MicroPlanner(String lexiconFilename)
	{
		raw_paragraphs = new HashMap();
		agg_paragraphs = new HashMap();

		Lexicon lexicon = new XMLLexicon(lexiconFilename);
		nlgFactory = new NLGFactory(lexicon);
		realizer = new Realiser(lexicon);
	}

	/**
	 * Converts the messages into phrases.
	 *
	 * This method acts as the "Lexicalization" step in NLG.  It selects
	 * the words that will be used to describe the messages.  The phrases
	 * are stored with each Message object and kept in paragraphs.  Each
	 * paragraph has all the messages for one method.
	 *
	 * @param documentPlan The vector of messages (usually produced by the DocumentPlanner).
	 * @return The number of phrases added
	 */
	public int lexicalize(Vector<Message> documentPlan)
	{
		int numPhrases = 0;

		for(Message message: documentPlan)
		{
			int id = message.getId();
			Vector<Message> phrases = (Vector)raw_paragraphs.get(id);
			if(phrases == null)
				phrases = new Vector<Message>();

			SPhraseSpec phrase = null;

			if(message instanceof ReturnMessage)
				phrase = handleMessage((ReturnMessage)message);
			else if(message instanceof ImportanceMessage)
				phrase = handleMessage((ImportanceMessage)message);
			else if(message instanceof CalledMessage)
				phrase = handleMessage((CalledMessage)message);
			else if(message instanceof QuickSummaryMessage)
				phrase = handleMessage((QuickSummaryMessage)message);
			else if(message instanceof OutputUsedMessage)
				phrase = handleMessage((OutputUsedMessage)message);
			else if(message instanceof UseMessage)
				phrase = handleMessage((UseMessage)message);

			if(phrase != null)
			{
				message.setPhrase(phrase);
				phrases.add(message);
			}

			raw_paragraphs.put(id, phrases);

			numPhrases++;
		}

		return numPhrases;
	}

	/**
	 * Groups the phrases in each paragraph into sentences.
	 *
	 * This methods acts as the "Aggregator" step in NLG.  It smooths the sentences
	 * so that they flow more like human-written English.
	 * <p>
	 * In practice, this method also implements the "Referring Expression Generation"
	 * as well as aggregation.  REG needs all of the same data as aggregation
	 * (namely, the Message type and lexicalized phrases), so we decided to combine
	 * them here.
	 */
	public void aggregate()
	{
		for(Object po: raw_paragraphs.values())
		{	// there has to be a better way to read the paragraphs...
			Vector<Message> messages = (Vector<Message>)po;
			Vector<NLGElement> newparagraph = new Vector<NLGElement>();

			// look for patterns in the messages and aggregate the phrases
			// for those patterns
			for(int i = 0; i < messages.size(); i++)
			{
				Message lastMessage = null;
				Message message = (Message)messages.get(i);
				Message nextMessage = null;

				// don't bother with messages which we won't display
				if(!message.isVisible())
					continue;

				if(i > 0) // if there is a last message
					lastMessage = (Message)messages.get(i-1);
				if(i < messages.size()-1) // if there is a next message
					nextMessage = (Message)messages.get(i+1);

				int id = message.getId();

				// example: "This method processes xml.  This method
				// returns an XML element." becomes "This method
				// processes xml and returns an XML element."
				if(message instanceof QuickSummaryMessage
				   && nextMessage instanceof ReturnMessage)
				{
					SPhraseSpec p1 = (SPhraseSpec)message.getPhrase();
					SPhraseSpec p2 = (SPhraseSpec)nextMessage.getPhrase();
					NLGElement newphrase = sharedParticipantConjunct(p1, p2, "and");
					newparagraph.add(newphrase);
					message.setVisible(false);     // we replaced these with newphrase
					nextMessage.setVisible(false); // so don't show them anymore

				// example: "Return data is used by a method that
				// parses xml.  Return data is used by a method that
				// processes xml elements." becomes "That [return
				// type] is used by methods that parse xml and 
				// process xml elements."
				} 
				
				
				else if(lastMessage instanceof ReturnMessage
					  && message instanceof OutputUsedMessage
					  && nextMessage instanceof OutputUsedMessage)
				{
					String mreturntype = ((ReturnMessage)lastMessage).getReturnType();
					SPhraseSpec p1 = (SPhraseSpec)message.getPhrase();
                                        SPhraseSpec p2 = (SPhraseSpec)nextMessage.getPhrase();
					NLGElement newphrase = joinOutputUsedPhrases((OutputUsedMessage)message,
									(OutputUsedMessage)nextMessage,
									mreturntype);
					newparagraph.add(newphrase);
					message.setVisible(false);
					nextMessage.setVisible(false);

				// example: "Return data is used by a method that
				// skips whitespace." becomes "That [return type]
				// is used by a method that skips whitespace."
				} else if(lastMessage instanceof ReturnMessage
                                          && message instanceof OutputUsedMessage
                                          && !(nextMessage instanceof OutputUsedMessage))
				{
					String mreturntype = ((ReturnMessage)lastMessage).getReturnType();
					SPhraseSpec newphrase = (SPhraseSpec)message.getPhrase();
					newphrase.setObject("that " + mreturntype);
					newparagraph.add(newphrase);
                                        message.setVisible(false);

				// example: "Read is called by 2 methods.  Read is
				// far more important than average." becomes
				// "Read seems far more important than average
				// because it is called by 2 methods."
				} else if(message instanceof CalledMessage
					  && nextMessage instanceof ImportanceMessage)
				{
					NLGElement newphrase = joinCallImportPhrases(
									(CalledMessage)message,
									(ImportanceMessage)nextMessage);
                                        newparagraph.add(newphrase);
                                        message.setVisible(false);
					nextMessage.setVisible(false);

				// if no aggregation to do, just add the phrase as is
				} else
				{
					if(message.isVisible())
						newparagraph.add(message.getPhrase());
				}

				agg_paragraphs.put(id, newparagraph);
			}
		}
	}

	/**
	 * Aggregate a called phrase and an importance phrase.
	 */
	public NLGElement joinCallImportPhrases(CalledMessage cm, ImportanceMessage im)
	{
		NPPhraseSpec obj = nlgFactory.createNounPhrase();
		obj.setNoun(((SPhraseSpec)im.getPhrase()).getObject());
		obj.addComplement("than average");

		SPhraseSpec phrase = nlgFactory.createClause();
		phrase.setSubject(cm.getMethod() + "()");
		phrase.setVerb("seem");
		phrase.setObject(obj);

		SPhraseSpec tmp = (SPhraseSpec)cm.getPhrase();
		tmp.setFeature(Feature.COMPLEMENTISER, "because");
		tmp.setObject("it");

		phrase.addComplement(tmp);

		return phrase;
	}

	/**
	 * Aggregate two output used phrases.
	 */
	public NLGElement joinOutputUsedPhrases(OutputUsedMessage oum1, OutputUsedMessage oum2, String mreturntype)
	{
		SPhraseSpec phrase = nlgFactory.createClause();
		phrase.setObject("That " + mreturntype);
		phrase.setVerb("use");
		phrase.setFeature(Feature.PASSIVE, true);

		NPPhraseSpec subj = nlgFactory.createNounPhrase();
                subj.setNoun("method");
                subj.setPlural(true);

		SPhraseSpec tmp1 = nlgFactory.createClause();
		tmp1.setVerb(oum1.getVP());
		tmp1.setObject("the " + oum1.getNP());
		//tmp1.setFeature(Feature.NUMBER, NumberAgreement.PLURAL);

		SPhraseSpec tmp2 = nlgFactory.createClause();
                tmp2.setVerb(oum2.getVP());
                tmp2.setObject("the " + oum2.getNP());
                //tmp2.setFeature(Feature.NUMBER, NumberAgreement.PLURAL);

		CoordinatedPhraseElement c = nlgFactory.createCoordinatedPhrase();
		c.addCoordinate(tmp1);
		c.addCoordinate(tmp2);
		c.setConjunction("and");

		subj.addComplement(c);

		phrase.setSubject(subj);

		return phrase;
	}

	/**
	 * Aggregate two phrases using the Shared Participant Conjunction aggregation.
	 *
	 * Described by Reiter and Dale, Section 5.3, Page 136.
	 */
	public NLGElement sharedParticipantConjunct(SPhraseSpec p1, SPhraseSpec p2, String conj)
	{
		// create a new phrase from p2 that does not contain the subject
		SPhraseSpec tmp = nlgFactory.createClause();
		tmp.setVerb(p2.getVerb());
		tmp.setObject(p2.getObject());

		CoordinatedPhraseElement c = nlgFactory.createCoordinatedPhrase();
		c.addCoordinate(p1);
		c.addCoordinate(tmp);
		c.setConjunction(conj);
		return c;
	}

	/**
	 * Converts all phrases in the vector of phrases into English sentences.
	 *
	 * This method acts as the "Realizer" step in NLG.  It would normally be called
	 * after "Aggregation" and "Referring Expression Generation."
	 *
	 * @return A String of all the sentences.
	 */
	public String realizeAll()
	{
		String summary = "";
		

		for(Object po: agg_paragraphs.values())
		{
			Vector<NLGElement> phrases = (Vector<NLGElement>)po;
			for(NLGElement phrase: phrases)
			{
				if(phrase != null)
					summary = summary + (realize(phrase) + " ");
			}
			summary = summary + ("\n\n");
		}

		return summary;
	}

	/**
	 * A private method to handle calling the realization library on one phrase.
	 *
	 * @return A String of one sentance for one phrase.
	 */
	private String realize(NLGElement phrase)
	{	
		return realizer.realiseSentence(phrase);
	}

	/* ************ Message Handlers ************ */
	/* These methods create a phrase for a particular type of message. */
	/* This is where the bulk of lexicalization happens. */

	private SPhraseSpec handleMessage(ReturnMessage message)
	{
		String methodname = message.getMethod();
		String returntype = message.getReturnType();

		SPhraseSpec phrase = nlgFactory.createClause();
		phrase.setSubject(methodname);
		phrase.setVerb("return");

		NPPhraseSpec obj = nlgFactory.createNounPhrase();
		obj.setNoun(returntype);
		obj.setSpecifier("a");
		phrase.setObject(obj);

		return phrase;
	}

	private SPhraseSpec handleMessage(ImportanceMessage message)
	{
		String methodname = message.getMethod();

		SPhraseSpec phrase = nlgFactory.createClause();
		phrase.setSubject(methodname);
		phrase.setVerb("is");
		phrase.addComplement("than average");

		String adjective;

		if(message.getPagerank() > 1.50 * message.getAvgPagerank())
			adjective = "far more";
		else if(message.getPagerank() > message.getAvgPagerank())
			adjective = "slightly more";
		else
		{
			phrase.setVerb("seems");
			adjective = "less";
		}

		NPPhraseSpec obj = nlgFactory.createNounPhrase();
		obj.setNoun("important");
		obj.addPreModifier(adjective);
		phrase.setObject(obj);

		return phrase;
	}

	private SPhraseSpec handleMessage(CalledMessage message)
	{
		String methodname = message.getMethod();

		SPhraseSpec phrase = nlgFactory.createClause();
		phrase.setObject(methodname);
		phrase.setVerb("call");
		phrase.setFeature(Feature.PASSIVE, true);

		int calledcount = message.getCalledCount();
		NPPhraseSpec subj = nlgFactory.createNounPhrase();
		subj.setNoun("method");
		subj.setPlural(true);

		if(calledcount == 0)
		{
			phrase.setNegated(true);
			subj.addPreModifier("any");
		}
		else if(calledcount == 1)
		{
			subj.addPreModifier("1");
			subj.setPlural(false);
		}
		else
		{
			subj.addPreModifier("" + calledcount);
		}

		phrase.setSubject(subj);

		return phrase;
	}

	private SPhraseSpec handleMessage(QuickSummaryMessage message)
	{
		String verb = message.getVerb();
		String object = message.getObject();

		SPhraseSpec phrase = nlgFactory.createClause();
		phrase.setSubject("this method");
		phrase.setVerb(verb);
		if (verb=="get"){
			phrase.setObject("the " + object);
		}
		else{
			phrase.setObject("the " + object);
		}//phrase.setFeature(Feature.NUMBER, NumberAgreement.PLURAL);

		return phrase;
	}

	private SPhraseSpec handleMessage(OutputUsedMessage message)
	{
		String VP = message.getVP();
		String NP = message.getNP();

		SPhraseSpec phrase = nlgFactory.createClause();
		phrase.setObject("Return data"); // modified in aggregate()
		phrase.setVerb("use");
		phrase.setFeature(Feature.PASSIVE, true);

		//phrase.setSubject("a method");
		NPPhraseSpec subj = nlgFactory.createNounPhrase();
		subj.setNoun("method");
		subj.setSpecifier("a");
		
		

		SPhraseSpec quicksummary = nlgFactory.createClause();
		quicksummary.setVerb(VP);
		quicksummary.setObject("the " + NP);
		quicksummary.setFeature(Feature.COMPLEMENTISER, "that");
		//quicksummary.setFeature(Feature.NUMBER, NumberAgreement.PLURAL);
		//quicksummary.setFeature(Feature.PASSIVE, false);
		subj.addComplement(quicksummary);

		phrase.setSubject(subj);
		if(VP.equals("null") || NP.equals("null")){
		return null;
		}
		else{
			return phrase;
		}
	}
	
	private SPhraseSpec handleMessage(UseMessage message)
	{
		SPhraseSpec phrase = nlgFactory.createClause();
		
		
		phrase.setSubject("The method can be");
		phrase.setVerb("use");
		phrase.setFeature(Feature.TENSE, Tense.PAST);
		NPPhraseSpec prepNounPhrase = nlgFactory.createNounPhrase("a", "statement");
		prepNounPhrase.addPreModifier(message.getType());
		PPPhraseSpec prepPhrase = nlgFactory.createPrepositionPhrase();
		prepPhrase.addComplement(prepNounPhrase);
		prepPhrase.setPreposition("in");
		phrase.addComplement(prepPhrase);
		//SPhraseSpec examplePhrase = nlgFactory.createClause("For example: ", message.getExample());
		phrase.addPostModifier("; for example: \"" + message.getExample() + "\" ");
		
		//quicksummary.setFeature(Feature.NUMBER, NumberAgreement.PLURAL);
		//quicksummary.setFeature(Feature.PASSIVE, false);
		
		if (message.getExample().equals("unknown")){
			return null;
		}
		else{
			return phrase;
		}
	}
}

