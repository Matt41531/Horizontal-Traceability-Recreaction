package sumslice.messages;

public class UseMessage extends Message {
	public String type;
	public String example;
	
	public void setType(String type){
		this.type = type;
	}
	
	public void setExample(String example){
		this.example = example;
	}
	
	public String getType(){
		return this.type;
	}
	
	public String getExample(){
		return this.example;
	}

}
