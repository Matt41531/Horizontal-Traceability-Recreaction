import java.io.BufferedReader;
import java.io.FileNotFoundException;
import java.io.FileReader;
import java.io.IOException;
import java.io.InputStreamReader;
import java.util.*;

import org.w3c.dom.Document;
import org.w3c.dom.NodeList;
import org.w3c.dom.Node;
import org.w3c.dom.Element;

import cli.TraceLabSDK.*;
import cli.TraceLabSDK.Types.TLArtifact;
import cli.TraceLabSDK.Types.TLArtifactsCollection;

@ComponentAttribute.Annotation(
        Name = "ND Page Rank Printer",
        DefaultLabel = "ND Page Rank Printer",
        Description = "This component reads a call graph and prints it to the TraceLab console.",
        Author = "Notre Dame TraceLab Team",
        Version = "0.1")

@IOSpecAttribute.Annotation.__Multiple({
	@IOSpecAttribute.Annotation(IOType = IOSpecType.__Enum.Input,
				    Name = "pageranks", 
				    DataType = HashMap.class,
				    Description = "Page Ranks produced from page rank component."),

        @IOSpecAttribute.Annotation(IOType = IOSpecType.__Enum.Output,
                                    Name = "str_pagerank",
                                    DataType = String.class,
                                    Description = "String representation of page ranks."),
})

public class ND_PageRankPrinter extends BaseComponent
{

	public ND_PageRankPrinter (ComponentLogger log)
	{
		super(log);
	}

	@Override
	public void Compute() throws ComponentException
	{
        HashMap<String, Double> nd_hashmap = (HashMap) super.get_Workspace().Load("pageranks");
        
        try {
			//SRCMLParser p = new SRCMLParser("/home/cmc/projects/tracelab/ND_CallGraphGen/rhino-src.xml");

			String out = "";
        	for(String k:nd_hashmap.keySet())
                out += (String.format(k + "||"+ nd_hashmap.get(k))+"\n");
				
        	super.get_Workspace().Store("str_pagerank", out);

		} catch(Exception e){
			super.get_Logger().Trace("ND_PageRankPrinter ERROR: " + e);
		}
	}

}

