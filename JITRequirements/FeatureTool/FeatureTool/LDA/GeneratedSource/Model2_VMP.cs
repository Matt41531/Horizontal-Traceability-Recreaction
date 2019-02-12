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
	/// Generated by Infer.NET 2.5 at 10:01 on dinsdag 17 september 2013.
	/// </remarks>
	public partial class Model2_VMP : IGeneratedAlgorithm
	{
		#region Fields
		/// <summary>Field backing the NumberOfIterationsDone property</summary>
		private int numberOfIterationsDone;
		/// <summary>Field backing the NumWordsInDoc property</summary>
		private int numWordsInDoc;
		/// <summary>Field backing the ThetaPrior property</summary>
		private Dirichlet thetaPrior;
		/// <summary>Field backing the PhiPrior property</summary>
		private Dirichlet[] phiPrior;
		/// <summary>Field backing the Words property</summary>
		private int[] words;
		/// <summary>Field backing the WordCounts property</summary>
		private double[] wordCounts;
		/// <summary>The number of iterations last computed by Changed_NumWordsInDoc_ThetaPrior_PhiPrior_Words_WordCounts. Set this to zero to force re-execution of Changed_NumWordsInDoc_ThetaPrior_PhiPrior_Words_WordCounts</summary>
		public int Changed_NumWordsInDoc_ThetaPrior_PhiPrior_Words_WordCounts_iterationsDone;
		/// <summary>The number of iterations last computed by Changed_numberOfIterationsDecreased_ThetaPrior_NumWordsInDoc_WordCounts_PhiPrior_Words. Set this to zero to force re-execution of Changed_numberOfIterationsDecreased_ThetaPrior_NumWordsInDoc_WordCounts_PhiPrior_Words</summary>
		public int Changed_numberOfIterationsDecreased_ThetaPrior_NumWordsInDoc_WordCounts_PhiPrior_Words_iterationsDone;
		/// <summary>The number of iterations last computed by Changed_ThetaPrior_Init_numberOfIterationsDecreased_NumWordsInDoc_WordCounts_PhiPrior_Words. Set this to zero to force re-execution of Changed_ThetaPrior_Init_numberOfIterationsDecreased_NumWordsInDoc_WordCounts_PhiPrior_Words</summary>
		public int Changed_ThetaPrior_Init_numberOfIterationsDecreased_NumWordsInDoc_WordCounts_PhiPrior_Words_iterationsDone;
		/// <summary>True if Changed_ThetaPrior_Init_numberOfIterationsDecreased_NumWordsInDoc_WordCounts_PhiPrior_Words has performed initialisation. Set this to false to force re-execution of Changed_ThetaPrior_Init_numberOfIterationsDecreased_NumWordsInDoc_WordCounts_PhiPrior_Words</summary>
		public bool Changed_ThetaPrior_Init_numberOfIterationsDecreased_NumWordsInDoc_WordCounts_PhiPrior_Words_isInitialised;
		/// <summary>The number of iterations last computed by Constant. Set this to zero to force re-execution of Constant</summary>
		public int Constant_iterationsDone;
		/// <summary>The number of iterations last computed by Changed_PhiPrior_Init_numberOfIterationsDecreased_ThetaPrior_NumWordsInDoc_WordCounts_Words. Set this to zero to force re-execution of Changed_PhiPrior_Init_numberOfIterationsDecreased_ThetaPrior_NumWordsInDoc_WordCounts_Words</summary>
		public int Changed_PhiPrior_Init_numberOfIterationsDecreased_ThetaPrior_NumWordsInDoc_WordCounts_Words_iterationsDone;
		/// <summary>True if Changed_PhiPrior_Init_numberOfIterationsDecreased_ThetaPrior_NumWordsInDoc_WordCounts_Words has performed initialisation. Set this to false to force re-execution of Changed_PhiPrior_Init_numberOfIterationsDecreased_ThetaPrior_NumWordsInDoc_WordCounts_Words</summary>
		public bool Changed_PhiPrior_Init_numberOfIterationsDecreased_ThetaPrior_NumWordsInDoc_WordCounts_Words_isInitialised;
		/// <summary>Message from use of 'Phi'</summary>
		public DistributionRefArray<Dirichlet,Vector> Phi_use_B;
		/// <summary>Message from use of 'vVector41'</summary>
		public Dirichlet vVector41_use_B;
		/// <summary>Message to marginal of 'vVector41'</summary>
		public Dirichlet vVector41_marginal_F;
		/// <summary>Message to marginal of 'Phi'</summary>
		public DistributionRefArray<Dirichlet,Vector> Phi_marginal_F;
		public PointMass<int> NumWordsInDoc_marginal;
		public PointMass<Dirichlet> ThetaPrior_marginal;
		public PointMass<Dirichlet[]> PhiPrior_marginal;
		public DistributionRefArray<Discrete,int> Words_marginal;
		public DistributionStructArray<Gaussian,double> WordCounts_marginal;
		#endregion

		#region Properties
		/// <summary>The number of iterations done from the initial state</summary>
		public int NumberOfIterationsDone
		{			get {
				return this.numberOfIterationsDone;
			}
		}

		/// <summary>The externally-specified value of 'NumWordsInDoc'</summary>
		public int NumWordsInDoc
		{			get {
				return this.numWordsInDoc;
			}
			set {
				if (this.numWordsInDoc!=value) {
					this.numWordsInDoc = value;
					this.numberOfIterationsDone = 0;
					this.Changed_PhiPrior_Init_numberOfIterationsDecreased_ThetaPrior_NumWordsInDoc_WordCounts_Words_isInitialised = false;
					this.Changed_ThetaPrior_Init_numberOfIterationsDecreased_NumWordsInDoc_WordCounts_PhiPrior_Words_isInitialised = false;
					this.Changed_numberOfIterationsDecreased_ThetaPrior_NumWordsInDoc_WordCounts_PhiPrior_Words_iterationsDone = 0;
					this.Changed_NumWordsInDoc_ThetaPrior_PhiPrior_Words_WordCounts_iterationsDone = 0;
				}
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
				this.Changed_PhiPrior_Init_numberOfIterationsDecreased_ThetaPrior_NumWordsInDoc_WordCounts_Words_isInitialised = false;
				this.Changed_ThetaPrior_Init_numberOfIterationsDecreased_NumWordsInDoc_WordCounts_PhiPrior_Words_iterationsDone = 0;
				this.Changed_numberOfIterationsDecreased_ThetaPrior_NumWordsInDoc_WordCounts_PhiPrior_Words_iterationsDone = 0;
				this.Changed_NumWordsInDoc_ThetaPrior_PhiPrior_Words_WordCounts_iterationsDone = 0;
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
				this.Changed_PhiPrior_Init_numberOfIterationsDecreased_ThetaPrior_NumWordsInDoc_WordCounts_Words_iterationsDone = 0;
				this.Changed_ThetaPrior_Init_numberOfIterationsDecreased_NumWordsInDoc_WordCounts_PhiPrior_Words_isInitialised = false;
				this.Changed_numberOfIterationsDecreased_ThetaPrior_NumWordsInDoc_WordCounts_PhiPrior_Words_iterationsDone = 0;
				this.Changed_NumWordsInDoc_ThetaPrior_PhiPrior_Words_WordCounts_iterationsDone = 0;
			}
		}

		/// <summary>The externally-specified value of 'Words'</summary>
		public int[] Words
		{			get {
				return this.words;
			}
			set {
				if ((value!=null)&&(value.Length!=this.numWordsInDoc)) {
					throw new ArgumentException(((("Provided array of length "+value.Length)+" when length ")+this.numWordsInDoc)+" was expected for variable \'Words\'");
				}
				this.words = value;
				this.numberOfIterationsDone = 0;
				this.Changed_PhiPrior_Init_numberOfIterationsDecreased_ThetaPrior_NumWordsInDoc_WordCounts_Words_isInitialised = false;
				this.Changed_ThetaPrior_Init_numberOfIterationsDecreased_NumWordsInDoc_WordCounts_PhiPrior_Words_isInitialised = false;
				this.Changed_numberOfIterationsDecreased_ThetaPrior_NumWordsInDoc_WordCounts_PhiPrior_Words_iterationsDone = 0;
				this.Changed_NumWordsInDoc_ThetaPrior_PhiPrior_Words_WordCounts_iterationsDone = 0;
			}
		}

		/// <summary>The externally-specified value of 'WordCounts'</summary>
		public double[] WordCounts
		{			get {
				return this.wordCounts;
			}
			set {
				if ((value!=null)&&(value.Length!=this.numWordsInDoc)) {
					throw new ArgumentException(((("Provided array of length "+value.Length)+" when length ")+this.numWordsInDoc)+" was expected for variable \'WordCounts\'");
				}
				this.wordCounts = value;
				this.numberOfIterationsDone = 0;
				this.Changed_PhiPrior_Init_numberOfIterationsDecreased_ThetaPrior_NumWordsInDoc_WordCounts_Words_isInitialised = false;
				this.Changed_ThetaPrior_Init_numberOfIterationsDecreased_NumWordsInDoc_WordCounts_PhiPrior_Words_isInitialised = false;
				this.Changed_numberOfIterationsDecreased_ThetaPrior_NumWordsInDoc_WordCounts_PhiPrior_Words_iterationsDone = 0;
				this.Changed_NumWordsInDoc_ThetaPrior_PhiPrior_Words_WordCounts_iterationsDone = 0;
			}
		}

		#endregion

		#region Methods
		/// <summary>Get the observed value of the specified variable.</summary>
		/// <param name="variableName">Variable name</param>
		public object GetObservedValue(string variableName)
		{
			if (variableName=="NumWordsInDoc") {
				return this.NumWordsInDoc;
			}
			if (variableName=="ThetaPrior") {
				return this.ThetaPrior;
			}
			if (variableName=="PhiPrior") {
				return this.PhiPrior;
			}
			if (variableName=="Words") {
				return this.Words;
			}
			if (variableName=="WordCounts") {
				return this.WordCounts;
			}
			throw new ArgumentException("Not an observed variable name: "+variableName);
		}

		/// <summary>Set the observed value of the specified variable.</summary>
		/// <param name="variableName">Variable name</param>
		/// <param name="value">Observed value</param>
		public void SetObservedValue(string variableName, object value)
		{
			if (variableName=="NumWordsInDoc") {
				this.NumWordsInDoc = (int)value;
				return ;
			}
			if (variableName=="ThetaPrior") {
				this.ThetaPrior = (Dirichlet)value;
				return ;
			}
			if (variableName=="PhiPrior") {
				this.PhiPrior = (Dirichlet[])value;
				return ;
			}
			if (variableName=="Words") {
				this.Words = (int[])value;
				return ;
			}
			if (variableName=="WordCounts") {
				this.WordCounts = (double[])value;
				return ;
			}
			throw new ArgumentException("Not an observed variable name: "+variableName);
		}

		/// <summary>The marginal distribution of the specified variable.</summary>
		/// <param name="variableName">Variable name</param>
		public object Marginal(string variableName)
		{
			if (variableName=="vVector41") {
				return this.VVector41Marginal();
			}
			if (variableName=="Phi") {
				return this.PhiMarginal();
			}
			if (variableName=="NumWordsInDoc") {
				return this.NumWordsInDocMarginal();
			}
			if (variableName=="ThetaPrior") {
				return this.ThetaPriorMarginal();
			}
			if (variableName=="PhiPrior") {
				return this.PhiPriorMarginal();
			}
			if (variableName=="Words") {
				return this.WordsMarginal();
			}
			if (variableName=="WordCounts") {
				return this.WordCountsMarginal();
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
			if (numberOfIterations<this.Changed_numberOfIterationsDecreased_ThetaPrior_NumWordsInDoc_WordCounts_PhiPrior_Words_iterationsDone) {
				this.Changed_PhiPrior_Init_numberOfIterationsDecreased_ThetaPrior_NumWordsInDoc_WordCounts_Words_isInitialised = false;
				this.Changed_ThetaPrior_Init_numberOfIterationsDecreased_NumWordsInDoc_WordCounts_PhiPrior_Words_isInitialised = false;
				this.Changed_numberOfIterationsDecreased_ThetaPrior_NumWordsInDoc_WordCounts_PhiPrior_Words_iterationsDone = 0;
			}
			this.Constant();
			this.Changed_PhiPrior_Init_numberOfIterationsDecreased_ThetaPrior_NumWordsInDoc_WordCounts_Words(initialise);
			this.Changed_ThetaPrior_Init_numberOfIterationsDecreased_NumWordsInDoc_WordCounts_PhiPrior_Words(initialise);
			this.Changed_numberOfIterationsDecreased_ThetaPrior_NumWordsInDoc_WordCounts_PhiPrior_Words(numberOfIterations);
			this.Changed_NumWordsInDoc_ThetaPrior_PhiPrior_Words_WordCounts();
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
			this.Changed_PhiPrior_Init_numberOfIterationsDecreased_ThetaPrior_NumWordsInDoc_WordCounts_Words_iterationsDone = 0;
		}

		/// <summary>Computations that depend on the observed value of PhiPrior and must reset on changes to numberOfIterationsDecreased and ThetaPrior and NumWordsInDoc and WordCounts and Words</summary>
		/// <param name="initialise">If true, reset messages that initialise loops</param>
		public void Changed_PhiPrior_Init_numberOfIterationsDecreased_ThetaPrior_NumWordsInDoc_WordCounts_Words(bool initialise)
		{
			if ((this.Changed_PhiPrior_Init_numberOfIterationsDecreased_ThetaPrior_NumWordsInDoc_WordCounts_Words_iterationsDone==1)&&((!initialise)||this.Changed_PhiPrior_Init_numberOfIterationsDecreased_ThetaPrior_NumWordsInDoc_WordCounts_Words_isInitialised)) {
				return ;
			}
			for(int T = 0; T<5; T++) {
				this.Phi_use_B[T] = ArrayHelper.MakeUniform<Dirichlet>(this.phiPrior[T]);
			}
			this.Changed_PhiPrior_Init_numberOfIterationsDecreased_ThetaPrior_NumWordsInDoc_WordCounts_Words_iterationsDone = 1;
			this.Changed_PhiPrior_Init_numberOfIterationsDecreased_ThetaPrior_NumWordsInDoc_WordCounts_Words_isInitialised = true;
			this.Changed_numberOfIterationsDecreased_ThetaPrior_NumWordsInDoc_WordCounts_PhiPrior_Words_iterationsDone = 0;
		}

		/// <summary>Computations that depend on the observed value of ThetaPrior and must reset on changes to numberOfIterationsDecreased and NumWordsInDoc and WordCounts and PhiPrior and Words</summary>
		/// <param name="initialise">If true, reset messages that initialise loops</param>
		public void Changed_ThetaPrior_Init_numberOfIterationsDecreased_NumWordsInDoc_WordCounts_PhiPrior_Words(bool initialise)
		{
			if ((this.Changed_ThetaPrior_Init_numberOfIterationsDecreased_NumWordsInDoc_WordCounts_PhiPrior_Words_iterationsDone==1)&&((!initialise)||this.Changed_ThetaPrior_Init_numberOfIterationsDecreased_NumWordsInDoc_WordCounts_PhiPrior_Words_isInitialised)) {
				return ;
			}
			this.vVector41_use_B = ArrayHelper.MakeUniform<Dirichlet>(this.thetaPrior);
			this.Changed_ThetaPrior_Init_numberOfIterationsDecreased_NumWordsInDoc_WordCounts_PhiPrior_Words_iterationsDone = 1;
			this.Changed_ThetaPrior_Init_numberOfIterationsDecreased_NumWordsInDoc_WordCounts_PhiPrior_Words_isInitialised = true;
			this.Changed_numberOfIterationsDecreased_ThetaPrior_NumWordsInDoc_WordCounts_PhiPrior_Words_iterationsDone = 0;
		}

		/// <summary>Computations that depend on the observed value of numberOfIterationsDecreased and ThetaPrior and NumWordsInDoc and WordCounts and PhiPrior and Words</summary>
		/// <param name="numberOfIterations">The number of times to iterate each loop</param>
		public void Changed_numberOfIterationsDecreased_ThetaPrior_NumWordsInDoc_WordCounts_PhiPrior_Words(int numberOfIterations)
		{
			if (this.Changed_numberOfIterationsDecreased_ThetaPrior_NumWordsInDoc_WordCounts_PhiPrior_Words_iterationsDone==numberOfIterations) {
				return ;
			}
			this.vVector41_marginal_F = ArrayHelper.MakeUniform<Dirichlet>(this.thetaPrior);
			// Create array for replicates of 'topic_F'
			DistributionRefArray<Discrete,int> topic_F = new DistributionRefArray<Discrete,int>(this.numWordsInDoc);
			for(int WInD = 0; WInD<this.numWordsInDoc; WInD++) {
				topic_F[WInD] = ArrayHelper.MakeUniform<Discrete>(Discrete.Uniform(this.thetaPrior.Dimension));
			}
			Discrete _hoist = default(Discrete);
			for(int WInD = 0; WInD<this.numWordsInDoc; WInD++) {
				_hoist = ArrayHelper.CopyStorage<Discrete>(topic_F[WInD]);
				WInD = this.numWordsInDoc-1;
			}
			// Create array for replicates of 'topic_selector_uses_B'
			Discrete[][] topic_selector_uses_B = new Discrete[this.numWordsInDoc][];
			for(int WInD = 0; WInD<this.numWordsInDoc; WInD++) {
				// Create array for 'topic_selector_uses' Backwards messages.
				topic_selector_uses_B[WInD] = new Discrete[2];
				for(int _ind = 0; _ind<2; _ind++) {
					topic_selector_uses_B[WInD][_ind] = ArrayHelper.MakeUniform<Discrete>(Discrete.Uniform(this.thetaPrior.Dimension));
				}
			}
			// Create array for replicates of 'topic_selector_cases_uses_B'
			DistributionStructArray<Bernoulli,bool>[][] topic_selector_cases_uses_B = new DistributionStructArray<Bernoulli,bool>[this.numWordsInDoc][];
			for(int WInD = 0; WInD<this.numWordsInDoc; WInD++) {
				// Create array for 'topic_selector_cases_uses' Backwards messages.
				topic_selector_cases_uses_B[WInD] = new DistributionStructArray<Bernoulli,bool>[5];
				for(int _iv = 0; _iv<5; _iv++) {
					// Create array for 'topic_selector_cases_uses' Backwards messages.
					topic_selector_cases_uses_B[WInD][_iv] = new DistributionStructArray<Bernoulli,bool>(3);
					for(int _ind = 0; _ind<3; _ind++) {
						topic_selector_cases_uses_B[WInD][_iv][_ind] = Bernoulli.Uniform();
					}
				}
			}
			// Create array for replicates of 'Phi_T_cond_topic_B'
			DistributionRefArray<DistributionRefArray<Dirichlet,Vector>,Vector[]> Phi_T_cond_topic_B = new DistributionRefArray<DistributionRefArray<Dirichlet,Vector>,Vector[]>(this.numWordsInDoc);
			for(int WInD = 0; WInD<this.numWordsInDoc; WInD++) {
				// Create array for replicates of 'Phi_T_cond_topic_B'
				Phi_T_cond_topic_B[WInD] = new DistributionRefArray<Dirichlet,Vector>(5);
				for(int T = 0; T<5; T++) {
					Phi_T_cond_topic_B[WInD][T] = ArrayHelper.MakeUniform<Dirichlet>(this.phiPrior[T]);
				}
			}
			// Create array for replicates of '_hoist3'
			DistributionRefArray<Dirichlet,Vector> _hoist3 = new DistributionRefArray<Dirichlet,Vector>(this.numWordsInDoc);
			for(int WInD = 0; WInD<this.numWordsInDoc; WInD++) {
				for(int T = 0; T<5; T++) {
					_hoist3[WInD] = ArrayHelper.CopyStorage<Dirichlet>(Phi_T_cond_topic_B[WInD][T]);
					T = 5-1;
				}
				for(int T = 0; T<5; T++) {
					_hoist3[WInD] = DiscreteFromDirichletOp.ProbsAverageLogarithm(this.words[WInD], _hoist3[WInD]);
					T = 5-1;
				}
			}
			// Create array for replicates of 'Phi_rep_rpt0_B'
			DistributionRefArray<DistributionRefArray<Dirichlet,Vector>,Vector[]> Phi_rep_rpt0_B = new DistributionRefArray<DistributionRefArray<Dirichlet,Vector>,Vector[]>(this.numWordsInDoc);
			for(int WInD = 0; WInD<this.numWordsInDoc; WInD++) {
				// Create array for replicates of 'Phi_rep_rpt0_B'
				Phi_rep_rpt0_B[WInD] = new DistributionRefArray<Dirichlet,Vector>(5);
				for(int T = 0; T<5; T++) {
					Phi_rep_rpt0_B[WInD][T] = ArrayHelper.MakeUniform<Dirichlet>(this.phiPrior[T]);
				}
			}
			// Create array for replicates of 'Phi_rep_B'
			DistributionRefArray<DistributionRefArray<Dirichlet,Vector>,Vector[]> Phi_rep_B = new DistributionRefArray<DistributionRefArray<Dirichlet,Vector>,Vector[]>(5);
			for(int T = 0; T<5; T++) {
				// Create array for 'Phi_rep' Backwards messages.
				Phi_rep_B[T] = new DistributionRefArray<Dirichlet,Vector>(this.numWordsInDoc);
				for(int WInD = 0; WInD<this.numWordsInDoc; WInD++) {
					Phi_rep_B[T][WInD] = ArrayHelper.MakeUniform<Dirichlet>(this.phiPrior[T]);
				}
			}
			// Create array for 'Phi_marginal' Forwards messages.
			this.Phi_marginal_F = new DistributionRefArray<Dirichlet,Vector>(5);
			for(int T = 0; T<5; T++) {
				this.Phi_marginal_F[T] = ArrayHelper.MakeUniform<Dirichlet>(this.phiPrior[T]);
			}
			// Create array for replicates of 'topic_selector_cases_B'
			DistributionRefArray<DistributionStructArray<Bernoulli,bool>,bool[]> topic_selector_cases_B = new DistributionRefArray<DistributionStructArray<Bernoulli,bool>,bool[]>(this.numWordsInDoc);
			for(int WInD = 0; WInD<this.numWordsInDoc; WInD++) {
				// Create array for 'topic_selector_cases' Backwards messages.
				topic_selector_cases_B[WInD] = new DistributionStructArray<Bernoulli,bool>(5);
				for(int _iv = 0; _iv<5; _iv++) {
					topic_selector_cases_B[WInD][_iv] = Bernoulli.Uniform();
				}
			}
			// Create array for replicates of 'topic_selector_B'
			DistributionRefArray<Discrete,int> topic_selector_B = new DistributionRefArray<Discrete,int>(this.numWordsInDoc);
			for(int WInD = 0; WInD<this.numWordsInDoc; WInD++) {
				topic_selector_B[WInD] = ArrayHelper.MakeUniform<Discrete>(Discrete.Uniform(this.thetaPrior.Dimension));
			}
			// Message to marginal of 'topic'
			// Create array for replicates of 'topic_marginal_F'
			DistributionRefArray<Discrete,int> topic_marginal_F = new DistributionRefArray<Discrete,int>(this.numWordsInDoc);
			for(int WInD = 0; WInD<this.numWordsInDoc; WInD++) {
				topic_marginal_F[WInD] = ArrayHelper.MakeUniform<Discrete>(Discrete.Uniform(this.thetaPrior.Dimension));
			}
			// Create array for replicates of 'vVector41_rep_rpt0_B'
			DistributionRefArray<Dirichlet,Vector> vVector41_rep_rpt0_B = new DistributionRefArray<Dirichlet,Vector>(this.numWordsInDoc);
			for(int WInD = 0; WInD<this.numWordsInDoc; WInD++) {
				vVector41_rep_rpt0_B[WInD] = ArrayHelper.MakeUniform<Dirichlet>(this.thetaPrior);
			}
			DistributionRefArray<Dirichlet,Vector> vVector41_rep_B = default(DistributionRefArray<Dirichlet,Vector>);
			// Create array for 'vVector41_rep' Backwards messages.
			vVector41_rep_B = new DistributionRefArray<Dirichlet,Vector>(this.numWordsInDoc);
			for(int WInD = 0; WInD<this.numWordsInDoc; WInD++) {
				vVector41_rep_B[WInD] = ArrayHelper.MakeUniform<Dirichlet>(this.thetaPrior);
			}
			for(int T = 0; T<5; T++) {
				// Message to 'Phi_marginal' from Variable factor
				this.Phi_marginal_F[T] = VariableVmpOp.MarginalAverageLogarithm<Dirichlet>(this.Phi_use_B[T], this.phiPrior[T], this.Phi_marginal_F[T]);
				for(int WInD = 0; WInD<this.numWordsInDoc; WInD++) {
					// Message to 'topic_selector_cases_uses' from Discrete factor
					topic_selector_cases_uses_B[WInD][T][2] = Bernoulli.FromLogOdds(DiscreteFromDirichletOp.AverageLogFactor(this.words[WInD], this.Phi_marginal_F[T]));
				}
			}
			for(int WInD = 0; WInD<this.numWordsInDoc; WInD++) {
				for(int _iv = 0; _iv<5; _iv++) {
					// Message to 'topic_selector_cases' from Replicate factor
					topic_selector_cases_B[WInD][_iv] = ReplicateOp.DefAverageLogarithm<Bernoulli>(topic_selector_cases_uses_B[WInD][_iv], topic_selector_cases_B[WInD][_iv]);
				}
			}
			for(int iteration = this.Changed_numberOfIterationsDecreased_ThetaPrior_NumWordsInDoc_WordCounts_PhiPrior_Words_iterationsDone; iteration<numberOfIterations; iteration++) {
				// Message to 'vVector41_marginal' from Variable factor
				this.vVector41_marginal_F = VariableVmpOp.MarginalAverageLogarithm<Dirichlet>(this.vVector41_use_B, this.thetaPrior, this.vVector41_marginal_F);
				for(int WInD = 0; WInD<this.numWordsInDoc; WInD++) {
					// Message to 'topic_selector_uses' from CasesInt factor
					topic_selector_uses_B[WInD][0] = IntCasesOp.IAverageLogarithm(topic_selector_cases_B[WInD], topic_selector_uses_B[WInD][0]);
					// Message to 'topic_selector' from Replicate factor
					topic_selector_B[WInD] = ReplicateOp.DefAverageLogarithm<Discrete>(topic_selector_uses_B[WInD], topic_selector_B[WInD]);
				}
				for(int WInD = 0; WInD<this.numWordsInDoc; WInD++) {
					_hoist = DiscreteFromDirichletOp.SampleAverageLogarithm(this.vVector41_marginal_F, _hoist);
					WInD = this.numWordsInDoc-1;
				}
				for(int WInD = 0; WInD<this.numWordsInDoc; WInD++) {
					// Message to 'topic_marginal' from Variable factor
					topic_marginal_F[WInD] = VariableVmpOp.MarginalAverageLogarithm<Discrete>(topic_selector_B[WInD], _hoist, topic_marginal_F[WInD]);
					// Message to 'vVector41_rep_rpt0' from Discrete factor
					vVector41_rep_rpt0_B[WInD] = DiscreteFromDirichletOp.ProbsAverageLogarithm(topic_marginal_F[WInD], vVector41_rep_rpt0_B[WInD]);
					// Message to 'vVector41_rep' from Enter factor
					vVector41_rep_B[WInD] = PowerPlateOp.ValueAverageLogarithm<Dirichlet>(vVector41_rep_rpt0_B[WInD], this.wordCounts[WInD], vVector41_rep_B[WInD]);
				}
				// Message to 'vVector41_use' from Replicate factor
				this.vVector41_use_B = ReplicateOp.DefAverageLogarithm<Dirichlet>(vVector41_rep_B, this.vVector41_use_B);
				for(int WInD = 0; WInD<this.numWordsInDoc; WInD++) {
					for(int T = 0; T<5; T++) {
						// Message to 'Phi_rep_rpt0' from EnterOne factor
						Phi_rep_rpt0_B[WInD][T] = GateEnterOneOp<Vector>.ValueAverageLogarithm<Dirichlet>(_hoist3[WInD], topic_marginal_F[WInD], T, Phi_rep_rpt0_B[WInD][T]);
						// Message to 'Phi_rep' from Enter factor
						Phi_rep_B[T][WInD] = PowerPlateOp.ValueAverageLogarithm<Dirichlet>(Phi_rep_rpt0_B[WInD][T], this.wordCounts[WInD], Phi_rep_B[T][WInD]);
					}
				}
				for(int T = 0; T<5; T++) {
					// Message to 'Phi_use' from Replicate factor
					this.Phi_use_B[T] = ReplicateOp.DefAverageLogarithm<Dirichlet>(Phi_rep_B[T], this.Phi_use_B[T]);
					// Message to 'Phi_marginal' from Variable factor
					this.Phi_marginal_F[T] = VariableVmpOp.MarginalAverageLogarithm<Dirichlet>(this.Phi_use_B[T], this.phiPrior[T], this.Phi_marginal_F[T]);
					for(int WInD = 0; WInD<this.numWordsInDoc; WInD++) {
						// Message to 'topic_selector_cases_uses' from Discrete factor
						topic_selector_cases_uses_B[WInD][T][2] = Bernoulli.FromLogOdds(DiscreteFromDirichletOp.AverageLogFactor(this.words[WInD], this.Phi_marginal_F[T]));
					}
				}
				for(int WInD = 0; WInD<this.numWordsInDoc; WInD++) {
					for(int _iv = 0; _iv<5; _iv++) {
						// Message to 'topic_selector_cases' from Replicate factor
						topic_selector_cases_B[WInD][_iv] = ReplicateOp.DefAverageLogarithm<Bernoulli>(topic_selector_cases_uses_B[WInD][_iv], topic_selector_cases_B[WInD][_iv]);
					}
				}
				this.OnProgressChanged(new ProgressChangedEventArgs(iteration));
			}
			// Message to 'vVector41_marginal' from Variable factor
			this.vVector41_marginal_F = VariableVmpOp.MarginalAverageLogarithm<Dirichlet>(this.vVector41_use_B, this.thetaPrior, this.vVector41_marginal_F);
			this.Changed_numberOfIterationsDecreased_ThetaPrior_NumWordsInDoc_WordCounts_PhiPrior_Words_iterationsDone = numberOfIterations;
		}

		/// <summary>
		/// Returns the marginal distribution for 'vVector41' given by the current state of the
		/// message passing algorithm.
		/// </summary>
		/// <returns>The marginal distribution</returns>
		public Dirichlet VVector41Marginal()
		{
			return this.vVector41_marginal_F;
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

		/// <summary>Computations that depend on the observed value of NumWordsInDoc and ThetaPrior and PhiPrior and Words and WordCounts</summary>
		public void Changed_NumWordsInDoc_ThetaPrior_PhiPrior_Words_WordCounts()
		{
			if (this.Changed_NumWordsInDoc_ThetaPrior_PhiPrior_Words_WordCounts_iterationsDone==1) {
				return ;
			}
			this.NumWordsInDoc_marginal = new PointMass<int>(this.numWordsInDoc);
			this.ThetaPrior_marginal = new PointMass<Dirichlet>(this.thetaPrior);
			this.PhiPrior_marginal = new PointMass<Dirichlet[]>(this.phiPrior);
			this.Words_marginal = new DistributionRefArray<Discrete,int>(this.numWordsInDoc, delegate(int WInD) {
				return ArrayHelper.MakeUniform<Discrete>(Discrete.Uniform(this.phiPrior[0].Dimension));
			});
			this.Words_marginal = Distribution.SetPoint<DistributionRefArray<Discrete,int>,int[]>(this.Words_marginal, this.words);
			this.WordCounts_marginal = new DistributionStructArray<Gaussian,double>(this.numWordsInDoc, delegate(int WInD) {
				return Gaussian.Uniform();
			});
			this.WordCounts_marginal = Distribution.SetPoint<DistributionStructArray<Gaussian,double>,double[]>(this.WordCounts_marginal, this.wordCounts);
			this.Changed_NumWordsInDoc_ThetaPrior_PhiPrior_Words_WordCounts_iterationsDone = 1;
		}

		/// <summary>
		/// Returns the marginal distribution for 'NumWordsInDoc' given by the current state of the
		/// message passing algorithm.
		/// </summary>
		/// <returns>The marginal distribution</returns>
		public PointMass<int> NumWordsInDocMarginal()
		{
			return this.NumWordsInDoc_marginal;
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

		/// <summary>
		/// Returns the marginal distribution for 'Words' given by the current state of the
		/// message passing algorithm.
		/// </summary>
		/// <returns>The marginal distribution</returns>
		public DistributionRefArray<Discrete,int> WordsMarginal()
		{
			return this.Words_marginal;
		}

		/// <summary>
		/// Returns the marginal distribution for 'WordCounts' given by the current state of the
		/// message passing algorithm.
		/// </summary>
		/// <returns>The marginal distribution</returns>
		public DistributionStructArray<Gaussian,double> WordCountsMarginal()
		{
			return this.WordCounts_marginal;
		}

		#endregion

		#region Events
		/// <summary>Event that is fired when the progress of inference changes, typically at the end of one iteration of the inference algorithm.</summary>
		public event EventHandler<ProgressChangedEventArgs> ProgressChanged;
		#endregion

	}

}
