import java.io.BufferedReader;
import java.io.FileNotFoundException;
import java.io.FileReader;
import java.io.FileWriter;
import java.io.PrintWriter;
import java.io.StringWriter;
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

import edu.uci.ics.jung.algorithms.scoring.PageRank;
import edu.uci.ics.jung.graph.DirectedSparseGraph;

@ComponentAttribute.Annotation(
        Name = "ND PageRank",
        DefaultLabel = "ND PageRank Calculator",
        Description = "This component reads a call graph and calculates the PageRank of each vertex.",
        Author = "Notre Dame TraceLab Team",
        Version = "0.1",
	ConfigurationType = Config.class)

@IOSpecAttribute.Annotation.__Multiple({
	@IOSpecAttribute.Annotation(IOType = IOSpecType.__Enum.Input,
				    Name = "callgraph", 
				    DataType = HashMap.class,
				    Description = "Call Graph produced from srcML."),

        @IOSpecAttribute.Annotation(IOType = IOSpecType.__Enum.Output,
                                    Name = "pageranks",
                                    DataType = HashMap.class,
                                    Description = "Map of graph vertices to PageRank values."),
})

public class ND_PageRank extends BaseComponent
{

	private Config config;

	public ND_PageRank (ComponentLogger log)
	{
		super(log);
		config = new Config();
		super.set_Configuration(config);
	}

	@Override
	public void Compute() throws ComponentException
	{
		HashMap<String, List<String>> nd_hashmap = (HashMap) super.get_Workspace().Load("callgraph");

		int edgeCnt = 0;
		double alpha = 0.15;

		/*try {
			alpha = Double.parseDouble(config.getAlpha());
		} catch(Exception e)
		{
			alpha = 0.15;
		}*/

		try {
			DirectedSparseGraph<String, Integer> graph = new DirectedSparseGraph<String, Integer>();

			/* it is necessary to add all vertices first */
			for(String k:nd_hashmap.keySet())
				graph.addVertex(k);

			/* and then add the edges */
			for(String k:nd_hashmap.keySet())
				for(String v:nd_hashmap.get(k))
					graph.addEdge(new Integer(edgeCnt++), k, v);

			PageRank<String, Integer> ranker = new PageRank<String, Integer>(graph, alpha);

			ranker.evaluate();

			HashMap<String, Double> pageranks = new HashMap<String, Double>();
			for (String v : graph.getVertices()) {
				pageranks.put(v, ranker.getVertexScore(v));
			}

			super.get_Workspace().Store("pageranks", pageranks);

		} catch(Exception e){

			/* can't believe it is this complicated to get the stack trace */
			StringWriter sw = new StringWriter();
			PrintWriter pw = new PrintWriter(sw);
			e.printStackTrace(pw);
			String errmessage = sw.toString();

			super.get_Logger().Trace("ND_PageRank ERROR: " + errmessage);
		}
	}

}

