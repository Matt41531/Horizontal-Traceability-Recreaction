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
        Name = "ND Call Graph Formatter",
        DefaultLabel = "ND Call Graph Formatter",
        Description = "This component reads a call graph formats it to the be used by the ND XML Sum Gen component.",
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
                                    Description = "String representation of call graph ranks."),
})

public class ND_CallGraphFormatter extends BaseComponent
{

	public ND_CallGraphFormatter (ComponentLogger log)
	{
		super(log);
	}
	
	
	public void writeFile(ArrayList<String> output) throws IOException {
	
        File fout = new File("/tmp/ND_CallGraphFormatter.txt");
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
		HashMap<String, List<String>> nd_hashmap = (HashMap) super.get_Workspace().Load("callgraph");

		try {
			//SRCMLParser p = new SRCMLParser("/home/cmc/projects/tracelab/ND_CallGraphGen/rhino-src.xml");
			
			ArrayList<String> out = new ArrayList<String>();

			for(String k:nd_hashmap.keySet())
				for(String v:nd_hashmap.get(k))
					out.add(String.format("%s||%s", k, v));
					
					
            Formatter f = new Formatter(out);
            
            
            ArrayList<String> output = f.ProcessCallGraph();
            output = new ArrayList<String>(new LinkedHashSet<String>(output));
            
            Collections.sort(output);
            
            writeFile(output);

		} catch(Exception e){
			super.get_Logger().Trace("ND_CallGraphFormatter ERROR: " + e);
		}
	}

}

