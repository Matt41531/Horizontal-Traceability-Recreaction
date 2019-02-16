
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
        Name = "Notre Dame XML FileParserComponent",
        DefaultLabel = "ND XML File Parser Component",
        Description = "A component that receives the path to a file and returns a " +
        		"string with the contents of the file.",
        Author = "Notre Dame TraceLab Team",
        Version = "0.2.6",
        ConfigurationType = Config.class)

@IOSpecAttribute.Annotation.__Multiple({
	@IOSpecAttribute.Annotation(IOType = IOSpecType.__Enum.Input, Name = "inputDir", 
		DataType = String.class, Description = "path to source file"),
	@IOSpecAttribute.Annotation(IOType = IOSpecType.__Enum.Output, 
	Name = "xmlfile", 
	DataType = String.class, 
	Description = "string from srcml")
})

public class ND_XMLFileParser extends BaseComponent{
	
	private Config config;
	
	public ND_XMLFileParser(ComponentLogger log) {
		super(log);
		// TODO Auto-generated constructor stub
		config = new Config();
		super.set_Configuration(config);
		
	}
	
	@Override
	public void Compute() throws ComponentException {
		// TODO Auto-generated method stub
		
		String filePath = config.getDirPath();
		Parser parser = new Parser();
		try {
			
			String xmlfile = parser.readFile(filePath);
			super.get_Workspace().Store("xmlfile", xmlfile);
			

		} catch (FileNotFoundException e) {
			// TODO Auto-generated catch block
			super.get_Logger().Trace("ND_XMLFileParser: " + e);
		}
		
		
		
	}

}

	
