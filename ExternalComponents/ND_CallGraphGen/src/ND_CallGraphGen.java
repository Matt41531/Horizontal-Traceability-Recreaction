import java.io.BufferedReader;
import java.io.FileNotFoundException;
import java.io.FileReader;
import java.io.IOException;
import java.io.InputStreamReader;
import java.util.ArrayList;
import java.util.HashMap;

import org.w3c.dom.Document;
import org.w3c.dom.NodeList;
import org.w3c.dom.Node;
import org.w3c.dom.Element;

import cli.TraceLabSDK.*;
import cli.TraceLabSDK.Types.TLArtifact;
import cli.TraceLabSDK.Types.TLArtifactsCollection;

@ComponentAttribute.Annotation(
        Name = "ND Call Graph Generator",
        DefaultLabel = "ND Call Graph Generator",
        Description = "This component reads srcml and generates a call graph.",
        Author = "Notre Dame TraceLab Team",
        Version = "0.1",
	ConfigurationType = Config.class)

@IOSpecAttribute.Annotation.__Multiple({
/*        @IOSpecAttribute.Annotation(IOType = IOSpecType.__Enum.Input,
                                    Name = "srcml_file",
                                    DataType = String.class,
                                    Description = "The name of the srcml file."),*/

	@IOSpecAttribute.Annotation(IOType = IOSpecType.__Enum.Output,
				    Name = "callgraph", 
				    DataType = HashMap.class,
				    Description = "Call Graph produced from srcML.")
})

public class ND_CallGraphGen extends BaseComponent
{
	private Config config;

	public ND_CallGraphGen (ComponentLogger log)
	{
		super(log);
		config = new Config();
		super.set_Configuration(config);
	}

	@Override
	public void Compute() throws ComponentException
	{
		try {
			SRCMLParser p = new SRCMLParser(config.getSrcmlFilename());
			super.get_Workspace().Store("callgraph", p.getCallGraph());

		} catch(Exception e){
			super.get_Logger().Trace("ND_CallGraphGen ERROR: " + e);
		}
	}

}

