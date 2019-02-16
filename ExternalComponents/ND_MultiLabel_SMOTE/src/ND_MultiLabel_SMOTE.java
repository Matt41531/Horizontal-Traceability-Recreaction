import cli.TraceLabSDK.BaseComponent;
import cli.TraceLabSDK.ComponentAttribute;
import cli.TraceLabSDK.ComponentException;
import cli.TraceLabSDK.ComponentLogger;
import cli.TraceLabSDK.IOSpecAttribute;
import cli.TraceLabSDK.IOSpecType;

@ComponentAttribute.Annotation(
        Name = "Notre Dame MultiLabel SMOTE Component",
        DefaultLabel = "ND MultiLabel SMOTE Component",
        Description = "This component accepts as an output two classifier types and passes them to two different prediction models",
        Author = "Notre Dame TraceLab Team",
        Version = "0.1",
        ConfigurationType = Config.class)

@IOSpecAttribute.Annotation.__Multiple({
			@IOSpecAttribute.Annotation(IOType = IOSpecType.__Enum.Output, 
					Name = "modelKind", 
					DataType = String.class, 
					Description = "The name of the classifer type")
			
})

 
public class ND_MultiLabel_SMOTE extends BaseComponent {
	private Config config;
	
	public ND_MultiLabel_SMOTE(ComponentLogger log){
		super(log);
		config = new Config();
		super.set_Configuration(config);
	}

	@Override
	public void Compute() throws ComponentException
	{
	 try {

		  String modelKind = (String)config.getModelKind(); //entry either: "LR_Model" or "NB_Model"
	      super.get_Workspace().Store("modelKind", modelKind);
          
         } catch(Exception e){
			  super.get_Logger().Trace("ND_MultiLabel_SMOTE ERROR: " + e);
		}
    }
}