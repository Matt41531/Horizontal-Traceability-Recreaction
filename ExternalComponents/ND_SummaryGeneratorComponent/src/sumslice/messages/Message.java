package sumslice.messages;

import simplenlg.framework.*;
import simplenlg.lexicon.*;
import simplenlg.realiser.english.*;
import simplenlg.phrasespec.*;
import simplenlg.features.*;

/**
 * A class to represent a "message."
 *
 * A message is a high-level representation of some information.  The
 * information will eventually be turned into phrase objects, and then
 * from phrases into English sentences.  For example, a message could
 * be the number of times a method is called.  The sentence resulting
 * from this message might be "Method fooBar called 19 times."
 * <p>
 * We have different types of messages for different types of data.
 * For example, a CallAmountMessage would inherit from this Message.
 *
 * @author Collin McMillan
 * @since 2013-03-12
 */
public class Message
{
	private int id;
	private String method;
	private boolean visible;
	private NLGElement phrase;

	public Message()
	{
		visible = true;
	}

        /**
         * Gets the method's ID.
         */
        public int getId()
        {
                return id;
        }

        public void setId(int id)
        {
                this.id = id;
        }

	/**
	 * Gets the method's name.
	 */
	public String getMethod()
	{
		return method;
	}

	public void setMethod(String method)
	{
		this.method = method;
	}

	/**
	 * Get the NLG phrase associated with this message.
	 */
	public NLGElement getPhrase()
	{
		return phrase;
	}

	public void setPhrase(NLGElement phrase)
	{
		this.phrase = phrase;
	}

        /**
         * Get whether the phrase should be realized or not.
         */
        public boolean isVisible()
        {
                return visible;
        }

        public void setVisible(boolean visible)
        {
                this.visible = visible;
        }
}

