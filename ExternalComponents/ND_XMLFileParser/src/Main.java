

import java.io.File;
import java.io.FileNotFoundException;
import java.io.PrintWriter;
import java.io.UnsupportedEncodingException;
import java.util.ArrayList;
import java.util.List;
import java.util.Scanner;


public class Main {

	public static void main(String [] args) throws FileNotFoundException{
		
		// TODO Auto-generated method stub
		String filePath = "src/rule.xml";						
		Parser parser = new Parser();
		String file = parser.readFile(filePath);
				
		String[] lines = file.split("\n");
		
        for(int i = 0; i < lines.length; i++){
        	
        	System.out.println("printing lines " + lines[i]);
        	
        }
			
	}
		
}	
	
