import java.io.*;
import java.util.*;

import org.w3c.dom.Document;
import org.w3c.dom.NodeList;
import org.w3c.dom.Node;
import org.w3c.dom.Element;

import cli.TraceLabSDK.*;
import cli.TraceLabSDK.Types.TLArtifact;
import cli.TraceLabSDK.Types.TLArtifactsCollection;

@ComponentAttribute.Annotation(
        Name = "ND Page Rank Formatter",
        DefaultLabel = "ND Page Rank Formatter",
        Description = "This component reads a page rank and formats it to the be used by the ND XML Sum Gen component.",
        Author = "Notre Dame TraceLab Team",
        Version = "0.1")

@IOSpecAttribute.Annotation.__Multiple({
	@IOSpecAttribute.Annotation(IOType = IOSpecType.__Enum.Input,
				    Name = "pageranks", 
				    DataType = HashMap.class,
				    Description = "Call Graph produced from srcML."),

        @IOSpecAttribute.Annotation(IOType = IOSpecType.__Enum.Output,
                                    Name = "str_pagerank",
                                    DataType = String.class,
                                    Description = "String representation of page ranks."),
})

public class ND_PageRankFormatter extends BaseComponent
{

	public ND_PageRankFormatter (ComponentLogger log)
	{
		super(log);
	}

	public void writeFile(ArrayList<String> output) throws IOException {
	
        File fout = new File("/tmp/ND_PageRankFormatter.txt");
        FileOutputStream fos = new FileOutputStream(fout);
        
        BufferedWriter bw = new BufferedWriter(new OutputStreamWriter(fos));
        
        for(int i = 0; i < output.size(); i++){
            bw.write(output.get(i) + "\n");
        }
        bw.close();
	}
	
	
	@Override
	public void Compute() throws ComponentException
	{
		HashMap<String, Double> nd_hashmap = (HashMap) super.get_Workspace().Load("pageranks");

		try {
			
			ArrayList<String> out = new ArrayList<String>();

        	for(String k:nd_hashmap.keySet())
                out.add(String.format(k + "||" + nd_hashmap.get(k)));
					
            Formatter f = new Formatter(out);
            
            ArrayList<String> output = f.ProcessCallGraph();
            output = new ArrayList<String>(new LinkedHashSet<String>(output));
            
            Collections.sort(output);
            
            writeFile(output);
            

		} catch(Exception e){
			super.get_Logger().Trace("ND_PageRankFormatter ERROR: " + e);
		}
	}

}

