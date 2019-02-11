//@author: Rrezarta Krasniqi

import java.io.FileNotFoundException;

public class MainLR {

	public static void main(String[] args) throws FileNotFoundException {

		String filePath = "src/nb_5_fold_cv_results.txt";
		Parser parser = new Parser();
		parser.readFile(filePath);
	}
}
