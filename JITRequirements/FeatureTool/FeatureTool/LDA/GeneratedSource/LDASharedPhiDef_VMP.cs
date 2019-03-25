using System;
using MicrosoftResearch.Infer;
using MicrosoftResearch.Infer.Distributions;
using MicrosoftResearch.Infer.Maths;
using MicrosoftResearch.Infer.Collections;
using MicrosoftResearch.Infer.Factors;

namespace MicrosoftResearch.Infer.Models.User
{
	/// <summary>
	/// Generated algorithm for performing inference
	/// </summary>
	/// <remarks>
	/// The easiest way to use this class is to wrap an instance in a CompiledAlgorithm object and use
	/// the methods on CompiledAlgorithm to set parameters and execute inference.
	/// 
	/// If you instead wish to use this class directly, you must perform the following steps:
	/// 1) Create an instance of the class
	/// 2) Set the value of any externally-set fields e.g. data, priors
	/// 3) Call the Execute(numberOfIterations) method
	/// 4) Use the XXXMarginal() methods to retrieve posterior marginals for different variables.
	/// 
	/// Generated by Infer.NET 2.5 at 10:27 on dinsdag 17 september 2013.
	/// </remarks>
	public partial class LDASharedPhiDef_VMP : IGeneratedAlgorithm
	{
		#region Fields
		/// <summary>Field backing the NumberOfIterationsDone property</summary>
		private int numberOfIterationsDone;
		/// <summary>Field backing the EvidencePrior property</summary>
		private Bernoulli evidencePrior;
		/// <summary>Field backing the PhiPrior property</summary>
		private Dirichlet[] phiPrior;
		/// <summary>Field backing the PhiConstraint property</summary>
		private DistributionRefArray<Dirichlet,Vector> phiConstraint;
		/// <summary>The number of iterations last computed by Changed_EvidencePrior_PhiPrior_PhiConstraint. Set this to zero to force re-execution of Changed_EvidencePrior_PhiPrior_PhiConstraint</summary>
		public int Changed_EvidencePrior_PhiPrior_PhiConstraint_iterationsDone;
		public PointMass<Bernoulli> EvidencePrior_marginal;
		public PointMass<Dirichlet[]> PhiPrior_marginal;
		public PointMass<DistributionRefArray<Dirichlet,Vector>> PhiConstraint_marginal;
		/// <summary>Message to marginal of 'EvidencePhiDef'</summary>
		public Bernoulli EvidencePhiDef_marginal_F;
		/// <summary>Message to marginal of 'PhiDef'</summary>
		public DistributionRefArray<Dirichlet,Vector> PhiDef_marginal_F;
		public Bernoulli EvidencePhiDef_selector_B;
		#endregion

		#region Properties
		/// <summary>The number of iterations done from the initial state</summary>
		public int NumberOfIterationsDone
		{			get {
				return this.numberOfIterationsDone;
			}
		}

		/// <summary>The externally-specified value of 'EvidencePrior'</summary>
		public Bernoulli EvidencePrior
		{			get {
				return this.evidencePrior;
			}
			set {
				if (this.evidencePrior!=value) {
					this.evidencePrior = value;
					this.numberOfIterationsDone = 0;
					this.Changed_EvidencePrior_PhiPrior_PhiConstraint_iterationsDone = 0;
				}
			}
		}

		/// <summary>The externally-specified value of 'PhiPrior'</summary>
		public Dirichlet[] PhiPrior
		{			get {
				return this.phiPrior;
			}
			set {
				if ((value!=null)&&(value.Length!=5)) {
					throw new ArgumentException(((("Provided array of length "+value.Length)+" when length ")+5)+" was expected for variable \'PhiPrior\'");
				}
				this.phiPrior = value;
				this.numberOfIterationsDone = 0;
				this.Changed_EvidencePrior_PhiPrior_PhiConstraint_iterationsDone = 0;
			}
		}

		/// <summary>The externally-specified value of 'PhiConstraint'</summary>
		public DistributionRefArray<Dirichlet,Vector> PhiConstraint
		{			get {
				return this.phiConstraint;
			}
			set {
				this.phiConstraint = value;
				this.numberOfIterationsDone = 0;
				this.Changed_EvidencePrior_PhiPrior_PhiConstraint_iterationsDone = 0;
			}
		}

		#endregion

