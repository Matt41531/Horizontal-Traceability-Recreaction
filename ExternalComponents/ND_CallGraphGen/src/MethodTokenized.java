/**
 * Java files tokenizer
 * @author MWM
 */

import japa.*;
import japa.parser.JavaParser;
import japa.parser.ast.CompilationUnit;
import japa.parser.ast.body.*;
import japa.parser.ast.visitor.VoidVisitorAdapter;

import java.io.FileInputStream;
import java.lang.Object.*;
import java.util.*;


public class MethodTokenized 
{
	String name;
	String body;
	
	ArrayList<String> members;
	ArrayList<String> comments;
	ArrayList<String> content;
ArrayList<String> calledMethods;
Map<String, String> varTypeMap;
public Map<String, String> getVarTypeMap() {
	return varTypeMap;
}

public void setVarTypeMap(Map<String, String> varTypeMap) {
	this.varTypeMap = varTypeMap;
}

	ArrayList<String> similarity_classes;
	ArrayList<Double> similarity_scores;
	
	
	public MethodTokenized()
	{
		
		members = new ArrayList<String>();
		comments = new ArrayList<String>();
		content = new ArrayList<String>();
		calledMethods = new ArrayList<String>();
		varTypeMap = new HashMap<String,String>();
		similarity_classes = new ArrayList<String>();
		similarity_scores = new ArrayList<Double>();
	}
	
	public void setMembers(List members)
	{
		for (int i=0 ; i < members.size() ; i++)
		{
			this.members.add(members.get(i).toString());
		}
	}
	
	public void setComments(List comments)
	{
		for (int i=0 ; i < comments.size() ; i++)
		{
			this.comments.add(comments.get(i).toString());
		}
	}
	
	public void setContent()
	{
		for (int i=0 ; i < comments.size() ; i++)
		{
			this.content.add(comments.get(i));
		}
		
		for (int i=0 ; i < members.size() ; i++)
		{
			this.content.add(members.get(i));
		}
		
	}
	
	public String getBody() {
		return body;
	}
	
	
public String getName() {
	return name;
}

public void setName(String n) {
	this.name = n;
}

public void setBody(String body) {
	this.body = body;
}

public ArrayList<String> getCalledMethods() {
	return calledMethods;
}

public void setCalledMethods(ArrayList<String> calledMethods) {
	this.calledMethods = calledMethods;
}
}
