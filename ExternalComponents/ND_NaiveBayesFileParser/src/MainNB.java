//@author: Rrezarta Krasniqi

import java.io.FileNotFoundException;

public class MainNB {

	public static void main(String[] args) throws FileNotFoundException {

		String filePath = "src/nb_5_fold_cv_results.txt";
		Parser parser = new Parser();
		String file = parser.readFile(filePath);
		
		String[] lines = file.split("\n");
		
		for(int i = 0; i < lines.length; i++){
			System.out.println(lines[i]);
		}		
	}
}