		#region Methods
		/// <summary>Get the observed value of the specified variable.</summary>
		/// <param name="variableName">Variable name</param>
		public object GetObservedValue(string variableName)
		{
			if (variableName=="EvidencePrior") {
				return this.EvidencePrior;
			}
			if (variableName=="PhiPrior") {
				return this.PhiPrior;
			}
			if (variableName=="PhiConstraint") {
				return this.PhiConstraint;
			}
			throw new ArgumentException("Not an observed variable name: "+variableName);
		}

		/// <summary>Set the observed value of the specified variable.</summary>
		/// <param name="variableName">Variable name</param>
		/// <param name="value">Observed value</param>
		public void SetObservedValue(string variableName, object value)
		{
			if (variableName=="EvidencePrior") {
				this.EvidencePrior = (Bernoulli)value;
				return ;
			}
			if (variableName=="PhiPrior") {
				this.PhiPrior = (Dirichlet[])value;
				return ;
			}
			if (variableName=="PhiConstraint") {
				this.PhiConstraint = (DistributionRefArray<Dirichlet,Vector>)value;
				return ;
			}
			throw new ArgumentException("Not an observed variable name: "+variableName);
		}

		/// <summary>The marginal distribution of the specified variable.</summary>
		/// <param name="variableName">Variable name</param>
		public object Marginal(string variableName)
		{
			if (variableName=="EvidencePrior") {
				return this.EvidencePriorMarginal();
			}
			if (variableName=="PhiPrior") {
				return this.PhiPriorMarginal();
			}
			if (variableName=="PhiConstraint") {
				return this.PhiConstraintMarginal();
			}
			if (variableName=="EvidencePhiDef") {
				return this.EvidencePhiDefMarginal();
			}
			if (variableName=="PhiDef") {
				return this.PhiDefMarginal();
			}
			throw new ArgumentException("This class was not built to infer "+variableName);
		}

		public T Marginal<T>(string variableName)
		{
			return Distribution.ChangeType<T>(this.Marginal(variableName));
		}

		/// <summary>The query-specific marginal distribution of the specified variable.</summary>
		/// <param name="variableName">Variable name</param>
		/// <param name="query">QueryType name. For example, GibbsSampling answers 'Marginal', 'Samples', and 'Conditionals' queries</param>
		public object Marginal(string variableName, string query)
		{
			if (query=="Marginal") {
				return this.Marginal(variableName);
			}
			if ((variableName=="EvidencePhiDef")&&(query=="MarginalDividedByPrior")) {
				return this.EvidencePhiDefMarginalDividedByPrior();
			}
			throw new ArgumentException(((("This class was not built to infer \'"+variableName)+"\' with query \'")+query)+"\'");
		}

		public T Marginal<T>(string variableName, string query)
		{
			return Distribution.ChangeType<T>(this.Marginal(variableName, query));
		}

		/// <summary>Update all marginals, by iterating message passing the given number of times</summary>
		/// <param name="numberOfIterations">The number of times to iterate each loop</param>
		/// <param name="initialise">If true, messages that initialise loops are reset when observed values change</param>
		private void Execute(int numberOfIterations, bool initialise)
		{
			this.Changed_EvidencePrior_PhiPrior_PhiConstraint();
			this.numberOfIterationsDone = numberOfIterations;
		}

		public void Execute(int numberOfIterations)
		{
			this.Execute(numberOfIterations, true);
		}

		public void Update(int additionalIterations)
		{
			this.Execute(this.numberOfIterationsDone+additionalIterations, false);
		}

		private void OnProgressChanged(ProgressChangedEventArgs e)
		{
			// Make a temporary copy of the event to avoid a race condition
			// if the last subscriber unsubscribes immediately after the null check and before the event is raised.
			EventHandler<ProgressChangedEventArgs> handler = this.ProgressChanged;
			if (handler!=null) {
				handler(this, e);
			}
		}

		/// <summary>Reset all messages to their initial values.  Sets NumberOfIterationsDone to 0.</summary>
		public void Reset()
		{
			this.Execute(0);
		}

