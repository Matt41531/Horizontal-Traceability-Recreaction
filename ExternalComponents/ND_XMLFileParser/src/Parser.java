import java.io.File;
import java.util.ArrayList;
import java.util.List;
import java.util.Scanner;

import cli.System.IO.FileNotFoundException;



public class Parser {

	public String readFile(String filePath) throws java.io.FileNotFoundException{
		
		File file = new File(filePath);
		 
        Scanner scanner = new Scanner(file);
 
        String completeFile = "";
        
		while (scanner.hasNextLine()) {
		    String line = scanner.nextLine();
		    completeFile += line + "\n";
		    
		}
		scanner.close();
		
        return completeFile;
        
	}
}
