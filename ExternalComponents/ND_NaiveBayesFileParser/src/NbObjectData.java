//@author: Rrezarta Krasniqi

public class NbObjectData {
	public NbObjectData(String type, double precision, double recall,
			double f1Score, double support) {
		super();
		this.type = type;
		this.precision = precision;
		this.recall = recall;
		this.f1Score = f1Score;
		this.support = support;
	}

	public String getType() {
		return type;
	}

	public void setType(String type) {
		this.type = type;
	}

	public double getPrecision() {
		return precision;
	}

	public void setPrecision(double precision) {
		this.precision = precision;
	}

	public double getRecall() {
		return recall;
	}

	public void setRecall(double recall) {
		this.recall = recall;
	}

	public double getF1Score() {
		return f1Score;
	}

	public void setF1Score(double f1Score) {
		this.f1Score = f1Score;
	}

	public double getSupport() {
		return support;
	}

	public void setSupport(double support) {
		this.support = support;
	}

	String type;
	double precision;
	double recall;
	double f1Score;
	double support;

	public NbObjectData() {
	}

}