		/// <summary>Computations that depend on the observed value of EvidencePrior and PhiPrior and PhiConstraint</summary>
		public void Changed_EvidencePrior_PhiPrior_PhiConstraint()
		{
			if (this.Changed_EvidencePrior_PhiPrior_PhiConstraint_iterationsDone==1) {
				return ;
			}
			this.EvidencePrior_marginal = new PointMass<Bernoulli>(this.evidencePrior);
			this.PhiPrior_marginal = new PointMass<Dirichlet[]>(this.phiPrior);
			this.PhiConstraint_marginal = new PointMass<DistributionRefArray<Dirichlet,Vector>>(this.phiConstraint);
			this.EvidencePhiDef_marginal_F = Bernoulli.Uniform();
			// Create array for 'PhiDef_marginal' Forwards messages.
			this.PhiDef_marginal_F = new DistributionRefArray<Dirichlet,Vector>(5);
			for(int T = 0; T<5; T++) {
				this.PhiDef_marginal_F[T] = ArrayHelper.MakeUniform<Dirichlet>(Dirichlet.Uniform(this.phiPrior[T].Dimension, Sparsity.FromSpec(MicrosoftResearch.Infer.Maths.StorageType.Sparse, 1E-11, 0)));
				// Message to 'PhiDef_marginal' from Variable factor
				this.PhiDef_marginal_F[T] = VariableVmpOp.MarginalAverageLogarithm<Dirichlet>(this.phiConstraint[T], this.phiPrior[T], this.PhiDef_marginal_F[T]);
			}
			DistributionStructArray<Bernoulli,bool>[] EvidencePhiDef_selector_cases_0_rep_uses_B = default(DistributionStructArray<Bernoulli,bool>[]);
			// Create array for 'EvidencePhiDef_selector_cases_0_rep_uses' Backwards messages.
			EvidencePhiDef_selector_cases_0_rep_uses_B = new DistributionStructArray<Bernoulli,bool>[5];
			for(int T = 0; T<5; T++) {
				// Create array for 'EvidencePhiDef_selector_cases_0_rep_uses' Backwards messages.
				EvidencePhiDef_selector_cases_0_rep_uses_B[T] = new DistributionStructArray<Bernoulli,bool>(2);
				for(int _ind = 0; _ind<2; _ind++) {
					EvidencePhiDef_selector_cases_0_rep_uses_B[T][_ind] = Bernoulli.Uniform();
				}
				// Message to 'EvidencePhiDef_selector_cases_0_rep_uses' from Random factor
				EvidencePhiDef_selector_cases_0_rep_uses_B[T][0] = Bernoulli.FromLogOdds(UnaryOp<Vector>.AverageLogFactor<Dirichlet>(this.PhiDef_marginal_F[T], this.phiPrior[T]));
				// Message to 'EvidencePhiDef_selector_cases_0_rep_uses' from Variable factor
				EvidencePhiDef_selector_cases_0_rep_uses_B[T][1] = Bernoulli.FromLogOdds(VariableVmpOp.AverageLogFactor<Dirichlet>(this.PhiDef_marginal_F[T]));
			}
			DistributionStructArray<Bernoulli,bool> EvidencePhiDef_selector_cases_0_rep_B = default(DistributionStructArray<Bernoulli,bool>);
			// Create array for 'EvidencePhiDef_selector_cases_0_rep' Backwards messages.
			EvidencePhiDef_selector_cases_0_rep_B = new DistributionStructArray<Bernoulli,bool>(5);
			for(int T = 0; T<5; T++) {
				EvidencePhiDef_selector_cases_0_rep_B[T] = Bernoulli.Uniform();
				// Message to 'EvidencePhiDef_selector_cases_0_rep' from Replicate factor
				EvidencePhiDef_selector_cases_0_rep_B[T] = ReplicateOp.DefAverageLogarithm<Bernoulli>(EvidencePhiDef_selector_cases_0_rep_uses_B[T], EvidencePhiDef_selector_cases_0_rep_B[T]);
			}
			Bernoulli[] EvidencePhiDef_selector_cases_0_uses_B = default(Bernoulli[]);
			// Create array for 'EvidencePhiDef_selector_cases_0_uses' Backwards messages.
			EvidencePhiDef_selector_cases_0_uses_B = new Bernoulli[5];
			for(int _ind = 0; _ind<5; _ind++) {
				EvidencePhiDef_selector_cases_0_uses_B[_ind] = Bernoulli.Uniform();
			}
			// Message to 'EvidencePhiDef_selector_cases_0_uses' from Replicate factor
			EvidencePhiDef_selector_cases_0_uses_B[1] = ReplicateOp.DefAverageLogarithm<Bernoulli>(EvidencePhiDef_selector_cases_0_rep_B, EvidencePhiDef_selector_cases_0_uses_B[1]);
			// Message to 'EvidencePhiDef_selector_cases_0_uses' from EqualRandom factor
			EvidencePhiDef_selector_cases_0_uses_B[4] = Bernoulli.FromLogOdds(ConstrainEqualRandomOp<Vector[]>.AverageLogFactor<DistributionRefArray<Dirichlet,Vector>>(this.PhiDef_marginal_F, this.phiConstraint));
			Bernoulli EvidencePhiDef_selector_cases_0_B = Bernoulli.Uniform();
			// Message to 'EvidencePhiDef_selector_cases_0' from Replicate factor
			EvidencePhiDef_selector_cases_0_B = ReplicateOp.DefAverageLogarithm<Bernoulli>(EvidencePhiDef_selector_cases_0_uses_B, EvidencePhiDef_selector_cases_0_B);
			DistributionStructArray<Bernoulli,bool> EvidencePhiDef_selector_cases_B = default(DistributionStructArray<Bernoulli,bool>);
			// Create array for 'EvidencePhiDef_selector_cases' Backwards messages.
			EvidencePhiDef_selector_cases_B = new DistributionStructArray<Bernoulli,bool>(2);
			for(int _ind0 = 0; _ind0<2; _ind0++) {
				EvidencePhiDef_selector_cases_B[_ind0] = Bernoulli.Uniform();
			}
			// Message to 'EvidencePhiDef_selector_cases' from Copy factor
			EvidencePhiDef_selector_cases_B[0] = ArrayHelper.SetTo<Bernoulli>(EvidencePhiDef_selector_cases_B[0], EvidencePhiDef_selector_cases_0_B);
			this.EvidencePhiDef_selector_B = Bernoulli.Uniform();
			// Message to 'EvidencePhiDef_selector' from Cases factor
			this.EvidencePhiDef_selector_B = CasesOp.BAverageLogarithm(EvidencePhiDef_selector_cases_B);
			// Message to 'EvidencePhiDef_marginal' from Variable factor
			this.EvidencePhiDef_marginal_F = VariableVmpOp.MarginalAverageLogarithm<Bernoulli>(this.EvidencePhiDef_selector_B, this.evidencePrior, this.EvidencePhiDef_marginal_F);
			this.Changed_EvidencePrior_PhiPrior_PhiConstraint_iterationsDone = 1;
		}

