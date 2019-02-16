import cli.TraceLabSDK.BaseComponent;
import cli.TraceLabSDK.ComponentAttribute;
import cli.TraceLabSDK.ComponentException;
import cli.TraceLabSDK.ComponentLogger;
import cli.TraceLabSDK.IOSpecAttribute;
import cli.TraceLabSDK.IOSpecType;


@ComponentAttribute.Annotation(
        Name = "Notre Dame Linear Regression Printer",
        DefaultLabel = "ND LR Printer Component",
        Description = "A component that receives a string from the workspace" +
        		" and prints the results using a defined separator.",
        Author = "Notre Dame TraceLab Team",
        Version = "0.1.1")

@IOSpecAttribute.Annotation.__Multiple({
	@IOSpecAttribute.Annotation(IOType = IOSpecType.__Enum.Input, Name = "lr_output_file", 
			DataType = String.class, Description = "String file name")
	
})

public class ND_LogisticRegressionPrinter extends BaseComponent{
	
	public ND_LogisticRegressionPrinter(ComponentLogger log) {
		super(log);
	}
	
	@Override
	public void Compute() throws ComponentException {
		
		String txtFile = (String) super.get_Workspace().Load("lr_output_file");
		if(null != txtFile){
			String[] lines = txtFile.split("\n");
		
			for(int i = 0; i < lines.length; i++){
			
				super.get_Logger().Trace(lines[i]);
				System.out.println(lines[i]);
			}
		}
	}
}

	