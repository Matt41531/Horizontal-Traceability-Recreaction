package sumslice.messages;

/**
 * @author Collin McMillan
 * @since 2013-03-12
 */
public class OutputUsedMessage extends Message
{
	private String VP;
	private String NP;

        public String getVP()
        {
                return VP;
        }

        public void setVP(String VP)
        {
                this.VP = VP;
        }

	public String getNP()
	{
		return NP;
	}

	public void setNP(String NP)
	{
		this.NP = NP;
	}
}