		/// <summary>
		/// Returns the marginal distribution for 'EvidencePrior' given by the current state of the
		/// message passing algorithm.
		/// </summary>
		/// <returns>The marginal distribution</returns>
		public PointMass<Bernoulli> EvidencePriorMarginal()
		{
			return this.EvidencePrior_marginal;
		}

		/// <summary>
		/// Returns the marginal distribution for 'PhiPrior' given by the current state of the
		/// message passing algorithm.
		/// </summary>
		/// <returns>The marginal distribution</returns>
		public PointMass<Dirichlet[]> PhiPriorMarginal()
		{
			return this.PhiPrior_marginal;
		}

		/// <summary>
		/// Returns the marginal distribution for 'PhiConstraint' given by the current state of the
		/// message passing algorithm.
		/// </summary>
		/// <returns>The marginal distribution</returns>
		public PointMass<DistributionRefArray<Dirichlet,Vector>> PhiConstraintMarginal()
		{
			return this.PhiConstraint_marginal;
		}

		/// <summary>
		/// Returns the marginal distribution for 'EvidencePhiDef' given by the current state of the
		/// message passing algorithm.
		/// </summary>
		/// <returns>The marginal distribution</returns>
		public Bernoulli EvidencePhiDefMarginal()
		{
			return this.EvidencePhiDef_marginal_F;
		}

		/// <summary>
		/// Returns the output message (the posterior divided by the prior) for 'EvidencePhiDef' given by the current state of the
		/// message passing algorithm.
		/// </summary>
		/// <returns>The output message (the posterior divided by the prior)</returns>
		public Bernoulli EvidencePhiDefMarginalDividedByPrior()
		{
			return this.EvidencePhiDef_selector_B;
		}

		/// <summary>
		/// Returns the marginal distribution for 'PhiDef' given by the current state of the
		/// message passing algorithm.
		/// </summary>
		/// <returns>The marginal distribution</returns>
		public DistributionRefArray<Dirichlet,Vector> PhiDefMarginal()
		{
			return this.PhiDef_marginal_F;
		}

		#endregion

		#region Events
		/// <summary>Event that is fired when the progress of inference changes, typically at the end of one iteration of the inference algorithm.</summary>
		public event EventHandler<ProgressChangedEventArgs> ProgressChanged;
		#endregion

	}

}