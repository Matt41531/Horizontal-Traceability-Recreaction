//@author: Rrezarta Krasniqi

import java.io.FileNotFoundException;

import cli.TraceLabSDK.BaseComponent;
import cli.TraceLabSDK.ComponentAttribute;
import cli.TraceLabSDK.ComponentException;
import cli.TraceLabSDK.ComponentLogger;
import cli.TraceLabSDK.IOSpecAttribute;
import cli.TraceLabSDK.IOSpecType;

@ComponentAttribute.Annotation(
		Name = "Notre Dame Logistic Regression File Parser Component",
		DefaultLabel = "ND LR File Parser Component",
		Description = "A component that receives the path to a file and returns a string with the content of the file",
		Author = "Notre Dame TraceLab Team",
		Version = "0.1",
		ConfigurationType = Config.class)

@IOSpecAttribute.Annotation.__Multiple({
	
	@IOSpecAttribute.Annotation(IOType = IOSpecType.__Enum.Input,
			Name = "modelKind", 
			DataType = String.class,
			Description = "The name of the input string"),
		
	@IOSpecAttribute.Annotation(IOType = IOSpecType.__Enum.Output,
		    Name = "lr_output_file", 
		    DataType = String.class,
		    Description = "String from srcml.")  
})


public class ND_LogisticRegressionFileParser extends BaseComponent 
{
	private Config config;
	String lr_output_file;

	public ND_LogisticRegressionFileParser(ComponentLogger log)
	{
		super(log);
		config = new Config();
		super.set_Configuration(config);
	}

	@Override
	public void Compute() throws ComponentException
	{
		String filePath = config.getDirPath();
		Parser parser = new Parser();
		
		String modelKind = (String) super.get_Workspace().Load("modelKind"); //"LR Model";
		//System.out.println("++modelKind: " + modelKind);
		try{
			if (modelKind != null && modelKind.equalsIgnoreCase("LR Model")){
				lr_output_file = parser.readFile(filePath);
				super.get_Workspace().Store("lr_output_file",lr_output_file);
			}

		} catch(FileNotFoundException e){
			super.get_Logger().Trace("ND_LogisticRegressionFileParser ERROR: " + e);
		}
	}
}



