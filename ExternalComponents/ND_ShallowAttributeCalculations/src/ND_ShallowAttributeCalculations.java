
import cli.TraceLabSDK.BaseComponent;
import cli.TraceLabSDK.ComponentAttribute;
import cli.TraceLabSDK.ComponentException;
import cli.TraceLabSDK.ComponentLogger;
import cli.TraceLabSDK.IOSpecAttribute;
import cli.TraceLabSDK.IOSpecType;

import java.io.IOException;
import org.apache.commons.exec.CommandLine;
import org.apache.commons.exec.DefaultExecutor;
import org.apache.commons.exec.ExecuteException;

@ComponentAttribute.Annotation(
		Name = "Notre Dame Shallow Attribute Calculations Component",
		DefaultLabel = "ND Shallow Attribute Calculations Component",
		Description = "This component calls a script to calculate three features: slen " +
				"(the number of words in the message normalized over all previous messages), " +
				"wc (the number of words not normalized) and ppau (the number of seconds between" +
				"the message and the previous message)",
		Author = "Notre Dame TraceLab Team",
		Version = "0.1",
		ConfigurationType = Config.class)

@IOSpecAttribute.Annotation.__Multiple({

	@IOSpecAttribute.Annotation(IOType = IOSpecType.__Enum.Output,
			Name = "dirPath", 
			DataType = String.class,
			Description = "Output")
})


public class ND_ShallowAttributeCalculations extends BaseComponent 
{
	private Config config;
	int exitValue;
	String dirPath;

	public ND_ShallowAttributeCalculations(ComponentLogger log)
	{
		super(log);
		config = new Config();
		super.set_Configuration(config);
	}

	@Override
	public void Compute() throws ComponentException
	{
		String dirPath = (String)config.getDirPath();
	    super.get_Workspace().Store("dirPath", "sh -x "+dirPath);

		try{
			runScript(dirPath);
		} catch(Exception e){
			super.get_Logger().Trace("ND_Shallow AttributeCalculations Component ERROR: " + e);
		}
	}

	public void runScript(String dirPath) throws Exception{
		this.dirPath = dirPath;

		CommandLine commandLine = CommandLine.parse(dirPath);
		DefaultExecutor exec = new DefaultExecutor();
		exec.setExitValue(0);
		try {
			exitValue = exec.execute(commandLine);
			//super.get_Logger().Trace(commandLine.toString().toUpperCase()+" "+"Completed");
		} catch (ExecuteException e) {
			System.err.println("Execution failed.");
			e.printStackTrace();
		} catch (IOException e) {
			System.err.println("Permission denied.");
			e.printStackTrace();
		}
	}
	
	public static void main(String args[])
	{
		ND_ShallowAttributeCalculations calculateShallowAttributes = new ND_ShallowAttributeCalculations(null);
		try {
			calculateShallowAttributes.Compute();
		} catch (ComponentException e) {
			e.printStackTrace();
		} catch (Exception e) {
			e.printStackTrace();
		}
	}
}



