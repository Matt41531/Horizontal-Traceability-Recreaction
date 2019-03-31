package sumslice.messages;

/**
 * @author Collin McMillan
 * @since 2013-03-12
 */
public class QuickSummaryMessage extends Message
{
	private String object;
	private String verb;

        public String getObject()
        {
                return object;
        }

        public void setObject(String object)
        {
                this.object = object;
        }

	public String getVerb()
	{
		return verb;
	}

	public void setVerb(String verb)
	{
		this.verb = verb;
	}
}

