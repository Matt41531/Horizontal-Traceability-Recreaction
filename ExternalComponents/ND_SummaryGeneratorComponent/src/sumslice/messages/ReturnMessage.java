package sumslice.messages;

/**
 * A message representing the return type of a method.  For example, a
 * sentence generated for a message of this type might be "Method fooBar
 * returns an int."
 *
 * @author Collin McMillan
 * @since 2013-03-12
 */
public class ReturnMessage extends Message
{
	private String returntype;

	public String getReturnType()
	{
		return returntype;
	}

	public void setReturnType(String returntype)
	{
		this.returntype = returntype;
	}
}

