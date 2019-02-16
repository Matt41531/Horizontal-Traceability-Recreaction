import org.xml.sax.Attributes;
import org.xml.sax.*;
import org.xml.sax.helpers.DefaultHandler;
import javax.xml.parsers.*;
import java.util.*;
import java.io.*;

public class SRCMLParser extends DefaultHandler {
List<ClassTokenized> classes = new ArrayList();
Stack<ClassTokenized> classStack = new Stack<ClassTokenized>();
Stack<MethodTokenized> methodStack = new Stack<MethodTokenized>();
ClassTokenized currentClass = null;
MethodTokenized currentMethod= null;
Map<String, List<String>> callGraph;
ArrayList<String> imports = new ArrayList<String>();
String currentPackage = null;
String currentCall = null;
String currentImport = null;
String currentName = null;
String currentType = null;
boolean isName = false;
boolean isPackage = false; // we process package names independently of the rest
boolean isType = false;
boolean isParamList = false;
boolean isConstructor = false;
boolean isCatch = false;
boolean isImport = false;
boolean isDecl = false;
boolean isCall = false;
StringBuilder classBuf = new StringBuilder();
StringBuilder methodBuf = new StringBuilder();

    public void startElement(String uri, String localName,        String qName, Attributes attributes) throws SAXException {

if (qName.equals("name")){
	isName = true;
	currentName = null;
}
else if (qName.equals("class")){
	newClass();
}
else if (qName.equals("constructor")){
	isConstructor = true;
	newMethod();
}
else if (qName.equals("function")){
	newMethod();
}
else if (qName.equals("static")){
	if(currentMethod == null)
		newMethod();
}
else if (qName.equals("package")){
	currentPackage = null;
	imports = new ArrayList<String>();
	isPackage = true;
}
else if (qName.equals("call")){
	isCall = true;
	currentCall = null;
}
else if (qName.equals("argument_list")){
	isCall = false;
	if(currentMethod != null)
		currentMethod.getCalledMethods().add(currentCall);
}
else if (qName.equals("import")){
	currentImport = null;
	isImport = true;
}
else if (qName.equals("decl")){
	isDecl = true;
}
else if (qName.equals("type")){
	isType = true;
}
else if (qName.equals("parameter_list")){
	isParamList = true;
}
else if (qName.equals("catch")){
	isCatch = true;
}
}

	private void newMethod() {
		currentMethod = new MethodTokenized();
		methodBuf = new StringBuilder();
	}

	private void newClass() {
		if(currentClass != null) {
			if(currentClass.getBody() != null)
				classBuf.insert(0, currentClass.getBody());
			currentClass.setBody(classBuf.toString());
			classStack.push(currentClass);
			classBuf = new StringBuilder();
			if(currentMethod != null) {
				if(currentMethod.getBody() != null)
					methodBuf.insert(0, currentMethod.getBody());
				currentMethod.setBody(methodBuf.toString());
				methodStack.push(currentMethod);
				methodBuf = new StringBuilder();
			}
		}
		currentClass = new ClassTokenized();
	currentMethod = null;
	currentClass.setImports(imports);
	}
    
    public void endElement(String uri, String localName,        String qName) throws SAXException {


if (qName.equals("class")){
	endClass();
}
else if (qName.equals("function")){
	endMethod();
}
else if (qName.equals("constructor")){
	isConstructor = false;
	endMethod();
}
else if (qName.equals("static")){
	if(currentMethod != null) {
	currentMethod.setName("<static initializer>");
	endMethod();
	
	}
}
else if (qName.equals("name")){
	isName = false;
	//try to find out where this name should go
	if(isType) {
		if(isDecl) {
			currentType = currentName;
		}
	}
	else {
	if(currentMethod != null) { 
		if(currentMethod.getName() == null) {
	if(isConstructor)
currentMethod.setName(currentClass.getName());
	else
		currentMethod.setName(currentName);
		}
		else if(isDecl) {
			if(currentType != null)
			currentMethod.getVarTypeMap().put(currentName, currentType);
			isDecl = false;
		}
	}
	else if(currentClass != null) {
		if(currentClass.getName() == null) {
		if(classStack.isEmpty())
			currentClass.setName(currentName);
		else {
			//inner class
			currentClass.setName(classStack.peek().getName()+"$"+currentName);
			currentClass.setLongName(classStack.peek().getLongName()+"$"+currentName);
		}
		 if(currentClass.getLongName() == null) {
			if(currentPackage != null)
		currentClass.setLongName(currentPackage+"."+currentName);
			else
				currentClass.setLongName(currentName);
		 }
		 }
	
	else if(isDecl) {
		if(currentType != null)
		currentClass.getVarTypeMap().put(currentName, currentType);
		isDecl = false;
	}
}
	}	
}
else if (qName.equals("decl")){
	isDecl = false;
	currentType = null;
}
else if (qName.equals("package")){
	isPackage = false;
}
else if (qName.equals("call")){
	isCall = false;
}
else if (qName.equals("import")){
	isImport = false;
		imports.add(currentImport);
}
else if (qName.equals("type")){
	isType = false;
}
else if (qName.equals("parameter_list")){
	isParamList = false;
}
else if (qName.equals("catch")){
	isCatch = false;
}
}


	private void endMethod() {
		if(currentMethod.getBody() != null)
		methodBuf.insert(0, currentMethod.getBody());
		currentMethod.setBody(methodBuf.toString());
		currentClass.getMethods().add(currentMethod);
		currentMethod = null;
		methodBuf = new StringBuilder();
	}

