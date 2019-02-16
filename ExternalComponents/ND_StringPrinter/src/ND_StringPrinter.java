
import java.io.File;
import java.io.FileNotFoundException;
import java.io.PrintWriter;
import java.io.UnsupportedEncodingException;
import java.util.ArrayList;
import java.util.List;

import cli.TraceLabSDK.BaseComponent;
import cli.TraceLabSDK.ComponentAttribute;
import cli.TraceLabSDK.ComponentException;
import cli.TraceLabSDK.ComponentLogger;
import cli.TraceLabSDK.IOSpecAttribute;
import cli.TraceLabSDK.IOSpecType;


@ComponentAttribute.Annotation(
        Name = "Notre Dame String Printer",
        DefaultLabel = "ND String Printer Component",
        Description = "A component that receives a string from the worspace" +
        		"and prints the results using a defined separator.",
        Author = "Notre Dame TraceLab Team",
        Version = "0.1.1")

@IOSpecAttribute.Annotation.__Multiple({
	@IOSpecAttribute.Annotation(IOType = IOSpecType.__Enum.Input, Name = "xmlfile", 
			DataType = String.class, Description = "String file name")
	
})

public class ND_StringPrinter extends BaseComponent{
	
	public ND_StringPrinter(ComponentLogger log) {
		super(log);
		// TODO Auto-generated constructor stub
	}
	
	@Override
	public void Compute() throws ComponentException {
		// TODO Auto-generated method stub
		
		String xmlfile = (String) super.get_Workspace().Load("xmlfile");
		String[] lines = xmlfile.split("\n");
		
		
		for(int i = 0; i < lines.length; i++){
			
			super.get_Logger().Trace("printing lines: " + lines[i]);
			System.out.println("printing lines: " + lines[i]);
			
		}
	
	}

}

	
