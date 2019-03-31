import java.io.BufferedReader;
import java.io.FileNotFoundException;
import java.io.FileReader;
import java.io.IOException;
import java.io.InputStreamReader;
import java.util.ArrayList;
import java.util.HashMap;
import java.util.List;

import org.w3c.dom.Document;
import org.w3c.dom.NodeList;
import org.w3c.dom.Node;
import org.w3c.dom.Element;

import cli.TraceLabSDK.*;
import cli.TraceLabSDK.Types.TLArtifact;
import cli.TraceLabSDK.Types.TLArtifactsCollection;

@ComponentAttribute.Annotation(
        Name = "ND Call Graph Printer",
        DefaultLabel = "ND Call Graph Printer",
        Description = "This component reads a call graph and prints it to the TraceLab console.",
        Author = "Notre Dame TraceLab Team",
        Version = "0.1")

@IOSpecAttribute.Annotation.__Multiple({
	@IOSpecAttribute.Annotation(IOType = IOSpecType.__Enum.Input,
				    Name = "callgraph", 
				    DataType = HashMap.class,
				    Description = "Call Graph produced from srcML."),

        @IOSpecAttribute.Annotation(IOType = IOSpecType.__Enum.Output,
                                    Name = "str_callgraph",
                                    DataType = String.class,
                                    Description = "String representation of call graph."),
})

public class ND_CallGraphPrinter extends BaseComponent
{

	public ND_CallGraphPrinter (ComponentLogger log)
	{
		super(log);
	}

	@Override
	public void Compute() throws ComponentException
	{
		HashMap<String, List<String>> nd_hashmap = (HashMap) super.get_Workspace().Load("callgraph");

		try {
			//SRCMLParser p = new SRCMLParser("/home/cmc/projects/tracelab/ND_CallGraphGen/rhino-src.xml");

			String out = "";

			for(String k:nd_hashmap.keySet())
				for(String v:nd_hashmap.get(k))
					out += String.format("\"%s\",\"%s\"\n", k, v);

			super.get_Workspace().Store("str_callgraph", out);

		} catch(Exception e){
			super.get_Logger().Trace("ND_CallGraphPrinter ERROR: " + e);
		}
	}

}

