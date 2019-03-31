//@author: Rrezarta Krasniqi

import java.io.FileNotFoundException;

import cli.TraceLabSDK.BaseComponent;
import cli.TraceLabSDK.ComponentAttribute;
import cli.TraceLabSDK.ComponentException;
import cli.TraceLabSDK.ComponentLogger;
import cli.TraceLabSDK.IOSpecAttribute;
import cli.TraceLabSDK.IOSpecType;

@ComponentAttribute.Annotation(
		Name = "Notre Dame Naive Bayes File Parser Component",
		DefaultLabel = "ND NB File Parser Component",
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
		    Name = "nb_output_file", 
		    DataType = String.class,
		    Description = "The name of output string.")  
})


public class ND_NaiveBayesFileParser extends BaseComponent 
{
	private Config config;
	String nb_output_file;


	public ND_NaiveBayesFileParser(ComponentLogger log)
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
		
		String modelKind = (String) super.get_Workspace().Load("modelKind"); //"NB Model";
		
		try{
			if (modelKind != null && modelKind.equalsIgnoreCase("NB Model")){
				nb_output_file = parser.readFile(filePath);
				super.get_Workspace().Store("nb_output_file", nb_output_file);
			}
		} catch(Exception e){
			super.get_Logger().Trace("ND_NaiveBayesFileParser ERROR: " + e);
		}
	}
}