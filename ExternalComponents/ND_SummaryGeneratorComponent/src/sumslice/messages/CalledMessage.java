package sumslice.messages;

import java.util.Set;
import java.util.HashSet;

/**
 * @author Collin McMillan
 * @since 2013-03-13
 */
public class CalledMessage extends Message
{
	private int called;
	private Set callerset;
	private int cmethod_1;
	private int cmethod_2;

	public CalledMessage()
	{
		super();

		callerset = new HashSet();
		cmethod_1 = 0;
		cmethod_2 = 0;
	}

        /**
         * Gets the number of times the method is called.
         */
        public int getCalledCount()
        {
                return called;
        }

        public void setCalledCount(int called)
        {
                this.called = called;
        }

        /**
         * Gets the set of methods that call this method.
         */
        public Set getCallerSet()
        {
                return callerset;
        }

        public void setCallerSet(Set callerset)
        {
                this.callerset = callerset;
        }

	/**
	 * Gets the ID of the most-important caller.
	 */
	public int getCallerOne()
	{
		return cmethod_1;
	}

	public void setCallerOne(int cmethod_1)
	{
		this.cmethod_1 = cmethod_1;
	}

        /**
         * Gets the ID of the second-most-important caller.
         */
        public int getCallerTwo()
        {
                return cmethod_2;
        }

        public void setCallerTwo(int cmethod_2)
        {
                this.cmethod_2 = cmethod_2;
        }
}