	private void endClass() {
		if(currentClass.getBody() != null)
		classBuf.insert(0, currentClass.getBody());
		currentClass.setBody(classBuf.toString());
		classes.add(currentClass);
		if(classStack.isEmpty())
			currentClass = null;
		else
		currentClass =  classStack.pop();
		if(methodStack.isEmpty())
			currentMethod = null;
		else
		currentMethod = methodStack.pop();
		classBuf = new StringBuilder();
	}
    
	public void characters(char ch[], int start, int length)
            throws SAXException {
    String origValue = new String(ch, start, length);
    // we keep origValue because we want to keep class and method bodies
            String value = origValue.trim();
            if(isParamList && !isCatch && currentMethod != null) {
            	//to produce a full long name we need everything in paramLists, except those names that are not in types
            	if(!(isName && !isType)) {
            String name = currentMethod.getName();
            //check to see if the param references an inner class
            //the Rhino dataset has them in long form e.g. c1$c2
            //Where as the average programmer would just reference c2
            for(ClassTokenized c:this.classes) {
            	if(c.getName().equals(currentClass.getName()+"$"+value)) {
            		name = name+currentClass.getName()+"$"+value;
                    currentMethod.setName(name);
                    return;		
            	}
            }
            name = name+value;
            currentMethod.setName(name);
            	}
            }  		
          
             if(isName) {
    if(currentName == null)
    	currentName = value;
    else
    	currentName = currentName+value;
   
      if(isPackage) {
    if(currentPackage == null)
    	currentPackage = value;
    else
    	currentPackage = currentPackage+"."+value;
    }
            else if(isImport) {
                if(currentImport == null)
                	currentImport = value;
                else
                	currentImport = currentImport+"."+value;
                }
      if(isCall) {
    if(currentCall == null)
    	currentCall = value;
    else
    	currentCall = currentCall+"."+value;
    }

                }
                 
      if(currentClass != null)
	   classBuf.append(origValue);
   if(currentMethod != null)
	   methodBuf.append(origValue);
    }

	public List<ClassTokenized> getClasses() {
		return classes;
	}

	public void setClasses(List<ClassTokenized> classes) {
		this.classes = classes;
	}
	
	public Map<String, List<String>> getCallGraph() {
		return callGraph;
	}

	public void setCallGraph(Map<String, List<String>> callGraph) {
		this.callGraph = callGraph;
	}

	public SRCMLParser(String file) throws IOException, SAXException, ParserConfigurationException {
        SAXParserFactory factory = SAXParserFactory.newInstance();
            InputStream    xmlInput  =              new FileInputStream(file);
            SAXParser      saxParser = factory.newSAXParser();
            
            saxParser.parse(xmlInput, this);
callGraph = new HashMap<String, List<String>>();
   resolveCallGraph();
	}
	
	public void resolveCallGraph() {
		for(ClassTokenized c:classes) {
			for(MethodTokenized m: c.getMethods()) {
				for(String m2: m.getCalledMethods()) {
					String fqn = resolveCall(c, m, m2);
					if(fqn != null) {
						String k = c.getLongName()+"."+m.getName();
						if(!callGraph.containsKey(k))
						callGraph.put(k, new ArrayList<String>());
						List<String> v =callGraph.get(k);
								v.add(fqn);
					}
				}
			}
		}
	}
	
	public String resolveCall(ClassTokenized c, MethodTokenized m, String m2) {
//first check for local method references, including recursive
		if(m.getName().split("\\(")[0].equals(m2))
			return c.getLongName()+"."+m.getName();
		if(c.getName().equals(m2))
			return c.getLongName()+"."+c.getName();
		for(MethodTokenized m3: c.getMethods()) {
			if(m3.getName().split("\\(")[0].equals(m2))
				return c.getLongName()+"."+m3.getName();
		}
		// check imports
//if(m2.equals("mhi.multiply"))
//	System.out.println("break");
		String m2Class = m2.split("\\.")[0];
		if(m.getVarTypeMap().containsKey(m2Class))
		m2Class = m.getVarTypeMap().get(m2Class);
		else 		if(c.getVarTypeMap().containsKey(m2Class))
			m2Class = c.getVarTypeMap().get(m2Class);
		String m2Name = m2Class;
		if(m2.contains("."))
		m2Name = m2.split("\\.")[1];
		if(c.getImports() != null) {
		for(String i:c.getImports()) {
			if(m2Class == null)
			System.out.printf("%s : %s\n", i, m2Class);
			if(i.endsWith(m2Class)) 
return i+"."+m2Name;
			ClassTokenized c2 = getClassByFQN(i);
			if(c2 != null) {
				for(MethodTokenized m3: c2.getMethods()) {
					if(m3.getName().split("\\(")[0].equals(m2))
						return c2.getLongName()+"."+m3.getName();
				}
						
			}
		}
		}
return null;		
	}
	
	public ClassTokenized getClassByFQN(String fqn) {
		for(ClassTokenized c:classes) {
			if(c.getLongName().equals(fqn))
				return c;
		}
	return null;	
	}
	
 public static void main(String[] args) {
	 
	 try {SRCMLParser p = new SRCMLParser("C:/Users/AMEER/Documents/test.xml");
	 System.out.println(p.getCallGraph());
 }
	 catch(Exception e) {
		 e.printStackTrace();
	 }
 }
	 }
