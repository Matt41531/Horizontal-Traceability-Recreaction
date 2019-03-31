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
import java.io.FileWriter;
import java.io.IOException;
import java.lang.Object.*;
import java.text.SimpleDateFormat;
import java.util.*;


public class ClassTokenized 
{
	String name;
	String longName;
	String body;
	
	ArrayList<String> members;
	ArrayList<String> imports;
	ArrayList<String> comments;
	ArrayList<String> content;
	Map<String, String> varTypeMap;
	public Map<String, String> getVarTypeMap() {
		return varTypeMap;
	}

	public void setVarTypeMap(Map<String, String> varTypeMap) {
		this.varTypeMap = varTypeMap;
	}

	ArrayList<MethodTokenized> methods;
	
	public ClassTokenized()
	{
		members = new ArrayList<String>();
		imports = new ArrayList<String>();
		comments = new ArrayList<String>();
		content = new ArrayList<String>();
		varTypeMap = new HashMap<String, String>();
		methods = new ArrayList<MethodTokenized>();
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
	
	public void printClassInfo()
	{
		System.out.println("\t *** Class Info ***");
		System.out.println(" name : "+this.name);
		System.out.println(" # of instructions : "+this.content.size());
		System.out.println(" # of methods : "+this.methods.size());
		System.out.println(" # of comments : "+this.comments.size());
	}
	
	public List<MethodTokenized> getMethods() 
	{
		return methods;
	}

	public String getName() 
	{
		return name;
	}
	
	public void setName(String name) {
		this.name = name;
	}
	
public void setBody(String body) {
	this.body = body;
}

public String getBody() {
	return this.body;
}

public String getLongName() {
	return longName;
}

public void setLongName(String longName) {
	this.longName = longName;
}

public ArrayList<String> getImports() {
	return imports;
}

public void setImports(ArrayList<String> imports) {
	this.imports = imports;
}

}
