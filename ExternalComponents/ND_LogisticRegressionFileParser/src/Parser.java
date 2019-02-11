//@author: Rrezarta Krasniqi

import java.io.File;
import java.util.ArrayList;
import java.util.Scanner;


public class Parser {
	
	public String readFile(String filePath) throws java.io.FileNotFoundException{
		
		Scanner input = new Scanner(new File(filePath));
		ArrayList<LrObjectData> listData = new ArrayList<LrObjectData>();
		String completeFile = "";
		
		try{
			input.nextLine();
			while(input.hasNext()){
				String type = input.next();
				double precision = input.nextDouble();
				double recall = input.nextDouble();
				double f1Score = input.nextDouble();
				double support = input.nextDouble();
				LrObjectData results = new LrObjectData(type, precision, recall, f1Score,support);
				listData.add(results);
			}
			for(LrObjectData data: listData){
				completeFile += "[" + data.getType() + ", Precision = " + data.getPrecision() + "]"+"\n"+
						        "[" + data.getType() + ", Recall = "    + data.getRecall() + "]"+"\n"+
						        "[" + data.getType() + ", f1-Score = "  + data.getF1Score() + "]"+"\n"+
						        "[" + data.getType() + ", Support = "   + data.getSupport() + "]"+"\n";
			System.out.println(completeFile);
			}
		}
		finally {
			input.close();
			return completeFile;
		}
	}	
}