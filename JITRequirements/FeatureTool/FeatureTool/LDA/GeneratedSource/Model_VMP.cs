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
	/// Generated by Infer.NET 2.5 at 10:18 on dinsdag 17 september 2013.
	/// </remarks>
	public partial class Model_VMP : IGeneratedAlgorithm
	{
		#region Fields
		/// <summary>Field backing the NumberOfIterationsDone property</summary>
		private int numberOfIterationsDone;
		/// <summary>Field backing the ThetaPrior property</summary>
		private Dirichlet thetaPrior;
		/// <summary>Field backing the PhiPrior property</summary>
		private Dirichlet[] phiPrior;
		/// <summary>The number of iterations last computed by Changed_ThetaPrior_PhiPrior. Set this to zero to force re-execution of Changed_ThetaPrior_PhiPrior</summary>
		public int Changed_ThetaPrior_PhiPrior_iterationsDone;
		/// <summary>The number of iterations last computed by Changed_numberOfIterationsDecreased_ThetaPrior_PhiPrior. Set this to zero to force re-execution of Changed_numberOfIterationsDecreased_ThetaPrior_PhiPrior</summary>
		public int Changed_numberOfIterationsDecreased_ThetaPrior_PhiPrior_iterationsDone;
		/// <summary>The number of iterations last computed by Changed_ThetaPrior_Init_numberOfIterationsDecreased_PhiPrior. Set this to zero to force re-execution of Changed_ThetaPrior_Init_numberOfIterationsDecreased_PhiPrior</summary>
		public int Changed_ThetaPrior_Init_numberOfIterationsDecreased_PhiPrior_iterationsDone;
		/// <summary>True if Changed_ThetaPrior_Init_numberOfIterationsDecreased_PhiPrior has performed initialisation. Set this to false to force re-execution of Changed_ThetaPrior_Init_numberOfIterationsDecreased_PhiPrior</summary>
		public bool Changed_ThetaPrior_Init_numberOfIterationsDecreased_PhiPrior_isInitialised;
		/// <summary>The number of iterations last computed by Constant. Set this to zero to force re-execution of Constant</summary>
		public int Constant_iterationsDone;
		/// <summary>The number of iterations last computed by Changed_PhiPrior_Init_numberOfIterationsDecreased_ThetaPrior. Set this to zero to force re-execution of Changed_PhiPrior_Init_numberOfIterationsDecreased_ThetaPrior</summary>
		public int Changed_PhiPrior_Init_numberOfIterationsDecreased_ThetaPrior_iterationsDone;
		/// <summary>True if Changed_PhiPrior_Init_numberOfIterationsDecreased_ThetaPrior has performed initialisation. Set this to false to force re-execution of Changed_PhiPrior_Init_numberOfIterationsDecreased_ThetaPrior</summary>
		public bool Changed_PhiPrior_Init_numberOfIterationsDecreased_ThetaPrior_isInitialised;
		/// <summary>Message from use of 'Phi'</summary>
		public DistributionRefArray<Dirichlet,Vector> Phi_use_B;
		/// <summary>Message from use of 'vVector12'</summary>
		public Dirichlet vVector12_use_B;
		/// <summary>Message to marginal of 'vVector12'</summary>
		public Dirichlet vVector12_marginal_F;
		/// <summary>Message to marginal of 'Phi'</summary>
		public DistributionRefArray<Dirichlet,Vector> Phi_marginal_F;
		/// <summary>Message to marginal of 'topic'</summary>
		public Discrete topic_marginal_F;
		public Discrete Word_F;
		public PointMass<Dirichlet> ThetaPrior_marginal;
		public PointMass<Dirichlet[]> PhiPrior_marginal;
		#endregion

		#region Properties
		/// <summary>The number of iterations done from the initial state</summary>
		public int NumberOfIterationsDone
		{			get {
				return this.numberOfIterationsDone;
			}
		}

		/// <summary>The externally-specified value of 'ThetaPrior'</summary>
		public Dirichlet ThetaPrior
		{			get {
				return this.thetaPrior;
			}
			set {
				this.thetaPrior = value;
				this.numberOfIterationsDone = 0;
				this.Changed_PhiPrior_Init_numberOfIterationsDecreased_ThetaPrior_isInitialised = false;
				this.Changed_ThetaPrior_Init_numberOfIterationsDecreased_PhiPrior_iterationsDone = 0;
				this.Changed_numberOfIterationsDecreased_ThetaPrior_PhiPrior_iterationsDone = 0;
				this.Changed_ThetaPrior_PhiPrior_iterationsDone = 0;
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
				this.Changed_PhiPrior_Init_numberOfIterationsDecreased_ThetaPrior_iterationsDone = 0;
				this.Changed_ThetaPrior_Init_numberOfIterationsDecreased_PhiPrior_isInitialised = false;
				this.Changed_numberOfIterationsDecreased_ThetaPrior_PhiPrior_iterationsDone = 0;
				this.Changed_ThetaPrior_PhiPrior_iterationsDone = 0;
			}
		}

		#endregion

		#region Methods
		/// <summary>Get the observed value of the specified variable.</summary>
		/// <param name="variableName">Variable name</param>
		public object GetObservedValue(string variableName)
		{
			if (variableName=="ThetaPrior") {
				return this.ThetaPrior;
			}
			if (variableName=="PhiPrior") {
				return this.PhiPrior;
			}
			throw new ArgumentException("Not an observed variable name: "+variableName);
		}

		/// <summary>Set the observed value of the specified variable.</summary>
		/// <param name="variableName">Variable name</param>
		/// <param name="value">Observed value</param>
		public void SetObservedValue(string variableName, object value)
		{
			if (variableName=="ThetaPrior") {
				this.ThetaPrior = (Dirichlet)value;
				return ;
			}
			if (variableName=="PhiPrior") {
				this.PhiPrior = (Dirichlet[])value;
				return ;
			}
			throw new ArgumentException("Not an observed variable name: "+variableName);
		}

		/// <summary>The marginal distribution of the specified variable.</summary>
		/// <param name="variableName">Variable name</param>
		public object Marginal(string variableName)
		{
			if (variableName=="Phi") {
				return this.PhiMarginal();
			}
			if (variableName=="vVector12") {
				return this.VVector12Marginal();
			}
			if (variableName=="Word") {
				return this.WordMarginal();
			}
			if (variableName=="topic") {
				return this.TopicMarginal();
			}
			if (variableName=="ThetaPrior") {
				return this.ThetaPriorMarginal();
			}
			if (variableName=="PhiPrior") {
				return this.PhiPriorMarginal();
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
			if (numberOfIterations<this.Changed_numberOfIterationsDecreased_ThetaPrior_PhiPrior_iterationsDone) {
				this.Changed_PhiPrior_Init_numberOfIterationsDecreased_ThetaPrior_isInitialised = false;
				this.Changed_ThetaPrior_Init_numberOfIterationsDecreased_PhiPrior_isInitialised = false;
				this.Changed_numberOfIterationsDecreased_ThetaPrior_PhiPrior_iterationsDone = 0;
			}
			this.Constant();
			this.Changed_PhiPrior_Init_numberOfIterationsDecreased_ThetaPrior(initialise);
			this.Changed_ThetaPrior_Init_numberOfIterationsDecreased_PhiPrior(initialise);
			this.Changed_numberOfIterationsDecreased_ThetaPrior_PhiPrior(numberOfIterations);
			this.Changed_ThetaPrior_PhiPrior();
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

		/// <summary>Computations that do not depend on observed values</summary>
		public void Constant()
		{
			if (this.Constant_iterationsDone==1) {
				return ;
			}
			// Create array for 'Phi_use' Backwards messages.
			this.Phi_use_B = new DistributionRefArray<Dirichlet,Vector>(5);
			this.Constant_iterationsDone = 1;
			this.Changed_PhiPrior_Init_numberOfIterationsDecreased_ThetaPrior_iterationsDone = 0;
		}

		/// <summary>Computations that depend on the observed value of PhiPrior and must reset on changes to numberOfIterationsDecreased and ThetaPrior</summary>
		/// <param name="initialise">If true, reset messages that initialise loops</param>
		public void Changed_PhiPrior_Init_numberOfIterationsDecreased_ThetaPrior(bool initialise)
		{
			if ((this.Changed_PhiPrior_Init_numberOfIterationsDecreased_ThetaPrior_iterationsDone==1)&&((!initialise)||this.Changed_PhiPrior_Init_numberOfIterationsDecreased_ThetaPrior_isInitialised)) {
				return ;
			}
			for(int T = 0; T<5; T++) {
				this.Phi_use_B[T] = ArrayHelper.MakeUniform<Dirichlet>(this.phiPrior[T]);
			}
			this.Changed_PhiPrior_Init_numberOfIterationsDecreased_ThetaPrior_iterationsDone = 1;
			this.Changed_PhiPrior_Init_numberOfIterationsDecreased_ThetaPrior_isInitialised = true;
			this.Changed_numberOfIterationsDecreased_ThetaPrior_PhiPrior_iterationsDone = 0;
		}

		/// <summary>Computations that depend on the observed value of ThetaPrior and must reset on changes to numberOfIterationsDecreased and PhiPrior</summary>
		/// <param name="initialise">If true, reset messages that initialise loops</param>
		public void Changed_ThetaPrior_Init_numberOfIterationsDecreased_PhiPrior(bool initialise)
		{
			if ((this.Changed_ThetaPrior_Init_numberOfIterationsDecreased_PhiPrior_iterationsDone==1)&&((!initialise)||this.Changed_ThetaPrior_Init_numberOfIterationsDecreased_PhiPrior_isInitialised)) {
				return ;
			}
			this.vVector12_use_B = ArrayHelper.MakeUniform<Dirichlet>(this.thetaPrior);
			this.Changed_ThetaPrior_Init_numberOfIterationsDecreased_PhiPrior_iterationsDone = 1;
			this.Changed_ThetaPrior_Init_numberOfIterationsDecreased_PhiPrior_isInitialised = true;
			this.Changed_numberOfIterationsDecreased_ThetaPrior_PhiPrior_iterationsDone = 0;
		}

		/// <summary>Computations that depend on the observed value of numberOfIterationsDecreased and ThetaPrior and PhiPrior</summary>
		/// <param name="numberOfIterations">The number of times to iterate each loop</param>
		public void Changed_numberOfIterationsDecreased_ThetaPrior_PhiPrior(int numberOfIterations)
		{
			if (this.Changed_numberOfIterationsDecreased_ThetaPrior_PhiPrior_iterationsDone==numberOfIterations) {
				return ;
			}
			this.vVector12_marginal_F = ArrayHelper.MakeUniform<Dirichlet>(this.thetaPrior);
			Discrete topic_F = ArrayHelper.MakeUniform<Discrete>(Discrete.Uniform(this.thetaPrior.Dimension));
			Discrete[] topic_selector_uses_B = default(Discrete[]);
			// Create array for 'topic_selector_uses' Backwards messages.
			topic_selector_uses_B = new Discrete[2];
			for(int _ind = 0; _ind<2; _ind++) {
				topic_selector_uses_B[_ind] = ArrayHelper.MakeUniform<Discrete>(Discrete.Uniform(this.thetaPrior.Dimension));
			}
			DistributionStructArray<Bernoulli,bool>[] topic_selector_cases_uses_B = default(DistributionStructArray<Bernoulli,bool>[]);
			// Create array for 'topic_selector_cases_uses' Backwards messages.
			topic_selector_cases_uses_B = new DistributionStructArray<Bernoulli,bool>[2];
			for(int _ind = 0; _ind<2; _ind++) {
				// Create array for 'topic_selector_cases_uses' Backwards messages.
				topic_selector_cases_uses_B[_ind] = new DistributionStructArray<Bernoulli,bool>(5);
				for(int _iv = 0; _iv<5; _iv++) {
					topic_selector_cases_uses_B[_ind][_iv] = Bernoulli.Uniform();
				}
			}
			DistributionStructArray<Bernoulli,bool>[] topic_selector_cases_depth1_uses_B = default(DistributionStructArray<Bernoulli,bool>[]);
			// Create array for 'topic_selector_cases_depth1_uses' Backwards messages.
			topic_selector_cases_depth1_uses_B = new DistributionStructArray<Bernoulli,bool>[5];
			for(int _iv = 0; _iv<5; _iv++) {
				// Create array for 'topic_selector_cases_depth1_uses' Backwards messages.
				topic_selector_cases_depth1_uses_B[_iv] = new DistributionStructArray<Bernoulli,bool>(8);
				for(int _ind = 0; _ind<8; _ind++) {
					topic_selector_cases_depth1_uses_B[_iv][_ind] = Bernoulli.Uniform();
				}
			}
			// Create array for replicates of 'Word_cond_topic_T_F'
			DistributionRefArray<Discrete,int> Word_cond_topic_T_F = new DistributionRefArray<Discrete,int>(5);
			for(int T = 0; T<5; T++) {
				Word_cond_topic_T_F[T] = ArrayHelper.MakeUniform<Discrete>(Discrete.Uniform(this.phiPrior[0].Dimension, Sparsity.FromSpec(MicrosoftResearch.Infer.Maths.StorageType.Sparse, 0.0, 0)));
			}
			// Message from use of 'Word'
			Discrete Word_use_B = ArrayHelper.MakeUniform<Discrete>(Discrete.Uniform(this.phiPrior[0].Dimension, Sparsity.FromSpec(MicrosoftResearch.Infer.Maths.StorageType.Sparse, 0.0, 0)));
			// Message to marginal of 'Word_cond_topic_T'
			// Create array for replicates of 'Word_cond_topic_T_marginal_F'
			DistributionRefArray<Discrete,int> Word_cond_topic_T_marginal_F = new DistributionRefArray<Discrete,int>(5);
			for(int T = 0; T<5; T++) {
				Word_cond_topic_T_marginal_F[T] = ArrayHelper.MakeUniform<Discrete>(Discrete.Uniform(this.phiPrior[0].Dimension, Sparsity.FromSpec(MicrosoftResearch.Infer.Maths.StorageType.Sparse, 0.0, 0)));
			}
			// Create array for replicates of 'Phi_T_cond_topic_B'
			DistributionRefArray<Dirichlet,Vector> Phi_T_cond_topic_B = new DistributionRefArray<Dirichlet,Vector>(5);
			for(int T = 0; T<5; T++) {
				Phi_T_cond_topic_B[T] = ArrayHelper.MakeUniform<Dirichlet>(this.phiPrior[T]);
			}
			// Create array for 'Phi_marginal' Forwards messages.
			this.Phi_marginal_F = new DistributionRefArray<Dirichlet,Vector>(5);
			for(int T = 0; T<5; T++) {
				this.Phi_marginal_F[T] = ArrayHelper.MakeUniform<Dirichlet>(this.phiPrior[T]);
			}
			DistributionStructArray<Bernoulli,bool> topic_selector_cases_depth1_B = default(DistributionStructArray<Bernoulli,bool>);
			// Create array for 'topic_selector_cases_depth1' Backwards messages.
			topic_selector_cases_depth1_B = new DistributionStructArray<Bernoulli,bool>(5);
			for(int _iv = 0; _iv<5; _iv++) {
				topic_selector_cases_depth1_B[_iv] = Bernoulli.Uniform();
			}
			DistributionStructArray<Bernoulli,bool> topic_selector_cases_B = default(DistributionStructArray<Bernoulli,bool>);
			// Create array for 'topic_selector_cases' Backwards messages.
			topic_selector_cases_B = new DistributionStructArray<Bernoulli,bool>(5);
			for(int _iv = 0; _iv<5; _iv++) {
				topic_selector_cases_B[_iv] = Bernoulli.Uniform();
			}
			Discrete topic_selector_B = ArrayHelper.MakeUniform<Discrete>(Discrete.Uniform(this.thetaPrior.Dimension));
			this.topic_marginal_F = ArrayHelper.MakeUniform<Discrete>(Discrete.Uniform(this.thetaPrior.Dimension));
			for(int iteration = this.Changed_numberOfIterationsDecreased_ThetaPrior_PhiPrior_iterationsDone; iteration<numberOfIterations; iteration++) {
				// Message to 'vVector12_marginal' from Variable factor
				this.vVector12_marginal_F = VariableVmpOp.MarginalAverageLogarithm<Dirichlet>(this.vVector12_use_B, this.thetaPrior, this.vVector12_marginal_F);
				for(int T = 0; T<5; T++) {
					// Message to 'Phi_marginal' from Variable factor
					this.Phi_marginal_F[T] = VariableVmpOp.MarginalAverageLogarithm<Dirichlet>(this.Phi_use_B[T], this.phiPrior[T], this.Phi_marginal_F[T]);
					// Message to 'Word_cond_topic_T' from Discrete factor
					Word_cond_topic_T_F[T] = DiscreteFromDirichletOp.SampleAverageLogarithm(this.Phi_marginal_F[T], Word_cond_topic_T_F[T]);
					// Message to 'Word_cond_topic_T_marginal' from Variable factor
					Word_cond_topic_T_marginal_F[T] = VariableVmpOp.MarginalAverageLogarithm<Discrete>(Word_use_B, Word_cond_topic_T_F[T], Word_cond_topic_T_marginal_F[T]);
					// Message to 'topic_selector_cases_depth1_uses' from Variable factor
					topic_selector_cases_depth1_uses_B[T][6] = Bernoulli.FromLogOdds(VariableVmpOp.AverageLogFactor<Discrete>(Word_cond_topic_T_marginal_F[T]));
					// Message to 'topic_selector_cases_depth1_uses' from Discrete factor
					topic_selector_cases_depth1_uses_B[T][5] = Bernoulli.FromLogOdds(DiscreteFromDirichletOp.AverageLogFactor(Word_cond_topic_T_marginal_F[T], this.Phi_marginal_F[T]));
				}
				for(int _iv = 0; _iv<5; _iv++) {
					// Message to 'topic_selector_cases_depth1' from Replicate factor
					topic_selector_cases_depth1_B[_iv] = ReplicateOp.DefAverageLogarithm<Bernoulli>(topic_selector_cases_depth1_uses_B[_iv], topic_selector_cases_depth1_B[_iv]);
					// Message to 'topic_selector_cases_uses' from Copy factor
					topic_selector_cases_uses_B[0][_iv] = ArrayHelper.SetTo<Bernoulli>(topic_selector_cases_uses_B[0][_iv], topic_selector_cases_depth1_B[_iv]);
				}
				// Message to 'topic_selector_cases' from Replicate factor
				topic_selector_cases_B = ReplicateOp.DefAverageLogarithm<DistributionStructArray<Bernoulli,bool>>(topic_selector_cases_uses_B, topic_selector_cases_B);
				// Message to 'topic_selector_uses' from CasesInt factor
				topic_selector_uses_B[0] = IntCasesOp.IAverageLogarithm(topic_selector_cases_B, topic_selector_uses_B[0]);
				// Message to 'topic_selector' from Replicate factor
				topic_selector_B = ReplicateOp.DefAverageLogarithm<Discrete>(topic_selector_uses_B, topic_selector_B);
				// Message to 'topic' from Discrete factor
				topic_F = DiscreteFromDirichletOp.SampleAverageLogarithm(this.vVector12_marginal_F, topic_F);
				// Message to 'topic_marginal' from Variable factor
				this.topic_marginal_F = VariableVmpOp.MarginalAverageLogarithm<Discrete>(topic_selector_B, topic_F, this.topic_marginal_F);
				// Message to 'vVector12_use' from Discrete factor
				this.vVector12_use_B = DiscreteFromDirichletOp.ProbsAverageLogarithm(this.topic_marginal_F, this.vVector12_use_B);
				for(int T = 0; T<5; T++) {
					// Message to 'Phi_T_cond_topic' from Discrete factor
					Phi_T_cond_topic_B[T] = DiscreteFromDirichletOp.ProbsAverageLogarithm(Word_cond_topic_T_marginal_F[T], Phi_T_cond_topic_B[T]);
					// Message to 'Phi_use' from EnterOne factor
					this.Phi_use_B[T] = GateEnterOneOp<Vector>.ValueAverageLogarithm<Dirichlet>(Phi_T_cond_topic_B[T], this.topic_marginal_F, T, this.Phi_use_B[T]);
				}
				this.OnProgressChanged(new ProgressChangedEventArgs(iteration));
			}
			// Message to 'vVector12_marginal' from Variable factor
			this.vVector12_marginal_F = VariableVmpOp.MarginalAverageLogarithm<Dirichlet>(this.vVector12_use_B, this.thetaPrior, this.vVector12_marginal_F);
			for(int T = 0; T<5; T++) {
				// Message to 'Phi_marginal' from Variable factor
				this.Phi_marginal_F[T] = VariableVmpOp.MarginalAverageLogarithm<Dirichlet>(this.Phi_use_B[T], this.phiPrior[T], this.Phi_marginal_F[T]);
			}
			this.Word_F = ArrayHelper.MakeUniform<Discrete>(Discrete.Uniform(this.phiPrior[0].Dimension, Sparsity.FromSpec(MicrosoftResearch.Infer.Maths.StorageType.Sparse, 0.0, 0)));
			DistributionStructArray<Bernoulli,bool> topic_selector_cases_F = default(DistributionStructArray<Bernoulli,bool>);
			// Create array for 'topic_selector_cases' Forwards messages.
			topic_selector_cases_F = new DistributionStructArray<Bernoulli,bool>(5);
			for(int _iv = 0; _iv<5; _iv++) {
				topic_selector_cases_F[_iv] = Bernoulli.Uniform();
			}
			// Message to 'topic_selector_cases' from CasesInt factor
			topic_selector_cases_F = IntCasesOp.CasesAverageLogarithm<DistributionStructArray<Bernoulli,bool>>(this.topic_marginal_F, topic_selector_cases_F);
			// Message to 'Word' from Exit factor
			this.Word_F = GateExitOp<int>.ExitAverageLogarithm<Discrete>(topic_selector_cases_F, Word_cond_topic_T_marginal_F, this.Word_F);
			this.Changed_numberOfIterationsDecreased_ThetaPrior_PhiPrior_iterationsDone = numberOfIterations;
		}

		/// <summary>
		/// Returns the marginal distribution for 'Phi' given by the current state of the
		/// message passing algorithm.
		/// </summary>
		/// <returns>The marginal distribution</returns>
		public DistributionRefArray<Dirichlet,Vector> PhiMarginal()
		{
			return this.Phi_marginal_F;
		}

		/// <summary>
		/// Returns the marginal distribution for 'vVector12' given by the current state of the
		/// message passing algorithm.
		/// </summary>
		/// <returns>The marginal distribution</returns>
		public Dirichlet VVector12Marginal()
		{
			return this.vVector12_marginal_F;
		}

		/// <summary>
		/// Returns the marginal distribution for 'Word' given by the current state of the
		/// message passing algorithm.
		/// </summary>
		/// <returns>The marginal distribution</returns>
		public Discrete WordMarginal()
		{
			return this.Word_F;
		}

		/// <summary>
		/// Returns the marginal distribution for 'topic' given by the current state of the
		/// message passing algorithm.
		/// </summary>
		/// <returns>The marginal distribution</returns>
		public Discrete TopicMarginal()
		{
			return this.topic_marginal_F;
		}

		/// <summary>Computations that depend on the observed value of ThetaPrior and PhiPrior</summary>
		public void Changed_ThetaPrior_PhiPrior()
		{
			if (this.Changed_ThetaPrior_PhiPrior_iterationsDone==1) {
				return ;
			}
			this.ThetaPrior_marginal = new PointMass<Dirichlet>(this.thetaPrior);
			this.PhiPrior_marginal = new PointMass<Dirichlet[]>(this.phiPrior);
			this.Changed_ThetaPrior_PhiPrior_iterationsDone = 1;
		}

		/// <summary>
		/// Returns the marginal distribution for 'ThetaPrior' given by the current state of the
		/// message passing algorithm.
		/// </summary>
		/// <returns>The marginal distribution</returns>
		public PointMass<Dirichlet> ThetaPriorMarginal()
		{
			return this.ThetaPrior_marginal;
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

		#endregion

		#region Events
		/// <summary>Event that is fired when the progress of inference changes, typically at the end of one iteration of the inference algorithm.</summary>
		public event EventHandler<ProgressChangedEventArgs> ProgressChanged;
		#endregion

	}

}
