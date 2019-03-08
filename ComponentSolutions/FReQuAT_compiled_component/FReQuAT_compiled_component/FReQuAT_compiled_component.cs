using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
//using System.Windows.Forms;
using System.Diagnostics;
using MicrosoftResearch.Infer.Distributions;
using MicrosoftResearch.Infer.Maths;

/* From PorterStreaming */
using System.Runtime.InteropServices;

/* From TFIDFMeasure */
using System.Collections;

/* From Utilities */
using System.IO;

/* From LSA */
using System.Reflection;

/* From BugzillaFeatureCollection */
using System.Xml.Linq;

/* From LDATopicsInferenceModel */
using MicrosoftResearch.Infer;
using MicrosoftResearch.Infer.Models;
using MicrosoftResearch.Infer.Factors;
using MicrosoftResearch.Infer.Collections;
// Located in c:\Program Files (x86)\COEST\TraceLab\lib\TraceLabSDK.dll
using TraceLabSDK;
using TraceLabSDK.Types;



namespace FReQuAT_compiled_component
{
    using GammaArray = DistributionStructArray<Gamma, double>;
    using DirichletArray = DistributionRefArray<Dirichlet, Vector>;
    using System.Diagnostics;

    [Component(Name = "FReQuAT Compiled",
           Description = "Feature Tool represented in TraceLab",
           Author = "Jen Lee & Matthew Rife: Based on contributions from Dr. Petra Heck and Andy Zaidman",
           Version = "1.0",
           ConfigurationType = typeof(FReQuAT_configuration)
        )]
    [IOSpec(IOType = IOSpecType.Output, Name = "outputName", DataType = typeof(TLArtifactsCollection))]
    //[IOSpec(IOType = IOSpecType.Output, Name = "outputName", DataType = typeof(int))]
    public class FReQuAT_compiled_component : BaseComponent
    {
        public FReQuAT_compiled_component(ComponentLogger log) : base(log) {
            this.Configuration = new FReQuAT_configuration();
        }

        public new FReQuAT_configuration Configuration
        {
            get => base.Configuration as FReQuAT_configuration;
            set => base.Configuration = value;
        }

        public override void Compute()
        {
            // your component implementation
            try
            {
                Logger.Trace(this.Configuration.XML.Absolute);
            }
            catch (Exception e)
            {
                Logger.Trace("Error: Missing input artifact", e);
                return;
            }
            //Run command line tool with parameters
            var inputFile = this.Configuration.XML.Absolute;
            try
            {
                Logger.Trace(this.Configuration.OutputDirectory.Absolute);
            }
            catch (Exception e)
            {
                Logger.Trace("Error: Invalid output directory", e);
            }
            var outputDirectory = this.Configuration.OutputDirectory.Absolute;
            string strCmdText;
            string strStartingText = "/C ";
            strCmdText = "/C ipconfig/all";
            System.Diagnostics.Process.Start("CMD.exe", (strStartingText + inputFile));
            //DEBUGGING prints        
            Logger.Trace(inputFile);
            Logger.Trace("Worked");
            new MainForm(inputFile, outputDirectory);
            Logger.Trace("TF-IDF: " + MainForm.lblTDF_Text);
        }
    }
     
    /// <summary>
    /// Latent Dirichlet Allocation (LDA) model implemented in Infer.NET.
    /// This version scales with number of documents.
    /// An optional parameter to the constructor specifies whether to use the
    /// fast version of the model (which uses power plates to deal efficiently
    /// with repeated words in the document) or the slower version where
    /// each word is considered separately. The only advantage of the latter
    /// is that it supports an evidence calculation.
    /// </summary>
    public class LDAShared : ILDA
    {
        /// <summary>
        /// Number of batches
        /// </summary>
        public int NumBatches { get; protected set; }
        /// <summary>
        /// Number of passes over the data
        /// </summary>
        public int NumPasses { get { return (IterationsPerPass == null) ? 0 : IterationsPerPass.Length; } }
        /// <summary>
        /// Number of iterations for each pass of the data
        /// </summary>
        public int[] IterationsPerPass { get; set; }
        /// <summary>
        /// Size of vocabulary
        /// </summary>
        public int SizeVocab { get; protected set; }
        /// <summary>
        /// Number of Topics
        /// </summary>
        public int NumTopics { get; protected set; }
        /// <summary>
        /// Sparsity specification for per-document distributions over topics
        /// </summary>
        public Sparsity ThetaSparsity { get; protected set; }
        /// <summary>
        /// Sparsity specification for per-topic distributions over words
        /// </summary>
        public Sparsity PhiSparsity { get; protected set; }
        /// <summary>
        /// Inference Engine for Phi definition model
        /// </summary>
        public InferenceEngine EnginePhiDef { get; protected set; }
        /// <summary>
        /// Main inference Engine
        /// </summary>
        public InferenceEngine Engine { get; protected set; }

        /// <summary>
        /// Shared variable array for per-topic word mixture variables - to be inferred
        /// </summary>
        protected SharedVariableArray<Vector> Phi;
        /// <summary>
        /// Shared variable for evidence
        /// </summary>
        protected SharedVariable<bool> Evidence;
        /// <summary>
        /// Model for documents (many copies)
        /// </summary>
        protected Model DocModel;
        /// <summary>
        /// Model for Phi definition (only one copy)
        /// </summary>
        protected Model PhiDefModel;
        /// <summary>
        /// Total number of documents (observed)
        /// </summary>
        protected Variable<int> NumDocuments;
        /// <summary>
        /// Number of words in each document (observed).
        /// For the fast version of the model, this is the number of unique words.
        /// </summary>
        protected VariableArray<int> NumWordsInDoc;
        /// <summary>
        /// Word indices in each document (observed)
        /// For the fast version of the model, these are the unique word indices.
        /// </summary>
        protected VariableArray<VariableArray<int>, int[][]> Words;
        /// <summary>
        /// Counts of unique words in each document (observed).
        /// This is used for the fast version only
        /// </summary>
        protected VariableArray<VariableArray<double>, double[][]> WordCounts;
        /// <summary>
        /// Per document distribution over topics (to be inferred)
        /// </summary>
        protected VariableArray<Vector> Theta;
        /// <summary>
        /// Copy of Phi for document model
        /// </summary>
        protected VariableArray<Vector> PhiDoc;
        /// <summary>
        /// Copy of Phi for definition model
        /// </summary>
        protected VariableArray<Vector> PhiDef;
        /// <summary>
        /// Prior for <see cref="Theta"/>
        /// </summary>
        protected VariableArray<Dirichlet> ThetaPrior;
        /// <summary>
        /// Prior for <see cref="Phi"/>
        /// </summary>
        protected VariableArray<Dirichlet> PhiPrior;
        /// <summary>
        /// Copy of model evidence variable for document model
        /// </summary>
        protected Variable<bool> EvidenceDoc;
        /// <summary>
        /// Copy of model evidence variable for phi definition model
        /// </summary>
        protected Variable<bool> EvidencePhiDef;
        /// <summary>
        /// Initialisation for breaking symmetry with respect to <see cref="Theta"/> (observed)
        /// </summary>
        protected Variable<IDistribution<Vector[]>> ThetaInit;

        /// <summary>
        /// Constructs an LDA model
        /// </summary>
        /// <param name="sizeVocab">Size of vocabulary</param>
        /// <param name="numTopics">Number of topics</param>
        public LDAShared(int numBatches, int sizeVocab, int numTopics)
        {
            SizeVocab = sizeVocab;
            NumTopics = numTopics;
            ThetaSparsity = Sparsity.Dense;
            PhiSparsity = Sparsity.ApproximateWithTolerance(0.00000000001); // Allow for round-off error
            NumDocuments = Variable.New<int>().Named("NumDocuments");
            NumBatches = numBatches;
            IterationsPerPass = new int[] { 1, 3, 5, 7, 9 };

            //---------------------------------------------
            // The model
            //---------------------------------------------
            Range D = new Range(NumDocuments).Named("D");
            Range W = new Range(SizeVocab).Named("W");
            Range T = new Range(NumTopics).Named("T");
            NumWordsInDoc = Variable.Array<int>(D).Named("NumWordsInDoc");
            Range WInD = new Range(NumWordsInDoc[D]).Named("WInD");

            Evidence = SharedVariable<bool>.Random(new Bernoulli(0.5)).Named("Evidence");
            Evidence.IsEvidenceVariable = true;

            Phi = SharedVariable<Vector>.Random(T, CreateUniformDirichletArray(numTopics, sizeVocab, PhiSparsity)).Named("Phi");

            // Phi definition sub-model - just one copy
            PhiDefModel = new Model(1).Named("PhiDefModel");

            IfBlock evidencePhiDefBlock = null;
            EvidencePhiDef = Evidence.GetCopyFor(PhiDefModel).Named("EvidencePhiDef");
            evidencePhiDefBlock = Variable.If(EvidencePhiDef);
            PhiDef = Variable.Array<Vector>(T).Named("PhiDef");
            PhiDef.SetSparsity(PhiSparsity);
            PhiDef.SetValueRange(W);
            PhiPrior = Variable.Array<Dirichlet>(T).Named("PhiPrior");
            PhiDef[T] = Variable<Vector>.Random(PhiPrior[T]);
            Phi.SetDefinitionTo(PhiDefModel, PhiDef);
            evidencePhiDefBlock.CloseBlock();

            // Document sub-model - many copies
            DocModel = new Model(numBatches).Named("DocModel");

            IfBlock evidenceDocBlock = null;
            EvidenceDoc = Evidence.GetCopyFor(DocModel).Named("EvidenceDoc");
            evidenceDocBlock = Variable.If(EvidenceDoc);
            Theta = Variable.Array<Vector>(D).Named("Theta");
            Theta.SetSparsity(ThetaSparsity);
            Theta.SetValueRange(T);
            ThetaPrior = Variable.Array<Dirichlet>(D).Named("ThetaPrior");
            Theta[D] = Variable<Vector>.Random(ThetaPrior[D]);
            PhiDoc = Phi.GetCopyFor(DocModel);
            PhiDoc.AddAttribute(new MarginalPrototype(Dirichlet.Uniform(sizeVocab, PhiSparsity)));
            Words = Variable.Array(Variable.Array<int>(WInD), D).Named("Words");
            WordCounts = Variable.Array(Variable.Array<double>(WInD), D).Named("WordCounts");
            using (Variable.ForEach(D))
            {
                using (Variable.ForEach(WInD))
                {
                    using (Variable.Repeat(WordCounts[D][WInD]))
                    {
                        Variable<int> topic = Variable.Discrete(Theta[D]).Named("topic");
                        using (Variable.Switch(topic))
                            Words[D][WInD] = Variable.Discrete(PhiDoc[topic]);
                    }
                }
            }
            evidenceDocBlock.CloseBlock();

            // Initialization to break symmetry
            ThetaInit = Variable.New<IDistribution<Vector[]>>().Named("ThetaInit");
            Theta.InitialiseTo(ThetaInit);
            EnginePhiDef = new InferenceEngine(new VariationalMessagePassing());
            EnginePhiDef.Compiler.ShowWarnings = false;
            EnginePhiDef.ModelName = "LDASharedPhiDef";

            Engine = new InferenceEngine(new VariationalMessagePassing());
            Engine.OptimiseForVariables = new IVariable[] { Theta, PhiDoc, EvidenceDoc };

            Engine.Compiler.ShowWarnings = false;
            Engine.ModelName = "LDAShared";
            Engine.Compiler.ReturnCopies = false;
            Engine.Compiler.FreeMemory = true;
        }

        /// <summary>
        /// Runs inference on the LDA model. 
        /// <para>
        /// Words in documents are observed, topic distributions per document (<see cref="Theta"/>)
        /// and word distributions per topic (<see cref="Phi"/>) are inferred.
        /// </para>
        /// </summary>
        /// <param name="wordsInDoc">For each document, the unique word counts in the document</param>
        /// <param name="alpha">Hyper-parameter for <see cref="Theta"/></param>
        /// <param name="beta>Hyper-parameter for <see cref="Phi"/></param>
        /// <param name="postTheta">Posterior marginals for <see cref="Theta"/></param>
        /// <param name="postPhi">Posterior marginals for <see cref="Phi"/></param>
        /// <returns>Log evidence - can be used for model selection.</returns>
        public virtual double Infer(
            Dictionary<int, int>[] wordsInDoc,
            double alpha, double beta,
            out Dirichlet[] postTheta, out Dirichlet[] postPhi)
        {
            int numDocs = wordsInDoc.Length;
            postTheta = new Dirichlet[numDocs];
            int numIters = Engine.NumberOfIterations;
            bool showProgress = Engine.ShowProgress;
            Engine.ShowProgress = false; // temporarily disable Infer.NET progress

            // Set up document index boundaries for each batch
            double numDocsPerBatch = ((double)numDocs) / NumBatches;
            if (numDocsPerBatch == 0) numDocsPerBatch = 1;
            int[] boundary = new int[NumBatches + 1];
            boundary[0] = 0;
            double currBoundary = 0.0;
            for (int batch = 1; batch <= NumBatches; batch++)
            {
                currBoundary += numDocsPerBatch;
                int bnd = (int)currBoundary;
                if (bnd > numDocs) bnd = numDocs;
                boundary[batch] = bnd;
            }
            boundary[NumBatches] = numDocs;

            PhiPrior.ObservedValue = new Dirichlet[NumTopics];
            for (int i = 0; i < NumTopics; i++) PhiPrior.ObservedValue[i] = Dirichlet.Symmetric(SizeVocab, beta);
            NumDocuments.ObservedValue = -1;
            try
            {
                var thetaInit = LDAModel.GetInitialisation(numDocs, NumTopics, ThetaSparsity);
                for (int pass = 0; pass < NumPasses; pass++)
                {
                    Engine.NumberOfIterations = IterationsPerPass[pass];
                  
                    if (showProgress) Utilities.LogMessageToFile(MainForm.logfile, String.Format(
                        "\nPass {0} ({1} iteration{2} per batch)",
                        pass, IterationsPerPass[pass], IterationsPerPass[pass] == 1 ? "" : "s"));
                    
                    if (pass == 0)
                        ThetaInit.ObservedValue = thetaInit;
                    else
                        ThetaInit.ObservedValue = Distribution<Vector>.Array(postTheta);

                    PhiDefModel.InferShared(EnginePhiDef, 0);
                    for (int batch = 0; batch < NumBatches; batch++)
                    {

                        int startDoc = boundary[batch];
                        int endDoc = boundary[batch + 1];
                        if (startDoc >= numDocs) break;
                        int numDocsInThisBatch = endDoc - startDoc;

                        // Set up the observed values
                        if (NumDocuments.ObservedValue != numDocsInThisBatch)
                        {
                            NumDocuments.ObservedValue = numDocsInThisBatch;

                            ThetaPrior.ObservedValue = new Dirichlet[numDocsInThisBatch];
                            for (int i = 0; i < numDocsInThisBatch; i++) ThetaPrior.ObservedValue[i] = Dirichlet.Symmetric(NumTopics, alpha);
                        }

                        int[] numWordsInDocBatch = new int[numDocsInThisBatch];
                        int[][] wordsInDocBatch = new int[numDocsInThisBatch][];
                        double[][] wordCountsInDocBatch = new double[numDocsInThisBatch][];
                        for (int i = 0, j = startDoc; j < endDoc; i++, j++)
                        {
                            numWordsInDocBatch[i] = wordsInDoc[j].Count;
                            wordsInDocBatch[i] = wordsInDoc[j].Keys.ToArray();
                            var cnts = wordsInDoc[j].Values;
                            wordCountsInDocBatch[i] = new double[cnts.Count];
                            int k = 0;
                            foreach (double val in cnts)
                                wordCountsInDocBatch[i][k++] = (double)val;
                        }
                        NumWordsInDoc.ObservedValue = numWordsInDocBatch;
                        Words.ObservedValue = wordsInDocBatch;
                        WordCounts.ObservedValue = wordCountsInDocBatch;

                        DocModel.InferShared(Engine, batch);
                        var postThetaBatch = Engine.Infer<Dirichlet[]>(Theta);
                        for (int i = 0, j = startDoc; j < endDoc; i++, j++)
                            postTheta[j] = postThetaBatch[i];

                        postPhi = Distribution.ToArray<Dirichlet[]>(Phi.Marginal<IDistribution<Vector[]>>());
                        
                        if (showProgress)
                        {
                            if ((batch % 80) == 0) Utilities.LogMessageToFile(MainForm.logfile, "");
                            Utilities.LogMessageToFile(MainForm.logfile, ".");
                        }
                        
                    }
                }
            }

            finally { Engine.NumberOfIterations = numIters; Engine.ShowProgress = showProgress; }
            if (showProgress) Utilities.LogMessageToFile(MainForm.logfile, "");
            postPhi = Distribution.ToArray<Dirichlet[]>(Phi.Marginal<IDistribution<Vector[]>>());

            return Model.GetEvidenceForAll(PhiDefModel, DocModel);
        }

        /// <summary>
        /// Creates a uniform distribution array over Dirichlets
        /// </summary>
        /// <param name="length">Length of array</param>
        /// <param name="valueLength">Dimension of each Dirichlet</param>
        /// <returns></returns>
        private static DirichletArray CreateUniformDirichletArray(
            int length, int valueLength, Sparsity sparsity)
        {
            Dirichlet[] result = new Dirichlet[length];
            for (int i = 0; i < length; i++)
                result[i] = Dirichlet.Uniform(valueLength, sparsity);
            return (DirichletArray)Distribution<Vector>.Array<Dirichlet>(result);
        }
    }

    /// <summary>
	/// Latent Dirichlet Allocation (LDA) prediction model implemented in Infer.NET.
	/// Use this class for obtaining predictive distributions over words for
	/// documents with known topic distributions
	/// </summary>
	public class LDATopicInferenceModel
    {
        /// <summary>
        /// Size of vocabulary
        /// </summary>
        public int SizeVocab { get; protected set; }
        /// <summary>
        /// Number of Topics
        /// </summary>
        public int NumTopics { get; protected set; }
        /// <summary>
        /// Inference engine
        /// </summary>
        public InferenceEngine Engine { get; protected set; }

        protected Variable<int> NumDocuments;
        /// <summary>
        /// Number of words in the document (observed).
        /// For the fast version of the model, this is the number of unique words.
        /// </summary>
        protected Variable<int> NumWordsInDoc;
        /// <summary>
        /// Word indices in the document (observed)
        /// For the fast version of the model, these are the unique word indices.
        /// </summary>
        protected VariableArray<int> Words;
        /// <summary>
        /// Counts of unique words in the document (observed).
        /// This is used for the fast version only
        /// </summary>
        protected VariableArray<double> WordCounts;
        /// <summary>
        /// Per document distribution over topics (to be inferred)
        /// </summary>
        protected Variable<Vector> Theta;
        /// <summary>
        /// Per topic distribution over words (to be inferred)
        /// </summary>
        protected VariableArray<Vector> Phi;
        /// <summary>
        /// Prior for <see cref="Theta"/>
        /// </summary>
        protected Variable<Dirichlet> ThetaPrior;
        /// <summary>
        /// Prior for <see cref="Phi"/>
        /// </summary>
        protected VariableArray<Dirichlet> PhiPrior;

        /// <summary>
        /// Constructs an LDA model
        /// </summary>
        /// <param name="sizeVocab">Size of vocabulary</param>
        /// <param name="numTopics">Number of topics</param>
        public LDATopicInferenceModel(
            int sizeVocab,
            int numTopics)
        {
            SizeVocab = sizeVocab;
            NumTopics = numTopics;

            //---------------------------------------------
            // The model
            //---------------------------------------------
            NumWordsInDoc = Variable.New<int>().Named("NumWordsInDoc");
            Range W = new Range(SizeVocab).Named("W");
            Range T = new Range(NumTopics).Named("T");
            Range WInD = new Range(NumWordsInDoc).Named("WInD");

            Theta = Variable.New<Vector>().Named("Theta");
            ThetaPrior = Variable.New<Dirichlet>().Named("ThetaPrior");
            ThetaPrior.SetValueRange(T);
            Theta = Variable<Vector>.Random(ThetaPrior);
            PhiPrior = Variable.Array<Dirichlet>(T).Named("PhiPrior");
            PhiPrior.SetValueRange(W);
            Phi = Variable.Array<Vector>(T).Named("Phi");
            Phi[T] = Variable.Random<Vector, Dirichlet>(PhiPrior[T]);

            Words = Variable.Array<int>(WInD).Named("Words");
            WordCounts = Variable.Array<double>(WInD).Named("WordCounts");
            using (Variable.ForEach(WInD))
            {
                using (Variable.Repeat(WordCounts[WInD]))
                {
                    var topic = Variable.Discrete(Theta).Attrib(new ValueRange(T)).Named("topic");
                    topic.SetValueRange(T);
                    using (Variable.Switch(topic))
                        Words[WInD] = Variable.Discrete(Phi[topic]);
                }
            }
            Engine = new InferenceEngine(new VariationalMessagePassing());
            Engine.Compiler.ShowWarnings = false;
        }

        /// <summary>
        /// Gets the predictive distributions for a set of documents
        /// <para>
        /// Topic distributions per document (<see cref="Theta"/>) and word distributions
        /// per topic (<see cref="Phi"/>) are observed, document distributions over words
        /// are inferred.
        /// </para>
        /// </summary>
        /// <param name="alpha">Hyper-parameter for <see cref="Theta"/></param>
        /// <param name="postPhi">The posterior topic word distributions</param>
        /// <param name="wordsInDoc">The unique word counts in the documents</param>
        /// <returns>The predictive distribution over words for each document</returns>
        public virtual Dirichlet[] InferTopic(
            double alpha,
            Dirichlet[] postPhi,
            Dictionary<int, int>[] wordsInDoc)
        {
            int numVocab = postPhi[0].Dimension;
            int numTopics = postPhi.Length;
            int numDocs = wordsInDoc.Length;
            Dirichlet[] result = new Dirichlet[numDocs];
            bool showProgress = Engine.ShowProgress;
            Engine.ShowProgress = false;
            PhiPrior.ObservedValue = postPhi;
            ThetaPrior.ObservedValue = Dirichlet.Symmetric(numTopics, alpha);

            try
            {
                for (int i = 0; i < numDocs; i++)
                {
                    NumWordsInDoc.ObservedValue = wordsInDoc[i].Count;
                    Words.ObservedValue = wordsInDoc[i].Keys.ToArray();
                    var cnts = wordsInDoc[i].Values;
                    var wordCounts = new double[cnts.Count];
                    int k = 0;
                    foreach (double val in cnts)
                        wordCounts[k++] = (double)val;
                    WordCounts.ObservedValue = wordCounts;

                    result[i] = Engine.Infer<Dirichlet>(Theta);
                    if (showProgress)
                    {
                        if ((i % 80) == 0) Utilities.LogMessageToFile(MainForm.logfile, "");
                        Utilities.LogMessageToFile(MainForm.logfile, ".");
                    }
                }
            }
            finally { Engine.ShowProgress = showProgress; }
            if (showProgress)
                Utilities.LogMessageToFile(MainForm.logfile, "");
            return result;
        }
    }

    public interface ILDA
    {
        double Infer(Dictionary<int, int>[] wordsInDoc, double alpha, double beta,
            out Dirichlet[] postTheta, out Dirichlet[] postPhi);

        InferenceEngine Engine { get; }
    }

    /// <summary>
    /// Latent Dirichlet Allocation (LDA) model implemented in Infer.NET.
    /// It keeps all messages in memory, and so scales poorly with respect to
    /// number of documents.
    /// An optional parameter to the constructor specifies whether to use the
    /// fast version of the model (which uses power plates to deal efficiently
    /// with repeated words in the document) or the slower version where
    /// each word is considered separately. The only advantage of the latter
    /// is that it supports an evidence calculation.
    /// See <see cref="LDAShared"/> for a version which scales better with number
    /// of documents.
    /// </summary>
    public class LDAModel : ILDA
    {
        /// <summary>
        /// Size of vocabulary
        /// </summary>
        public int SizeVocab { get; protected set; }
        /// <summary>
        /// Number of Topics
        /// </summary>
        public int NumTopics { get; protected set; }
        /// <summary>
        /// Sparsity specification for per-document distributions over topics
        /// </summary>
        public Sparsity ThetaSparsity { get; protected set; }
        /// <summary>
        /// Sparsity specification for per-topic distributions over words
        /// </summary>
        public Sparsity PhiSparsity { get; protected set; }
        /// <summary>
        /// Inference engine
        /// </summary>
        public InferenceEngine Engine { get; protected set; }

        /// <summary>
        /// Total number of documents (observed)
        /// </summary>
        protected Variable<int> NumDocuments;
        /// <summary>
        /// Number of words in each document (observed).
        /// For the fast version of the model, this is the number of unique words.
        /// </summary>
        protected VariableArray<int> NumWordsInDoc;
        /// <summary>
        /// Word indices in each document (observed)
        /// For the fast version of the model, these are the unique word indices.
        /// </summary>
        protected VariableArray<VariableArray<int>, int[][]> Words;
        /// <summary>
        /// Counts of unique words in each document (observed).
        /// This is used for the fast version only
        /// </summary>
        protected VariableArray<VariableArray<double>, double[][]> WordCounts;
        /// <summary>
        /// Per document distribution over topics (to be inferred)
        /// </summary>
        protected VariableArray<Vector> Theta;
        /// <summary>
        /// Per topic distribution over words (to be inferred)
        /// </summary>
        protected VariableArray<Vector> Phi;
        /// <summary>
        /// Prior for <see cref="Theta"/>
        /// </summary>
        protected VariableArray<Dirichlet> ThetaPrior;
        /// <summary>
        /// Prior for <see cref="Phi"/>
        /// </summary>
        protected VariableArray<Dirichlet> PhiPrior;
        /// <summary>
        /// Model evidence
        /// </summary>
        protected Variable<bool> Evidence;
        /// <summary>
        /// Initialisation for breaking symmetry with respect to <see cref="Theta"/> (observed)
        /// </summary>
        protected Variable<IDistribution<Vector[]>> ThetaInit;

        /// <summary>
        /// Constructs an LDA model
        /// </summary>
        /// <param name="sizeVocab">Size of vocabulary</param>
        /// <param name="numTopics">Number of topics</param>
        public LDAModel(int sizeVocab, int numTopics)
        {
            SizeVocab = sizeVocab;
            NumTopics = numTopics;
            ThetaSparsity = Sparsity.Dense;
            PhiSparsity = Sparsity.ApproximateWithTolerance(0.00000000001); // Allow for round-off error
            NumDocuments = Variable.New<int>().Named("NumDocuments");

            //---------------------------------------------
            // The model
            //---------------------------------------------
            Range D = new Range(NumDocuments).Named("D");
            Range W = new Range(SizeVocab).Named("W");
            Range T = new Range(NumTopics).Named("T");
            NumWordsInDoc = Variable.Array<int>(D).Named("NumWordsInDoc");
            Range WInD = new Range(NumWordsInDoc[D]).Named("WInD");

            // Surround model by a stochastic If block so that we can compute model evidence
            Evidence = Variable.Bernoulli(0.5).Named("Evidence");
            IfBlock evidenceBlock = Variable.If(Evidence);

            Theta = Variable.Array<Vector>(D);
            Theta.SetSparsity(ThetaSparsity);
            Theta.SetValueRange(T);
            ThetaPrior = Variable.Array<Dirichlet>(D).Named("ThetaPrior");
            Theta[D] = Variable<Vector>.Random(ThetaPrior[D]);
            Phi = Variable.Array<Vector>(T);
            Phi.SetSparsity(PhiSparsity);
            Phi.SetValueRange(W);
            PhiPrior = Variable.Array<Dirichlet>(T).Named("PhiPrior");
            Phi[T] = Variable<Vector>.Random(PhiPrior[T]);
            Words = Variable.Array(Variable.Array<int>(WInD), D).Named("Words");
            WordCounts = Variable.Array(Variable.Array<double>(WInD), D).Named("WordCounts");
            using (Variable.ForEach(D))
            {
                using (Variable.ForEach(WInD))
                {
                    using (Variable.Repeat(WordCounts[D][WInD]))
                    {
                        Variable<int> topic = Variable.Discrete(Theta[D]).Named("topic");
                        using (Variable.Switch(topic))
                            Words[D][WInD] = Variable.Discrete(Phi[topic]);
                    }
                }
            }

            evidenceBlock.CloseBlock();

            ThetaInit = Variable.New<IDistribution<Vector[]>>().Named("ThetaInit");
            Theta.InitialiseTo(ThetaInit);
            Engine = new InferenceEngine(new VariationalMessagePassing());
            Engine.Compiler.ShowWarnings = false;
            Engine.ModelName = "LDAModel";
        }

        /// <summary>
        /// Gets random initialisation for <see cref="Theta"/>. This initialises downward messages from <see cref="Theta"/>.
        /// The sole purpose is to break symmetry in the inference - it does not change the model.
        /// </summary>
        /// <param name="sparsity">The sparsity settings</param>
        /// <returns></returns>
        /// <remarks>This is implemented so as to support sparse initialisations</remarks>
        public static IDistribution<Vector[]> GetInitialisation(
            int numDocs, int numTopics, Sparsity sparsity)
        {
            Dirichlet[] initTheta = new Dirichlet[numDocs];
            double baseVal = 1.0 / numTopics;

            for (int i = 0; i < numDocs; i++)
            {
                // Choose a random topic
                Vector v = Vector.Zero(numTopics, sparsity);
                int topic = Rand.Int(numTopics);
                v[topic] = 1.0;
                initTheta[i] = Dirichlet.PointMass(v);
            }
            return Distribution<Vector>.Array(initTheta);
        }
        /// <summary>
        /// Runs inference on the LDA model. 
        /// <para>
        /// Words in documents are observed, topic distributions per document (<see cref="Theta"/>)
        /// and word distributions per topic (<see cref="Phi"/>) are inferred.
        /// </para>
        /// </summary>
        /// <param name="wordsInDoc">For each document, the unique word counts in the document</param>
        /// <param name="alpha">Hyper-parameter for <see cref="Theta"/></param>
        /// <param name="beta>Hyper-parameter for <see cref="Phi"/></param>
        /// <param name="postTheta">Posterior marginals for <see cref="Theta"/></param>
        /// <param name="postPhi">Posterior marginals for <see cref="Phi"/></param>
        /// <returns>Log evidence - can be used for model selection.</returns>
        public virtual double Infer(
            Dictionary<int, int>[] wordsInDoc,
            double alpha, double beta,
            out Dirichlet[] postTheta, out Dirichlet[] postPhi)
        {
            // Set up the observed values
            int numDocs = wordsInDoc.Length;
            NumDocuments.ObservedValue = numDocs;

            int[] numWordsInDoc = new int[numDocs];
            int[][] wordIndices = new int[numDocs][];
            double[][] wordCounts = new double[numDocs][];
            for (int i = 0; i < numDocs; i++)
            {
                numWordsInDoc[i] = wordsInDoc[i].Count;
                wordIndices[i] = wordsInDoc[i].Keys.ToArray();
                var cnts = wordsInDoc[i].Values;
                wordCounts[i] = new double[cnts.Count];
                int k = 0;
                foreach (double val in cnts)
                    wordCounts[i][k++] = (double)val;
            }

            NumWordsInDoc.ObservedValue = numWordsInDoc;
            Words.ObservedValue = wordIndices;
            WordCounts.ObservedValue = wordCounts;
            ThetaInit.ObservedValue = GetInitialisation(numDocs, NumTopics, ThetaSparsity);
            ThetaPrior.ObservedValue = new Dirichlet[numDocs];
            for (int i = 0; i < numDocs; i++) ThetaPrior.ObservedValue[i] = Dirichlet.Symmetric(NumTopics, alpha);
            PhiPrior.ObservedValue = new Dirichlet[NumTopics];
            for (int i = 0; i < NumTopics; i++) PhiPrior.ObservedValue[i] = Dirichlet.Symmetric(SizeVocab, beta);
            Engine.OptimiseForVariables = new IVariable[] { Theta, Phi, Evidence };
            postTheta = Engine.Infer<Dirichlet[]>(Theta);
            postPhi = Engine.Infer<Dirichlet[]>(Phi);
            return Engine.Infer<Bernoulli>(Evidence).LogOdds;
        }
    }

    class LDA
    {
        public static double[][] LDAmatrix;

        /// <summary>
        /// Run a single test for a single model
        /// </summary>
        /// <param name="sizeVocab">Size of the vocabulary</param>
        /// <param name="numTopics">Number of topics</param>
        /// <param name="alpha">Background pseudo-counts for distributions over topics</param>
        /// <param name="beta">Background pseudo-counts for distributions over words</param>
        /// <param name="shared">If true, uses shared variable version of the model</param>
        /// <param name="vocabulary">Vocabulary</param>
        public static void RunTest(
            int sizeVocab,
            int numTopics,
            Dictionary<int, int>[] allWords,
            double alpha,
            double beta,
            bool shared,
            Dictionary<int, string> vocabulary
            )
        {
            Stopwatch stopWatch = new Stopwatch();
            // Square root of number of documents is the optimal for memory
            int batchCount = (int)Math.Sqrt((double)allWords.Length);
            Rand.Restart(5);
            ILDA model;
            //LDAPredictionModel predictionModel;
            //LDATopicInferenceModel topicInfModel;
            if (shared)
            {
                model = new LDAShared(batchCount, sizeVocab, numTopics);
                ((LDAShared)model).IterationsPerPass = Enumerable.Repeat(10, 5).ToArray();
            }
            else
            {
                model = new LDAModel(sizeVocab, numTopics);
                model.Engine.NumberOfIterations = 50;
            }

            Utilities.LogMessageToFile(MainForm.logfile, "\n\n************************************");
            Utilities.LogMessageToFile(MainForm.logfile,
                String.Format("\nTraining {0}LDA model...\n",
                shared ? "batched " : "non-batched "));

            // Train the model - we will also get rough estimates of execution time and memory
            Dirichlet[] postTheta, postPhi;
            GC.Collect();
            PerformanceCounter memCounter = new PerformanceCounter("Memory", "Available MBytes");
            float preMem = memCounter.NextValue();
            stopWatch.Reset();
            stopWatch.Start();
            double logEvidence = model.Infer(allWords, alpha, beta, out postTheta, out postPhi);
            stopWatch.Stop();
            float postMem = memCounter.NextValue();
            double approxMB = preMem - postMem;
            GC.KeepAlive(model); // Keep the model alive to this point (for the	memory counter)
            Utilities.LogMessageToFile(MainForm.logfile, String.Format("Approximate memory usage: {0:F2} MB", approxMB));
            Utilities.LogMessageToFile(MainForm.logfile, String.Format("Approximate execution time (including model compilation): {0} seconds", stopWatch.ElapsedMilliseconds / 1000));

            // Calculate average log evidence over total training words
            int totalWords = allWords.Sum(doc => doc.Sum(w => w.Value));
            Utilities.LogMessageToFile(MainForm.logfile, String.Format("\nTotal number of training words = {0}", totalWords));
            Utilities.LogMessageToFile(MainForm.logfile, String.Format("Average log evidence of model: {0:F2}", logEvidence / (double)totalWords));

            //if (vocabulary != null)
            //{
            //    int numWordsToPrint = 20;
            //    // Print out the top n words for each topic
            //    for (int i = 0; i < postPhi.Length; i++)
            //    {
            //        double[] pc = postPhi[i].PseudoCount.ToArray();
            //        int[] wordIndices = new int[pc.Length];
            //        for (int j = 0; j < wordIndices.Length; j++)
            //            wordIndices[j] = j;
            //        Array.Sort(pc, wordIndices);
            //        Debug.WriteLine("Top {0} words in topic {1}:", numWordsToPrint, i);
            //        int idx = wordIndices.Length;
            //        for (int j = 0; j < numWordsToPrint; j++)
            //            Debug.Write("\t" + vocabulary[wordIndices[--idx]]);
            //        Debug.WriteLine("");
            //    }
            //}

            // nieuwe waardes
            LDAmatrix = new double[postTheta.Length][];
            for (int i = 0; i < postTheta.Length; i++)
            {
                LDAmatrix[i] = new double[postTheta[i].PseudoCount.Count];
                LDAmatrix[i] = postTheta[i].PseudoCount.ToArray();
            }
        }

        /// <summary>
        /// A topic pair
        /// </summary>
        public struct TopicPair
        {
            public int InferredTopic;
            public int TrueTopic;
        }

        /// <summary>
        /// Count the number of correct predictions of the best topic.
        /// This uses a simple greedy algorithm to determine the topic mapping (use with caution!)
        /// </summary>
        /// <param name="topicPairCounts">A dictionary mapping (inferred, true) pairs to counts</param>
        /// <param name="numTopics">The number of topics</param>
        /// <returns></returns>
        public static int CountCorrectTopicPredictions(Dictionary<TopicPair, int> topicPairCounts, int numTopics)
        {
            int[] topicMapping = new int[numTopics];
            for (int i = 0; i < numTopics; i++) topicMapping[i] = -1;

            // Sort by count
            List<KeyValuePair<TopicPair, int>> kvps = new List<KeyValuePair<TopicPair, int>>(topicPairCounts);
            kvps.Sort(
                delegate (KeyValuePair<TopicPair, int> kvp1, KeyValuePair<TopicPair, int> kvp2)
                {
                    return kvp2.Value.CompareTo(kvp1.Value);
                }
            );

            int correctCount = 0;
            while (kvps.Count > 0)
            {
                KeyValuePair<TopicPair, int> kvpHead = kvps[0];
                int inferredTopic = kvpHead.Key.InferredTopic;
                int trueTopic = kvpHead.Key.TrueTopic;
                topicMapping[inferredTopic] = trueTopic;
                correctCount += kvpHead.Value;
                kvps.Remove(kvpHead);
                // Now delete anything in the list that has either of these
                for (int i = kvps.Count - 1; i >= 0; i--)
                {
                    KeyValuePair<TopicPair, int> kvp = kvps[i];
                    int infTop = kvp.Key.InferredTopic;
                    int trueTop = kvp.Key.TrueTopic;
                    if (infTop == inferredTopic || trueTop == trueTopic)
                        kvps.Remove(kvp);
                }
            }

            return correctCount;
        }

        private static float[] GetTopicVector(int doc)
        {
            int _numTopics = LDAmatrix[0].Length;
            float[] w = new float[_numTopics];
            for (int i = 0; i < _numTopics; i++)
                w[i] = (float)LDAmatrix[doc][i];
            return w;
        }

        public static float GetSimilarity(int doc_i, int doc_j)
        {
            float[] vector1 = GetTopicVector(doc_i);
            float[] vector2 = GetTopicVector(doc_j);

            return TFIDFMeasure.TermVector.ComputeCosineSimilarity(vector1, vector2);

        }
    }

    class TigrisFeatureCollection : FeatureCollection
    {
        //fills the featureList with Feature objects read from an XML file
        //xmlFilePath: path of the XML file, e.g. "features.xml"
        //bAllComments: true if all comments are loaded as a document, false if only first comment is loaded
        //bCode: true if code is kept, false if code should be removed
        //bWTitle: true if title is doubled in weight
        public TigrisFeatureCollection(string xmlFilePath, bool bAllComments, bool bCode, bool bWTitle)
        {
            //read XML file
            XDocument xdoc = XDocument.Load(xmlFilePath);

            //get all <feature> elements in the file
            IEnumerable<XElement> xFeat = from xf in xdoc.Descendants("issue")
                                              //where (string)xf.Element("bug_severity") == "enhancement"
                                          select xf;

            //read through each <feature> element to create a Feature object
            foreach (XElement item in xFeat)
            {
                string iD = (string)(from xe in item.Descendants("issue_id") select xe).First();
                string title = (string)(from xe in item.Descendants("short_desc") select xe).First();
                IEnumerable<string> comm = from xe in item.Descendants("thetext") select (string)xe;
                string desc = comm.First(); //in Bugzilla XML the description is the first comment
                string dupId = (string)(from xe in item.Descendants("is_duplicate").Descendants("issue_id") select xe).FirstOrDefault();
                Feature ft = new Feature(iD, title, desc, comm.Skip(1), bAllComments, bWTitle);
                ft.duplicate_id = dupId;
                featureList.Add(ft);
            }

            bugTag = "issue";
        }
    }

    class BugzillaFeatureCollection : FeatureCollection
    {


        //fills the featureList with Feature objects read from an XML file
        //xmlFilePath: path of the XML file, e.g. "features.xml"
        //bAllComments: true if all comments are loaded as a document, false if only first comment is loaded
        //bCode: true if code is kept, false if code should be removed
        //bWTitle: true if title is doubled in weight
        public BugzillaFeatureCollection(string xmlFilePath, bool bAllComments, bool bCode, bool bWTitle)
        {
            //read XML file
            XDocument xdoc = XDocument.Load(xmlFilePath);

            //get all <feature> elements in the file
            IEnumerable<XElement> xFeat = from xf in xdoc.Descendants("bug")
                                              //where (string)xf.Element("bug_severity") == "enhancement"
                                          select xf;

            //read through each <feature> element to create a Feature object
            foreach (XElement item in xFeat)
            {
                string iD = (string)(from xe in item.Descendants("bug_id") select xe).First();
                string title = (string)(from xe in item.Descendants("short_desc") select xe).First();
                IEnumerable<string> comm = from xe in item.Descendants("thetext") select (string)xe;
                string desc = comm.First(); //in Bugzilla XML the description is the first comment
                string dup_id = (string)(from xe in item.Descendants("dup_id") select xe).FirstOrDefault();

                //check if it is a valid feature request
                string resolution = (string)(from xe in item.Descendants("resolution") select xe).First();

                if (!resolution.EndsWith("INVALID"))
                {
                    Feature ft = new Feature(iD, title, desc, comm.Skip(1), bAllComments, bWTitle);
                    ft.duplicate_id = dup_id;
                    featureList.Add(ft);
                }
                else
                {
                    string s = "";
                }
            }

            bugTag = "bug";
        }


    }

    enum svdCounters { SVD_MXV, SVD_COUNTERS };
    enum storeVals { STORQ = 1, RETRQ, STORP, RETRP };

    class SMat
    {
        public int rows;
        public int cols;
        public int vals;       // Total non-zero entries. 
        public int[] pointr;   // For each col (plus 1), index of first non-zero entry. 
        public int[] rowind;   // For each nz entry, the row index. 
        public double[] value; // For each nz entry, the value. 
    }

    class DMat
    {
        public int rows;
        public int cols;
        public double[][] value; // Accessed by [row][col]. Free value[0] and value to free.
    }

    class SVDRec
    {
        public int d;      // Dimensionality (rank) 
        public DMat Ut;    // Transpose of left singular vectors. (d by m). The vectors are the rows of Ut. 
        public double[] S; // Array of singular values. (length d) 
        public DMat Vt;    // Transpose of right singular vectors. (d by n). The vectors are the rows of Vt. 
    }

    static class SVD
    {
        private static int[] SVDCount = new int[(int)(svdCounters.SVD_COUNTERS)];
        private static double eps, eps1, reps, eps34, halfm, s;
        private static int ierr, ia, ic, mic, m2 = 0;
        private static double[] OPBTemp;
        private const int MAXLL = 2;
        private static double[][] LanStore;

        // Functions for reading-writing data
        private static SMat svdLoadSparseMatrix(string datafile)
        {
            try
            {
                SMat S = new SMat();
                using (FileStream stream = new FileStream(datafile, FileMode.Open))
                {
                    using (BinaryReader reader = new BinaryReader(stream))
                    {
                        S.rows = System.Net.IPAddress.NetworkToHostOrder(reader.ReadInt32());
                        S.cols = System.Net.IPAddress.NetworkToHostOrder(reader.ReadInt32());
                        S.vals = System.Net.IPAddress.NetworkToHostOrder(reader.ReadInt32());
                        S.pointr = new int[S.cols + 1];
                        for (int k = 0; k < S.cols + 1; ++k) S.pointr[k] = 0;
                        S.rowind = new int[S.vals];
                        S.value = new double[S.vals];
                        for (int c = 0, v = 0; c < S.cols; c++)
                        {
                            int n = System.Net.IPAddress.NetworkToHostOrder(reader.ReadInt32());
                            S.pointr[c] = v;
                            for (int i = 0; i < n; i++, v++)
                            {
                                int r = System.Net.IPAddress.NetworkToHostOrder(reader.ReadInt32());
                                //double nBuf = reader.ReadDouble();
                                int nBuf = System.Net.IPAddress.NetworkToHostOrder(reader.ReadInt32());
                                byte[] b = BitConverter.GetBytes(nBuf);
                                float f = BitConverter.ToSingle(b, 0);
                                S.rowind[v] = r;
                                S.value[v] = f;
                            }
                            S.pointr[S.cols] = S.vals;
                        }
                        reader.Close();
                    }
                    stream.Close();
                }
                return S;
            }
            catch (Exception e)
            {
                Utilities.LogMessageToFile(MainForm.logfile, e.Message);
                return null;
            }
        }

        private static SMat svdLoadSparseMatrix(float[][] weightMatrix)
        {
            try
            {
                SMat S = new SMat();
                S.rows = weightMatrix.GetLength(0);
                S.cols = weightMatrix[0].GetLength(0);
                S.pointr = new int[S.cols + 1];
                for (int k = 0; k < S.cols + 1; ++k) S.pointr[k] = 0;
                int nrNonZero = 0;
                for (int i = 0; i < S.cols; i++)
                {
                    for (int j = 0; j < S.rows; j++)
                    {
                        if (weightMatrix[j][i] > 0)
                        {
                            nrNonZero++;
                        }
                    }
                }
                S.vals = nrNonZero;//S.rows * S.cols;
                S.rowind = new int[nrNonZero];//S.vals];
                S.value = new double[nrNonZero];//S.vals];
                for (int c = 0, v = 0; c < S.cols; c++)
                {
                    //int n = System.Net.IPAddress.NetworkToHostOrder(reader.ReadInt32());
                    int n = S.rows;
                    S.pointr[c] = v;
                    for (int i = 0; i < n; i++/*, v++*/)
                    {
                        //int r = System.Net.IPAddress.NetworkToHostOrder(reader.ReadInt32());
                        double nBuf = (double)weightMatrix[i][c];
                        if (nBuf > 0)
                        {
                            int r = i;

                            //int nBuf = System.Net.IPAddress.NetworkToHostOrder(reader.ReadInt32());
                            //byte[] b = BitConverter.GetBytes(nBuf);
                            //float f = BitConverter.ToSingle(b, 0);
                            S.rowind[v] = r;
                            S.value[v] = nBuf;
                            v++;
                        }
                    }
                }
                S.pointr[S.cols] = S.vals;
                return S;
            }
            catch (Exception e)
            {
                Utilities.LogMessageToFile(MainForm.logfile, e.Message);
                return null;
            }
        }
        private static void svdWriteDenseMatrix(DMat D, string filename)
        {
            using (FileStream stream = new FileStream(filename, FileMode.Create))
            {
                using (BinaryWriter writer = new BinaryWriter(stream))
                {
                    writer.Write(System.Net.IPAddress.HostToNetworkOrder(D.rows));
                    writer.Write(System.Net.IPAddress.HostToNetworkOrder(D.cols));
                    for (int k = 0; k < D.rows; ++k)
                    {
                        for (int j = 0; j < D.cols; ++j)
                        {
                            float buf = (float)(D.value[k][j]);
                            byte[] b = BitConverter.GetBytes(buf);
                            int x = BitConverter.ToInt32(b, 0);
                            writer.Write(System.Net.IPAddress.HostToNetworkOrder(x));
                        }
                    }
                    writer.Flush();
                    writer.Close();
                }
                stream.Close();
            }
        }

        public static void svdWriteDenseArray(double[] a, int n, string filename)
        {
            System.IO.StreamWriter file = new System.IO.StreamWriter(filename);
            file.WriteLine(n.ToString());
            for (int i = 0; i < n; ++i)
            {
                float buf = (float)(a[i]);
                file.WriteLine(buf.ToString());
            }
            file.Flush();
            file.Close();
        }
        //End reading-writing

        //Some elementary functions
        private static int svd_imax(int a, int b)
        {
            return (a > b) ? a : b;
        }

        private static int svd_imin(int a, int b)
        {
            return (a < b) ? a : b;
        }

        private static double svd_fsign(double a, double b)
        {
            if ((a >= 0.0 && b >= 0.0) || (a < 0.0 && b < 0.0)) return (a);
            else return -a;
        }

        private static double svd_dmax(double a, double b)
        {
            return (a > b) ? a : b;
        }

        private static double svd_dmin(double a, double b)
        {
            return (a < b) ? a : b;
        }

        private static double svd_pythag(double a, double b)
        {
            double p, r, s, t, u, temp;
            p = svd_dmax(Math.Abs(a), Math.Abs(b));
            if (p != 0.0)
            {
                temp = svd_dmin(Math.Abs(a), Math.Abs(b)) / p;
                r = temp * temp;
                t = 4.0 + r;
                while (t != 4.0)
                {
                    s = r / t;
                    u = 1.0 + 2.0 * s;
                    p *= u;
                    temp = s / u;
                    r *= temp * temp;
                    t = 4.0 + r;
                }
            }
            return (p);
        }
        // End

        private static void svdResetCounters()
        {
            for (int i = 0; i < (int)(svdCounters.SVD_COUNTERS); i++)
            {
                SVDCount[i] = 0;
            }
        }

        private static int check_parameters(SMat A, int dimensions, int iterations, double endl, double endr)
        {
            int error_index;
            error_index = 0;
            if (endl > endr) error_index = 2;
            else if (dimensions > iterations) error_index = 3;
            else if (A.cols <= 0 || A.rows <= 0) error_index = 4;
            else if (iterations <= 0 || iterations > A.cols || iterations > A.rows) error_index = 5;
            else if (dimensions <= 0 || dimensions > iterations) error_index = 6;
            if (error_index > 0) Utilities.LogMessageToFile(MainForm.logfile, "svdLAS2 parameter error: %s\n");
            return error_index;
        }

        private static SMat svdNewSMat(int rows, int cols, int vals)
        {
            SMat S = new SMat();
            S.rows = rows;
            S.cols = cols;
            S.vals = vals;
            S.pointr = new int[cols + 1];
            S.rowind = new int[vals];
            S.value = new double[vals];
            return S;
        }

        private static SMat svdTransposeS(SMat S)
        {
            int r, c, i, j;
            SMat N = svdNewSMat(S.cols, S.rows, S.vals);
            for (i = 0; i < S.vals; i++) N.pointr[S.rowind[i]]++;
            N.pointr[S.rows] = S.vals - N.pointr[S.rows - 1];
            for (r = S.rows - 1; r > 0; r--) N.pointr[r] = N.pointr[r + 1] - N.pointr[r - 1];
            N.pointr[0] = 0;
            for (c = 0, i = 0; c < S.cols; c++)
            {
                for (; i < S.pointr[c + 1]; i++)
                {
                    r = S.rowind[i];
                    j = N.pointr[r + 1]++;
                    N.rowind[j] = c;
                    N.value[j] = S.value[i];
                }
            }
            return N;
        }

        //Set of functions for operations with wectors. In original code they pass shifted array pointer like follows:
        //void f(double* dx);
        //If this functions is called as f(&x[3]) passed array starts from element 3. This mechanism is used only
        //for svd_dcopy. On that reason only svd_dcopy supports it. 
        private static void svd_dcopy(int n, double[] dx, int startdx, int incx, double[] dy, int startdy, int incy)
        {
            int i;
            int dx_index = startdx;
            int dy_index = startdx;

            if (n <= 0 || incx == 0 || incy == 0) return;
            if (incx == 1 && incy == 1)
            {
                for (i = 0; i < n; i++)
                {
                    dy[i] = dx[i];
                }
            }
            else
            {
                if (incx < 0) dx_index += (-n + 1) * incx;
                if (incy < 0) dy_index += (-n + 1) * incy;
                for (i = 0; i < n; i++)
                {
                    dy[dy_index] = dx[dx_index];
                    dx_index += incx;
                    dy_index += incy;
                }
            }
            return;
        }

        private static double svd_ddot(int n, double[] dx, int incx, double[] dy, int incy)
        {
            int i;
            double dot_product;
            int dx_index = 0, dy_index = 0;

            if (n <= 0 || incx == 0 || incy == 0) return (0.0);
            dot_product = 0.0;
            if (incx == 1 && incy == 1)
            {
                for (i = 0; i < n; i++)
                {
                    dot_product += dx[i] * dy[i];
                }
            }
            else
            {
                if (incx < 0) dx_index = (-n + 1) * incx;
                if (incy < 0) dy_index = (-n + 1) * incy;
                for (i = 0; i < n; i++)
                {
                    dot_product += dx[dx_index] * dy[dy_index];
                    dx_index += incx;
                    dy_index += incy;
                }
            }
            return (dot_product);
        }

        private static void svd_daxpy(int n, double da, double[] dx, int incx, double[] dy, int incy)
        {
            int i;
            int dx_index = 0;
            int dy_index = 0;

            if (n <= 0 || incx == 0 || incy == 0 || da == 0.0) return;
            if (incx == 1 && incy == 1)
            {
                for (i = 0; i < n; i++)
                {
                    dy[i] += da * dx[i];
                }
            }
            else
            {
                if (incx < 0) dx_index = (-n + 1) * incx;
                if (incy < 0) dy_index = (-n + 1) * incy;
                for (i = 0; i < n; i++)
                {
                    dy[i] += da * dx[i];
                    dx_index += incx;
                    dy_index += incy;
                }
            }
        }

        private static void svd_datx(int n, double da, double[] dx, int incx, double[] dy, int incy)
        {
            int i;
            int dx_index = 0;
            int dy_index = 0;

            if (n <= 0 || incx == 0 || incy == 0 || da == 0.0) return;
            if (incx == 1 && incy == 1)
            {
                for (i = 0; i < n; i++)
                {
                    dy[i] = da * dx[i];
                }
            }
            else
            {
                if (incx < 0) dx_index = (-n + 1) * incx;
                if (incy < 0) dy_index = (-n + 1) * incy;
                for (i = 0; i < n; i++)
                {
                    dy[dy_index] = da * dx[dx_index];
                    dx_index += incx;
                    dy_index += incy;
                }
            }
        }

        private static void svd_dswap(int n, double[] dx, int incx, double[] dy, int incy)
        {
            int i;
            double dtemp;
            int dx_index = 0;
            int dy_index = 0;

            if (n <= 0 || incx == 0 || incy == 0) return;
            if (incx == 1 && incy == 1)
            {
                for (i = 0; i < n; i++)
                {
                    dtemp = dy[i];
                    dy[i] = dx[i];
                    dx[i] = dtemp;
                }
            }
            else
            {
                if (incx < 0) dx_index = (-n + 1) * incx;
                if (incy < 0) dy_index = (-n + 1) * incy;
                for (i = 0; i < n; i++)
                {
                    dtemp = dy[i];
                    dy[i] = dx[i];
                    dx[i] = dtemp;
                    dx_index += incx;
                    dy_index += incy;
                }
            }
        }
        //End functions for operations with vectors.

        //The purpose of that function is to find machine precision.
        private static void machar(ref int ibeta, ref int it, ref int irnd, ref int machep, ref int negep)
        {
            double beta, betain, betah, a, b, ZERO, ONE, TWO, temp, tempa, temp1;
            b = 0.0;
            int i, itemp;

            ONE = (double)1;
            TWO = ONE + ONE;
            ZERO = ONE - ONE;

            a = ONE;
            temp1 = ONE;
            while (temp1 - ONE == ZERO)
            {
                a = a + a;
                temp = a + ONE;
                temp1 = temp - a;
                b += a;
            }
            b = ONE;
            itemp = 0;
            while (itemp == 0)
            {
                b = b + b;
                temp = a + b;
                itemp = (int)(temp - a);
            }

            ibeta = itemp;
            beta = (double)ibeta;

            it = 0;
            b = ONE;
            temp1 = ONE;
            while (temp1 - ONE == ZERO)
            {
                it = it + 1;
                b = b * beta;
                temp = b + ONE;
                temp1 = temp - b;
            }

            irnd = 0;
            betah = beta / TWO;
            temp = a + betah;
            if (temp - a != ZERO) irnd = 1;
            tempa = a + beta;
            temp = tempa + betah;
            if ((irnd == 0) && (temp - tempa != ZERO)) irnd = 2;

            negep = it + 3;
            betain = ONE / beta;
            a = ONE;
            for (i = 0; i < negep; i++) a = a * betain;
            b = a;
            temp = ONE - a;
            while (temp - ONE == ZERO)
            {
                a = a * beta;
                negep = negep - 1;
                temp = ONE - a;
            }
            negep = -(negep);

            machep = -(it) - 3;
            a = b;
            temp = ONE + a;
            while (temp - ONE == ZERO)
            {
                a = a * beta;
                machep = machep + 1;
                temp = ONE + a;
            }
            eps = a;
        }

        private static double svd_random2(ref int iy)
        {
            if (m2 == 0)
            {
                m2 = 1 << (8 * (int)sizeof(int) - 2);
                halfm = m2;
                ia = 8 * (int)(halfm * Math.Atan(1.0) / 8.0) + 5;
                ic = 2 * (int)(halfm * (0.5 - Math.Sqrt(3.0) / 6.0)) + 1;
                mic = (m2 - ic) + m2;
                s = 0.5 / halfm;
            }
            iy = iy * ia;
            if (iy > mic) iy = (iy - m2) - m2;
            iy = iy + ic;
            if (iy / 2 > m2) iy = (iy - m2) - m2;
            if (iy < 0) iy = (iy + m2) + m2;
            return ((double)(iy) * s);
        }

        private static void svd_opb(SMat A, double[] x, double[] y, double[] temp)
        {
            int i, j, end;
            int[] pointr = A.pointr;
            int[] rowind = A.rowind;

            double[] value = A.value;
            int n = A.cols;

            SVDCount[(int)(svdCounters.SVD_MXV)] += 2;
            for (i = 0; i < n; ++i) y[i] = 0.0;
            for (i = 0; i < A.rows; i++) temp[i] = 0.0;

            for (i = 0; i < A.cols; i++)
            {
                end = pointr[i + 1];
                for (j = pointr[i]; j < end; j++)
                {
                    temp[rowind[j]] += value[j] * x[i];
                }
            }

            for (i = 0; i < A.cols; i++)
            {
                end = pointr[i + 1];
                for (j = pointr[i]; j < end; j++)
                {
                    y[i] += value[j] * temp[rowind[j]];
                }
            }
        }

        private static void store(int n, int isw, int j, double[] s)
        {
            switch (isw)
            {
                case (int)storeVals.STORQ:
                    if (LanStore[j + MAXLL] == null)
                    {
                        LanStore[j + MAXLL] = new double[n];
                    }
                    svd_dcopy(n, s, 0, 1, LanStore[j + MAXLL], 0, 1);
                    break;
                case (int)storeVals.RETRQ:
                    if (LanStore[j + MAXLL] == null)
                    {
                        Utilities.LogMessageToFile(MainForm.logfile, String.Format("svdLAS2: store (RETRQ) called on index {0} (not allocated) ", (j + MAXLL)));
                    }
                    svd_dcopy(n, LanStore[j + MAXLL], 0, 1, s, 0, 1);
                    break;
                case (int)storeVals.STORP:
                    if (j >= MAXLL)
                    {
                        Utilities.LogMessageToFile(MainForm.logfile, "svdLAS2: store (STORP) called with j >= MAXLL");
                        break;
                    }
                    if (LanStore[j] == null)
                    {
                        LanStore[j] = new double[n];
                    }
                    svd_dcopy(n, s, 0, 1, LanStore[j], 0, 1);
                    break;
                case (int)storeVals.RETRP:
                    if (j >= MAXLL)
                    {
                        Utilities.LogMessageToFile(MainForm.logfile, "svdLAS2: store (RETRP) called with j >= MAXLL");
                        break;
                    }
                    if (LanStore[j] == null)
                    {
                        Utilities.LogMessageToFile(MainForm.logfile, "svdLAS2: store (RETRP) called on index %d (not allocated) " + j.ToString());
                    }
                    svd_dcopy(n, LanStore[j], 0, 1, s, 0, 1);
                    break;
            }
        }

        private static double startv(SMat A, double[][] wptr, int step, int n)
        {
            double rnm2, t;
            double[] r;
            int irand = 0, id, i;

            rnm2 = svd_ddot(n, wptr[0], 1, wptr[0], 1);
            irand = 918273 + step;
            r = wptr[0];
            for (id = 0; id < 3; id++)
            {
                if (id > 0 || step > 0 || rnm2 == 0)
                {
                    for (i = 0; i < n; i++)
                    {
                        r[i] = svd_random2(ref irand);
                    }
                }
                svd_dcopy(n, wptr[0], 0, 1, wptr[3], 0, 1);
                svd_opb(A, wptr[3], wptr[0], OPBTemp);
                svd_dcopy(n, wptr[0], 0, 1, wptr[3], 0, 1);
                rnm2 = svd_ddot(n, wptr[0], 1, wptr[3], 1);
                if (rnm2 > 0.0) break;
            }

            // fatal error 
            if (rnm2 <= 0.0)
            {
                ierr = 8192;
                return (-1);
            }

            if (step > 0)
            {
                for (i = 0; i < step; i++)
                {
                    store(n, (int)storeVals.RETRQ, i, wptr[5]);
                    t = -svd_ddot(n, wptr[3], 1, wptr[5], 1);
                    svd_daxpy(n, t, wptr[5], 1, wptr[0], 1);
                }

                t = svd_ddot(n, wptr[4], 1, wptr[0], 1);
                svd_daxpy(n, -t, wptr[2], 1, wptr[0], 1);
                svd_dcopy(n, wptr[0], 0, 1, wptr[3], 0, 1);
                t = svd_ddot(n, wptr[3], 1, wptr[0], 1);
                if (t <= eps * rnm2) t = 0.0;
                rnm2 = t;
            }
            return Math.Sqrt(rnm2);
        }

        private static void svd_dscal(int n, double da, double[] dx, int incx)
        {
            int i;
            int dx_index = 0;
            if (n <= 0 || incx == 0) return;
            if (incx < 0) dx_index += (-n + 1) * incx;
            for (i = 0; i < n; i++)
            {
                dx[dx_index] *= da;
                dx_index += incx;
            }
        }

        private static void stpone(SMat A, double[][] wrkptr, ref double rnmp, ref double tolp, int n)
        {
            double t, rnm, anorm;
            double[] alf;
            alf = wrkptr[6];

            rnm = startv(A, wrkptr, 0, n);
            if (rnm == 0.0 || ierr != 0) return;

            t = 1.0 / rnm;
            svd_datx(n, t, wrkptr[0], 1, wrkptr[1], 1);
            svd_dscal(n, t, wrkptr[3], 1);

            svd_opb(A, wrkptr[3], wrkptr[0], OPBTemp);
            alf[0] = svd_ddot(n, wrkptr[0], 1, wrkptr[3], 1);
            svd_daxpy(n, -alf[0], wrkptr[1], 1, wrkptr[0], 1);
            t = svd_ddot(n, wrkptr[0], 1, wrkptr[3], 1);
            svd_daxpy(n, -t, wrkptr[1], 1, wrkptr[0], 1);
            alf[0] += t;
            svd_dcopy(n, wrkptr[0], 0, 1, wrkptr[4], 0, 1);
            rnm = Math.Sqrt(svd_ddot(n, wrkptr[0], 1, wrkptr[4], 1));
            anorm = rnm + Math.Abs(alf[0]);
            rnmp = rnm;
            tolp = reps * anorm;
        }

        private static int svd_idamax(int n, double[] dx, int incx)
        {
            int ix, i, imax;
            double dtemp, dmax;
            if (n < 1) return (-1);
            if (n == 1) return (0);
            if (incx == 0) return (-1);
            if (incx < 0) ix = (-n + 1) * incx;
            else ix = 0;
            imax = ix;
            dmax = Math.Abs(dx[ix]);
            for (i = 1; i < n; i++)
            {
                dtemp = Math.Abs(dx[i]);
                if (dtemp > dmax)
                {
                    dmax = dtemp;
                    imax = ix;
                }
            }
            return (imax);
        }

        private static void purge(int n, int ll, double[] r, double[] q, double[] ra,
            double[] qa, double[] wrk, double[] eta, double[] oldeta, int step, double rnmp, double tol)
        {
            double t, tq, tr, reps1;
            int k, iteration;
            bool flag;
            double rnm = rnmp;

            if (step < ll + 2) return;

            k = svd_idamax(step - (ll + 1), eta, 1) + ll;
            if (Math.Abs(eta[k]) > reps)
            {
                reps1 = eps1 / reps;
                iteration = 0;
                flag = true;
                while (iteration < 2 && flag == true)
                {
                    if (rnm > tol)
                    {
                        tq = 0.0;
                        tr = 0.0;
                        for (int i = ll; i < step; i++)
                        {
                            store(n, (int)(storeVals.RETRQ), i, wrk);
                            t = -svd_ddot(n, qa, 1, wrk, 1);
                            tq += Math.Abs(t);
                            svd_daxpy(n, t, wrk, 1, q, 1);
                            t = -svd_ddot(n, ra, 1, wrk, 1);
                            tr += Math.Abs(t);
                            svd_daxpy(n, t, wrk, 1, r, 1);
                        }
                        svd_dcopy(n, q, 0, 1, qa, 0, 1);
                        t = -svd_ddot(n, r, 1, qa, 1);
                        tr += Math.Abs(t);
                        svd_daxpy(n, t, q, 1, r, 1);
                        svd_dcopy(n, r, 0, 1, ra, 0, 1);
                        rnm = Math.Sqrt(svd_ddot(n, ra, 1, r, 1));
                        if (tq <= reps1 && tr <= reps1 * rnm) flag = false;
                    }
                    iteration++;
                }
                for (int i = ll; i <= step; i++)
                {
                    eta[i] = eps1;
                    oldeta[i] = eps1;
                }
            }
            rnmp = rnm;
        }

        private static void ortbnd(double[] alf, double[] eta, double[] oldeta, double[] bet, int step, double rnm)
        {
            int i;
            if (step < 1) return;
            if (rnm != 0.0)
            {
                if (step > 1)
                {
                    oldeta[0] = (bet[1] * eta[1] + (alf[0] - alf[step]) * eta[0] -
                             bet[step] * oldeta[0]) / rnm + eps1;
                }
                for (i = 1; i <= step - 2; i++)
                    oldeta[i] = (bet[i + 1] * eta[i + 1] + (alf[i] - alf[step]) * eta[i] +
                             bet[i] * eta[i - 1] - bet[step] * oldeta[i]) / rnm + eps1;
            }
            oldeta[step - 1] = eps1;
            svd_dswap(step, oldeta, 1, eta, 1);
            eta[step] = eps1;
        }

        private static int lanczos_step(SMat A, int first, int last, double[][] wptr, double[] alf, double[] eta,
            double[] oldeta, double[] bet, ref int ll, ref int enough, ref double rnmp, ref double tolp, int n)
        {
            double t, anorm;
            double[] mid;
            double rnm = rnmp;
            double tol = tolp;
            int i, j;
            for (j = first; j < last; j++)
            {
                mid = wptr[2];
                wptr[2] = wptr[1];
                wptr[1] = mid;
                mid = wptr[3];
                wptr[3] = wptr[4];
                wptr[4] = mid;

                store(n, (int)(storeVals.STORQ), j - 1, wptr[2]);
                if (j - 1 < MAXLL) store(n, (int)(storeVals.STORP), j - 1, wptr[4]);
                bet[j] = rnm;

                if (bet[j] == 0.0)
                {
                    rnm = startv(A, wptr, j, n);
                    if (rnm < 0.0)
                    {
                        Utilities.LogMessageToFile(MainForm.logfile, String.Format("Fatal error: {0}", ierr));
                        Environment.Exit(1);
                    }
                    if (ierr != 0) return j;
                    if (rnm == 0.0) enough = 1;
                }

                if (enough == 1)
                {
                    mid = wptr[2];
                    wptr[2] = wptr[1];
                    wptr[1] = mid;
                    break;
                }

                t = 1.0 / rnm;
                svd_datx(n, t, wptr[0], 1, wptr[1], 1);
                svd_dscal(n, t, wptr[3], 1);
                svd_opb(A, wptr[3], wptr[0], OPBTemp);
                svd_daxpy(n, -rnm, wptr[2], 1, wptr[0], 1);
                alf[j] = svd_ddot(n, wptr[0], 1, wptr[3], 1);
                svd_daxpy(n, -alf[j], wptr[1], 1, wptr[0], 1);

                if (j <= MAXLL && (Math.Abs(alf[j - 1]) > 4.0 * Math.Abs(alf[j])))
                {
                    ll = j;
                }
                for (i = 0; i < svd_imin(ll, j - 1); i++)
                {
                    store(n, (int)(storeVals.RETRP), i, wptr[5]);
                    t = svd_ddot(n, wptr[5], 1, wptr[0], 1);
                    store(n, (int)(storeVals.RETRQ), i, wptr[5]);
                    svd_daxpy(n, -t, wptr[5], 1, wptr[0], 1);
                    eta[i] = eps1;
                    oldeta[i] = eps1;
                }

                t = svd_ddot(n, wptr[0], 1, wptr[4], 1);
                svd_daxpy(n, -t, wptr[2], 1, wptr[0], 1);
                if (bet[j] > 0.0) bet[j] = bet[j] + t;
                t = svd_ddot(n, wptr[0], 1, wptr[3], 1);
                svd_daxpy(n, -t, wptr[1], 1, wptr[0], 1);
                alf[j] = alf[j] + t;
                svd_dcopy(n, wptr[0], 0, 1, wptr[4], 0, 1);
                rnm = Math.Sqrt(svd_ddot(n, wptr[0], 1, wptr[4], 1));
                anorm = bet[j] + Math.Abs(alf[j]) + rnm;
                tol = reps * anorm;

                ortbnd(alf, eta, oldeta, bet, j, rnm);

                purge(n, ll, wptr[0], wptr[1], wptr[4], wptr[3], wptr[5], eta, oldeta, j, rnmp, tol);
                if (rnm <= tol) rnm = 0.0;
            }
            rnmp = rnm;
            tolp = tol;
            return j;
        }

        private static void imtqlb(int n, double[] d, double[] e, double[] bnd)
        {
            int last, l, m, i, iteration;
            bool exchange, convergence, underflow;
            double b, test, g, r, s, c, p, f;
            if (n == 1) return;
            ierr = 0;
            bnd[0] = 1.0;
            last = n - 1;
            for (i = 1; i < n; i++)
            {
                bnd[i] = 0.0;
                e[i - 1] = e[i];
            }
            e[last] = 0.0;
            for (l = 0; l < n; l++)
            {
                iteration = 0;
                while (iteration <= 30)
                {
                    for (m = l; m < n; m++)
                    {
                        convergence = false;
                        if (m == last) break;
                        else
                        {
                            test = Math.Abs(d[m]) + Math.Abs(d[m + 1]);
                            if (test + Math.Abs(e[m]) == test) convergence = false;
                        }
                        if (convergence) break;
                    }
                    p = d[l];
                    f = bnd[l];
                    if (m != l)
                    {
                        if (iteration == 30)
                        {
                            ierr = l;
                            return;
                        }
                        iteration += 1;
                        g = (d[l + 1] - p) / (2.0 * e[l]);
                        r = svd_pythag(g, 1.0);
                        g = d[m] - p + e[l] / (g + svd_fsign(r, g));
                        s = 1.0;
                        c = 1.0;
                        p = 0.0;
                        underflow = false;
                        i = m - 1;
                        while (underflow == false && i >= l)
                        {
                            f = s * e[i];
                            b = c * e[i];
                            r = svd_pythag(f, g);
                            e[i + 1] = r;
                            if (r == 0.0) underflow = false;
                            else
                            {
                                s = f / r;
                                c = g / r;
                                g = d[i + 1] - p;
                                r = (d[i] - g) * s + 2.0 * c * b;
                                p = s * r;
                                d[i + 1] = g + p;
                                g = c * r - b;
                                f = bnd[i + 1];
                                bnd[i + 1] = s * bnd[i] + c * f;
                                bnd[i] = c * bnd[i] - s * f;
                                i--;
                            }
                        }
                        if (underflow)
                        {
                            d[i + 1] -= p;
                            e[m] = 0.0;
                        }
                        else
                        {
                            d[l] -= p;
                            e[l] = g;
                            e[m] = 0.0;
                        }
                    }
                    else
                    {
                        exchange = false;
                        if (l != 0)
                        {
                            i = l;
                            while (i >= 1 && exchange == false)
                            {
                                if (p < d[i - 1])
                                {
                                    d[i] = d[i - 1];
                                    bnd[i] = bnd[i - 1];
                                    i--;
                                }
                                else exchange = false;
                            }
                        }
                        if (exchange) i = 0;
                        d[i] = p;
                        bnd[i] = f;
                        iteration = 31;
                    }
                }
            }
        }

        private static void svd_dsort2(int igap, int n, double[] array1, double[] array2)
        {
            double temp;
            int i, j, index;
            if (igap == 0) return;
            else
            {
                for (i = igap; i < n; i++)
                {
                    j = i - igap;
                    index = i;
                    while (j >= 0 && array1[j] > array1[index])
                    {
                        temp = array1[j];
                        array1[j] = array1[index];
                        array1[index] = temp;
                        temp = array2[j];
                        array2[j] = array2[index];
                        array2[index] = temp;
                        j -= igap;
                        index = j + igap;
                    }
                }
            }
            svd_dsort2(igap / 2, n, array1, array2);
        }

        private static int error_bound(ref int enough, double endl, double endr, double[] ritz, double[] bnd, int step, double tol)
        {
            int mid, i, neig;
            double gapl, gap;

            mid = svd_idamax(step + 1, bnd, 1);

            for (i = ((step + 1) + (step - 1)) / 2; i >= mid + 1; i -= 1)
                if (Math.Abs(ritz[i - 1] - ritz[i]) < eps34 * Math.Abs(ritz[i]))
                    if (bnd[i] > tol && bnd[i - 1] > tol)
                    {
                        bnd[i - 1] = Math.Sqrt(bnd[i] * bnd[i] + bnd[i - 1] * bnd[i - 1]);
                        bnd[i] = 0.0;
                    }


            for (i = ((step + 1) - (step - 1)) / 2; i <= mid - 1; i += 1)
                if (Math.Abs(ritz[i + 1] - ritz[i]) < eps34 * Math.Abs(ritz[i]))
                    if (bnd[i] > tol && bnd[i + 1] > tol)
                    {
                        bnd[i + 1] = Math.Sqrt(bnd[i] * bnd[i] + bnd[i + 1] * bnd[i + 1]);
                        bnd[i] = 0.0;
                    }

            neig = 0;
            gapl = ritz[step] - ritz[0];
            for (i = 0; i <= step; i++)
            {
                gap = gapl;
                if (i < step) gapl = ritz[i + 1] - ritz[i];
                gap = svd_dmin(gap, gapl);
                if (gap > bnd[i]) bnd[i] = bnd[i] * (bnd[i] / gap);
                if (bnd[i] <= 16.0 * eps * Math.Abs(ritz[i]))
                {
                    neig++;
                    if (enough == 0)
                    {
                        int k1 = 0;
                        if (endl < ritz[i]) k1 = 1;
                        int k2 = 0;
                        if (ritz[i] < endr) k2 = 1;
                        enough = k1 & k2;
                    }
                }
            }
            return neig;
        }

        private static int lanso(SMat A, int iterations, int dimensions, double endl, double endr,
            double[] ritz, double[] bnd, double[][] wptr, ref int neigp, int n)
        {
            double[] alf, eta, oldeta, bet, wrk;
            double rnm, tol;
            int ll, first, last, id2, id3, i, l, neig = 0, j = 0, intro = 0;

            alf = wptr[6];
            eta = wptr[7];
            oldeta = wptr[8];
            bet = wptr[9];
            wrk = wptr[5];

            rnm = 0.0;
            tol = 0.0;
            stpone(A, wptr, ref rnm, ref tol, n);

            if (rnm == 0.0 || ierr != 0) return 0;
            eta[0] = eps1;
            oldeta[0] = eps1;
            ll = 0;
            first = 1;
            last = svd_imin(dimensions + svd_imax(8, dimensions), iterations);
            int ENOUGH = 0;
            while (ENOUGH == 0)
            {
                if (rnm <= tol) rnm = 0.0;
                j = lanczos_step(A, first, last, wptr, alf, eta, oldeta, bet, ref ll, ref ENOUGH, ref rnm, ref tol, n);

                if (ENOUGH > 0) j = j - 1;
                else j = last - 1;
                first = j + 1;
                bet[j + 1] = rnm;

                l = 0;
                for (id2 = 0; id2 < j; id2++)
                {
                    if (l > j) break;
                    for (i = l; i <= j; i++) if (bet[i + 1] == 0.0) break;
                    if (i > j) i = j;

                    svd_dcopy(i - l + 1, alf, l, 1, ritz, l, -1);
                    svd_dcopy(i - l, bet, l + 1, 1, wrk, l + 1, -1);
                    imtqlb(i - l + 1, ritz, wrk, bnd);

                    if (ierr != 0)
                    {
                        Utilities.LogMessageToFile(MainForm.logfile, String.Format("svdLAS2: imtqlb failed to converge {0}", ierr));
                    }
                    for (id3 = l; id3 <= i; id3++)
                    {
                        bnd[id3] = rnm * Math.Abs(bnd[id3]);
                    }
                    l = i + 1;
                }

                svd_dsort2((j + 1) / 2, j + 1, ritz, bnd);

                neig = error_bound(ref ENOUGH, endl, endr, ritz, bnd, j, tol);
                neigp = neig;

                if (neig < dimensions)
                {
                    if (neig == 0)
                    {
                        last = first + 9;
                        intro = first;
                    }
                    else
                    {
                        last = first + svd_imax(3, 1 + ((j - intro) * (dimensions - neig)) / neig);
                    }
                    last = svd_imin(last, iterations);
                }
                else
                {
                    ENOUGH = 1;
                }
                int RES = 0;
                if (first >= iterations) RES = 1;
                ENOUGH = ENOUGH | RES;
            }
            store(n, (int)(storeVals.STORQ), j, wptr[1]);
            return j;
        }

        private static void imtql2(int nm, int n, double[] d, double[] e, double[] z)
        {
            int index, nnm, j, last, l, m, i, k, iteration;
            double b, test, g, r, s, c, p, f;
            bool convergence, underflow;
            if (n == 1) return;
            ierr = 0;
            last = n - 1;
            for (i = 1; i < n; i++) e[i - 1] = e[i];
            e[last] = 0.0;
            nnm = n * nm;
            for (l = 0; l < n; l++)
            {
                iteration = 0;
                while (iteration <= 30)
                {
                    for (m = l; m < n; m++)
                    {
                        convergence = false;
                        if (m == last) break;
                        else
                        {
                            test = Math.Abs(d[m]) + Math.Abs(d[m + 1]);
                            if (test + Math.Abs(e[m]) == test) convergence = true;
                        }
                        if (convergence) break;
                    }
                    if (m != l)
                    {
                        if (iteration == 30)
                        {
                            ierr = l;
                            return;
                        }
                        p = d[l];
                        iteration += 1;

                        g = (d[l + 1] - p) / (2.0 * e[l]);
                        r = svd_pythag(g, 1.0);
                        g = d[m] - p + e[l] / (g + svd_fsign(r, g));
                        s = 1.0;
                        c = 1.0;
                        p = 0.0;
                        underflow = false;
                        i = m - 1;
                        while (underflow == false && i >= l)
                        {
                            f = s * e[i];
                            b = c * e[i];
                            r = svd_pythag(f, g);
                            e[i + 1] = r;
                            if (r == 0.0) underflow = false;
                            else
                            {
                                s = f / r;
                                c = g / r;
                                g = d[i + 1] - p;
                                r = (d[i] - g) * s + 2.0 * c * b;
                                p = s * r;
                                d[i + 1] = g + p;
                                g = c * r - b;

                                for (k = 0; k < nnm; k += n)
                                {
                                    index = k + i;
                                    f = z[index + 1];
                                    z[index + 1] = s * z[index] + c * f;
                                    z[index] = c * z[index] - s * f;
                                }
                                i--;
                            }
                        }
                        if (underflow)
                        {
                            d[i + 1] -= p;
                            e[m] = 0.0;
                        }
                        else
                        {
                            d[l] -= p;
                            e[l] = g;
                            e[m] = 0.0;
                        }
                    }
                    else break;
                }
            }

            for (l = 1; l < n; l++)
            {
                i = l - 1;
                k = i;
                p = d[i];
                for (j = l; j < n; j++)
                {
                    if (d[j] < p)
                    {
                        k = j;
                        p = d[j];
                    }
                }
                if (k != i)
                {
                    d[k] = d[i];
                    d[i] = p;
                    for (j = 0; j < nnm; j += n)
                    {
                        p = z[j + i];
                        z[j + i] = z[j + k];
                        z[j + k] = p;
                    }
                }
            }
        }

        private static void svd_opa(SMat A, double[] x, double[] y)
        {
            int end, i, j;
            int[] pointr = A.pointr;
            int[] rowind = A.rowind;
            double[] value = A.value;

            SVDCount[(int)(svdCounters.SVD_MXV)]++;
            for (int k = 0; k < A.rows; ++k)
            {
                y[k] = 0.0;
            }

            for (i = 0; i < A.cols; i++)
            {
                end = pointr[i + 1];
                for (j = pointr[i]; j < end; j++)
                {
                    y[rowind[j]] += value[j] * x[i];
                }
            }
        }

        private static void rotateArray(SVDRec R, int x)
        {
            if (x == 0) return;
            x *= R.Vt.cols;

            int i, j, n, start, nRow, nCol;
            double t1, t2;
            int size = R.Vt.rows * R.Vt.cols;
            j = start = 0;
            t1 = R.Vt.value[0][0];
            for (i = 0; i < size; i++)
            {
                if (j >= x) n = j - x;
                else n = j - x + size;
                nRow = n / R.Vt.cols;
                nCol = n - nRow * R.Vt.cols;
                t2 = R.Vt.value[nRow][nCol];
                R.Vt.value[nRow][nCol] = t1;
                t1 = t2;
                j = n;
                if (j == start)
                {
                    start = ++j;
                    nRow = j / R.Vt.cols;
                    nCol = j - nRow * R.Vt.cols;
                    t1 = R.Vt.value[nRow][nCol];
                }
            }
        }

        private static int ritvec(int n, SMat A, SVDRec R, double kappa, double[] ritz, double[] bnd,
            double[] alf, double[] bet, double[] w2, int steps, int neig)
        {

            int js, jsq, i, k, id2, tmp, nsig = 0, x;
            double tmp0, tmp1, xnorm;
            double[] s;
            double[] xv2;
            double[] w1 = R.Vt.value[0];

            js = steps + 1;
            jsq = js * js;

            s = new double[jsq];
            for (k = 0; k < jsq; ++k) s[k] = 0.0;
            xv2 = new double[n];

            for (i = 0; i < jsq; i += (js + 1)) s[i] = 1.0;
            svd_dcopy(js, alf, 0, 1, w1, 0, -1);
            svd_dcopy(steps, bet, 1, 1, w2, 1, -1);

            imtql2(js, js, w1, w2, s);
            if (ierr != 0) return 0;

            nsig = 0;
            x = 0;
            id2 = jsq - js;
            for (k = 0; k < js; k++)
            {
                tmp = id2;
                if (bnd[k] <= kappa * Math.Abs(ritz[k]) && k > js - neig - 1)
                {
                    if (--x < 0) x = R.d - 1;
                    w1 = R.Vt.value[x];
                    for (i = 0; i < n; i++) w1[i] = 0.0;
                    for (i = 0; i < js; i++)
                    {
                        store(n, (int)(storeVals.RETRQ), i, w2);
                        svd_daxpy(n, s[tmp], w2, 1, w1, 1);
                        tmp -= js;
                    }
                    nsig++;
                }
                id2++;
            }

            // x is now the location of the highest singular value. 
            rotateArray(R, x);
            R.d = svd_imin(R.d, nsig);
            for (x = 0; x < R.d; x++)
            {
                svd_opb(A, R.Vt.value[x], xv2, OPBTemp);
                tmp0 = svd_ddot(n, R.Vt.value[x], 1, xv2, 1);
                svd_daxpy(n, -tmp0, R.Vt.value[x], 1, xv2, 1);
                tmp0 = Math.Sqrt(tmp0);
                xnorm = Math.Sqrt(svd_ddot(n, xv2, 1, xv2, 1));

                svd_opa(A, R.Vt.value[x], R.Ut.value[x]);
                tmp1 = 1.0 / tmp0;
                svd_dscal(A.rows, tmp1, R.Ut.value[x], 1);
                xnorm *= tmp1;
                bnd[i] = xnorm;
                R.S[x] = tmp0;
            }
            return nsig;
        }

        private static SVDRec svdLAS2(SMat A)
        {
            int ibeta, it, irnd, machep, negep, n, steps, nsig, neig;
            double kappa = 1e-6;

            double[] las2end = new double[2] { -1.0e-30, 1.0e-30 };
            double[] ritz, bnd;

            double[][] wptr = new double[10][];

            svdResetCounters();

            int dimensions = A.rows;
            if (A.cols < dimensions) dimensions = A.cols;
            int iterations = dimensions;

            // Check parameters 
            if (check_parameters(A, dimensions, iterations, las2end[0], las2end[1]) > 0) return null;

            // If A is wide, the SVD is computed on its transpose for speed.
            bool transpose = false;
            if (A.cols >= A.rows * 1.2)
            {
                Utilities.LogMessageToFile(MainForm.logfile, "TRANSPOSING THE MATRIX FOR SPEED\n");
                transpose = true;
                A = svdTransposeS(A);
            }

            n = A.cols;
            // Compute machine precision 
            ibeta = it = irnd = machep = negep = 0;
            machar(ref ibeta, ref it, ref irnd, ref machep, ref negep);
            eps1 = eps * Math.Sqrt((double)n);
            reps = Math.Sqrt(eps);
            eps34 = reps * Math.Sqrt(reps);
            Utilities.LogMessageToFile(MainForm.logfile, String.Format("Machine precision {0} {1} {2} {3} {4}", ibeta, it, irnd, machep, negep));

            // Allocate temporary space.  
            wptr[0] = new double[n];
            for (int i = 0; i < n; ++i) wptr[0][i] = 0.0;
            wptr[1] = new double[n];
            wptr[2] = new double[n];
            wptr[3] = new double[n];
            wptr[4] = new double[n];
            wptr[5] = new double[n];
            wptr[6] = new double[iterations];
            wptr[7] = new double[iterations];
            wptr[8] = new double[iterations];
            wptr[9] = new double[iterations + 1];
            ritz = new double[iterations + 1];
            for (int i = 0; i < iterations + 1; ++i) ritz[0] = 0.0;
            bnd = new double[iterations + 1];
            for (int i = 0; i < iterations + 1; ++i) bnd[0] = 0.0;

            LanStore = new double[iterations + MAXLL][];
            for (int i = 0; i < iterations + MAXLL; ++i)
            {
                LanStore[i] = null;
            }
            OPBTemp = new double[A.rows];

            // Actually run the lanczos thing: 
            neig = 0;
            steps = lanso(A, iterations, dimensions, las2end[0], las2end[1], ritz, bnd, wptr, ref neig, n);

            Utilities.LogMessageToFile(MainForm.logfile, String.Format("NUMBER OF LANCZOS STEPS {0}", steps + 1));
            Utilities.LogMessageToFile(MainForm.logfile, String.Format("RITZ VALUES STABILIZED = RANK {0}", neig));

            kappa = svd_dmax(Math.Abs(kappa), eps34);

            SVDRec R = new SVDRec();
            R.d = dimensions;
            DMat Tmp1 = new DMat();
            Tmp1.rows = R.d;
            Tmp1.cols = A.rows;
            Tmp1.value = new double[Tmp1.rows][];
            for (int mm = 0; mm < Tmp1.rows; ++mm)
            {
                Tmp1.value[mm] = new double[Tmp1.cols];
                for (int j = 0; j < Tmp1.cols; ++j)
                {
                    Tmp1.value[mm][j] = 0.0;
                }
            }
            R.Ut = Tmp1;
            R.S = new double[R.d];
            for (int k = 0; k < R.d; ++k)
            {
                R.S[k] = 0.0;
            }
            DMat Tmp2 = new DMat();
            Tmp2.rows = R.d;
            Tmp2.cols = A.cols;
            Tmp2.value = new double[Tmp2.rows][];
            for (int mm = 0; mm < Tmp2.rows; ++mm)
            {
                Tmp2.value[mm] = new double[Tmp2.cols];
                for (int j = 0; j < Tmp2.cols; ++j)
                {
                    Tmp2.value[mm][j] = 0.0;
                }
            }
            R.Vt = Tmp2;

            nsig = ritvec(n, A, R, kappa, ritz, bnd, wptr[6], wptr[9], wptr[5], steps, neig);

            // This swaps and transposes the singular matrices if A was transposed. 
            if (transpose)
            {
                DMat T;
                T = R.Ut;
                R.Ut = R.Vt;
                R.Vt = T;
            }
            return R;
        }

        public static void ProcessData(float[][] weightMatrix, string resultnameblock, bool doTranspose)
        {
            DateTime start = DateTime.Now;

            SMat A = svdLoadSparseMatrix(weightMatrix);
            if (A == null)
            {
                Utilities.LogMessageToFile(MainForm.logfile, "Error reading matrix");
                Environment.Exit(1);
            }
            Utilities.LogMessageToFile(MainForm.logfile, String.Format("NUMBER OF ROWS {0}", A.rows));
            Utilities.LogMessageToFile(MainForm.logfile, String.Format("NUMBER OF COLS {0}", A.cols));

            if (doTranspose)
            {
                A = svdTransposeS(A);
            }

            Utilities.LogMessageToFile(MainForm.logfile, "Computing the SVD...\n");
            SVDRec R = svdLAS2(A);
            if (R == null)
            {
                Utilities.LogMessageToFile(MainForm.logfile, "Error SVD matrix");
                Environment.Exit(1);
            }

            //we reduce size to rank when writing matrices
            R.Ut.rows = R.d;
            R.Vt.rows = R.d;

            svdWriteDenseMatrix(R.Ut, resultnameblock + "-Ut");
            svdWriteDenseArray(R.S, R.d, resultnameblock + "-S");
            svdWriteDenseMatrix(R.Vt, resultnameblock + "-Vt");

            DateTime end = DateTime.Now;
            TimeSpan duration = end - start;
            double time = duration.Hours * 60.0 * 60.0 + duration.Minutes * 60.0 + duration.Seconds + duration.Milliseconds / 1000.0;
            Utilities.LogMessageToFile(MainForm.logfile, String.Format("Time for encoding: {0:########.00} seconds", time));
        }

        public static void ProcessData(string datafile, string resultnameblock, bool doTranspose)
        {
            DateTime start = DateTime.Now;

            SMat A = svdLoadSparseMatrix(datafile);
            if (A == null)
            {
                Utilities.LogMessageToFile(MainForm.logfile, "Error reading matrix");
                Environment.Exit(1);
            }
            Utilities.LogMessageToFile(MainForm.logfile, String.Format("NUMBER OF ROWS {0}", A.rows));
            Utilities.LogMessageToFile(MainForm.logfile, String.Format("NUMBER OF COLS {0}", A.cols));

            if (doTranspose)
            {
                A = svdTransposeS(A);
            }

            Utilities.LogMessageToFile(MainForm.logfile, "Computing the SVD...\n");
            SVDRec R = svdLAS2(A);
            if (R == null)
            {
                Utilities.LogMessageToFile(MainForm.logfile, "Error SVD matrix");
                Environment.Exit(1);
            }

            //we reduce size to rank when writing matrices
            R.Ut.rows = R.d;
            R.Vt.rows = R.d;

            svdWriteDenseMatrix(R.Ut, resultnameblock + "-Ut");
            svdWriteDenseArray(R.S, R.d, resultnameblock + "-S");
            svdWriteDenseMatrix(R.Vt, resultnameblock + "-Vt");

            DateTime end = DateTime.Now;
            TimeSpan duration = end - start;
            double time = duration.Hours * 60.0 * 60.0 + duration.Minutes * 60.0 + duration.Seconds + duration.Milliseconds / 1000.0;
            Utilities.LogMessageToFile(MainForm.logfile, String.Format("Time for encoding: {0:########.00} seconds", time));
        }
    }

    [Serializable]
    class EverGrowingDictionary
    {
        const int m_nDictionarySize = 0xffffff;

        private int m_nWordCounter;
        private int m_index;

        private int[] m_oneByteLookup = new int[256];
        private int[] m_twoByteLookup = new int[0xffff + 1];
        private char[] m_dictionary = new char[m_nDictionarySize];
        private int[] m_lookup = new int[0xffffff + 1];
        private int[] m_self = new int[m_nDictionarySize];
        private string[] m_vocabulary = null;

        public EverGrowingDictionary()
        {
            for (int i = 0; i < 256; ++i)
            {
                m_oneByteLookup[i] = 0;
            }
            for (int i = 0; i < 0xffff + 1; ++i)
            {
                m_twoByteLookup[i] = 0;
            }
            for (int i = 0; i < 0xffffff + 1; ++i)
            {
                m_lookup[i] = 0;
            }
            for (int i = 0; i < m_nDictionarySize; ++i)
            {
                m_self[i] = 0;
            }

            m_nWordCounter = 0;
            m_dictionary[0] = ' ';
            m_index = 1;
        }

        private int addToDictionary(char[] word, int len, int pos)
        {
            ++m_nWordCounter;
            int old_index = m_index;
            for (int k = 3; k < len; ++k)
            {
                m_dictionary[m_index++] = word[k];
                if (m_index >= m_nDictionarySize)
                {
                    //dictionary is full, next step cause buffer overflow
                    return -4;
                }
            }
            m_dictionary[m_index] = ' ';
            m_self[m_index] = m_nWordCounter;
            ++m_index;
            if (m_index >= m_nDictionarySize)
            {
                //dictionary is full, next step cause buffer overflow
                return -4;
            }
            m_self[old_index] = m_lookup[pos];
            m_lookup[pos] = old_index;
            return m_nWordCounter - 1;
        }

        public int processWord(char[] word, int len)
        {
            len = word.Length;
            int pos = 0;
            int m = len;
            if (m > 3) m = 3;
            for (int i = 0; i < m; ++i)
            {
                pos <<= 8;
                pos |= word[i];
            }
            pos &= 0x00ffffff;

            int lPos = m_lookup[pos];
            if (lPos == 0)
            {
                return addToDictionary(word, len, pos);
            }

            while (true)
            {
                bool isOK = true;
                int n = lPos;
                for (int k = 3; k < len; ++k, ++n)
                {
                    if (m_dictionary[n] != word[k])
                    {
                        isOK = false;
                        break;
                    }
                }
                if (isOK && m_dictionary[n] == 0x20)
                {
                    return m_self[n] - 1;  //that means the word is already in the dictionary
                }
                lPos = m_self[lPos];
                if (lPos == 0)
                {
                    return addToDictionary(word, len, pos);
                }
            }
        }

        public int GetDictionarySize()
        {
            return m_index;
        }

        public int GetNumberOfWords()
        {
            return m_nWordCounter;
        }

        public int GetWordIndex(string strWord)
        {
            if (strWord == null) return -1;
            int len = strWord.Length;
            if (len > 255) return -2;
            if (len == 0) return -5;
            char[] word = strWord.ToCharArray();
            if (len > 3) return processWord(word, len);

            if (len == 3)
            {
                char[] wordA = new char[4];
                wordA[0] = word[0];
                wordA[1] = word[1];
                wordA[2] = word[2];
                wordA[3] = '_';
                return processWord(wordA, 4);
            }
            if (len == 2)
            {
                int nPos = word[0];
                nPos <<= 8;
                nPos |= word[1];
                if (m_twoByteLookup[nPos] == 0)
                {
                    ++m_nWordCounter;
                    m_twoByteLookup[nPos] = m_nWordCounter;
                    return m_nWordCounter - 1;
                }
                else
                {
                    return m_twoByteLookup[nPos] - 1;
                }
            }
            if (len == 1)
            {
                if (m_oneByteLookup[word[0]] == 0)
                {
                    ++m_nWordCounter;
                    m_oneByteLookup[word[0]] = m_nWordCounter;
                    return m_nWordCounter - 1;
                }
                else
                {
                    return m_oneByteLookup[word[0]] - 1;
                }
            }
            return -3;
        }

        public void MakeVocabulary()
        {
            if (m_nWordCounter == 0) return;
            m_vocabulary = new string[m_nWordCounter];
            for (int i = 0; i < m_nWordCounter; ++i)
            {
                m_vocabulary[i] = string.Empty;
            }

            //one byte lookup
            for (int i = 0; i < 256; ++i)
            {
                if (m_oneByteLookup[i] != 0)
                {
                    m_vocabulary[m_oneByteLookup[i] - 1] = ((char)i).ToString();
                }
            }

            //two bytes lookup
            char[] s = new char[2];
            for (int i = 0; i < 0xffff + 1; ++i)
            {
                if (m_twoByteLookup[i] != 0)
                {
                    s[0] = (char)(i >> 8);
                    s[1] = (char)(i & 0xff);
                    m_vocabulary[m_twoByteLookup[i] - 1] = new string(s);
                }
            }

            //other words
            char[] w = new char[256];
            for (int i = 0; i < 0xffffff + 1; ++i)
            {
                if (m_lookup[i] != 0)
                {
                    w[0] = (char)((i >> 16) & 0xff);
                    w[1] = (char)((i >> 8) & 0xff);
                    w[2] = (char)(i & 0xff);

                    int startindex = m_lookup[i];
                    while (true)
                    {
                        int index = startindex;
                        int cnt = 3;
                        while (m_dictionary[index] != ' ')
                        {
                            w[cnt] = m_dictionary[index];
                            ++cnt;
                            ++index;
                            if (index >= m_nDictionarySize) break;
                        }

                        if (index == startindex) //actually we should not get here
                        {
                            string s1 = new string(w, 0, cnt);
                            m_vocabulary[m_self[index + 1] - 1] = s1;
                            break;
                        }

                        if (w[cnt - 1] == '_') --cnt; //filter underscore symbol at the end, if exists
                        string s2 = new string(w, 0, cnt);
                        m_vocabulary[m_self[index] - 1] = s2;
                        startindex = m_self[startindex];
                        if (startindex == 0) break;
                    }
                }
            }
        }

        public void OutputVocabulary(string filename)
        {
            if (m_vocabulary == null) return;

            using (StreamWriter fileWordsStream = new StreamWriter(filename))
            {
                for (int i = 0; i < m_nWordCounter; ++i)
                {
                    if (m_vocabulary[i] == string.Empty)
                    {
                        Debug.WriteLine("Vocabulary is corrupted.");
                        Environment.Exit(1);
                    }
                    else
                    {
                        fileWordsStream.WriteLine(i.ToString() + " " + m_vocabulary[i]);
                    }
                }
                fileWordsStream.Flush();
                fileWordsStream.Close();
            }
        }
    }

    public class Among
    {
        public Among(System.String s, int substring_i, int result, System.String methodname, SnowballProgram methodobject)
        {
            this.s_size = s.Length;
            this.s = s;
            this.substring_i = substring_i;
            this.result = result;
            this.methodobject = methodobject;
            if (methodname.Length == 0)
            {
                this.method = null;
            }
            else
            {
                try
                {
                    this.method = methodobject.GetType().GetMethod(methodname, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.DeclaredOnly, null, new System.Type[0], null);
                }
                catch (System.MethodAccessException e)
                {
                    // FIXME - debug message
                    this.method = null;
                }
            }
        }

        public int s_size; /* search string */
        public System.String s; /* search string */
        public int substring_i; /* index to longest matching substring */
        public int result; /* result of the lookup */
        public System.Reflection.MethodInfo method; /* method to use if substring matches */
        public SnowballProgram methodobject; /* object to invoke method on */
    }

    public class SnowballProgram
    {
        /// <summary> Get the current string.</summary>
        virtual public System.String GetCurrent()
        {
            return current.ToString();
        }
        protected internal SnowballProgram()
        {
            current = new System.Text.StringBuilder();
            SetCurrent("");
        }

        /// <summary> Set the current string.</summary>
        public virtual void SetCurrent(System.String value_Renamed)
        {
            //// current.Replace(current.ToString(0, current.Length - 0), value_Renamed, 0, current.Length - 0);
            current.Remove(0, current.Length);
            current.Append(value_Renamed);
            cursor = 0;
            limit = current.Length;
            limit_backward = 0;
            bra = cursor;
            ket = limit;
        }

        // current string
        protected internal System.Text.StringBuilder current;

        protected internal int cursor;
        protected internal int limit;
        protected internal int limit_backward;
        protected internal int bra;
        protected internal int ket;

        protected internal virtual void copy_from(SnowballProgram other)
        {
            current = other.current;
            cursor = other.cursor;
            limit = other.limit;
            limit_backward = other.limit_backward;
            bra = other.bra;
            ket = other.ket;
        }

        protected internal virtual bool in_grouping(char[] s, int min, int max)
        {
            if (cursor >= limit)
                return false;
            char ch = current[cursor];
            if (ch > max || ch < min)
                return false;
            ch -= (char)(min);
            if ((s[ch >> 3] & (0x1 << (ch & 0x7))) == 0)
                return false;
            cursor++;
            return true;
        }

        protected internal virtual bool in_grouping_b(char[] s, int min, int max)
        {
            if (cursor <= limit_backward)
                return false;
            char ch = current[cursor - 1];
            if (ch > max || ch < min)
                return false;
            ch -= (char)(min);
            if ((s[ch >> 3] & (0x1 << (ch & 0x7))) == 0)
                return false;
            cursor--;
            return true;
        }

        protected internal virtual bool out_grouping(char[] s, int min, int max)
        {
            if (cursor >= limit)
                return false;
            char ch = current[cursor];
            if (ch > max || ch < min)
            {
                cursor++;
                return true;
            }
            ch -= (char)(min);
            if ((s[ch >> 3] & (0x1 << (ch & 0x7))) == 0)
            {
                cursor++;
                return true;
            }
            return false;
        }

        protected internal virtual bool out_grouping_b(char[] s, int min, int max)
        {
            if (cursor <= limit_backward)
                return false;
            char ch = current[cursor - 1];
            if (ch > max || ch < min)
            {
                cursor--;
                return true;
            }
            ch -= (char)(min);
            if ((s[ch >> 3] & (0x1 << (ch & 0x7))) == 0)
            {
                cursor--;
                return true;
            }
            return false;
        }

        protected internal virtual bool in_range(int min, int max)
        {
            if (cursor >= limit)
                return false;
            char ch = current[cursor];
            if (ch > max || ch < min)
                return false;
            cursor++;
            return true;
        }

        protected internal virtual bool in_range_b(int min, int max)
        {
            if (cursor <= limit_backward)
                return false;
            char ch = current[cursor - 1];
            if (ch > max || ch < min)
                return false;
            cursor--;
            return true;
        }

        protected internal virtual bool out_range(int min, int max)
        {
            if (cursor >= limit)
                return false;
            char ch = current[cursor];
            if (!(ch > max || ch < min))
                return false;
            cursor++;
            return true;
        }

        protected internal virtual bool out_range_b(int min, int max)
        {
            if (cursor <= limit_backward)
                return false;
            char ch = current[cursor - 1];
            if (!(ch > max || ch < min))
                return false;
            cursor--;
            return true;
        }

        protected internal virtual bool eq_s(int s_size, System.String s)
        {
            if (limit - cursor < s_size)
                return false;
            int i;
            for (i = 0; i != s_size; i++)
            {
                if (current[cursor + i] != s[i])
                    return false;
            }
            cursor += s_size;
            return true;
        }

        protected internal virtual bool eq_s_b(int s_size, System.String s)
        {
            if (cursor - limit_backward < s_size)
                return false;
            int i;
            for (i = 0; i != s_size; i++)
            {
                if (current[cursor - s_size + i] != s[i])
                    return false;
            }
            cursor -= s_size;
            return true;
        }

        protected internal virtual bool eq_v(System.Text.StringBuilder s)
        {
            return eq_s(s.Length, s.ToString());
        }

        protected internal virtual bool eq_v_b(System.Text.StringBuilder s)
        {
            return eq_s_b(s.Length, s.ToString());
        }

        protected internal virtual int find_among(Among[] v, int v_size)
        {
            int i = 0;
            int j = v_size;

            int c = cursor;
            int l = limit;

            int common_i = 0;
            int common_j = 0;

            bool first_key_inspected = false;

            while (true)
            {
                int k = i + ((j - i) >> 1);
                int diff = 0;
                int common = common_i < common_j ? common_i : common_j; // smaller
                Among w = v[k];
                int i2;
                for (i2 = common; i2 < w.s_size; i2++)
                {
                    if (c + common == l)
                    {
                        diff = -1;
                        break;
                    }
                    diff = current[c + common] - w.s[i2];
                    if (diff != 0)
                        break;
                    common++;
                }
                if (diff < 0)
                {
                    j = k;
                    common_j = common;
                }
                else
                {
                    i = k;
                    common_i = common;
                }
                if (j - i <= 1)
                {
                    if (i > 0)
                        break; // v->s has been inspected
                    if (j == i)
                        break; // only one item in v

                    // - but now we need to go round once more to get
                    // v->s inspected. This looks messy, but is actually
                    // the optimal approach.

                    if (first_key_inspected)
                        break;
                    first_key_inspected = true;
                }
            }
            while (true)
            {
                Among w = v[i];
                if (common_i >= w.s_size)
                {
                    cursor = c + w.s_size;
                    if (w.method == null)
                        return w.result;
                    bool res;
                    try
                    {
                        System.Object resobj = w.method.Invoke(w.methodobject, (System.Object[])new System.Object[0]);
                        // {{Aroush}} UPGRADE_TODO: The equivalent in .NET for method 'java.lang.Object.toString' may return a different value. 'ms-help://MS.VSCC.2003/commoner/redir/redirect.htm?keyword="jlca1043_3"'
                        res = resobj.ToString().Equals("true");
                    }
                    catch (System.Reflection.TargetInvocationException e)
                    {
                        res = false;
                        // FIXME - debug message
                    }
                    catch (System.UnauthorizedAccessException e)
                    {
                        res = false;
                        // FIXME - debug message
                    }
                    cursor = c + w.s_size;
                    if (res)
                        return w.result;
                }
                i = w.substring_i;
                if (i < 0)
                    return 0;
            }
        }

        // find_among_b is for backwards processing. Same comments apply
        protected internal virtual int find_among_b(Among[] v, int v_size)
        {
            int i = 0;
            int j = v_size;

            int c = cursor;
            int lb = limit_backward;

            int common_i = 0;
            int common_j = 0;

            bool first_key_inspected = false;

            while (true)
            {
                int k = i + ((j - i) >> 1);
                int diff = 0;
                int common = common_i < common_j ? common_i : common_j;
                Among w = v[k];
                int i2;
                for (i2 = w.s_size - 1 - common; i2 >= 0; i2--)
                {
                    if (c - common == lb)
                    {
                        diff = -1;
                        break;
                    }
                    diff = current[c - 1 - common] - w.s[i2];
                    if (diff != 0)
                        break;
                    common++;
                }
                if (diff < 0)
                {
                    j = k;
                    common_j = common;
                }
                else
                {
                    i = k;
                    common_i = common;
                }
                if (j - i <= 1)
                {
                    if (i > 0)
                        break;
                    if (j == i)
                        break;
                    if (first_key_inspected)
                        break;
                    first_key_inspected = true;
                }
            }
            while (true)
            {
                Among w = v[i];
                if (common_i >= w.s_size)
                {
                    cursor = c - w.s_size;
                    if (w.method == null)
                        return w.result;

                    bool res;
                    try
                    {
                        System.Object resobj = w.method.Invoke(w.methodobject, (System.Object[])new System.Object[0]);
                        // {{Aroush}} UPGRADE_TODO: The equivalent in .NET for method 'java.lang.Object.toString' may return a different value. 'ms-help://MS.VSCC.2003/commoner/redir/redirect.htm?keyword="jlca1043_3"'
                        res = resobj.ToString().Equals("true");
                    }
                    catch (System.Reflection.TargetInvocationException e)
                    {
                        res = false;
                        // FIXME - debug message
                    }
                    catch (System.UnauthorizedAccessException e)
                    {
                        res = false;
                        // FIXME - debug message
                    }
                    cursor = c - w.s_size;
                    if (res)
                        return w.result;
                }
                i = w.substring_i;
                if (i < 0)
                    return 0;
            }
        }

        /* to replace chars between c_bra and c_ket in current by the
        * chars in s.
        */
        protected internal virtual int replace_s(int c_bra, int c_ket, System.String s)
        {
            int adjustment = s.Length - (c_ket - c_bra);
            if (current.Length > bra)
                current.Replace(current.ToString(bra, ket - bra), s, bra, ket - bra);
            else
                current.Append(s);
            limit += adjustment;
            if (cursor >= c_ket)
                cursor += adjustment;
            else if (cursor > c_bra)
                cursor = c_bra;
            return adjustment;
        }

        protected internal virtual void slice_check()
        {
            if (bra < 0 || bra > ket || ket > limit || limit > current.Length)
            // this line could be removed
            {
                System.Console.Error.WriteLine("faulty slice operation");
                // FIXME: report error somehow.
                /*
                fprintf(stderr, "faulty slice operation:\n");
                debug(z, -1, 0);
                exit(1);
                */
            }
        }

        protected internal virtual void slice_from(System.String s)
        {
            slice_check();
            replace_s(bra, ket, s);
        }

        protected internal virtual void slice_from(System.Text.StringBuilder s)
        {
            slice_from(s.ToString());
        }

        protected internal virtual void slice_del()
        {
            slice_from("");
        }

        protected internal virtual void insert(int c_bra, int c_ket, System.String s)
        {
            int adjustment = replace_s(c_bra, c_ket, s);
            if (c_bra <= bra)
                bra += adjustment;
            if (c_bra <= ket)
                ket += adjustment;
        }

        protected internal virtual void insert(int c_bra, int c_ket, System.Text.StringBuilder s)
        {
            insert(c_bra, c_ket, s.ToString());
        }

        /* Copy the slice into the supplied StringBuffer */
        protected internal virtual System.Text.StringBuilder slice_to(System.Text.StringBuilder s)
        {
            slice_check();
            int len = ket - bra;
            //// s.Replace(s.ToString(0, s.Length - 0), current.ToString(bra, ket), 0, s.Length - 0);
            s.Remove(0, s.Length);
            s.Append(current.ToString(bra, ket));
            return s;
        }

        protected internal virtual System.Text.StringBuilder assign_to(System.Text.StringBuilder s)
        {
            //// s.Replace(s.ToString(0, s.Length - 0), current.ToString(0, limit), 0, s.Length - 0);
            s.Remove(0, s.Length);
            s.Append(current.ToString(0, limit));
            return s;
        }

        /*
        extern void debug(struct SN_env * z, int number, int line_count)
        {   int i;
        int limit = SIZE(z->p);
        //if (number >= 0) printf("%3d (line %4d): '", number, line_count);
        if (number >= 0) printf("%3d (line %4d): [%d]'", number, line_count,limit);
        for (i = 0; i <= limit; i++)
        {   if (z->lb == i) printf("{");
        if (z->bra == i) printf("[");
        if (z->c == i) printf("|");
        if (z->ket == i) printf("]");
        if (z->l == i) printf("}");
        if (i < limit)
        {   int ch = z->p[i];
        if (ch == 0) ch = '#';
        printf("%c", ch);
        }
        }
        printf("'\n");
        }*/
    }

    /// <summary> Generated class implementing code defined by a snowball script.</summary>
    public class EnglishStemmer : SnowballProgram
    {

        public EnglishStemmer()
        {
            InitBlock();
        }
        private void InitBlock()
        {
            a_0 = new Among[] { new Among("gener", -1, -1, "", this) };
            a_1 = new Among[] { new Among("ied", -1, 2, "", this), new Among("s", -1, 3, "", this), new Among("ies", 1, 2, "", this), new Among("sses", 1, 1, "", this), new Among("ss", 1, -1, "", this), new Among("us", 1, -1, "", this) };
            a_2 = new Among[] { new Among("", -1, 3, "", this), new Among("bb", 0, 2, "", this), new Among("dd", 0, 2, "", this), new Among("ff", 0, 2, "", this), new Among("gg", 0, 2, "", this), new Among("bl", 0, 1, "", this), new Among("mm", 0, 2, "", this), new Among("nn", 0, 2, "", this), new Among("pp", 0, 2, "", this), new Among("rr", 0, 2, "", this), new Among("at", 0, 1, "", this), new Among("tt", 0, 2, "", this), new Among("iz", 0, 1, "", this) };
            a_3 = new Among[] { new Among("ed", -1, 2, "", this), new Among("eed", 0, 1, "", this), new Among("ing", -1, 2, "", this), new Among("edly", -1, 2, "", this), new Among("eedly", 3, 1, "", this), new Among("ingly", -1, 2, "", this) };
            a_4 = new Among[] { new Among("anci", -1, 3, "", this), new Among("enci", -1, 2, "", this), new Among("ogi", -1, 13, "", this), new Among("li", -1, 16, "", this), new Among("bli", 3, 12, "", this), new Among("abli", 4, 4, "", this), new Among("alli", 3, 8, "", this), new Among("fulli", 3, 14, "", this), new Among("lessli", 3, 15, "", this), new Among("ousli", 3, 10, "", this), new Among("entli", 3, 5, "", this), new Among("aliti", -1, 8, "", this), new Among("biliti", -1, 12, "", this), new Among("iviti", -1, 11, "", this), new Among("tional", -1, 1, "", this), new Among("ational", 14, 7, "", this), new Among("alism", -1, 8, "", this), new Among("ation", -1, 7, "", this), new Among("ization", 17, 6, "", this), new Among("izer", -1, 6, "", this), new Among("ator", -1, 7, "", this), new Among("iveness", -1, 11, "", this), new Among("fulness", -1, 9, "", this), new Among("ousness", -1, 10, "", this) };
            a_5 = new Among[] { new Among("icate", -1, 4, "", this), new Among("ative", -1, 6, "", this), new Among("alize", -1, 3, "", this), new Among("iciti", -1, 4, "", this), new Among("ical", -1, 4, "", this), new Among("tional", -1, 1, "", this), new Among("ational", 5, 2, "", this), new Among("ful", -1, 5, "", this), new Among("ness", -1, 5, "", this) };
            a_6 = new Among[] { new Among("ic", -1, 1, "", this), new Among("ance", -1, 1, "", this), new Among("ence", -1, 1, "", this), new Among("able", -1, 1, "", this), new Among("ible", -1, 1, "", this), new Among("ate", -1, 1, "", this), new Among("ive", -1, 1, "", this), new Among("ize", -1, 1, "", this), new Among("iti", -1, 1, "", this), new Among("al", -1, 1, "", this), new Among("ism", -1, 1, "", this), new Among("ion", -1, 2, "", this), new Among("er", -1, 1, "", this), new Among("ous", -1, 1, "", this), new Among("ant", -1, 1, "", this), new Among("ent", -1, 1, "", this), new Among("ment", 15, 1, "", this), new Among("ement", 16, 1, "", this) };
            a_7 = new Among[] { new Among("e", -1, 1, "", this), new Among("l", -1, 2, "", this) };
            a_8 = new Among[] { new Among("succeed", -1, -1, "", this), new Among("proceed", -1, -1, "", this), new Among("exceed", -1, -1, "", this), new Among("canning", -1, -1, "", this), new Among("inning", -1, -1, "", this), new Among("earring", -1, -1, "", this), new Among("herring", -1, -1, "", this), new Among("outing", -1, -1, "", this) };
            a_9 = new Among[] { new Among("andes", -1, -1, "", this), new Among("atlas", -1, -1, "", this), new Among("bias", -1, -1, "", this), new Among("cosmos", -1, -1, "", this), new Among("dying", -1, 3, "", this), new Among("early", -1, 9, "", this), new Among("gently", -1, 7, "", this), new Among("howe", -1, -1, "", this), new Among("idly", -1, 6, "", this), new Among("lying", -1, 4, "", this), new Among("news", -1, -1, "", this), new Among("only", -1, 10, "", this), new Among("singly", -1, 11, "", this), new Among("skies", -1, 2, "", this), new Among("skis", -1, 1, "", this), new Among("sky", -1, -1, "", this), new Among("tying", -1, 5, "", this), new Among("ugly", -1, 8, "", this) };
        }

        private Among[] a_0;
        private Among[] a_1;
        private Among[] a_2;
        private Among[] a_3;
        private Among[] a_4;
        private Among[] a_5;
        private Among[] a_6;
        private Among[] a_7;
        private Among[] a_8;
        private Among[] a_9;

        private static readonly char[] g_v = new char[] { (char)(17), (char)(65), (char)(16), (char)(1) };
        private static readonly char[] g_v_WXY = new char[] { (char)(1), (char)(17), (char)(65), (char)(208), (char)(1) };
        private static readonly char[] g_valid_LI = new char[] { (char)(55), (char)(141), (char)(2) };

        private bool B_Y_found;
        private int I_p2;
        private int I_p1;

        protected internal virtual void copy_from(EnglishStemmer other)
        {
            B_Y_found = other.B_Y_found;
            I_p2 = other.I_p2;
            I_p1 = other.I_p1;
            base.copy_from(other);
        }

        private bool r_prelude()
        {
            int v_1;
            int v_2;
            int v_3;
            int v_4;
            // (, line 23
            // unset Y_found, line 24
            B_Y_found = false;
            // do, line 25
            v_1 = cursor;
            do
            {
                // (, line 25
                // [, line 25
                bra = cursor;
                // literal, line 25
                if (!(eq_s(1, "y")))
                {
                    goto lab0_brk;
                }
                // ], line 25
                ket = cursor;
                if (!(in_grouping(g_v, 97, 121)))
                {
                    goto lab0_brk;
                }
                // <-, line 25
                slice_from("Y");
                // set Y_found, line 25
                B_Y_found = true;
            }
            while (false);

        lab0_brk:;

            cursor = v_1;
            // do, line 26
            v_2 = cursor;

            do
            {
                // repeat, line 26
                while (true)
                {
                    v_3 = cursor;
                    do
                    {
                        // (, line 26
                        // goto, line 26
                        while (true)
                        {
                            v_4 = cursor;
                            do
                            {
                                // (, line 26
                                if (!(in_grouping(g_v, 97, 121)))
                                {
                                    goto lab5_brk;
                                }
                                // [, line 26
                                bra = cursor;
                                // literal, line 26
                                if (!(eq_s(1, "y")))
                                {
                                    goto lab5_brk;
                                }
                                // ], line 26
                                ket = cursor;
                                cursor = v_4;
                                goto golab4_brk;
                            }
                            while (false);

                        lab5_brk:;

                            cursor = v_4;
                            if (cursor >= limit)
                            {
                                goto lab3_brk;
                            }
                            cursor++;
                        }

                    golab4_brk:;

                        // <-, line 26
                        slice_from("Y");
                        // set Y_found, line 26
                        B_Y_found = true;
                        goto replab2;
                    }
                    while (false);

                lab3_brk:;

                    cursor = v_3;
                    goto replab2_brk;

                replab2:;
                }

            replab2_brk:;

            }
            while (false);

        lab1_brk:;

            cursor = v_2;
            return true;
        }

        private bool r_mark_regions()
        {
            int v_1;
            int v_2;
            // (, line 29
            I_p1 = limit;
            I_p2 = limit;
            // do, line 32
            v_1 = cursor;
            do
            {
                // (, line 32
                // or, line 36
                do
                {
                    v_2 = cursor;
                    do
                    {
                        // among, line 33
                        if (find_among(a_0, 1) == 0)
                        {
                            goto lab2_brk;
                        }
                        goto lab1_brk;
                    }
                    while (false);

                lab2_brk:;

                    cursor = v_2;
                    // (, line 36
                    // gopast, line 36
                    while (true)
                    {
                        do
                        {
                            if (!(in_grouping(g_v, 97, 121)))
                            {
                                goto lab4_brk;
                            }
                            goto golab3_brk;
                        }
                        while (false);

                    lab4_brk:;

                        if (cursor >= limit)
                        {
                            goto lab0_brk;
                        }
                        cursor++;
                    }

                golab3_brk:;

                    // gopast, line 36
                    while (true)
                    {
                        do
                        {
                            if (!(out_grouping(g_v, 97, 121)))
                            {
                                goto lab6_brk;
                            }
                            goto golab5_brk;
                        }
                        while (false);

                    lab6_brk:;

                        if (cursor >= limit)
                        {
                            goto lab0_brk;
                        }
                        cursor++;
                    }

                golab5_brk:;

                }
                while (false);

            lab1_brk:;

                // setmark p1, line 37
                I_p1 = cursor;
                // gopast, line 38
                while (true)
                {
                    do
                    {
                        if (!(in_grouping(g_v, 97, 121)))
                        {
                            goto lab8_brk;
                        }
                        goto golab7_brk;
                    }
                    while (false);

                lab8_brk:;

                    if (cursor >= limit)
                    {
                        goto lab0_brk;
                    }
                    cursor++;
                }

            golab7_brk:;

                // gopast, line 38
                while (true)
                {
                    do
                    {
                        if (!(out_grouping(g_v, 97, 121)))
                        {
                            goto lab10_brk;
                        }
                        goto golab9_brk;
                    }
                    while (false);

                lab10_brk:;

                    if (cursor >= limit)
                    {
                        goto lab0_brk;
                    }
                    cursor++;
                }

            golab9_brk:;

                // setmark p2, line 38
                I_p2 = cursor;
            }
            while (false);

        lab0_brk:;

            cursor = v_1;
            return true;
        }

        private bool r_shortv()
        {
            int v_1;
            // (, line 44
            // or, line 46

            do
            {
                v_1 = limit - cursor;
                do
                {
                    // (, line 45
                    if (!(out_grouping_b(g_v_WXY, 89, 121)))
                    {
                        goto lab1_brk;
                    }
                    if (!(in_grouping_b(g_v, 97, 121)))
                    {
                        goto lab1_brk;
                    }
                    if (!(out_grouping_b(g_v, 97, 121)))
                    {
                        goto lab1_brk;
                    }
                    goto lab0_brk;
                }
                while (false);

            lab1_brk:;

                cursor = limit - v_1;
                // (, line 47
                if (!(out_grouping_b(g_v, 97, 121)))
                {
                    return false;
                }
                if (!(in_grouping_b(g_v, 97, 121)))
                {
                    return false;
                }
                // atlimit, line 47
                if (cursor > limit_backward)
                {
                    return false;
                }
            }
            while (false);

        lab0_brk:;

            return true;
        }

        private bool r_R1()
        {
            if (!(I_p1 <= cursor))
            {
                return false;
            }
            return true;
        }

        private bool r_R2()
        {
            if (!(I_p2 <= cursor))
            {
                return false;
            }
            return true;
        }

        private bool r_Step_1a()
        {
            int among_var;
            int v_1;
            // (, line 53
            // [, line 54
            ket = cursor;
            // substring, line 54
            among_var = find_among_b(a_1, 6);
            if (among_var == 0)
            {
                return false;
            }
            // ], line 54
            bra = cursor;
            switch (among_var)
            {

                case 0:
                    return false;

                case 1:
                    // (, line 55
                    // <-, line 55
                    slice_from("ss");
                    break;

                case 2:
                    // (, line 57
                    // or, line 57

                    do
                    {
                        v_1 = limit - cursor;
                        do
                        {
                            // (, line 57
                            // next, line 57
                            if (cursor <= limit_backward)
                            {
                                goto lab1_brk;
                            }
                            cursor--;
                            // atlimit, line 57
                            if (cursor > limit_backward)
                            {
                                goto lab1_brk;
                            }
                            // <-, line 57
                            slice_from("ie");
                            goto lab0_brk;
                        }
                        while (false);

                    lab1_brk:;

                        cursor = limit - v_1;
                        // <-, line 57
                        slice_from("i");
                    }
                    while (false);

                lab0_brk:;

                    break;

                case 3:
                    // (, line 58
                    // next, line 58
                    if (cursor <= limit_backward)
                    {
                        return false;
                    }
                    cursor--;
                    // gopast, line 58
                    while (true)
                    {
                        do
                        {
                            if (!(in_grouping_b(g_v, 97, 121)))
                            {
                                goto lab3_brk;
                            }
                            goto golab2_brk;
                        }
                        while (false);

                    lab3_brk:;

                        if (cursor <= limit_backward)
                        {
                            return false;
                        }
                        cursor--;
                    }

                golab2_brk:;

                    // delete, line 58
                    slice_del();
                    break;
            }
            return true;
        }

        private bool r_Step_1b()
        {
            int among_var;
            int v_1;
            int v_3;
            int v_4;
            // (, line 63
            // [, line 64
            ket = cursor;
            // substring, line 64
            among_var = find_among_b(a_3, 6);
            if (among_var == 0)
            {
                return false;
            }
            // ], line 64
            bra = cursor;
            switch (among_var)
            {

                case 0:
                    return false;

                case 1:
                    // (, line 66
                    // call R1, line 66
                    if (!r_R1())
                    {
                        return false;
                    }
                    // <-, line 66
                    slice_from("ee");
                    break;

                case 2:
                    // (, line 68
                    // test, line 69
                    v_1 = limit - cursor;
                    // gopast, line 69
                    while (true)
                    {
                        do
                        {
                            if (!(in_grouping_b(g_v, 97, 121)))
                            {
                                goto lab1_brk;
                            }
                            goto golab0_brk;
                        }
                        while (false);

                    lab1_brk:;

                        if (cursor <= limit_backward)
                        {
                            return false;
                        }
                        cursor--;
                    }

                golab0_brk:;

                    cursor = limit - v_1;
                    // delete, line 69
                    slice_del();
                    // test, line 70
                    v_3 = limit - cursor;
                    // substring, line 70
                    among_var = find_among_b(a_2, 13);
                    if (among_var == 0)
                    {
                        return false;
                    }
                    cursor = limit - v_3;
                    switch (among_var)
                    {

                        case 0:
                            return false;

                        case 1:
                            // (, line 72
                            // <+, line 72
                            {
                                int c = cursor;
                                insert(cursor, cursor, "e");
                                cursor = c;
                            }
                            break;

                        case 2:
                            // (, line 75
                            // [, line 75
                            ket = cursor;
                            // next, line 75
                            if (cursor <= limit_backward)
                            {
                                return false;
                            }
                            cursor--;
                            // ], line 75
                            bra = cursor;
                            // delete, line 75
                            slice_del();
                            break;

                        case 3:
                            // (, line 76
                            // atmark, line 76
                            if (cursor != I_p1)
                            {
                                return false;
                            }
                            // test, line 76
                            v_4 = limit - cursor;
                            // call shortv, line 76
                            if (!r_shortv())
                            {
                                return false;
                            }
                            cursor = limit - v_4;
                            // <+, line 76
                            {
                                int c = cursor;
                                insert(cursor, cursor, "e");
                                cursor = c;
                            }
                            break;
                    }
                    break;
            }
            return true;
        }

        private bool r_Step_1c()
        {
            int v_1;
            int v_2;
            // (, line 82
            // [, line 83
            ket = cursor;
            // or, line 83

            do
            {
                v_1 = limit - cursor;
                do
                {
                    // literal, line 83
                    if (!(eq_s_b(1, "y")))
                    {
                        goto lab1_brk;
                    }
                    goto lab0_brk;
                }
                while (false);

            lab1_brk:;

                cursor = limit - v_1;
                // literal, line 83
                if (!(eq_s_b(1, "Y")))
                {
                    return false;
                }
            }
            while (false);

        lab0_brk:;

            // ], line 83
            bra = cursor;
            if (!(out_grouping_b(g_v, 97, 121)))
            {
                return false;
            }
            // not, line 84
            {
                v_2 = limit - cursor;
                do
                {
                    // atlimit, line 84
                    if (cursor > limit_backward)
                    {
                        goto lab2_brk;
                    }
                    return false;
                }
                while (false);

            lab2_brk:;

                cursor = limit - v_2;
            }
            // <-, line 85
            slice_from("i");
            return true;
        }

        private bool r_Step_2()
        {
            int among_var;
            // (, line 88
            // [, line 89
            ket = cursor;
            // substring, line 89
            among_var = find_among_b(a_4, 24);
            if (among_var == 0)
            {
                return false;
            }
            // ], line 89
            bra = cursor;
            // call R1, line 89
            if (!r_R1())
            {
                return false;
            }
            switch (among_var)
            {

                case 0:
                    return false;

                case 1:
                    // (, line 90
                    // <-, line 90
                    slice_from("tion");
                    break;

                case 2:
                    // (, line 91
                    // <-, line 91
                    slice_from("ence");
                    break;

                case 3:
                    // (, line 92
                    // <-, line 92
                    slice_from("ance");
                    break;

                case 4:
                    // (, line 93
                    // <-, line 93
                    slice_from("able");
                    break;

                case 5:
                    // (, line 94
                    // <-, line 94
                    slice_from("ent");
                    break;

                case 6:
                    // (, line 96
                    // <-, line 96
                    slice_from("ize");
                    break;

                case 7:
                    // (, line 98
                    // <-, line 98
                    slice_from("ate");
                    break;

                case 8:
                    // (, line 100
                    // <-, line 100
                    slice_from("al");
                    break;

                case 9:
                    // (, line 101
                    // <-, line 101
                    slice_from("ful");
                    break;

                case 10:
                    // (, line 103
                    // <-, line 103
                    slice_from("ous");
                    break;

                case 11:
                    // (, line 105
                    // <-, line 105
                    slice_from("ive");
                    break;

                case 12:
                    // (, line 107
                    // <-, line 107
                    slice_from("ble");
                    break;

                case 13:
                    // (, line 108
                    // literal, line 108
                    if (!(eq_s_b(1, "l")))
                    {
                        return false;
                    }
                    // <-, line 108
                    slice_from("og");
                    break;

                case 14:
                    // (, line 109
                    // <-, line 109
                    slice_from("ful");
                    break;

                case 15:
                    // (, line 110
                    // <-, line 110
                    slice_from("less");
                    break;

                case 16:
                    // (, line 111
                    if (!(in_grouping_b(g_valid_LI, 99, 116)))
                    {
                        return false;
                    }
                    // delete, line 111
                    slice_del();
                    break;
            }
            return true;
        }

        private bool r_Step_3()
        {
            int among_var;
            // (, line 115
            // [, line 116
            ket = cursor;
            // substring, line 116
            among_var = find_among_b(a_5, 9);
            if (among_var == 0)
            {
                return false;
            }
            // ], line 116
            bra = cursor;
            // call R1, line 116
            if (!r_R1())
            {
                return false;
            }
            switch (among_var)
            {

                case 0:
                    return false;

                case 1:
                    // (, line 117
                    // <-, line 117
                    slice_from("tion");
                    break;

                case 2:
                    // (, line 118
                    // <-, line 118
                    slice_from("ate");
                    break;

                case 3:
                    // (, line 119
                    // <-, line 119
                    slice_from("al");
                    break;

                case 4:
                    // (, line 121
                    // <-, line 121
                    slice_from("ic");
                    break;

                case 5:
                    // (, line 123
                    // delete, line 123
                    slice_del();
                    break;

                case 6:
                    // (, line 125
                    // call R2, line 125
                    if (!r_R2())
                    {
                        return false;
                    }
                    // delete, line 125
                    slice_del();
                    break;
            }
            return true;
        }

        private bool r_Step_4()
        {
            int among_var;
            int v_1;
            // (, line 129
            // [, line 130
            ket = cursor;
            // substring, line 130
            among_var = find_among_b(a_6, 18);
            if (among_var == 0)
            {
                return false;
            }
            // ], line 130
            bra = cursor;
            // call R2, line 130
            if (!r_R2())
            {
                return false;
            }
            switch (among_var)
            {

                case 0:
                    return false;

                case 1:
                    // (, line 133
                    // delete, line 133
                    slice_del();
                    break;

                case 2:
                    // (, line 134
                    // or, line 134

                    do
                    {
                        v_1 = limit - cursor;
                        do
                        {
                            // literal, line 134
                            if (!(eq_s_b(1, "s")))
                            {
                                goto lab1_brk;
                            }
                            goto lab0_brk;
                        }
                        while (false);

                    lab1_brk:;

                        cursor = limit - v_1;
                        // literal, line 134
                        if (!(eq_s_b(1, "t")))
                        {
                            return false;
                        }
                    }
                    while (false);

                lab0_brk:;

                    // delete, line 134
                    slice_del();
                    break;
            }
            return true;
        }

        private bool r_Step_5()
        {
            int among_var;
            int v_1;
            int v_2;
            // (, line 138
            // [, line 139
            ket = cursor;
            // substring, line 139
            among_var = find_among_b(a_7, 2);
            if (among_var == 0)
            {
                return false;
            }
            // ], line 139
            bra = cursor;
            switch (among_var)
            {

                case 0:
                    return false;

                case 1:
                    // (, line 140
                    // or, line 140

                    do
                    {
                        v_1 = limit - cursor;
                        do
                        {
                            // call R2, line 140
                            if (!r_R2())
                            {
                                goto lab1_brk;
                            }
                            goto lab0_brk;
                        }
                        while (false);

                    lab1_brk:;

                        cursor = limit - v_1;
                        // (, line 140
                        // call R1, line 140
                        if (!r_R1())
                        {
                            return false;
                        }
                        // not, line 140
                        {
                            v_2 = limit - cursor;
                            do
                            {
                                // call shortv, line 140
                                if (!r_shortv())
                                {
                                    goto lab2_brk;
                                }
                                return false;
                            }
                            while (false);

                        lab2_brk:;

                            cursor = limit - v_2;
                        }
                    }
                    while (false);
                lab0_brk:;
                    // delete, line 140
                    slice_del();
                    break;

                case 2:
                    // (, line 141
                    // call R2, line 141
                    if (!r_R2())
                    {
                        return false;
                    }
                    // literal, line 141
                    if (!(eq_s_b(1, "l")))
                    {
                        return false;
                    }
                    // delete, line 141
                    slice_del();
                    break;
            }
            return true;
        }

        private bool r_exception2()
        {
            // (, line 145
            // [, line 147
            ket = cursor;
            // substring, line 147
            if (find_among_b(a_8, 8) == 0)
            {
                return false;
            }
            // ], line 147
            bra = cursor;
            // atlimit, line 147
            if (cursor > limit_backward)
            {
                return false;
            }
            return true;
        }

        private bool r_exception1()
        {
            int among_var;
            // (, line 157
            // [, line 159
            bra = cursor;
            // substring, line 159
            among_var = find_among(a_9, 18);
            if (among_var == 0)
            {
                return false;
            }
            // ], line 159
            ket = cursor;
            // atlimit, line 159
            if (cursor < limit)
            {
                return false;
            }
            switch (among_var)
            {

                case 0:
                    return false;

                case 1:
                    // (, line 163
                    // <-, line 163
                    slice_from("ski");
                    break;

                case 2:
                    // (, line 164
                    // <-, line 164
                    slice_from("sky");
                    break;

                case 3:
                    // (, line 165
                    // <-, line 165
                    slice_from("die");
                    break;

                case 4:
                    // (, line 166
                    // <-, line 166
                    slice_from("lie");
                    break;

                case 5:
                    // (, line 167
                    // <-, line 167
                    slice_from("tie");
                    break;

                case 6:
                    // (, line 171
                    // <-, line 171
                    slice_from("idl");
                    break;

                case 7:
                    // (, line 172
                    // <-, line 172
                    slice_from("gentl");
                    break;

                case 8:
                    // (, line 173
                    // <-, line 173
                    slice_from("ugli");
                    break;

                case 9:
                    // (, line 174
                    // <-, line 174
                    slice_from("earli");
                    break;

                case 10:
                    // (, line 175
                    // <-, line 175
                    slice_from("onli");
                    break;

                case 11:
                    // (, line 176
                    // <-, line 176
                    slice_from("singl");
                    break;
            }
            return true;
        }

        private bool r_postlude()
        {
            int v_1;
            int v_2;
            // (, line 192
            // Boolean test Y_found, line 192
            if (!(B_Y_found))
            {
                return false;
            }
            // repeat, line 192
            while (true)
            {
                v_1 = cursor;
                do
                {
                    // (, line 192
                    // goto, line 192
                    while (true)
                    {
                        v_2 = cursor;
                        do
                        {
                            // (, line 192
                            // [, line 192
                            bra = cursor;
                            // literal, line 192
                            if (!(eq_s(1, "Y")))
                            {
                                goto lab3_brk;
                            }
                            // ], line 192
                            ket = cursor;
                            cursor = v_2;
                            goto golab2_brk;
                        }
                        while (false);

                    lab3_brk:;

                        cursor = v_2;
                        if (cursor >= limit)
                        {
                            goto lab1_brk;
                        }
                        cursor++;
                    }
                golab2_brk:;

                    // <-, line 192
                    slice_from("y");
                    goto replab0;
                }
                while (false);

            lab1_brk:;

                cursor = v_1;
                goto replab0_brk;

            replab0:;
            }

        replab0_brk:;

            return true;
        }

        public virtual bool Stem()
        {
            int v_1;
            int v_2;
            int v_3;
            int v_4;
            int v_5;
            int v_6;
            int v_7;
            int v_8;
            int v_9;
            int v_10;
            int v_11;
            int v_12;
            int v_13;
            // (, line 194
            // or, line 196

            do
            {
                v_1 = cursor;
                do
                {
                    // call exception1, line 196
                    if (!r_exception1())
                    {
                        goto lab1_brk;
                    }
                    goto lab0_brk;
                }
                while (false);

            lab1_brk:;

                cursor = v_1;
                // (, line 196
                // test, line 198
                v_2 = cursor;
                // hop, line 198
                {
                    int c = cursor + 3;
                    if (0 > c || c > limit)
                    {
                        return false;
                    }
                    cursor = c;
                }
                cursor = v_2;
                // do, line 199
                v_3 = cursor;
                do
                {
                    // call prelude, line 199
                    if (!r_prelude())
                    {
                        goto lab2_brk;
                    }
                }
                while (false);

            lab2_brk:;

                cursor = v_3;
                // do, line 200
                v_4 = cursor;
                do
                {
                    // call mark_regions, line 200
                    if (!r_mark_regions())
                    {
                        goto lab3_brk;
                    }
                }
                while (false);

            lab3_brk:;

                cursor = v_4;
                // backwards, line 201
                limit_backward = cursor; cursor = limit;
                // (, line 201
                // do, line 203
                v_5 = limit - cursor;
                do
                {
                    // call Step_1a, line 203
                    if (!r_Step_1a())
                    {
                        goto lab4_brk;
                    }
                }
                while (false);

            lab4_brk:;

                cursor = limit - v_5;
                // or, line 205

                do
                {
                    v_6 = limit - cursor;
                    do
                    {
                        // call exception2, line 205
                        if (!r_exception2())
                        {
                            goto lab6_brk;
                        }
                        goto lab5_brk;
                    }
                    while (false);

                lab6_brk:;

                    cursor = limit - v_6;
                    // (, line 205
                    // do, line 207
                    v_7 = limit - cursor;
                    do
                    {
                        // call Step_1b, line 207
                        if (!r_Step_1b())
                        {
                            goto lab7_brk;
                        }
                    }
                    while (false);

                lab7_brk:;

                    cursor = limit - v_7;
                    // do, line 208
                    v_8 = limit - cursor;
                    do
                    {
                        // call Step_1c, line 208
                        if (!r_Step_1c())
                        {
                            goto lab8_brk;
                        }
                    }
                    while (false);

                lab8_brk:;

                    cursor = limit - v_8;
                    // do, line 210
                    v_9 = limit - cursor;
                    do
                    {
                        // call Step_2, line 210
                        if (!r_Step_2())
                        {
                            goto lab9_brk;
                        }
                    }
                    while (false);

                lab9_brk:;

                    cursor = limit - v_9;
                    // do, line 211
                    v_10 = limit - cursor;
                    do
                    {
                        // call Step_3, line 211
                        if (!r_Step_3())
                        {
                            goto lab10_brk;
                        }
                    }
                    while (false);

                lab10_brk:;

                    cursor = limit - v_10;
                    // do, line 212
                    v_11 = limit - cursor;
                    do
                    {
                        // call Step_4, line 212
                        if (!r_Step_4())
                        {
                            goto lab11_brk;
                        }
                    }
                    while (false);

                lab11_brk:;

                    cursor = limit - v_11;
                    // do, line 214
                    v_12 = limit - cursor;
                    do
                    {
                        // call Step_5, line 214
                        if (!r_Step_5())
                        {
                            goto lab12_brk;
                        }
                    }
                    while (false);

                lab12_brk:;

                    cursor = limit - v_12;
                }
                while (false);

            lab5_brk:;

                cursor = limit_backward; // do, line 217
                v_13 = cursor;
                do
                {
                    // call postlude, line 217
                    if (!r_postlude())
                    {
                        goto lab13_brk;
                    }
                }
                while (false);

            lab13_brk:;

                cursor = v_13;
            }
            while (false);

        lab0_brk:;

            return true;
        }
    }

    class StopWordFilter
    {
        private char[] m_stopWords = null;
        private int m_nSize = 0;
        private int[] m_oneByteLookup = new int[256];
        private int[] m_twoByteLookup = new int[0xffff + 1];
        private int[] m_twoByteSelf = null;

        private void makeOneByteSelfAddress()
        {
            for (int i = 0; i < 256; ++i)
            {
                m_oneByteLookup[i] = 0x00;
            }
            int scan = 0x202020;
            for (int i = 0; i < m_nSize; ++i)
            {
                scan <<= 8;
                scan |= m_stopWords[i];
                scan &= 0xffffff;
                if ((scan >> 16) == 0x20 && (scan & 0xff) == 0x20)
                {
                    m_oneByteLookup[m_stopWords[i - 1]] = i - 1;
                }
            }
        }

        void makeTwoByteSelfAddress()
        {
            m_twoByteSelf = new int[m_nSize + 1];
            for (int i = 0; i < 0xffff + 1; ++i)
            {
                m_twoByteLookup[i] = 0x00;
            }
            for (int i = 0; i < m_nSize + 1; ++i)
            {
                m_twoByteSelf[i] = 0x00;
            }
            int scan = 0x202020;
            for (int i = 0; i < m_nSize; ++i)
            {
                scan <<= 8;
                scan |= m_stopWords[i];
                scan &= 0xffffff;
                if ((scan >> 16) == 0x20 && (((scan >> 8) & 0xff) != 0x20 && (scan & 0xff) != 0x20))
                {
                    m_twoByteSelf[i + 1] = m_twoByteLookup[scan & 0xffff];
                    m_twoByteLookup[scan & 0xffff] = i + 1;
                }
            }
        }

        public bool isThere(byte[] word, int len)
        {
            if (word == null) return false;
            if (len <= 0) len = word.Length;

            if (len == 0) return false;
            if (len == 1)
            {
                if (m_oneByteLookup[word[0]] > 0) return true;
                else return false;
            }
            if (len == 2)
            {
                int scan = word[0];
                scan <<= 8;
                scan |= word[1];
                if (m_twoByteLookup[scan] > 0) return true;
                else return false;
            }
            if (len > 2)
            {
                int scan = word[0];
                scan <<= 8;
                scan |= word[1];
                int pos = m_twoByteLookup[scan];
                if (pos == 0) return false;
                while (true)
                {
                    bool isOK = true;
                    int n = pos;
                    for (int k = 2; k < len; ++k)
                    {
                        if (m_stopWords[n] != word[k])
                        {
                            isOK = false;
                            break;
                        }
                        ++n;
                        if (n == m_nSize) return false;
                    }
                    if (isOK == true) return true;
                    pos = m_twoByteSelf[pos];
                    if (pos == 0) return false;
                }
            }
            return false;
        }

        public StopWordFilter()
        {
            string s = "";
            s += "about a above across after again against all almost alone along ";
            s += "already also although always among an and another any anybody ";
            s += "0 1 2 3 4 5 6 7 8 9 ";
            s += "anyone anything anywhere are area areas around as ask asked asking ";
            s += "asks at away b back backed backing backs be became because become ";
            s += "becomes been before began behind being beings best better between ";
            s += "big both but by c came can cannot case cases certain certainly ";
            s += "clear clearly come could d did differ different differently do ";
            s += "does done down downed downing downs during e each early either ";
            s += "end ended ending ends enough even evenly ever every everybody ";
            s += "everyone everything everywhere f face faces fact facts far felt ";
            s += "few find finds first for four from full fully further furthered ";
            s += "furthering furthers g gave general generally get gets give given ";
            s += "gives go going good goods got great greater greatest group grouped ";
            s += "grouping groups h had has have having he her here herself high ";
            s += "higher highest him himself his how however i if important in ";
            s += "interest interested interesting interests into is it its itself ";
            s += "j just k keep keeps kind knew know known knows l large largely ";
            s += "last later latest least less let lets like likely long longer longest ";
            s += "m made make making man many may me member members men might more ";
            s += "most mostly mr mrs much must my myself n necessary need needed ";
            s += "needing needs never new new newer newest next no nobody non noone ";
            s += "not nothing now nowhere number numbers o of off often old older ";
            s += "oldest on once one only open opened opening opens or order ordered ";
            s += "ordering orders other others our out over p part parted parting ";
            s += "parts per perhaps place places point pointed pointing points possible ";
            s += "present presented presenting presents problem problems put puts q ";
            s += "quite r rather really right right room rooms s said same saw say ";
            s += "says second seconds see seem seemed seeming seems sees several shall ";
            s += "she should show showed showing shows side sides since small smaller ";
            s += "smallest so some somebody someone something somewhere state states ";
            s += "still such sure t take taken than that the their them then there ";
            s += "therefore these they thing things think thinks this those though ";
            s += "thought thoughts three through thus to today together too took toward ";
            s += "turn turned turning turns two u under until up upon us use used uses ";
            s += "v very w want wanted wanting wants was way ways we well wells went ";
            s += "were what when where whether which while who whole whose why will ";
            s += "with within without work worked working works would x y year years ";
            s += "yet you young younger youngest your z yours";
            m_stopWords = s.ToCharArray();

            m_nSize = m_stopWords.Length;
            makeOneByteSelfAddress();
            makeTwoByteSelfAddress();
        }
    }

    /* Copyright 2011, Andrew Polar

   Licensed under the Apache License, Version 2.0 (the "License");
   you may not use this file except in compliance with the License.
   You may obtain a copy of the License at

       http://www.apache.org/licenses/LICENSE-2.0

   Unless required by applicable law or agreed to in writing, software
   distributed under the License is distributed on an "AS IS" BASIS,
   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
   See the License for the specific language governing permissions and
   limitations under the License.
 */

    //This is simple test of efficiency of Latent Semantic Analysis prepared by Andrew Polar.
    //Domain: EzCodeSample.com

    class LSA
    {
        static StopWordFilter stopWordFilter = new StopWordFilter();
        static EnglishStemmer englishStemmer = new EnglishStemmer();
        static EverGrowingDictionary dictionary = new EverGrowingDictionary();
        static System.Text.Encoding enc = System.Text.Encoding.ASCII;
        static float[,] U = null;
        static float[,] V;
        static float[][] matrix = null;
        static float[,] LSAMatrixDocTerm = null;
        static float[] singularValues;
        static int URows = 0;
        static int UCols = 0;
        //Next property contains number of categories that are identified correctly.
        static int m_totalCorrectCategories = 0;
        static string resultFile;


        static byte[] charFilter = {
            0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20, //spaces  8
	        0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20, //spaces  16
	        0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20, //spaces  24
	        0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20, //spaces  32
	        0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20, //spaces  40
	        0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20, //spaces  48
	        0x30, 0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, //numbers  56
	        0x38, 0x39, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20, //2 numbers and spaces  64
	        0x20, 0x61, 0x62, 0x63, 0x64, 0x65, 0x66, 0x67, //upper case to lower case 72
	        0x68, 0x69, 0x6A, 0x6B, 0x6C, 0x6D, 0x6E, 0x6F, //upper case to lower case 80
	        0x70, 0x71, 0x72, 0x73, 0x74, 0x75, 0x76, 0x77, //upper case to lower case 88
	        0x78, 0x79, 0x7A, 0x20, 0x20, 0x20, 0x20, 0x20, //96
	        0x20, 0x61, 0x62, 0x63, 0x64, 0x65, 0x66, 0x67, //this must be lower case 104
	        0x68, 0x69, 0x6A, 0x6B, 0x6C, 0x6D, 0x6E, 0x6F, //lower case 112
	        0x70, 0x71, 0x72, 0x73, 0x74, 0x75, 0x76, 0x77, //120
	        0x78, 0x79, 0x7A, 0x20, 0x20, 0x20, 0x20, 0x20  //128
        };

        private const int nWordSize = 256;
        static byte[] word = new byte[nWordSize];

        private static void enumerateFiles(List<string> files, string folder, string extension)
        {
            DirectoryInfo dirInfo = new DirectoryInfo(folder);
            FileInfo[] fiArray = dirInfo.GetFiles(extension, SearchOption.AllDirectories);
            foreach (FileInfo fi in fiArray)
            {
                files.Add(fi.FullName);
            }
        }

        private static byte[] GetFileData(string fileName)
        {
            FileStream fStream = new FileStream(fileName, FileMode.Open, FileAccess.Read);
            int length = (int)fStream.Length;
            byte[] data = new byte[length];
            int count;
            int sum = 0;
            while ((count = fStream.Read(data, sum, length - sum)) > 0)
            {
                sum += count;
            }
            fStream.Close();
            return data;
        }

        private static byte[] GetFileData(Feature f)
        {
            byte[] data = new byte[f.doc.Length];
            data = Encoding.ASCII.GetBytes(f.doc);
            return data;
        }

        private static void processFiles(ref List<string> files)
        {
            string path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string dataFolder = Path.Combine(path, "data");
            Directory.CreateDirectory(dataFolder);
            string fileList = Path.Combine(dataFolder, "ProcessedFilesList.txt");
            string docWordMatrix = Path.Combine(dataFolder, "DocWordMatrix.dat");
            if (File.Exists(fileList)) File.Delete(fileList);
            if (File.Exists(docWordMatrix)) File.Delete(docWordMatrix);

            StreamWriter fileListStream = new StreamWriter(fileList);
            BinaryWriter docWordStream = new BinaryWriter(File.Open(docWordMatrix, FileMode.Create));

            ArrayList wordCounter = new ArrayList();
            int nFileCounter = 0;
            foreach (string file in files)
            {
                wordCounter.Clear();
                int nWordsSoFar = dictionary.GetNumberOfWords();
                wordCounter.Capacity = nWordsSoFar;
                for (int i = 0; i < nWordsSoFar; ++i)
                {
                    wordCounter.Add(0);
                }

                byte[] data = GetFileData(file);
                int counter = 0;
                for (int i = 0; i < data.Length; ++i)
                {
                    byte b = data[i];
                    if (b < 128)
                    {
                        b = charFilter[b];
                        if (b != 0x20 && counter < nWordSize)
                        {
                            word[counter] = b;
                            ++counter;
                        }
                        else
                        {
                            if (counter > 0)
                            {
                                if (!stopWordFilter.isThere(word, counter))
                                {
                                    string strWord = enc.GetString(word, 0, counter);
                                    englishStemmer.SetCurrent(strWord);
                                    if (englishStemmer.Stem())
                                    {
                                        strWord = englishStemmer.GetCurrent();
                                    }
                                    int nWordIndex = dictionary.GetWordIndex(strWord);

                                    //we check errors
                                    if (nWordIndex < 0)
                                    {
                                        if (nWordIndex == -1)
                                        {
                                            Debug.WriteLine("Erorr: word = NULL");
                                            Environment.Exit(1);
                                        }
                                        if (nWordIndex == -2)
                                        {
                                            Debug.WriteLine("Error: word length > 255");
                                            Environment.Exit(1);
                                        }
                                        if (nWordIndex == -3)
                                        {
                                            Debug.WriteLine("Error: uknown");
                                            Environment.Exit(1);
                                        }
                                        if (nWordIndex == -4)
                                        {
                                            Debug.WriteLine("Error: memory buffer for dictionary is too short");
                                            Environment.Exit(1);
                                        }
                                        if (nWordIndex == -5)
                                        {
                                            Debug.WriteLine("Error: word length = 0");
                                            Environment.Exit(1);
                                        }
                                    }

                                    if (nWordIndex < nWordsSoFar)
                                    {
                                        int element = (int)wordCounter[nWordIndex];
                                        wordCounter[nWordIndex] = element + 1;
                                    }
                                    else
                                    {
                                        wordCounter.Add(1);
                                        ++nWordsSoFar;
                                    }
                                }
                                counter = 0;
                            } //word processed
                        }
                    }
                }//file processed
                Utilities.LogMessageToFile(MainForm.logfile, "File: " + file + ", words: " + dictionary.GetNumberOfWords() + ", size: " + dictionary.GetDictionarySize());
                fileListStream.WriteLine(nFileCounter.ToString() + " " + file);

                int pos = 0;
                foreach (int x in wordCounter)
                {
                    if (x > 0)
                    {
                        docWordStream.Write(nFileCounter);
                        docWordStream.Write(pos);
                        short value = (short)(x);
                        docWordStream.Write(value);
                    }
                    ++pos;
                }

                ++nFileCounter;
            }//end foreach block, all files are processed
            fileListStream.Flush();
            fileListStream.Close();
            docWordStream.Flush();
            docWordStream.Close();
        }

        private static void processFiles(FeatureCollection featCol)
        {
            string path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string dataFolder = Path.Combine(path, "data");
            Directory.CreateDirectory(dataFolder);
            string fileList = Path.Combine(dataFolder, "ProcessedFilesList.txt");
            string docWordMatrix = Path.Combine(dataFolder, "DocWordMatrix.dat");
            if (File.Exists(fileList)) File.Delete(fileList);
            if (File.Exists(docWordMatrix)) File.Delete(docWordMatrix);

            StreamWriter fileListStream = new StreamWriter(fileList);
            BinaryWriter docWordStream = new BinaryWriter(File.Open(docWordMatrix, FileMode.Create));

            ArrayList wordCounter = new ArrayList();
            int nFileCounter = 0;
            foreach (Feature ft in featCol.featureList)
            {

                wordCounter.Clear();
                int nWordsSoFar = dictionary.GetNumberOfWords();
                wordCounter.Capacity = nWordsSoFar;
                for (int i = 0; i < nWordsSoFar; ++i)
                {
                    wordCounter.Add(0);
                }

                byte[] data = GetFileData(ft);
                int counter = 0;
                for (int i = 0; i < data.Length; ++i)
                {
                    byte b = data[i];
                    if (b < 128)
                    {
                        b = charFilter[b];
                        if (b != 0x20 && counter < nWordSize)
                        {
                            word[counter] = b;
                            ++counter;
                        }
                        else
                        {
                            if (counter > 0)
                            {
                                if (!stopWordFilter.isThere(word, counter))
                                {
                                    string strWord = enc.GetString(word, 0, counter);
                                    englishStemmer.SetCurrent(strWord);
                                    if (englishStemmer.Stem())
                                    {
                                        strWord = englishStemmer.GetCurrent();
                                    }
                                    int nWordIndex = dictionary.GetWordIndex(strWord);

                                    //we check errors
                                    if (nWordIndex < 0)
                                    {
                                        if (nWordIndex == -1)
                                        {
                                            Debug.WriteLine("Erorr: word = NULL");
                                            Environment.Exit(1);
                                        }
                                        if (nWordIndex == -2)
                                        {
                                            Debug.WriteLine("Error: word length > 255");
                                            Environment.Exit(1);
                                        }
                                        if (nWordIndex == -3)
                                        {
                                            Debug.WriteLine("Error: uknown");
                                            Environment.Exit(1);
                                        }
                                        if (nWordIndex == -4)
                                        {
                                            Debug.WriteLine("Error: memory buffer for dictionary is too short");
                                            Environment.Exit(1);
                                        }
                                        if (nWordIndex == -5)
                                        {
                                            Debug.WriteLine("Error: word length = 0");
                                            Environment.Exit(1);
                                        }
                                    }

                                    if (nWordIndex < nWordsSoFar)
                                    {
                                        int element = (int)wordCounter[nWordIndex];
                                        wordCounter[nWordIndex] = element + 1;
                                    }
                                    else
                                    {
                                        wordCounter.Add(1);
                                        ++nWordsSoFar;
                                    }
                                }
                                counter = 0;
                            } //word processed
                        }
                    }
                }//file processed
                fileListStream.WriteLine(nFileCounter.ToString() + " " + ft.title + " " + "Feature: " + ft.id.ToString() + ", words: " + dictionary.GetNumberOfWords() + ", size: " + dictionary.GetDictionarySize());

                int pos = 0;
                foreach (int x in wordCounter)
                {
                    if (x > 0)
                    {
                        docWordStream.Write(nFileCounter);
                        docWordStream.Write(pos);
                        short value = (short)(x);
                        docWordStream.Write(value);
                    }
                    ++pos;
                }

                ++nFileCounter;

            }//end foreach block, all files are processed
            fileListStream.Flush();
            fileListStream.Close();
            docWordStream.Flush();
            docWordStream.Close();
        }

        private static bool reformatMatrix()
        {
            string path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            path = Path.Combine(path, "data");
            string matrixFile = Path.Combine(path, "DocWordMatrix.dat");
            if (!File.Exists(matrixFile))
            {
                Utilities.LogMessageToFile(MainForm.logfile, "Matrix data file not found");
                return false;
            }

            FileInfo fi = new FileInfo(matrixFile);
            long nBytes = fi.Length;
            int nNonZeros = (int)(nBytes / 10);
            ArrayList list = new ArrayList();
            list.Clear();

            int nRows = 0;
            int nTotalCols = 0;
            using (FileStream stream = new FileStream(matrixFile, FileMode.Open))
            {
                int nCurrent = 0;
                int nCols = -1;
                using (BinaryReader reader = new BinaryReader(stream))
                {
                    for (long i = 0; i < nNonZeros; ++i)
                    {
                        int nR = reader.ReadInt32();
                        int nC = reader.ReadInt32();
                        short sum = reader.ReadInt16();
                        //double sum = reader.ReadDouble();
                        if (nC > nTotalCols) nTotalCols = nC;
                        ++nCols;
                        if (nR != nCurrent)
                        {
                            ++nRows;
                            list.Add(nCols);
                            nCurrent = nR;
                            nCols = 0;
                        }
                    }
                    list.Add(nCols + 1);
                    reader.Close();
                }
                stream.Close();
            }
            ++nRows;
            ++nTotalCols;
            Utilities.LogMessageToFile(MainForm.logfile, "");

            string formattedMatrix = Path.Combine(path, "matrix");

            FileStream inStream = new FileStream(matrixFile, FileMode.Open);
            BinaryReader inReader = new BinaryReader(inStream);

            using (FileStream stream = new FileStream(formattedMatrix, FileMode.Create))
            {
                using (BinaryWriter writer = new BinaryWriter(stream))
                {
                    writer.Write(System.Net.IPAddress.HostToNetworkOrder(nTotalCols));
                    writer.Write(System.Net.IPAddress.HostToNetworkOrder(nRows));
                    writer.Write(System.Net.IPAddress.HostToNetworkOrder(nNonZeros));
                    //Debug.WriteLine(nRows + "  " + nTotalCols + "  " + nNonZeros);

                    for (int k = 0; k < list.Count; ++k)
                    {
                        int nNonZeroCols = (int)list[k];
                        writer.Write(System.Net.IPAddress.HostToNetworkOrder(nNonZeroCols));
                        for (int j = 0; j < nNonZeroCols; ++j)
                        {
                            int nR = inReader.ReadInt32();
                            int nC = inReader.ReadInt32();
                            short sum = inReader.ReadInt16();
                            //double sum = inReader.ReadDouble();

                            writer.Write(System.Net.IPAddress.HostToNetworkOrder(nC));
                            float fSum = (float)(sum);
                            byte[] b = BitConverter.GetBytes(fSum);
                            int x = BitConverter.ToInt32(b, 0);
                            writer.Write(System.Net.IPAddress.HostToNetworkOrder(x));
                            //writer.Write(sum);
                        }
                    }

                    writer.Flush();
                    writer.Close();
                }
                stream.Close();
            }

            inReader.Close();
            inStream.Close();
            return true;
        }

        private static bool prepareDocDocMatrix(/*int nSelectedFile,*/ string resultBlockName, /*The next property is used to reduce the dimensionality in SVD.
        //It tells to ignore singular values that are below the threshold
        //relativley to original. For example if it is 0.1, the value S[n] 
        //will be set to 0.0 if S[n]/S[0] < 0.1.*/ double m_singularNumbersThreashold)
        {
            //read matrix
            int nRows = -1;
            int nCols = -1;
            string fUTfileName = resultBlockName + "-Ut";
            using (FileStream stream = new FileStream(fUTfileName, FileMode.Open))
            {
                using (BinaryReader reader = new BinaryReader(stream))
                {
                    nCols = System.Net.IPAddress.NetworkToHostOrder(reader.ReadInt32());
                    nRows = System.Net.IPAddress.NetworkToHostOrder(reader.ReadInt32());
                    reader.Close();
                }
                stream.Close();
            }
            if (nRows < 0 || nCols < 0) return false;

            U = new float[nRows, nCols];
            URows = nRows;
            UCols = nCols;
            for (int i = 0; i < nRows; ++i)
            {
                for (int j = 0; j < nCols; ++j)
                {
                    U[i, j] = (float)(0.0);
                }
            }

            using (FileStream stream = new FileStream(fUTfileName, FileMode.Open))
            {
                using (BinaryReader reader = new BinaryReader(stream))
                {
                    int nRowsRead = System.Net.IPAddress.NetworkToHostOrder(reader.ReadInt32());
                    int nColsRead = System.Net.IPAddress.NetworkToHostOrder(reader.ReadInt32());
                    for (int i = 0; i < nRowsRead; ++i)
                    {
                        for (int j = 0; j < nColsRead; ++j)
                        {
                            int nBuf = System.Net.IPAddress.NetworkToHostOrder(reader.ReadInt32());
                            byte[] b = BitConverter.GetBytes(nBuf);
                            U[j, i] = BitConverter.ToSingle(b, 0);
                        }
                    }
                    reader.Close();
                }
                stream.Close();
            }
            //emd reading matrix

            //Read singular values
            int rank = 0;
            string singularValuesFile = resultBlockName + "-S";
            string line = string.Empty;
            System.IO.StreamReader file = new System.IO.StreamReader(singularValuesFile);
            line = file.ReadLine();
            if (line == null)
            {
                Debug.WriteLine("Misformatted file: {0}", singularValuesFile);
                return false;
            }
            try
            {
                rank = Convert.ToInt32(line);
            }
            catch (Exception)
            {
                Debug.WriteLine("Misformatted file: {0}", singularValuesFile);
                return false;
            }
            if (rank != nCols)
            {
                Debug.WriteLine("Data mismatch");
                return false;
            }
            float[] singularValues = new float[rank];
            int cnt = 0;
            double maxSingularValue = 1.0;
            try
            {
                while ((line = file.ReadLine()) != null)
                {

                    if (m_singularNumbersThreashold < 1)
                    {
                        if (cnt == 0)
                        {
                            maxSingularValue = (float)(Convert.ToDouble(line));
                        }
                        singularValues[cnt] = (float)(Convert.ToDouble(line));
                        if ((double)(singularValues[cnt]) / maxSingularValue < m_singularNumbersThreashold)
                        {
                            singularValues[cnt] = 0.0f;
                        }
                    }
                    else
                    {
                        if (cnt <= (int)m_singularNumbersThreashold)
                        {
                            singularValues[cnt] = (float)(Convert.ToDouble(line));
                        }
                        else
                        {
                            singularValues[cnt] = 0.0f;
                        }
                    }
                    ++cnt;
                }
            }
            catch (Exception)
            {
                Debug.WriteLine("Misformatted file: {0}", singularValues);
                return false;
            }
            file.Close();
            //end reading singular values

            for (int i = 0; i < nRows; ++i)
            {
                for (int j = 0; j < nCols; ++j)
                {
                    U[i, j] *= singularValues[j];
                }
            }
            return true;
        }

        private static bool PrepareDocWordMatrix(string resultBlockName, int kDim, double k)
        {
            // U * S 

            if (kDim == 0)
            {
                if (k == 0)
                {
                    k = 0.04;
                }
                prepareDocDocMatrix(resultBlockName, k);
            }
            else
            { prepareDocDocMatrix(resultBlockName, kDim); }

            //read matrix V
            int nRows = -1;
            int nCols = -1;
            string fVTfileName = resultBlockName + "-Vt";
            using (FileStream stream = new FileStream(fVTfileName, FileMode.Open))
            {
                using (BinaryReader reader = new BinaryReader(stream))
                {
                    nRows = System.Net.IPAddress.NetworkToHostOrder(reader.ReadInt32());
                    nCols = System.Net.IPAddress.NetworkToHostOrder(reader.ReadInt32());
                    reader.Close();
                }
                stream.Close();
            }
            if (nRows < 0 || nCols < 0) return false;

            float[,] V = new float[nRows, nCols];
            int VRows = nRows;
            int VCols = nCols;
            for (int i = 0; i < nRows; ++i)
            {
                for (int j = 0; j < nCols; ++j)
                {
                    V[i, j] = (float)(0.0);
                }
            }

            using (FileStream stream = new FileStream(fVTfileName, FileMode.Open))
            {
                using (BinaryReader reader = new BinaryReader(stream))
                {
                    int nRowsRead = System.Net.IPAddress.NetworkToHostOrder(reader.ReadInt32());
                    int nColsRead = System.Net.IPAddress.NetworkToHostOrder(reader.ReadInt32());
                    for (int i = 0; i < nRowsRead; ++i)
                    {
                        for (int j = 0; j < nColsRead; ++j)
                        {
                            int nBuf = System.Net.IPAddress.NetworkToHostOrder(reader.ReadInt32());
                            byte[] b = BitConverter.GetBytes(nBuf);
                            V[i, j] = BitConverter.ToSingle(b, 0);
                        }
                    }
                    reader.Close();
                }
                stream.Close();
            }
            //emd reading matrix


            LSAMatrixDocTerm = new float[U.GetLength(0), V.GetLength(1)];
            for (int i = 0; i < U.GetLength(0); i++)
            {
                for (int j = 0; j < V.GetLength(1); j++)
                {


                    LSAMatrixDocTerm[i, j] = 0;
                    for (int n = 0; n < U.GetLength(1); n++)
                    {
                        LSAMatrixDocTerm[i, j] += U[i, n] * V[n, j];
                    }
                }
            }


            return true;
        }

        static bool findSortedListOfRelatedFiles(int nWhich, int length, int[] chart)
        {
            if (U == null)
            {
                Debug.WriteLine("Matrix is not ready");
                return false;
            }
            if (URows < nWhich)
            {
                Debug.WriteLine("File index is out of range");
                return false;
            }

            float[] magnitude = new float[URows];
            for (int i = 0; i < URows; ++i)
            {
                magnitude[i] = (float)(0.0);
                for (int j = 0; j < UCols; ++j)
                {
                    magnitude[i] += U[i, j] * U[i, j];
                }
                magnitude[i] = (float)(Math.Sqrt(magnitude[i]));
            }
            float[] vector = new float[UCols];
            for (int k = 0; k < UCols; ++k)
            {
                vector[k] = U[nWhich, k];
            }
            float vectorMagnitude = (float)(0.0);
            for (int k = 0; k < UCols; ++k)
            {
                vectorMagnitude += vector[k] * vector[k];
            }
            vectorMagnitude = (float)(Math.Sqrt(vectorMagnitude));

            //compute un-centered correlation
            float[] coeff = new float[URows];
            int[] order = new int[URows];
            for (int i = 0; i < URows; ++i)
            {
                order[i] = i;
                coeff[i] = 0.0f;
                for (int j = 0; j < UCols; ++j)
                {
                    coeff[i] += U[i, j] * vector[j];
                }
                coeff[i] /= vectorMagnitude;
                coeff[i] /= magnitude[i];
            }

            Array.Sort(coeff, order);
            Array.Reverse(coeff);
            Array.Reverse(order);
            int currentCategory = chart[nWhich];
            for (int i = 0; i < length; ++i)
            {
                if (chart[order[i]] == currentCategory)
                {
                    ++m_totalCorrectCategories;
                }
            }
            --m_totalCorrectCategories;
            return true;
        }

        static void verifyCategorizationForPatentCorpus128SVD(string resultFile)
        {
            int[] chart = new int[]
            {
                0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,
                1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,
                2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,
                3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,
                4,4,4,4,4,4,4,4,4,4,4,4,4,4,4,4,
                5,5,5,5,5,5,5,5,5,5,5,5,5,5,5,5,
                6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,
                7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7
            };

            m_totalCorrectCategories = 0;
            for (int i = 0; i < 128; ++i)
            {
                prepareDocDocMatrix(/*i,*/ resultFile, 0.04);
                findSortedListOfRelatedFiles(i, 16, chart);
            }
            int totalTests = 128 * 15;
            Utilities.LogMessageToFile(MainForm.logfile, String.Format("\nTotal correct categories in LSA {0} out of {1}, ratio {2}\n", m_totalCorrectCategories,
                totalTests, (double)(m_totalCorrectCategories) / (double)(totalTests)));
        }

        static void verifyCategorizationForPatentCorpus128Cosine()
        {
            string path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            path = Path.Combine(path, "data");
            string matrixFile = Path.Combine(path, "DocWordMatrix.dat");
            FileInfo fi = new FileInfo(matrixFile);
            int nRecords = (int)(fi.Length / 10);
            int nRows = -1;
            int nCols = -1;

            //Identify sizes
            using (FileStream stream = new FileStream(matrixFile, FileMode.Open))
            {
                using (BinaryReader reader = new BinaryReader(stream))
                {
                    for (int i = 0; i < nRecords; ++i)
                    {
                        int nR = reader.ReadInt32();
                        int nC = reader.ReadInt32();
                        short buf = reader.ReadInt16();
                        if (nRows < nR) nRows = nR;
                        if (nCols < nC) nCols = nC;
                    }
                    reader.Close();
                }
                stream.Close();
            }

            if (nRows < 0)
            {
                Debug.WriteLine("Error reading data");
                return;
            }
            if (nCols < 0)
            {
                Debug.WriteLine("Error reading data");
                return;
            }
            ++nRows;
            ++nCols;

            float[,] matrix = new float[nRows, nCols];
            for (int i = 0; i < nRows; ++i)
            {
                for (int j = 0; j < nCols; ++j)
                {
                    matrix[i, j] = 0.0f;
                }
            }
            using (FileStream stream = new FileStream(matrixFile, FileMode.Open))
            {
                using (BinaryReader reader = new BinaryReader(stream))
                {
                    for (int i = 0; i < nRecords; ++i)
                    {
                        int nR = reader.ReadInt32();
                        int nC = reader.ReadInt32();
                        short buf = reader.ReadInt16();
                        matrix[nR, nC] = (float)(buf);
                    }
                    reader.Close();
                }
                stream.Close();
            }

            int[] chart = new int[]
            {
                0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,
                1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,
                2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,
                3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,
                4,4,4,4,4,4,4,4,4,4,4,4,4,4,4,4,
                5,5,5,5,5,5,5,5,5,5,5,5,5,5,5,5,
                6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,
                7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7
            };
            int LENGTH = 16;

            m_totalCorrectCategories = 0;
            for (int nWhich = 0; nWhich < nRows; ++nWhich)
            {

                int[] order = new int[nRows];
                for (int k = 0; k < nRows; ++k) order[k] = k;
                double[] cosine = new double[nRows];
                double[] vector = new double[nCols];
                double vectorMagnitude = 0.0;
                for (int i = 0; i < nCols; ++i)
                {
                    vector[i] = matrix[nWhich, i];
                    vectorMagnitude += vector[i] * vector[i];
                }
                vectorMagnitude = Math.Sqrt(vectorMagnitude);

                for (int i = 0; i < nRows; ++i)
                {
                    double dotProduct = 0.0;
                    double mangnitude = 0.0;
                    for (int j = 0; j < nCols; ++j)
                    {
                        if (matrix[i, j] > 0.0)
                        {
                            dotProduct += matrix[i, j] * vector[j];
                            mangnitude += matrix[i, j] * matrix[i, j];
                        }
                    }
                    cosine[i] = dotProduct / Math.Sqrt(mangnitude) / vectorMagnitude;
                }

                Array.Sort(cosine, order);
                Array.Reverse(cosine);
                Array.Reverse(order);
                int currentCategory = chart[nWhich];
                for (int i = 0; i < LENGTH; ++i)
                {
                    if (chart[order[i]] == currentCategory)
                    {
                        ++m_totalCorrectCategories;
                    }
                }
                --m_totalCorrectCategories;
            }

            int totalTests = 128 * 15;
            Utilities.LogMessageToFile(MainForm.logfile, String.Format("\nTotal correct categories in cosine comparison {0} out of {1}, ratio {2:#0.#####}\n", m_totalCorrectCategories,
                totalTests, (double)(m_totalCorrectCategories) / (double)(totalTests)));
        }

        // In case of usage from within VS.
        public static void MainLSA(FeatureCollection ftCol, TFIDFMeasure tf, int kDim)
        {
            string path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            path = Path.Combine(path, "data");
            string matrixFile = Path.Combine(path, "matrix");
            string resultFile = Path.Combine(path, "result");
            processFiles(ftCol);
            reformatMatrix();
            SVD.ProcessData(tf._termWeight, resultFile, true);
            PrepareDocWordMatrix(resultFile, kDim, 0);

        }

        //***************1 apr 2014 find optimal k **************************

        public static void TermFreqToFloat(TFIDFMeasure tf)
        {
            matrix = new float[tf._termFreq.GetLength(0)][];
            int l = tf._termFreq[0].GetLength(0);
            for (int i = 0; i < tf._termFreq.GetLength(0); i++)
            {
                matrix[i] = new float[l];
                for (int j = 0; j < l; j++)
                {
                    matrix[i][j] = (float)tf._termFreq[i][j];
                }
            }
        }

        // In case of usage from within VS.
        public static void MainLSALoopUnweighed(FeatureCollection ftCol, TFIDFMeasure tf)
        {
            string path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            path = Path.Combine(path, "data");
            string matrixFile = Path.Combine(path, "matrix");
            resultFile = Path.Combine(path, "result");
            processFiles(ftCol);
            reformatMatrix();
            TermFreqToFloat(tf);
            SVD.ProcessData(matrix, resultFile, true);
            ReadUandS(resultFile);
            LSAMatrixDocTerm = new float[U.GetLength(0), V.GetLength(1)];
            Utilities.LogMessageToFile(MainForm.logfile, DateTime.Now.ToString() + " SVD calculated and files read into U, V and S");
        }

        // In case of usage from within VS.
        public static void MainLSALoop(FeatureCollection ftCol, TFIDFMeasure tf)
        {
            string path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            path = Path.Combine(path, "data");
            string matrixFile = Path.Combine(path, "matrix");
            resultFile = Path.Combine(path, "result");
            processFiles(ftCol);
            reformatMatrix();
            SVD.ProcessData(tf._termWeight, resultFile, true);
            ReadUandS(resultFile);
            LSAMatrixDocTerm = new float[U.GetLength(0), V.GetLength(1)];
            Utilities.LogMessageToFile(MainForm.logfile, DateTime.Now.ToString() + " SVD calculated and files read into U, V and S");
        }

        public static void LSAloop(int k, int step)
        {
            //for (int k = 0; k < UCols; k = k + step)
            //{
            // U * S 
            PrepareDocWordMatrixLoop(k, k + step);
            //}
        }

        private static bool ReadUandS(string resultBlockName)
        {
            //read matrix
            int nRows = -1;
            int nCols = -1;
            string fUTfileName = resultBlockName + "-Ut";
            using (FileStream stream = new FileStream(fUTfileName, FileMode.Open))
            {
                using (BinaryReader reader = new BinaryReader(stream))
                {
                    nCols = System.Net.IPAddress.NetworkToHostOrder(reader.ReadInt32());
                    nRows = System.Net.IPAddress.NetworkToHostOrder(reader.ReadInt32());
                    reader.Close();
                }
                stream.Close();
            }
            if (nRows < 0 || nCols < 0) return false;

            U = new float[nRows, nCols];
            URows = nRows;
            UCols = nCols;
            for (int i = 0; i < nRows; ++i)
            {
                for (int j = 0; j < nCols; ++j)
                {
                    U[i, j] = (float)(0.0);
                }
            }

            using (FileStream stream = new FileStream(fUTfileName, FileMode.Open))
            {
                using (BinaryReader reader = new BinaryReader(stream))
                {
                    int nRowsRead = System.Net.IPAddress.NetworkToHostOrder(reader.ReadInt32());
                    int nColsRead = System.Net.IPAddress.NetworkToHostOrder(reader.ReadInt32());
                    for (int i = 0; i < nRowsRead; ++i)
                    {
                        for (int j = 0; j < nColsRead; ++j)
                        {
                            int nBuf = System.Net.IPAddress.NetworkToHostOrder(reader.ReadInt32());
                            byte[] b = BitConverter.GetBytes(nBuf);
                            U[j, i] = BitConverter.ToSingle(b, 0);
                        }
                    }
                    reader.Close();
                }
                stream.Close();
            }
            //emd reading matrix

            //read matrix V
            nRows = -1;
            nCols = -1;
            string fVTfileName = resultBlockName + "-Vt";
            using (FileStream stream = new FileStream(fVTfileName, FileMode.Open))
            {
                using (BinaryReader reader = new BinaryReader(stream))
                {
                    nRows = System.Net.IPAddress.NetworkToHostOrder(reader.ReadInt32());
                    nCols = System.Net.IPAddress.NetworkToHostOrder(reader.ReadInt32());
                    reader.Close();
                }
                stream.Close();
            }
            if (nRows < 0 || nCols < 0) return false;

            V = new float[nRows, nCols];
            int VRows = nRows;
            int VCols = nCols;
            for (int i = 0; i < nRows; ++i)
            {
                for (int j = 0; j < nCols; ++j)
                {
                    V[i, j] = (float)(0.0);
                }
            }

            using (FileStream stream = new FileStream(fVTfileName, FileMode.Open))
            {
                using (BinaryReader reader = new BinaryReader(stream))
                {
                    int nRowsRead = System.Net.IPAddress.NetworkToHostOrder(reader.ReadInt32());
                    int nColsRead = System.Net.IPAddress.NetworkToHostOrder(reader.ReadInt32());
                    for (int i = 0; i < nRowsRead; ++i)
                    {
                        for (int j = 0; j < nColsRead; ++j)
                        {
                            int nBuf = System.Net.IPAddress.NetworkToHostOrder(reader.ReadInt32());
                            byte[] b = BitConverter.GetBytes(nBuf);
                            V[i, j] = BitConverter.ToSingle(b, 0);
                        }
                    }
                    reader.Close();
                }
                stream.Close();
            }
            //emd reading matrix

            //Read singular values
            int rank = 0;
            string singularValuesFile = resultBlockName + "-S";
            string line = string.Empty;
            System.IO.StreamReader file = new System.IO.StreamReader(singularValuesFile);
            line = file.ReadLine();
            if (line == null)
            {
                Debug.WriteLine("Misformatted file: {0}", singularValuesFile);
                return false;
            }
            try
            {
                rank = Convert.ToInt32(line);
            }
            catch (Exception)
            {
                Debug.WriteLine("Misformatted file: {0}", singularValuesFile);
                return false;
            }
            if (rank != U.GetLength(1))
            {
                Debug.WriteLine("Data mismatch");
                return false;
            }
            singularValues = new float[rank];
            int cnt = 0;
            try
            {
                while ((line = file.ReadLine()) != null)
                {
                    singularValues[cnt] = (float)(Convert.ToDouble(line));
                    ++cnt;
                }
            }
            catch (Exception)
            {
                Debug.WriteLine("Misformatted file: {0}", singularValues);
                return false;
            }
            file.Close();
            //end reading singular values

            // U*S
            for (int i = 0; i < U.GetLength(0); ++i)
            {
                for (int j = 0; j < U.GetLength(1); ++j)
                {
                    U[i, j] *= singularValues[j];
                }
            }

            return true;
        }

        public static bool PrepareDocWordMatrixLoop(int k, int nextk)
        {
            //LSAMatrixDocTerm = new float[U.GetLength(0), V.GetLength(1)];
            if (nextk > U.GetLength(1)) nextk = U.GetLength(1);
            for (int i = 0; i < U.GetLength(0); i++)
            {
                for (int j = 0; j < V.GetLength(1); j++)
                {
                    //LSAMatrixDocTerm[i, j] = 0;
                    for (int n = k; n < nextk; n++)
                    //for (int n = 0; n < U.GetLength(1); n++)
                    {
                        LSAMatrixDocTerm[i, j] += U[i, n] * V[n, j];
                    }
                }
            }


            return true;
        }



        //*********************************************************************************************

        // In case of usage from within VS.
        public static void MainLSA(FeatureCollection ftCol, TFIDFMeasure tf, double k)
        {
            string path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            path = Path.Combine(path, "data");
            string matrixFile = Path.Combine(path, "matrix");
            string resultFile = Path.Combine(path, "result");
            processFiles(ftCol);
            reformatMatrix();
            SVD.ProcessData(tf._termWeight, resultFile, true);
            PrepareDocWordMatrix(resultFile, 0, k);

        }

        // In case of usage from within VS.
        public static void MainLSA(FeatureCollection ftCol, int kDim)
        {
            string path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            path = Path.Combine(path, "data");
            string matrixFile = Path.Combine(path, "matrix");
            string resultFile = Path.Combine(path, "result");
            processFiles(ftCol);
            reformatMatrix();
            SVD.ProcessData(matrixFile, resultFile, true);
            PrepareDocWordMatrix(resultFile, kDim, 0);

        }

        private static float[] GetTermVector(int doc)
        {
            int _numTerms = LSAMatrixDocTerm.GetLength(1);
            float[] w = new float[_numTerms];
            for (int i = 0; i < _numTerms; i++)
                w[i] = LSAMatrixDocTerm[doc, i];
            return w;
        }

        private static float[] GetDocVector(int term)
        {
            int _numDocs = LSAMatrixDocTerm.GetLength(0);
            float[] w = new float[_numDocs];
            for (int i = 0; i < _numDocs; i++)
                w[i] = LSAMatrixDocTerm[i, term];
            return w;
        }

        public static float GetSimilarity(int doc_i, int doc_j)
        {
            float[] vector1 = GetTermVector(doc_i);
            float[] vector2 = GetTermVector(doc_j);

            return TFIDFMeasure.TermVector.ComputeCosineSimilarity(vector1, vector2);

        }

        public static float GetWordSimilarity(int term_i, int term_j)
        {
            float[] vector1 = GetDocVector(term_i);
            float[] vector2 = GetDocVector(term_j);

            return TFIDFMeasure.TermVector.ComputeCosineSimilarity(vector1, vector2);
        }

        //// In case of usage from within VS. INITIAL VERSION
        //static void Main(string[] args)
        //{
        //    DateTime start = DateTime.Now;

        //    string rootFolder = @"..//..//..//..//PATENTCORPUS128";
        //    string extension = "*.txt";

        //    DirectoryInfo di = new DirectoryInfo(rootFolder);
        //    if (!di.Exists)
        //    {
        //        Debug.WriteLine("The data folder not found, please correct the path");
        //        return;
        //    }

        //    string path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        //    path = Path.Combine(path, "data");
        //    string matrixFile = Path.Combine(path, "matrix");
        //    string resultFile = Path.Combine(path, "result");
        //    //
        //    List<string> files = new List<string>();
        //    files.Clear();
        //    enumerateFiles(files, rootFolder, extension);
        //    processFiles(ref files);
        //    reformatMatrix();
        //    SVD.ProcessData(matrixFile, resultFile, true);
        //    //At this point the data SVD completed and next part is
        //    //simple test for particular technology LSA and for specific 
        //    //data which is PATENTCORPUS128.  For a different data
        //    //it has to be adjusted.
        //    //Don't call next two functions if you use different data.
        //    verifyCategorizationForPatentCorpus128SVD(resultFile);
        //    verifyCategorizationForPatentCorpus128Cosine();
        //    //
        //    DateTime end = DateTime.Now;
        //    TimeSpan duration = end - start;
        //    double time = duration.Minutes * 60.0 + duration.Seconds + duration.Milliseconds / 1000.0;
        //    Debug.WriteLine("Total processing time {0:########.00} seconds", time);
        //}
    }

    public class Utilities
    {
        /// <summary>
        /// Randomly create true theta and phi arrays
        /// </summary>
        /// <param name="numVocab">Vocabulary size</param>
        /// <param name="numTopics">Number of topics</param>
        /// <param name="numDocs">Number of documents</param>
        /// <param name="averageDocLength">Avarage document length</param>
        /// <param name="trueTheta">Theta array (output)</param>
        /// <param name="truePhi">Phi array (output)</param>
        public static void CreateTrueThetaAndPhi(
            int numVocab, int numTopics, int numDocs, int averageDocLength, int averageWordsPerTopic,
            out Dirichlet[] trueTheta, out Dirichlet[] truePhi)
        {
            truePhi = new Dirichlet[numTopics];
            for (int i = 0; i < numTopics; i++)
            {
                truePhi[i] = Dirichlet.Uniform(numVocab);
                truePhi[i].PseudoCount.SetAllElementsTo(0.0);
                // Draw the number of unique words in the topic.
                int numUniqueWordsPerTopic = Poisson.Sample((double)averageWordsPerTopic);
                if (numUniqueWordsPerTopic >= numVocab) numUniqueWordsPerTopic = numVocab;
                if (numUniqueWordsPerTopic < 1) numUniqueWordsPerTopic = 1;
                double expectedRepeatOfWordInTopic =
                    ((double)numDocs) * averageDocLength / numUniqueWordsPerTopic;
                int[] shuffledWordIndices = Rand.Perm(numVocab);
                for (int j = 0; j < numUniqueWordsPerTopic; j++)
                {
                    int wordIndex = shuffledWordIndices[j];
                    // Draw the count for that word
                    int cnt = Poisson.Sample(expectedRepeatOfWordInTopic);
                    truePhi[i].PseudoCount[wordIndex] = cnt + 1.0;
                }
            }

            trueTheta = new Dirichlet[numDocs];
            for (int i = 0; i < numDocs; i++)
            {
                trueTheta[i] = Dirichlet.Uniform(numTopics);
                trueTheta[i].PseudoCount.SetAllElementsTo(0.0);
                // Draw the number of unique topics in the doc.
                int numUniqueTopicsPerDoc = Math.Min(1 + Poisson.Sample(1.0), numTopics);
                double expectedRepeatOfTopicInDoc =
                    averageDocLength / numUniqueTopicsPerDoc;
                int[] shuffledTopicIndices = Rand.Perm(numTopics);
                for (int j = 0; j < numUniqueTopicsPerDoc; j++)
                {
                    int topicIndex = shuffledTopicIndices[j];
                    // Draw the count for that topic
                    int cnt = Poisson.Sample(expectedRepeatOfTopicInDoc);
                    trueTheta[i].PseudoCount[topicIndex] = cnt + 1.0;
                }
            }
        }

        /// Generate LDA data - returns an array of dictionaries mapping unique word index
        /// to word count per document.
        /// <param name="trueTheta">Known Theta</param>
        /// <param name="truePhi">Known Phi</param>
        /// <param name="averageNumWords">Average number of words to sample per doc</param>
        /// <returns></returns>
        public static Dictionary<int, int>[] GenerateLDAData(Dirichlet[] trueTheta, Dirichlet[] truePhi, int averageNumWords)
        {
            int numVocab = truePhi[0].Dimension;
            int numTopics = truePhi.Length;
            int numDocs = trueTheta.Length;

            // Sample from the model
            Vector[] topicDist = new Vector[numDocs];
            Vector[] wordDist = new Vector[numTopics];
            for (int i = 0; i < numDocs; i++)
                topicDist[i] = trueTheta[i].Sample();
            for (int i = 0; i < numTopics; i++)
                wordDist[i] = truePhi[i].Sample();

            var wordCounts = new Dictionary<int, int>[numDocs];
            for (int i = 0; i < numDocs; i++)
            {
                int LengthOfDoc = Poisson.Sample((double)averageNumWords);

                var counts = new Dictionary<int, int>();
                for (int j = 0; j < LengthOfDoc; j++)
                {
                    int topic = Discrete.Sample(topicDist[i]);
                    int w = Discrete.Sample(wordDist[topic]);
                    if (!counts.ContainsKey(w))
                        counts.Add(w, 1);
                    else
                        counts[w] = counts[w] + 1;
                }
                wordCounts[i] = counts;
            }
            return wordCounts;
        }

        /// <summary>
        /// Calculate perplexity for test words
        /// </summary>
        /// <param name="predictiveDist">Predictive distribution for each document</param>
        /// <param name="testWordsPerDoc">Test words per document</param>
        /// <returns></returns>
        public static double Perplexity(Discrete[] predictiveDist, Dictionary<int, int>[] testWordsPerDoc)
        {
            double num = 0.0;
            double den = 0.0;
            int numDocs = predictiveDist.Length;
            for (int i = 0; i < numDocs; i++)
            {
                Discrete d = predictiveDist[i];
                var counts = testWordsPerDoc[i];
                foreach (KeyValuePair<int, int> kvp in counts)
                {
                    num += kvp.Value * d.GetLogProb(kvp.Key);
                    den += kvp.Value;
                }
            }
            return Math.Exp(-num / den);
        }

        /// Load data. Each line is of the form cnt,wrd1_index:count,wrd2_index:count,...
        /// </summary>
        /// <param name="fileName">The file name</param>
        /// <param name="vocabulary">Vocabulary (output)</param>
        /// <returns></returns>
        public static Dictionary<int, int>[] LoadWordCounts(string fileName)
        {
            List<Dictionary<int, int>> ld = new List<Dictionary<int, int>>();
            using (StreamReader sr = new StreamReader(fileName))
            {
                string str = null;
                while ((str = sr.ReadLine()) != null)
                {
                    string[] split = str.Split(' ', ':');
                    int numUniqueTerms = int.Parse(split[0]);
                    var dict = new Dictionary<int, int>();
                    for (int i = 0; i < (split.Length - 1) / 2; i++)
                        dict.Add(int.Parse(split[2 * i + 1]), int.Parse(split[2 * i + 2]));
                    ld.Add(dict);
                }
            }
            return ld.ToArray();
        }

        /// <summary>
        /// Load the vocabulary
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public static Dictionary<int, string> LoadVocabulary(string fileName)
        {
            Dictionary<int, string> vocab = new Dictionary<int, string>();
            using (StreamReader sr = new StreamReader(fileName))
            {
                string str = null;
                int idx = 0;
                while ((str = sr.ReadLine()) != null)
                    vocab.Add(idx++, str);
            }
            return vocab;
        }

        /// <summary>
        /// Get the vocabulary size for the data (max index + 1)
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static int GetVocabularySize(Dictionary<int, int>[] data)
        {
            int max = int.MinValue;
            foreach (Dictionary<int, int> dict in data)
            {
                foreach (int key in dict.Keys)
                {
                    if (key > max)
                        max = key;
                }
            }
            return max + 1;

        }


        public static void LogMessageToFile(string fileName, string message)
        {
            System.IO.StreamWriter sw =
               System.IO.File.AppendText(fileName); // Change filename
            try
            {
                string logLine =
                   System.String.Format(
                      "{0:G}: {1}.", System.DateTime.Now, message);
                sw.WriteLine(logLine);
            }
            finally
            {
                sw.Close();
            }
        }
    }

    /// <summary>
	/// Stop words are frequently occurring, insignificant words words 
	/// that appear in a database record, article or web page. 
	/// Common stop words include
	/// </summary>
	public class StopWordsHandler
    {
        public static string[] stopWordsList = new string[] {
            //completed stop word list from ftp://ftp.cs.cornell.edu/pub/smart/english.stop
            //PH 5 apr 2013
                                                            "a",
                                                            "able",
                                                            "about",
                                                            "above",
                                                            "according",
                                                            "accordingly",
                                                            "across",
                                                            "actually",
                                                            "afore",
                                                            "aforesaid",
                                                            "after",
                                                            "afterwards",
                                                            "again",
                                                            "against",
                                                            "agin",
                                                            "ago",
                                                            "aint",
                                                            "ain't",
                                                            "albeit",
                                                            "all",
                                                            "allow",
                                                            "allows",
                                                            "almost",
                                                            "alone",
                                                            "along",
                                                            "alongside",
                                                            "already",
                                                            "also",
                                                            "although",
                                                            "always",
                                                            "am",
                                                            "american",
                                                            "amid",
                                                            "amidst",
                                                            "among",
                                                            "amongst",
                                                            "an",
                                                            "and",
                                                            "anent",
                                                            "another",
                                                            "any",
                                                            "anybody",
                                                            "anyhow",
                                                            "anyone",
                                                            "anything",
                                                            "anyway",
                                                            "anyways",
                                                            "anywhere",
                                                            "apart",
                                                            "appear",
                                                            "appreciate",
                                                            "appropriate",
                                                            "are",
                                                            "aren't",
                                                            "around",
                                                            "as",
                                                            "a's",
                                                            "aside",
                                                            "ask",
                                                            "asking",
                                                            "aslant",
                                                            "associated",
                                                            "astride",
                                                            "at",
                                                            "athwart",
                                                            "available",
                                                            "away",
                                                            "awfully",
                                                            "b",
                                                            "back",
                                                            "bar",
                                                            "barring",
                                                            "be",
                                                            "became",
                                                            "because",
                                                            "become",
                                                            "becomes",
                                                            "becoming",
                                                            "been",
                                                            "before",
                                                            "beforehand",
                                                            "behind",
                                                            "being",
                                                            "believe",
                                                            "below",
                                                            "beneath",
                                                            "beside",
                                                            "besides",
                                                            "best",
                                                            "better",
                                                            "between",
                                                            "betwixt",
                                                            "beyond",
                                                            "both",
                                                            "brief",
                                                            "but",
                                                            "by",
                                                            "c",
                                                            "came",
                                                            "can",
                                                            "cannot",
                                                            "cant",
                                                            "can't",
                                                            "cause",
                                                            "causes",
                                                            "certain",
                                                            "certainly",
                                                            "changes",
                                                            "circa",
                                                            "clearly",
                                                            "close",
                                                            "c'mon",
                                                            "co",
                                                            "com",
                                                            "come",
                                                            "comes",
                                                            "concerning",
                                                            "consequently",
                                                            "consider",
                                                            "considering",
                                                            "contain",
                                                            "containing",
                                                            "contains",
                                                            "corresponding",
                                                            "cos",
                                                            "could",
                                                            "couldn't",
                                                            "couldst",
                                                            "course",
                                                            "c's",
                                                            "currently",
                                                            "d",
                                                            "dare",
                                                            "dared",
                                                            "daren't",
                                                            "dares",
                                                            "daring",
                                                            "definitely",
                                                            "described",
                                                            "despite",
                                                            "did",
                                                            "didn't",
                                                            "different",
                                                            "directly",
                                                            "do",
                                                            "does",
                                                            "doesn't",
                                                            "doing",
                                                            "done",
                                                            "don't",
                                                            "dost",
                                                            "doth",
                                                            "down",
                                                            "downwards",
                                                            "during",
                                                            "durst",
                                                            "e",
                                                            "each",
                                                            "early",
                                                            "edu",
                                                            "eg",
                                                            "eight",
                                                            "either",
                                                            "else",
                                                            "elsewhere",
                                                            "em",
                                                            "english",
                                                            "enough",
                                                            "entirely",
                                                            "ere",
                                                            "especially",
                                                            "et",
                                                            "etc",
                                                            "even",
                                                            "ever",
                                                            "every",
                                                            "everybody",
                                                            "everyone",
                                                            "everything",
                                                            "everywhere",
                                                            "ex",
                                                            "exactly",
                                                            "example",
                                                            "except",
                                                            "excepting",
                                                            "f",
                                                            "failing",
                                                            "far",
                                                            "few",
                                                            "fifth",
                                                            "first",
                                                            "five",
                                                            "followed",
                                                            "following",
                                                            "follows",
                                                            "for",
                                                            "former",
                                                            "formerly",
                                                            "forth",
                                                            "four",
                                                            "from",
                                                            "further",
                                                            "furthermore",
                                                            "g",
                                                            "get",
                                                            "gets",
                                                            "getting",
                                                            "given",
                                                            "gives",
                                                            "go",
                                                            "goes",
                                                            "going",
                                                            "gone",
                                                            "gonna",
                                                            "got",
                                                            "gotta",
                                                            "gotten",
                                                            "greetings",
                                                            "h",
                                                            "had",
                                                            "hadn't",
                                                            "happens",
                                                            "hard",
                                                            "hardly",
                                                            "has",
                                                            "hasn't",
                                                            "hast",
                                                            "hath",
                                                            "have",
                                                            "haven't",
                                                            "having",
                                                            "he",
                                                            "he'd",
                                                            "he'll",
                                                            "hello",
                                                            "help",
                                                            "hence",
                                                            "her",
                                                            "here",
                                                            "hereafter",
                                                            "hereby",
                                                            "herein",
                                                            "here's",
                                                            "hereupon",
                                                            "hers",
                                                            "herself",
                                                            "he's",
                                                            "hi",
                                                            "high",
                                                            "him",
                                                            "himself",
                                                            "his",
                                                            "hither",
                                                            "home",
                                                            "hopefully",
                                                            "how",
                                                            "howbeit",
                                                            "however",
                                                            "how's",
                                                            "i",
                                                            "id",
                                                            "i'd",
                                                            "ie",
                                                            "if",
                                                            "ignored",
                                                            "ill",
                                                            "i'll",
                                                            "i'm",
                                                            "immediate",
                                                            "immediately",
                                                            "important",
                                                            "in",
                                                            "inasmuch",
                                                            "inc",
                                                            "indeed",
                                                            "indicate",
                                                            "indicated",
                                                            "indicates",
                                                            "inner",
                                                            "inside",
                                                            "insofar",
                                                            "instantly",
                                                            "instead",
                                                            "into",
                                                            "inward",
                                                            "is",
                                                            "isn't",
                                                            "it",
                                                            "it'd",
                                                            "it'll",
                                                            "its",
                                                            "it's",
                                                            "itself",
                                                            "i've",
                                                            "j",
                                                            "just",
                                                            "k",
                                                            "keep",
                                                            "keeps",
                                                            "kept",
                                                            "know",
                                                            "known",
                                                            "knows",
                                                            "l",
                                                            "large",
                                                            "last",
                                                            "lately",
                                                            "later",
                                                            "latter",
                                                            "latterly",
                                                            "least",
                                                            "left",
                                                            "less",
                                                            "lest",
                                                            "let",
                                                            "let's",
                                                            "like",
                                                            "liked",
                                                            "likely",
                                                            "likewise",
                                                            "little",
                                                            "living",
                                                            "long",
                                                            "look",
                                                            "looking",
                                                            "looks",
                                                            "ltd",
                                                            "m",
                                                            "mainly",
                                                            "many",
                                                            "may",
                                                            "maybe",
                                                            "mayn't",
                                                            "me",
                                                            "mean",
                                                            "meanwhile",
                                                            "merely",
                                                            "mid",
                                                            "midst",
                                                            "might",
                                                            "mightn't",
                                                            "mine",
                                                            "minus",
                                                            "more",
                                                            "moreover",
                                                            "most",
                                                            "mostly",
                                                            "much",
                                                            "must",
                                                            "mustn't",
                                                            "my",
                                                            "myself",
                                                            "n",
                                                            "name",
                                                            "namely",
                                                            "nd",
                                                            "near",
                                                            "nearly",
                                                            "neath",
                                                            "necessary",
                                                            "need",
                                                            "needed",
                                                            "needing",
                                                            "needn't",
                                                            "needs",
                                                            "neither",
                                                            "never",
                                                            "nevertheless",
                                                            "new",
                                                            "next",
                                                            "nigh",
                                                            "nigher",
                                                            "nighest",
                                                            "nine",
                                                            "nisi",
                                                            "no",
                                                            "nobody",
                                                            "non",
                                                            "none",
                                                            "noone",
                                                            "no-one",
                                                            "nor",
                                                            "normally",
                                                            "not",
                                                            "nothing",
                                                            "notwithstanding",
                                                            "novel",
                                                            "now",
                                                            "nowhere",
                                                            "o",
                                                            "obviously",
                                                            "o'er",
                                                            "of",
                                                            "off",
                                                            "often",
                                                            "oh",
                                                            "ok",
                                                            "okay",
                                                            "old",
                                                            "on",
                                                            "once",
                                                            "one",
                                                            "ones",
                                                            "oneself",
                                                            "only",
                                                            "onto",
                                                            "open",
                                                            "or",
                                                            "other",
                                                            "others",
                                                            "otherwise",
                                                            "ought",
                                                            "oughtn't",
                                                            "our",
                                                            "ours",
                                                            "ourselves",
                                                            "out",
                                                            "outside",
                                                            "over",
                                                            "overall",
                                                            "own",
                                                            "p",
                                                            "particular",
                                                            "particularly",
                                                            "past",
                                                            "pending",
                                                            "per",
                                                            "perhaps",
                                                            "placed",
                                                            "please",
                                                            "plus",
                                                            "possible",
                                                            "present",
                                                            "presumably",
                                                            "probably",
                                                            "provided",
                                                            "provides",
                                                            "providing",
                                                            "public",
                                                            "q",
                                                            "qua",
                                                            "que",
                                                            "quite",
                                                            "qv",
                                                            "r",
                                                            "rather",
                                                            "rd",
                                                            "re",
                                                            "real",
                                                            "really",
                                                            "reasonably",
                                                            "regarding",
                                                            "regardless",
                                                            "regards",
                                                            "relatively",
                                                            "respecting",
                                                            "respectively",
                                                            "right",
                                                            "round",
                                                            "s",
                                                            "said",
                                                            "same",
                                                            "sans",
                                                            "save",
                                                            "saving",
                                                            "saw",
                                                            "say",
                                                            "saying",
                                                            "says",
                                                            "second",
                                                            "secondly",
                                                            "see",
                                                            "seeing",
                                                            "seem",
                                                            "seemed",
                                                            "seeming",
                                                            "seems",
                                                            "seen",
                                                            "self",
                                                            "selves",
                                                            "sensible",
                                                            "sent",
                                                            "serious",
                                                            "seriously",
                                                            "seven",
                                                            "several",
                                                            "shall",
                                                            "shalt",
                                                            "shan't",
                                                            "she",
                                                            "shed",
                                                            "shell",
                                                            "she's",
                                                            "short",
                                                            "should",
                                                            "shouldn't",
                                                            "since",
                                                            "six",
                                                            "small",
                                                            "so",
                                                            "some",
                                                            "somebody",
                                                            "somehow",
                                                            "someone",
                                                            "something",
                                                            "sometime",
                                                            "sometimes",
                                                            "somewhat",
                                                            "somewhere",
                                                            "soon",
                                                            "sorry",
                                                            "special",
                                                            "specified",
                                                            "specify",
                                                            "specifying",
                                                            "still",
                                                            "sub",
                                                            "such",
                                                            "summat",
                                                            "sup",
                                                            "supposing",
                                                            "sure",
                                                            "t",
                                                            "take",
                                                            "taken",
                                                            "tell",
                                                            "tends",
                                                            "th",
                                                            "than",
                                                            "thank",
                                                            "thanks",
                                                            "thanx",
                                                            "that",
                                                            "that'd",
                                                            "that'll",
                                                            "thats",
                                                            "that's",
                                                            "the",
                                                            "thee",
                                                            "their",
                                                            "theirs",
                                                            "their's",
                                                            "them",
                                                            "themselves",
                                                            "then",
                                                            "thence",
                                                            "there",
                                                            "thereafter",
                                                            "thereby",
                                                            "therefore",
                                                            "therein",
                                                            "theres",
                                                            "there's",
                                                            "thereupon",
                                                            "these",
                                                            "they",
                                                            "they'd",
                                                            "they'll",
                                                            "they're",
                                                            "they've",
                                                            "thine",
                                                            "think",
                                                            "third",
                                                            "this",
                                                            "tho",
                                                            "thorough",
                                                            "thoroughly",
                                                            "those",
                                                            "thou",
                                                            "though",
                                                            "three",
                                                            "thro'",
                                                            "through",
                                                            "throughout",
                                                            "thru",
                                                            "thus",
                                                            "thyself",
                                                            "till",
                                                            "to",
                                                            "today",
                                                            "together",
                                                            "too",
                                                            "took",
                                                            "touching",
                                                            "toward",
                                                            "towards",
                                                            "tried",
                                                            "tries",
                                                            "truly",
                                                            "try",
                                                            "trying",
                                                            "t's",
                                                            "twas",
                                                            "tween",
                                                            "twere",
                                                            "twice",
                                                            "twill",
                                                            "twixt",
                                                            "two",
                                                            "twould",
                                                            "u",
                                                            "un",
                                                            "under",
                                                            "underneath",
                                                            "unfortunately",
                                                            "unless",
                                                            "unlike",
                                                            "unlikely",
                                                            "until",
                                                            "unto",
                                                            "up",
                                                            "upon",
                                                            "us",
                                                            "use",
                                                            "used",
                                                            "useful",
                                                            "uses",
                                                            "using",
                                                            "usually",
                                                            "uucp",
                                                            "v",
                                                            "value",
                                                            "various",
                                                            "versus",
                                                            "very",
                                                            "via",
                                                            "vice",
                                                            "vis-a-vis",
                                                            "viz",
                                                            "vs",
                                                            "w",
                                                            "wanna",
                                                            "want",
                                                            "wanting",
                                                            "wants",
                                                            "was",
                                                            "wasn't",
                                                            "way",
                                                            "we",
                                                            "we'd",
                                                            "welcome",
                                                            "well",
                                                            "we'll",
                                                            "went",
                                                            "were",
                                                            "we're",
                                                            "weren't",
                                                            "wert",
                                                            "we've",
                                                            "what",
                                                            "whatever",
                                                            "what'll",
                                                            "what's",
                                                            "when",
                                                            "whence",
                                                            "whencesoever",
                                                            "whenever",
                                                            "when's",
                                                            "where",
                                                            "whereafter",
                                                            "whereas",
                                                            "whereby",
                                                            "wherein",
                                                            "where's",
                                                            "whereupon",
                                                            "wherever",
                                                            "whether",
                                                            "which",
                                                            "whichever",
                                                            "whichsoever",
                                                            "while",
                                                            "whilst",
                                                            "whither",
                                                            "who",
                                                            "who'd",
                                                            "whoever",
                                                            "whole",
                                                            "who'll",
                                                            "whom",
                                                            "whore",
                                                            "who's",
                                                            "whose",
                                                            "whoso",
                                                            "whosoever",
                                                            "why",
                                                            "will",
                                                            "willing",
                                                            "wish",
                                                            "with",
                                                            "within",
                                                            "without",
                                                            "wonder",
                                                            "wont",
                                                            "won't",
                                                            "would",
                                                            "wouldn't",
                                                            "wouldst",
                                                            "x",
                                                            "y",
                                                            "ye",
                                                            "yes",
                                                            "yet",
                                                            "you",
                                                            "you'd",
                                                            "you'll",
                                                            "your",
                                                            "you're",
                                                            "yours",
                                                            "yourself",
                                                            "yourselves",
                                                            "you've",
                                                            "z",
                                                            "zero" 
                                                            //"a", 
                                                            //"about", 
                                                            //"above", 
                                                            //"across", 
                                                            //"afore", 
                                                            //"aforesaid", 
                                                            //"after", 
                                                            //"again", 
                                                            //"against", 
                                                            //"agin", 
                                                            //"ago", 
                                                            //"aint", 
                                                            //"albeit", 
                                                            //"all", 
                                                            //"almost", 
                                                            //"alone", 
                                                            //"along", 
                                                            //"alongside", 
                                                            //"already", 
                                                            //"also", 
                                                            //"although", 
                                                            //"always", 
                                                            //"am", 
                                                            //"american", 
                                                            //"amid", 
                                                            //"amidst", 
                                                            //"among", 
                                                            //"amongst", 
                                                            //"an", 
                                                            //"and", 
                                                            //"anent", 
                                                            //"another", 
                                                            //"any", 
                                                            //"anybody", 
                                                            //"anyone", 
                                                            //"anything", 
                                                            //"are", 
                                                            //"aren't", 
                                                            //"around", 
                                                            //"as", 
                                                            //"aslant", 
                                                            //"astride", 
                                                            //"at", 
                                                            //"athwart", 
                                                            //"away", 
                                                            //"b", 
                                                            //"back", 
                                                            //"bar", 
                                                            //"barring", 
                                                            //"be", 
                                                            //"because", 
                                                            //"been", 
                                                            //"before", 
                                                            //"behind", 
                                                            //"being", 
                                                            //"below", 
                                                            //"beneath", 
                                                            //"beside", 
                                                            //"besides", 
                                                            //"best", 
                                                            //"better", 
                                                            //"between", 
                                                            //"betwixt", 
                                                            //"beyond", 
                                                            //"both", 
                                                            //"but", 
                                                            //"by", 
                                                            //"c", 
                                                            //"can", 
                                                            //"cannot", 
                                                            //"can't", 
                                                            //"certain", 
                                                            //"circa", 
                                                            //"close", 
                                                            //"concerning", 
                                                            //"considering", 
                                                            //"cos", 
                                                            //"could", 
                                                            //"couldn't", 
                                                            //"couldst", 
                                                            //"d", 
                                                            //"dare", 
                                                            //"dared", 
                                                            //"daren't", 
                                                            //"dares", 
                                                            //"daring", 
                                                            //"despite", 
                                                            //"did", 
                                                            //"didn't", 
                                                            //"different", 
                                                            //"directly", 
                                                            //"do", 
                                                            //"does", 
                                                            //"doesn't", 
                                                            //"doing", 
                                                            //"done", 
                                                            //"don't", 
                                                            //"dost", 
                                                            //"doth", 
                                                            //"down", 
                                                            //"during", 
                                                            //"durst", 
                                                            //"e", 
                                                            //"each", 
                                                            //"early", 
                                                            //"either", 
                                                            //"em", 
                                                            //"english", 
                                                            //"enough", 
                                                            //"ere", 
                                                            //"even", 
                                                            //"ever", 
                                                            //"every", 
                                                            //"everybody", 
                                                            //"everyone", 
                                                            //"everything", 
                                                            //"except", 
                                                            //"excepting", 
                                                            //"f", 
                                                            //"failing", 
                                                            //"far", 
                                                            //"few", 
                                                            //"first", 
                                                            //"five", 
                                                            //"following", 
                                                            //"for", 
                                                            //"four", 
                                                            //"from", 
                                                            //"g", 
                                                            //"gonna", 
                                                            //"gotta", 
                                                            //"h", 
                                                            //"had", 
                                                            //"hadn't", 
                                                            //"hard", 
                                                            //"has", 
                                                            //"hasn't", 
                                                            //"hast", 
                                                            //"hath", 
                                                            //"have", 
                                                            //"haven't", 
                                                            //"having", 
                                                            //"he", 
                                                            //"he'd", 
                                                            //"he'll", 
                                                            //"her", 
                                                            //"here", 
                                                            //"here's", 
                                                            //"hers", 
                                                            //"herself", 
                                                            //"he's", 
                                                            //"high", 
                                                            //"him", 
                                                            //"himself", 
                                                            //"his", 
                                                            //"home", 
                                                            //"how", 
                                                            //"howbeit", 
                                                            //"however", 
                                                            //"how's", 
                                                            //"i", 
                                                            //"id", 
                                                            //"if", 
                                                            //"ill", 
                                                            //"i'm", 
                                                            //"immediately", 
                                                            //"important", 
                                                            //"in", 
                                                            //"inside", 
                                                            //"instantly", 
                                                            //"into", 
                                                            //"is", 
                                                            //"isn't", 
                                                            //"it", 
                                                            //"it'll", 
                                                            //"its", 
                                                            //"it's", 
                                                            //"itself", 
                                                            //"i've", 
                                                            //"j", 
                                                            //"just", 
                                                            //"k", 
                                                            //"l", 
                                                            //"large", 
                                                            //"last", 
                                                            //"later", 
                                                            //"least", 
                                                            //"left", 
                                                            //"less", 
                                                            //"lest", 
                                                            //"let's", 
                                                            //"like", 
                                                            //"likewise", 
                                                            //"little", 
                                                            //"living", 
                                                            //"long", 
                                                            //"m", 
                                                            //"many", 
                                                            //"may", 
                                                            //"mayn't", 
                                                            //"me", 
                                                            //"mid", 
                                                            //"midst", 
                                                            //"might", 
                                                            //"mightn't", 
                                                            //"mine", 
                                                            //"minus", 
                                                            //"more", 
                                                            //"most", 
                                                            //"much", 
                                                            //"must", 
                                                            //"mustn't", 
                                                            //"my", 
                                                            //"myself", 
                                                            //"n", 
                                                            //"near", 
                                                            //"neath", 
                                                            //"need", 
                                                            //"needed", 
                                                            //"needing", 
                                                            //"needn't", 
                                                            //"needs", 
                                                            //"neither", 
                                                            //"never", 
                                                            //"nevertheless", 
                                                            //"new", 
                                                            //"next", 
                                                            //"nigh", 
                                                            //"nigher", 
                                                            //"nighest", 
                                                            //"nisi", 
                                                            //"no", 
                                                            //"nobody", 
                                                            //"none", 
                                                            //"no-one", 
                                                            //"nor", 
                                                            //"not", 
                                                            //"nothing", 
                                                            //"notwithstanding", 
                                                            //"now", 
                                                            //"o", 
                                                            //"o'er", 
                                                            //"of", 
                                                            //"off", 
                                                            //"often", 
                                                            //"on", 
                                                            //"once", 
                                                            //"one", 
                                                            //"oneself", 
                                                            //"only", 
                                                            //"onto", 
                                                            //"open", 
                                                            //"or", 
                                                            //"other", 
                                                            //"otherwise", 
                                                            //"ought", 
                                                            //"oughtn't", 
                                                            //"our", 
                                                            //"ours", 
                                                            //"ourselves", 
                                                            //"out", 
                                                            //"outside", 
                                                            //"over", 
                                                            //"own", 
                                                            //"p", 
                                                            //"past", 
                                                            //"pending", 
                                                            //"per", 
                                                            //"perhaps", 
                                                            //"plus", 
                                                            //"possible", 
                                                            //"present", 
                                                            //"probably", 
                                                            //"provided", 
                                                            //"providing", 
                                                            //"public", 
                                                            //"q", 
                                                            //"qua", 
                                                            //"quite", 
                                                            //"r", 
                                                            //"rather", 
                                                            //"re", 
                                                            //"real", 
                                                            //"really", 
                                                            //"respecting", 
                                                            //"right", 
                                                            //"round", 
                                                            //"s", 
                                                            //"same", 
                                                            //"sans", 
                                                            //"save", 
                                                            //"saving", 
                                                            //"second", 
                                                            //"several", 
                                                            //"shall", 
                                                            //"shalt", 
                                                            //"shan't", 
                                                            //"she", 
                                                            //"shed", 
                                                            //"shell", 
                                                            //"she's", 
                                                            //"short", 
                                                            //"should", 
                                                            //"shouldn't", 
                                                            //"since", 
                                                            //"six", 
                                                            //"small", 
                                                            //"so", 
                                                            //"some", 
                                                            //"somebody", 
                                                            //"someone", 
                                                            //"something", 
                                                            //"sometimes", 
                                                            //"soon", 
                                                            //"special", 
                                                            //"still", 
                                                            //"such", 
                                                            //"summat", 
                                                            //"supposing", 
                                                            //"sure", 
                                                            //"t", 
                                                            //"than", 
                                                            //"that", 
                                                            //"that'd", 
                                                            //"that'll", 
                                                            //"that's", 
                                                            //"the", 
                                                            //"thee", 
                                                            //"their", 
                                                            //"theirs", 
                                                            //"their's", 
                                                            //"them", 
                                                            //"themselves", 
                                                            //"then", 
                                                            //"there", 
                                                            //"there's", 
                                                            //"these", 
                                                            //"they", 
                                                            //"they'd", 
                                                            //"they'll", 
                                                            //"they're", 
                                                            //"they've", 
                                                            //"thine", 
                                                            //"this", 
                                                            //"tho", 
                                                            //"those", 
                                                            //"thou", 
                                                            //"though", 
                                                            //"three", 
                                                            //"thro'", 
                                                            //"through", 
                                                            //"throughout", 
                                                            //"thru", 
                                                            //"thyself", 
                                                            //"till", 
                                                            //"to", 
                                                            //"today", 
                                                            //"together", 
                                                            //"too", 
                                                            //"touching", 
                                                            //"toward", 
                                                            //"towards", 
                                                            //"twas", 
                                                            //"tween", 
                                                            //"twere", 
                                                            //"twill", 
                                                            //"twixt", 
                                                            //"two", 
                                                            //"twould", 
                                                            //"u", 
                                                            //"under", 
                                                            //"underneath", 
                                                            //"unless", 
                                                            //"unlike", 
                                                            //"until", 
                                                            //"unto", 
                                                            //"up", 
                                                            //"upon", 
                                                            //"us", 
                                                            //"used", 
                                                            //"usually", 
                                                            //"v", 
                                                            //"versus", 
                                                            //"very", 
                                                            //"via", 
                                                            //"vice", 
                                                            //"vis-a-vis", 
                                                            //"w", 
                                                            //"wanna", 
                                                            //"wanting", 
                                                            //"was", 
                                                            //"wasn't", 
                                                            //"way", 
                                                            //"we", 
                                                            //"we'd", 
                                                            //"well", 
                                                            //"were", 
                                                            //"weren't", 
                                                            //"wert", 
                                                            //"we've", 
                                                            //"what", 
                                                            //"whatever", 
                                                            //"what'll", 
                                                            //"what's", 
                                                            //"when", 
                                                            //"whencesoever", 
                                                            //"whenever", 
                                                            //"when's", 
                                                            //"whereas", 
                                                            //"where's", 
                                                            //"whether", 
                                                            //"which", 
                                                            //"whichever", 
                                                            //"whichsoever", 
                                                            //"while", 
                                                            //"whilst", 
                                                            //"who", 
                                                            //"who'd", 
                                                            //"whoever", 
                                                            //"whole", 
                                                            //"who'll", 
                                                            //"whom", 
                                                            //"whore", 
                                                            //"who's", 
                                                            //"whose", 
                                                            //"whoso", 
                                                            //"whosoever", 
                                                            //"will", 
                                                            //"with", 
                                                            //"within", 
                                                            //"without", 
                                                            //"wont", 
                                                            //"would", 
                                                            //"wouldn't", 
                                                            //"wouldst", 
                                                            //"x", 
                                                            //"y", 
                                                            //"ye", 
                                                            //"yet", 
                                                            //"you", 
                                                            //"you'd", 
                                                            //"you'll", 
                                                            //"your", 
                                                            //"you're", 
                                                            //"yours", 
                                                            //"yourself", 
                                                            //"yourselves", 
                                                            //"you've", 
                                                            //"z"

		};

        public static string[] extraStopWordsList = new string[] {  "e.g",
                                                                    "i.e",
                                                                    "abc0003",
                                                                    "abc-t123",
                                                                    "achmetow",
                                                                    "afaict",
                                                                    "afaik",
                                                                    "asap",
                                                                    "aug",
                                                                    "b04",
                                                                    "clr",
                                                                    "cq",
                                                                    "cq1803",
                                                                    "cqnnn",
                                                                    "def123",
                                                                    "denis",
                                                                    "e.g",
                                                                    "e4",
                                                                    "ed",
                                                                    "edt",
                                                                    "eugene",
                                                                    "fyi",
                                                                    "give",
                                                                    "h1",
                                                                    "h2",
                                                                    "i.e",
                                                                    "i20070621-1340",
                                                                    "i20080617-2000",
                                                                    "i20091126-2200-e3x",
                                                                    "iv",
                                                                    "jan",
                                                                    "jong",
                                                                    "m20070212-1330",
                                                                    "m20071023-1652",
                                                                    "m20080221-1800",
                                                                    "m20080911-1700",
                                                                    "m20100909-0800",
                                                                    "m20110909-1335",
                                                                    "m20120208-0800",
                                                                    "p1-p5",
                                                                    "p2",
                                                                    "p3",
                                                                    "pb",
                                                                    "pebkac",
                                                                    "pingel",
                                                                    "pm",
                                                                    "pov",
                                                                    "px",
                                                                    "r4e",
                                                                    "rc1",
                                                                    "roy",
                                                                    "rv",
                                                                    "sp2",
                                                                    "sr1",
                                                                    "steffen",
                                                                    "tehrnhoefer",
                                                                    "thomas",
                                                                    "timur",
                                                                    "v20090912-0400-e3x",
                                                                    "v20091015-0500-e3x",
                                                                    "v20100608-0100-e3x",
                                                                    "v20110422-0200",
                                                                    "v201202080800",
                                                                    "wed",
                                                                    "ws",
                                                                    "xxx",
                                                                    "xxxx",
                                                                    "zoe", 
                                                                    /*//extra for 394920
                                                                    "bug", 
                                                                    "mark",
                                                                    "duplicate", 
                                                                    "helpful",
                                                                    "dialog",
                                                                    "editor",
                                                                    "ui",
                                                                    "user",
                                                                    "nice",
                                                                    "great",
                                                                    "made",
                                                                    "value",
                                                                    "happy",
                                                                    "dirty",
                                                                    "eyesight",
                                                                    "side-by-side",
                                                                    "exist",
                                                                    "over-weight"
                                                                    //extra for 394920*/
        };


        private static Hashtable _stopwords = null;

        public static object AddElement(IDictionary collection, Object key, object newValue)
        {
            object element = collection[key];
            collection[key] = newValue;
            return element;
        }

        public static bool IsStopword(string str)
        {

            //int index=Array.BinarySearch(stopWordsList, str)
            return _stopwords.ContainsKey(str.ToLower());
        }


        public StopWordsHandler(bool bSyn)
        {
            //if (_stopwords == null)
            //{
            _stopwords = new Hashtable();
            double dummy = 0;
            foreach (string word in stopWordsList)
            {
                AddElement(_stopwords, word, dummy);
            }
            if (bSyn == true)
            {
                foreach (string word in extraStopWordsList)
                {
                    AddElement(_stopwords, word, dummy);
                }

            }
            //}
        }
    }


    /// <summary>
	/// Summary description for Tokeniser.
	/// Partition string into SUBwords
	/// </summary>
	internal class Tokeniser
    {
        //PH 11 apr
        bool bStem; //true if stemming is done
        bool bRemoveStop; //true if stop words are removed
        bool bLower; //true if lower casing is done
                     //System.IO.StreamWriter sw = new System.IO.StreamWriter("Removed.txt", true);

        public static string[] ArrayListToArray(ArrayList arraylist)
        {
            string[] array = new string[arraylist.Count];
            for (int i = 0; i < arraylist.Count; i++) array[i] = (string)arraylist[i];
            return array;
        }

        public string[] Partition(string input, bool bBi, bool bSyn, bool bCode, out int nrMatches)
        {
            //PH 5 apr
            //Regex r=new Regex("([ \\t{}():;. \n])");		
            //PH 15 apr
            //Regex r = new Regex(@"[a-z]");			

            if (bLower) input = input.ToLower();

            nrMatches = 0;

            if (bCode)
            {
                //remove cvs logs
                Regex r14 = new Regex(@"[C|c]hecking\sin(.|\n)*done");
                nrMatches += r14.Matches(input).Count;
                input = r14.Replace(input, "\n");
                Regex r15 = new Regex(@"[C|c]hecking\sin(.|\n)*revision.*\n");
                nrMatches += r15.Matches(input).Count;
                input = r15.Replace(input, "\n");
                Regex r16 = new Regex(@"/cvs/(.|\n)*revision.*\n");
                nrMatches += r16.Matches(input).Count;
                //Regex r16 = new Regex(@"/cvs");
                //test
                MatchCollection testMatches = Regex.Matches(input, r16.ToString());
                //foreach (Match m in testMatches)
                //{
                //    Debug.WriteLine(m.ToString());
                //}
                //do it
                input = r16.Replace(input, "\n");
            }

            //PH 15 apr
            // Remove URLs 
            Regex r1 = new Regex(@"\w+\://\S+");
            nrMatches += r1.Matches(input).Count;
            //test
            //MatchCollection matches1 = Regex.Matches(input, r1.ToString());
            //foreach (Match m in matches1)
            //{
            //   sw.WriteLine(m.ToString());
            //}
            //do it
            input = r1.Replace(input, " ");

            // Remove file paths
            Regex r2 = new Regex(@"((\w+/\w+(/\w+)+)|(\w+(\\\w+)+))");
            nrMatches += r2.Matches(input).Count;
            //test
            //MatchCollection matches2 = Regex.Matches(input, r2.ToString());
            //foreach (Match m in matches2)
            //{
            //    sw.WriteLine(m.ToString());
            //}
            //sw.Close();
            //do it
            input = r2.Replace(input, " ");

            // Remove quoted text
            Regex r3 = new Regex(@"\n>.*\n");
            nrMatches += r3.Matches(input).Count;
            //test
            //MatchCollection matches3 = Regex.Matches(input, r3.ToString());
            //foreach (Match m in matches3)
            //{
            //    sw.WriteLine(m.ToString());
            //}
            //sw.Close();
            //do it
            input = r3.Replace(input, "\n ");

            if (bCode)
            {
                //remove source code
                //remove stack traces
                Regex r4 = new Regex(@"\n\s*at\s*\S+(\.\S)*\(.*\)");
                nrMatches += r4.Matches(input).Count;
                input = r4.Replace(input, "\n ");
                Regex r5 = new Regex(@"\n[\w\.]+[e|E]xception.*(?=\n)");
                nrMatches += r5.Matches(input).Count;
                input = r5.Replace(input, "\n ");
                Regex r6 = new Regex(@"\-\-\s[E|e]rror\s[D|d]etails(.|\n)*[E|e]xception\s[S|s]tack\s[t|T]race\:");
                nrMatches += r6.Matches(input).Count;
                input = r6.Replace(input, "\n ");

                //remove build ID and other related comments
                Regex r7 = new Regex(@"[B|b]uild\s[I|i][d|D].*\n");
                nrMatches += r7.Matches(input).Count;
                input = r7.Replace(input, "\n ");
                Regex r8 = new Regex(@"[U|u]ser\-[A|a]gent.*\n");
                nrMatches += r8.Matches(input).Count;
                input = r8.Replace(input, "\n ");
                Regex r9 = new Regex(@"[S|s]teps.*[R|r]eproduce\:");
                nrMatches += r9.Matches(input).Count;
                input = r9.Replace(input, " ");
                Regex r10 = new Regex(@"[M|m]ore\s[I|i]nformation\:");
                nrMatches += r10.Matches(input).Count;
                input = r10.Replace(input, " ");
                Regex r11 = new Regex(@"[R|r]eproducible\:.*(?=\n)");
                nrMatches += r11.Matches(input).Count;
                input = r11.Replace(input, " ");
                Regex r13 = new Regex(@"\n[T|t]hread\s\[.*\n(.*line.*\n)+");
                nrMatches += r13.Matches(input).Count;
                input = r13.Replace(input, "\n ");

                //remove code lines
                Regex r12 = new Regex(@"\n.*[\;|\}|\{](?=\n)");
                nrMatches += r12.Matches(input).Count;
                input = r12.Replace(input, "\n ");


            } //end if bCode

            // Get words
            MatchCollection matches = Regex.Matches(input, @"\b[a-z|A-Z]\w*((\.|\-|_|')*(\w+))*");
            String[] tokens = new String[matches.Count];
            int j = 0;
            foreach (Match m in matches)
            {
                tokens[j] = m.Value;
                j++;
            }

            ArrayList filter = new ArrayList();
            string strLast = "";

            //added stemming PH 27 march
            PorterStemming ps = new PorterStemming();

            for (int i = 0; i < tokens.Length; i++)
            {
                string s;
                if (bSyn)
                {
                    s = CorrectTerm(tokens[i]);
                }
                else
                {
                    s = tokens[i];
                }
                if (bRemoveStop)
                {
                    if (!StopWordsHandler.IsStopword(tokens[i]))
                    {
                        if (bStem)
                        {
                            s = ps.stemTerm(s.Replace("'", ""));
                        }
                        else
                        {
                            s = s.Replace("'", "");
                        }
                        if (!bBi) filter.Add(s);
                        if (strLast != "" && bBi) filter.Add(strLast + " " + s);
                        strLast = s;
                    }
                    else
                    {
                        if (strLast != "" && bBi) filter.Add(strLast + " " + s);
                        strLast = "";
                    }
                }
                else
                {
                    if (bStem)
                    {
                        s = ps.stemTerm(s.Replace("'", ""));
                    }
                    else
                    {
                        s = s.Replace("'", "");
                    }
                    if (!bBi) filter.Add(s);
                    if (strLast != "" && bBi) filter.Add(strLast + " " + s);
                    strLast = s;
                }
            }
            return ArrayListToArray(filter);
        }


        public Tokeniser(bool bStem, bool bRemoveStop, bool bLower)
        {
            this.bStem = bStem;
            this.bRemoveStop = bRemoveStop;
            this.bLower = bLower;
            lstWrong.Clear();
            lstWrong.AddRange(wrongWords);
        }

        public static string[] wrongWords = new string[] {
            "ablity",
            "accidently",
            "achive",
            "appered",
            "assuption",
            "attachements",
            "auth",
            "avaliability",
            "behaviour",
            "bolded",
            "bugzill",
            "challening",
            "ci",
            "ctlr",
            "currenlty",
            "custonm",
            "defautl",
            "defs",
            "delgators",
            "depdencies",
            "dev",
            "diabled",
            "dialogue",
            "didnt",
            "diff",
            "dnd",
            "drop-down",
            "e.g",
            "elses",
            "e-mails",
            "enterting",
            "exisiting",
            "focussed",
            "impl",
            "incase",
            "int",
            "isse",
            "junit3",
            "junit4",
            "key-bindings",
            "maintainance",
            "meta-data",
            "peformquery",
            "plug-in",
            "presync",
            "read-only",
            "recognise",
            "reopen",
            "repos",
            "reuse",
            "rev",
            "sec",
            "short-cut",
            "similiar",
            "simliar",
            "someting",
            "specifiy",
            "sub-menu",
            "submited",
            "submition",
            "sub-section",
            "sub-task",
            "sub-tasks",
            "successfull",
            "sufficent",
            "swe",
            "sync",
            "synchronisation",
            "synchronising",
            "synchronization",
            "syncing",
            "synctaskjob",
            "taks",
            "taskes",
            "task-repositories",
            "timeout",
            "validatesetttings",
            "wil",
            "work-arounds",
            "wouldt",
            "xml_rpc",
            "xmlrpc",
            "unsubmitted",
            "dup"
        };

        public static string[] correctWords = new string[] {
            "ability",
            "accidentally",
            "achieve",
            "appeared",
            "assumption",
            "attachments",
            "authentication",
            "availability",
            "behavior",
            "bold",
            "bugzilla",
            "challenging",
            "checkin",
            "ctrl",
            "currently",
            "custom",
            "default",
            "definitions",
            "delegators",
            "dependencies",
            "development",
            "disabled",
            "dialog",
            "didn't",
            "difference",
            "drag and drop",
            "dropdown",
            "abbr",
            "else's",
            "email",
            "entering",
            "existing",
            "focused",
            "implementation",
            "in case",
            "integer",
            "issue",
            "junit",
            "junit",
            "keybindings",
            "maintenance",
            "metadata",
            "performquery",
            "plugin",
            "presynchronization",
            "readonly",
            "recognize",
            "re-open",
            "repositories",
            "re-use",
            "revision",
            "seconds",
            "shortcut",
            "similar",
            "similar",
            "something",
            "specify",
            "submenu",
            "submitted",
            "submission",
            "subsection",
            "subtask",
            "subtasks",
            "successful",
            "sufficient",
            "we",
            "synchronization",
            "synchronization",
            "synchronizing",
            "synchronization",
            "synchronizing",
            "synchronizetasksjob",
            "task",
            "tasks",
            "taskrepositories",
            "time-out",
            "validatesettings",
            "will",
            "workarounds",
            "would",
            "xml-rpc",
            "xml-rpc",
            "submit",
            "duplicate"
        };

        List<string> lstWrong = new List<string>();

        private string CorrectTerm(string input)
        {
            if (lstWrong.Contains(input))
            {
                return correctWords[lstWrong.IndexOf(input)];
            }
            else
            {
                return input;
            }
        }

    }

    /// <summary>
	/// Summary description for TF_IDFLib.
	/// </summary>
	public class TFIDFMeasure
    {
        private string[] _docs;
        //private string[][] _ngramDoc;
        private int _numDocs = 0;
        private int _numTerms = 0;
        public ArrayList _terms;
        public int[][] _termFreq;
        public float[][] _termWeight;
        private int[] _maxTermFreq;
        private int[] _docFreq;
        //private List<string[]> _parsedDocs = new List<string[]>();

        public class TermVector
        {
            public static float ComputeCosineSimilarity(float[] vector1, float[] vector2)
            {
                if (vector1.Length != vector2.Length)
                    throw new Exception("DIFER LENGTH");


                float denom = (VectorLength(vector1) * VectorLength(vector2));
                if (denom == 0F)
                    return 0F;
                else
                    return (InnerProduct(vector1, vector2) / denom);

            }

            public static float InnerProduct(float[] vector1, float[] vector2)
            {

                if (vector1.Length != vector2.Length)
                    throw new Exception("DIFFER LENGTH ARE NOT ALLOWED");


                float result = 0F;
                for (int i = 0; i < vector1.Length; i++)
                    result += vector1[i] * vector2[i];

                return result;
            }

            public static float VectorLength(float[] vector)
            {
                float sum = 0.0F;
                for (int i = 0; i < vector.Length; i++)
                    sum = sum + (vector[i] * vector[i]);

                return (float)Math.Sqrt(sum);
            }

        }

        private IDictionary _wordsIndex = new Hashtable();

        public TFIDFMeasure(string[] documents, bool bStem, bool bRemoveStop, bool bLower, bool bBi, bool bSyn, bool bCode, bool bMulti, bool bDoc)
        {
            _docs = documents;
            _numDocs = documents.Length;
            MyInit(bStem, bRemoveStop, bLower, bBi, bSyn, bCode, bMulti, bDoc);
        }

        public TFIDFMeasure(FeatureCollection fc, bool bStem, bool bRemoveStop, bool bWTitle, bool bBi, bool bSyn, bool bLower, bool bCode, bool bMulti, bool bDoc)
        {
            _numDocs = fc.featureList.Count;
            _docs = new string[fc.featureList.Count];
            int i = 0;
            foreach (Feature f in fc.featureList)
            {
                _docs[i] = f.doc;
                i++;
            }
            MyInit(bStem, bRemoveStop, bLower, bBi, bSyn, bCode, bMulti, bDoc);
        }

        //Cross-measure TFIDF with title en summary of feature iFt vs all comments of all others
        public TFIDFMeasure(FeatureCollection fc, int iFt, bool bStem, bool bRemoveStop, bool bWTitle, bool bBi, bool bSyn, bool bLower, bool bCode, bool bMulti, bool bDoc)
        {
            _numDocs = fc.featureList.Count;
            _docs = new string[fc.featureList.Count];
            int i = 0;
            foreach (Feature f in fc.featureList)
            {
                if (fc.featureList.IndexOf(f) != iFt)
                {
                    _docs[i] = f.doc;
                }
                else
                {
                    _docs[i] = f.title + " \n " + f.description;
                }
                i++;
            }
            MyInit(bStem, bRemoveStop, bLower, bBi, bSyn, bCode, bMulti, bDoc);
        }

        private void GeneratNgramText()
        {

        }

        private ArrayList GenerateTerms(string[] docs, bool bStem, bool bRemoveStop, bool bLower, bool bBi, bool bSyn, bool bCode, bool bMulti, bool bDoc)
        {
            ArrayList uniques = new ArrayList();
            List<int> counts = new List<int>();
            List<int> docCounts = new List<int>();
            //_ngramDoc=new string[_numDocs][] ;
            int totalMatches = 0;
            for (int i = 0; i < docs.Length; i++)
            {
                Tokeniser tokenizer = new Tokeniser(bStem, bRemoveStop, bLower);
                int nrMatches = 0;
                string[] words = tokenizer.Partition(docs[i], bBi, bSyn, bCode, out nrMatches);
                totalMatches += nrMatches;
                //Utilities.LogMessageToFile(MainForm.logfile, i + ": " + nrMatches + " matches removed");
                //_parsedDocs.Add(words);
                //words = tokenizer.Partition(docs[i], bBi, bSyn, bCode);

                for (int j = 0; j < words.Length; j++)
                {
                    if (!uniques.Contains(words[j]))
                    {
                        uniques.Add(words[j]);
                        counts.Add(1);
                        if (bDoc) docCounts.Add(i);
                    }
                    else
                    {
                        int wordIndex = uniques.IndexOf(words[j]);
                        counts[wordIndex] += 1;
                        if (bDoc)
                        {
                            if (docCounts[wordIndex] < i)
                            {
                                docCounts[wordIndex] = -1;
                            }
                        }
                    }
                }
            }
            //Utilities.LogMessageToFile(MainForm.logfile, "TOTAL: " + totalMatches + " matches removed");

            if (bMulti)
            {
                for (int i = 0; i < counts.Count; i++)
                {
                    if (bDoc)
                    {
                        if (counts[i] == 1 || docCounts[i] > -1)
                        {
                            uniques.RemoveAt(i);
                            counts.RemoveAt(i);
                            docCounts.RemoveAt(i);
                            i--;
                        }
                    }
                    else
                    {
                        if (counts[i] == 1)
                        {
                            uniques.RemoveAt(i);
                            counts.RemoveAt(i);
                            i--;
                        }
                    }
                }
            }

            return uniques;
        }



        private static object AddElement(IDictionary collection, object key, object newValue)
        {
            object element = collection[key];
            collection[key] = newValue;
            return element;
        }

        private int GetTermIndex(string term)
        {
            object index = _wordsIndex[term];
            if (index == null) return -1;
            return (int)index;
        }

        private void MyInit(bool bStem, bool bRemoveStop, bool bLower, bool bBi, bool bSyn, bool bCode, bool bMulti, bool bDoc)
        {
            _terms = GenerateTerms(_docs, bStem, bRemoveStop, bLower, bBi, bSyn, bCode, bMulti, bDoc);
            _numTerms = _terms.Count;

            _maxTermFreq = new int[_numDocs];
            _docFreq = new int[_numTerms];
            _termFreq = new int[_numTerms][];
            _termWeight = new float[_numTerms][];

            for (int i = 0; i < _terms.Count; i++)
            {
                _termWeight[i] = new float[_numDocs];
                _termFreq[i] = new int[_numDocs];

                AddElement(_wordsIndex, _terms[i], i);
            }

            GenerateTermFrequency(bStem, bRemoveStop, bLower, bBi, bSyn, bCode);
            // hierna is er een matrix met termen per doc
            //if (bMulti)
            //{

            //    //remove words with total count = 1
            //    for (int i = 0; i < _terms.Count; i++)
            //    {
            //        int totalCount = 0;
            //        int index = 0;
            //        for (int j = 0; j < _numDocs; j++)
            //        {
            //            totalCount = totalCount + _termFreq[i][j];
            //            if (_termFreq[i][j] == 1)
            //            {
            //                index = j;
            //            }
            //        }
            //        if (totalCount == 1)
            //        {
            //            _termFreq[i][index] = 0;
            //        }
            //    }
            //}
            GenerateTermWeight();

        }

        private float Log(float num)
        {
            return (float)Math.Log(num);//log2
        }

        private void GenerateTermFrequency(bool bStem, bool bRemoveStop, bool bLower, bool bBi, bool bSyn, bool bCode)
        {
            for (int i = 0; i < _numDocs; i++)
            {
                string curDoc = _docs[i];
                IDictionary freq = GetWordFrequency(curDoc, bStem, bRemoveStop, bLower, bBi, bSyn, bCode);
                //IDictionary freq = GetWordFrequency(i, bStem, bRemoveStop, bLower, bBi, bSyn, bCode);
                IDictionaryEnumerator enums = freq.GetEnumerator();
                _maxTermFreq[i] = int.MinValue;
                while (enums.MoveNext())
                {
                    string word = (string)enums.Key;
                    int wordFreq = (int)enums.Value;
                    int termIndex = GetTermIndex(word);
                    if (termIndex != -1)
                    {
                        _termFreq[termIndex][i] = wordFreq;
                        _docFreq[termIndex]++;
                        if (wordFreq > _maxTermFreq[i]) _maxTermFreq[i] = wordFreq;
                    }
                    else
                    {
                    }
                }
            }
        }


        private void GenerateTermWeight()
        {
            for (int i = 0; i < _numTerms; i++)
            {
                for (int j = 0; j < _numDocs; j++)
                    _termWeight[i][j] = ComputeTermWeight(i, j);
            }
        }

        private float GetTermFrequency(int term, int doc)
        {
            int freq = _termFreq[term][doc];
            int maxfreq = _maxTermFreq[doc];

            return ((float)freq / (float)maxfreq);
        }

        private float GetInverseDocumentFrequency(int term)
        {
            int df = _docFreq[term];
            return Log((float)(_numDocs) / (float)df);
        }

        private float ComputeTermWeight(int term, int doc)
        {
            float tf = GetTermFrequency(term, doc);
            float idf = GetInverseDocumentFrequency(term);
            return tf * idf;
        }

        public float[] GetTermVector(int doc)
        {
            float[] w = new float[_numTerms];
            for (int i = 0; i < _numTerms; i++)
                w[i] = _termWeight[i][doc];


            return w;
        }

        public float GetSimilarity(int doc_i, int doc_j)
        {
            float[] vector1 = GetTermVector(doc_i);
            float[] vector2 = GetTermVector(doc_j);

            return TermVector.ComputeCosineSimilarity(vector1, vector2);

        }

        //private IDictionary GetWordFrequency(int docNr, bool bStem, bool bRemoveStop, bool bLower, bool bBi, bool bSyn, bool bCode)
        //{
        //    //PH 11 apr string convertedInput=input.ToLower();

        //    //Tokeniser tokenizer = new Tokeniser(bStem, bRemoveStop, bLower);
        //    //String[] words = tokenizer.Partition(input, bBi, bSyn, bCode);
        //    String[] words = _parsedDocs[docNr];
        //    Array.Sort(words);

        //    String[] distinctWords = GetDistinctWords(words);

        //    IDictionary result = new Hashtable();
        //    for (int i = 0; i < distinctWords.Length; i++)
        //    {
        //        object tmp;
        //        tmp = CountWords(distinctWords[i], words);
        //        result[distinctWords[i]] = tmp;

        //    }

        //    return result;
        //}				

        private IDictionary GetWordFrequency(string input, bool bStem, bool bRemoveStop, bool bLower, bool bBi, bool bSyn, bool bCode)
        {
            //PH 11 apr string convertedInput=input.ToLower();

            Tokeniser tokenizer = new Tokeniser(bStem, bRemoveStop, bLower);
            int nrMatches = 0;
            String[] words = tokenizer.Partition(input, bBi, bSyn, bCode, out nrMatches);
            Array.Sort(words);

            String[] distinctWords = GetDistinctWords(words);

            IDictionary result = new Hashtable();
            for (int i = 0; i < distinctWords.Length; i++)
            {
                object tmp;
                tmp = CountWords(distinctWords[i], words);
                result[distinctWords[i]] = tmp;

            }

            return result;
        }

        private string[] GetDistinctWords(String[] input)
        {
            if (input == null)
                return new string[0];
            else
            {
                ArrayList list = new ArrayList();

                for (int i = 0; i < input.Length; i++)
                    if (!list.Contains(input[i])) // N-GRAM SIMILARITY?				
                        list.Add(input[i]);

                return Tokeniser.ArrayListToArray(list);
            }
        }



        private int CountWords(string word, string[] words)
        {
            int itemIdx = Array.BinarySearch(words, word);

            if (itemIdx > 0)
                while (itemIdx > 0 && words[itemIdx].Equals(word))
                    itemIdx--;

            int count = 0;
            while (itemIdx < words.Length && itemIdx >= 0)
            {
                if (words[itemIdx].Equals(word)) count++;

                itemIdx++;
                if (itemIdx < words.Length)
                    if (!words[itemIdx].Equals(word)) break;

            }

            return count;
        }
    }

    /*

	   Porter stemmer in CSharp, based on the Java port. The original paper is in

		   Porter, 1980, An algorithm for suffix stripping, Program, Vol. 14,
		   no. 3, pp 130-137,

	   See also http://www.tartarus.org/~martin/PorterStemmer

	   History:

	   Release 1

	   Bug 1 (reported by Gonzalo Parra 16/10/99) fixed as marked below.
	   The words 'aed', 'eed', 'oed' leave k at 'a' for step 3, and b[k-1]
	   is then out outside the bounds of b.

	   Release 2

	   Similarly,

	   Bug 2 (reported by Steve Dyrdahl 22/2/00) fixed as marked below.
	   'ion' by itself leaves j = -1 in the test for 'ion' in step 5, and
	   b[j] is then outside the bounds of b.

	   Release 3

	   Considerably revised 4/9/00 in the light of many helpful suggestions
	   from Brian Goetz of Quiotix Corporation (brian@quiotix.com).

	   Release 4
	   
	   This revision allows the Porter Stemmer Algorithm to be exported via the
	   .NET Framework. To facilate its use via .NET, the following commands need to be
	   issued to the operating system to register the component so that it can be
	   imported into .Net compatible languages, such as Delphi.NET, Visual Basic.NET,
	   Visual C++.NET, etc. 
	   
	   1. Create a stong name: 		
			sn -k Keyfile.snk  
	   2. Compile the C# class, which creates an assembly PorterStemmerAlgorithm.dll
			csc /t:library PorterStemmerAlgorithm.cs
	   3. Register the dll with the Windows Registry 
		  and so expose the interface to COM Clients via the type library 
		  ( PorterStemmerAlgorithm.tlb will be created)
			regasm /tlb PorterStemmerAlgorithm.dll
	   4. Load the component in the Global Assembly Cache
			gacutil -i PorterStemmerAlgorithm.dll
		
	   Note: You must have the .Net Studio installed.
	   
	   Once this process is performed you should be able to import the class 
	   via the appropiate mechanism in the language that you are using.
	   
	   i.e in Delphi 7 .NET this is simply a matter of selecting: 
			Project | Import Type Libary
	   And then selecting Porter stemmer in CSharp Version 1.4"!
	   
	   Cheers Leif
	
	*/

    /**
	  * Stemmer, implementing the Porter Stemming Algorithm
	  *
	  * The Stemmer class transforms a word into its root form.  The input
	  * word can be provided a character at time (by calling add()), or at once
	  * by calling one of the various stem(something) methods.
	  */

    public interface StemmerInterface
    {
        string stemTerm(string s);
    }

    [ClassInterface(ClassInterfaceType.None)]
    public class PorterStemming : StemmerInterface
    {
        private char[] b;
        private int i,     /* offset into b */
            i_end, /* offset to end of stemmed word */
            j, k;
        private static int INC = 200;
        /* unit of size whereby b is increased */

        public PorterStemming()
        {
            b = new char[INC];
            i = 0;
            i_end = 0;
        }

        /* Implementation of the .NET interface - added as part of realease 4 (Leif) */
        public string stemTerm(string s)
        {
            setTerm(s);
            stem();
            return getTerm();
        }

        /*
			SetTerm and GetTerm have been simply added to ease the 
			interface with other lanaguages. They replace the add functions 
			and toString function. This was done because the original functions stored
			all stemmed words (and each time a new woprd was added, the buffer would be
			re-copied each time, making it quite slow). Now, The class interface 
			that is provided simply accepts a term and returns its stem, 
			instead of storing all stemmed words.
			(Leif)
		*/

        void setTerm(string s)
        {
            i = s.Length;
            char[] new_b = new char[i];
            for (int c = 0; c < i; c++)
                new_b[c] = s[c];

            b = new_b;

        }

        public string getTerm()
        {
            return new String(b, 0, i_end);
        }


        /* Old interface to the class - left for posterity. However, it is not
		 * used when accessing the class via .NET (Leif)*/

        /**
		 * Add a character to the word being stemmed.  When you are finished
		 * adding characters, you can call stem(void) to stem the word.
		 */

        public void add(char ch)
        {
            if (i == b.Length)
            {
                char[] new_b = new char[i + INC];
                for (int c = 0; c < i; c++)
                    new_b[c] = b[c];
                b = new_b;
            }
            b[i++] = ch;
        }


        /** Adds wLen characters to the word being stemmed contained in a portion
		 * of a char[] array. This is like repeated calls of add(char ch), but
		 * faster.
		 */

        public void add(char[] w, int wLen)
        {
            if (i + wLen >= b.Length)
            {
                char[] new_b = new char[i + wLen + INC];
                for (int c = 0; c < i; c++)
                    new_b[c] = b[c];
                b = new_b;
            }
            for (int c = 0; c < wLen; c++)
                b[i++] = w[c];
        }

        /**
		 * After a word has been stemmed, it can be retrieved by toString(),
		 * or a reference to the internal buffer can be retrieved by getResultBuffer
		 * and getResultLength (which is generally more efficient.)
		 */
        public override string ToString()
        {
            return new String(b, 0, i_end);
        }

        /**
		 * Returns the length of the word resulting from the stemming process.
		 */
        public int getResultLength()
        {
            return i_end;
        }

        /**
		 * Returns a reference to a character buffer containing the results of
		 * the stemming process.  You also need to consult getResultLength()
		 * to determine the length of the result.
		 */
        public char[] getResultBuffer()
        {
            return b;
        }

        /* cons(i) is true <=> b[i] is a consonant. */
        private bool cons(int i)
        {
            switch (b[i])
            {
                case 'a': case 'e': case 'i': case 'o': case 'u': return false;
                case 'y': return (i == 0) ? true : !cons(i - 1);
                default: return true;
            }
        }

        /* m() measures the number of consonant sequences between 0 and j. if c is
		   a consonant sequence and v a vowel sequence, and <..> indicates arbitrary
		   presence,

			  <c><v>       gives 0
			  <c>vc<v>     gives 1
			  <c>vcvc<v>   gives 2
			  <c>vcvcvc<v> gives 3
			  ....
		*/
        private int m()
        {
            int n = 0;
            int i = 0;
            while (true)
            {
                if (i > j) return n;
                if (!cons(i)) break; i++;
            }
            i++;
            while (true)
            {
                while (true)
                {
                    if (i > j) return n;
                    if (cons(i)) break;
                    i++;
                }
                i++;
                n++;
                while (true)
                {
                    if (i > j) return n;
                    if (!cons(i)) break;
                    i++;
                }
                i++;
            }
        }

        /* vowelinstem() is true <=> 0,...j contains a vowel */
        private bool vowelinstem()
        {
            int i;
            for (i = 0; i <= j; i++)
                if (!cons(i))
                    return true;
            return false;
        }

        /* doublec(j) is true <=> j,(j-1) contain a double consonant. */
        private bool doublec(int j)
        {
            if (j < 1)
                return false;
            if (b[j] != b[j - 1])
                return false;
            return cons(j);
        }

        /* cvc(i) is true <=> i-2,i-1,i has the form consonant - vowel - consonant
		   and also if the second c is not w,x or y. this is used when trying to
		   restore an e at the end of a short word. e.g.

			  cav(e), lov(e), hop(e), crim(e), but
			  snow, box, tray.

		*/
        private bool cvc(int i)
        {
            if (i < 2 || !cons(i) || cons(i - 1) || !cons(i - 2))
                return false;
            int ch = b[i];
            if (ch == 'w' || ch == 'x' || ch == 'y')
                return false;
            return true;
        }

        private bool ends(String s)
        {
            int l = s.Length;
            int o = k - l + 1;
            if (o < 0)
                return false;
            char[] sc = s.ToCharArray();
            for (int i = 0; i < l; i++)
                if (b[o + i] != sc[i])
                    return false;
            j = k - l;
            return true;
        }

        /* setto(s) sets (j+1),...k to the characters in the string s, readjusting
		   k. */
        private void setto(String s)
        {
            int l = s.Length;
            int o = j + 1;
            char[] sc = s.ToCharArray();
            for (int i = 0; i < l; i++)
                b[o + i] = sc[i];
            k = j + l;
        }

        /* r(s) is used further down. */
        private void r(String s)
        {
            if (m() > 0)
                setto(s);
        }

        /* step1() gets rid of plurals and -ed or -ing. e.g.
			   caresses  ->  caress
			   ponies    ->  poni
			   ties      ->  ti
			   caress    ->  caress
			   cats      ->  cat

			   feed      ->  feed
			   agreed    ->  agree
			   disabled  ->  disable

			   matting   ->  mat
			   mating    ->  mate
			   meeting   ->  meet
			   milling   ->  mill
			   messing   ->  mess

			   meetings  ->  meet

		*/

        private void step1()
        {
            if (b[k] == 's')
            {
                if (ends("sses"))
                    k -= 2;
                else if (ends("ies"))
                    setto("i");
                else if (b[k - 1] != 's')
                    k--;
            }
            if (ends("eed"))
            {
                if (m() > 0)
                    k--;
            }
            else if ((ends("ed") || ends("ing")) && vowelinstem())
            {
                k = j;
                if (ends("at"))
                    setto("ate");
                else if (ends("bl"))
                    setto("ble");
                else if (ends("iz"))
                    setto("ize");
                else if (doublec(k))
                {
                    k--;
                    int ch = b[k];
                    if (ch == 'l' || ch == 's' || ch == 'z')
                        k++;
                }
                else if (m() == 1 && cvc(k)) setto("e");
            }
        }

        /* step2() turns terminal y to i when there is another vowel in the stem. */
        private void step2()
        {
            if (ends("y") && vowelinstem())
                b[k] = 'i';
        }

        /* step3() maps double suffices to single ones. so -ization ( = -ize plus
		   -ation) maps to -ize etc. note that the string before the suffix must give
		   m() > 0. */
        private void step3()
        {
            if (k == 0)
                return;

            /* For Bug 1 */
            switch (b[k - 1])
            {
                case 'a':
                    if (ends("ational")) { r("ate"); break; }
                    if (ends("tional")) { r("tion"); break; }
                    break;
                case 'c':
                    if (ends("enci")) { r("ence"); break; }
                    if (ends("anci")) { r("ance"); break; }
                    break;
                case 'e':
                    if (ends("izer")) { r("ize"); break; }
                    break;
                case 'l':
                    if (ends("bli")) { r("ble"); break; }
                    if (ends("alli")) { r("al"); break; }
                    if (ends("entli")) { r("ent"); break; }
                    if (ends("eli")) { r("e"); break; }
                    if (ends("ousli")) { r("ous"); break; }
                    break;
                case 'o':
                    if (ends("ization")) { r("ize"); break; }
                    if (ends("ation")) { r("ate"); break; }
                    if (ends("ator")) { r("ate"); break; }
                    break;
                case 's':
                    if (ends("alism")) { r("al"); break; }
                    if (ends("iveness")) { r("ive"); break; }
                    if (ends("fulness")) { r("ful"); break; }
                    if (ends("ousness")) { r("ous"); break; }
                    break;
                case 't':
                    if (ends("aliti")) { r("al"); break; }
                    if (ends("iviti")) { r("ive"); break; }
                    if (ends("biliti")) { r("ble"); break; }
                    break;
                case 'g':
                    if (ends("logi")) { r("log"); break; }
                    break;
                default:
                    break;
            }
        }

        /* step4() deals with -ic-, -full, -ness etc. similar strategy to step3. */
        private void step4()
        {
            switch (b[k])
            {
                case 'e':
                    if (ends("icate")) { r("ic"); break; }
                    if (ends("ative")) { r(""); break; }
                    if (ends("alize")) { r("al"); break; }
                    break;
                case 'i':
                    if (ends("iciti")) { r("ic"); break; }
                    break;
                case 'l':
                    if (ends("ical")) { r("ic"); break; }
                    if (ends("ful")) { r(""); break; }
                    break;
                case 's':
                    if (ends("ness")) { r(""); break; }
                    break;
            }
        }

        /* step5() takes off -ant, -ence etc., in context <c>vcvc<v>. */
        private void step5()
        {
            if (k == 0)
                return;

            /* for Bug 1 */
            switch (b[k - 1])
            {
                case 'a':
                    if (ends("al")) break; return;
                case 'c':
                    if (ends("ance")) break;
                    if (ends("ence")) break; return;
                case 'e':
                    if (ends("er")) break; return;
                case 'i':
                    if (ends("ic")) break; return;
                case 'l':
                    if (ends("able")) break;
                    if (ends("ible")) break; return;
                case 'n':
                    if (ends("ant")) break;
                    if (ends("ement")) break;
                    if (ends("ment")) break;
                    /* element etc. not stripped before the m */
                    if (ends("ent")) break; return;
                case 'o':
                    if (ends("ion") && j >= 0 && (b[j] == 's' || b[j] == 't')) break;
                    /* j >= 0 fixes Bug 2 */
                    if (ends("ou")) break; return;
                /* takes care of -ous */
                case 's':
                    if (ends("ism")) break; return;
                case 't':
                    if (ends("ate")) break;
                    if (ends("iti")) break; return;
                case 'u':
                    if (ends("ous")) break; return;
                case 'v':
                    if (ends("ive")) break; return;
                case 'z':
                    if (ends("ize")) break; return;
                default:
                    return;
            }
            if (m() > 1)
                k = j;
        }

        /* step6() removes a final -e if m() > 1. */
        private void step6()
        {
            j = k;

            if (b[k] == 'e')
            {
                int a = m();
                if (a > 1 || a == 1 && !cvc(k - 1))
                    k--;
            }
            if (b[k] == 'l' && doublec(k) && m() > 1)
                k--;
        }

        /** Stem the word placed into the Stemmer buffer through calls to add().
		 * Returns true if the stemming process resulted in a word different
		 * from the input.  You can retrieve the result with
		 * getResultLength()/getResultBuffer() or toString().
		 */
        public void stem()
        {
            k = i - 1;
            if (k > 1)
            {
                step1();
                step2();
                step3();
                step4();
                step5();
                step6();
            }
            i_end = k + 1;
            i = 0;
        }
    }

    public class Feature
    {
        public string id;
        public string title;
        public string description;
        public IEnumerable<string> comments;
        public Dictionary<string, int> wordCount = new Dictionary<string, int>();
        public string doc; //text of title and comments
        public string duplicate_id;
        public int dupRank;
        public float dupSim;

        //bAllComments = true means leave in all comments
        public Feature(string id, string title, string desc, IEnumerable<string> comm, bool bAllComments, bool bWTitle)
        {
            this.id = id;
            this.title = title;
            description = desc;
            comments = comm;
            doc += getText(title);
            if (bWTitle) doc += "\n " + getText(title);
            doc += "\n " + getText(desc);
            if (bAllComments == true)
            {
                foreach (string c in comm)
                {
                    doc += "\n " + getText(c);
                }
            }
            //createDictionary(true);
        }

        public Feature(string id, string dup)
        {
            this.id = id;
            this.duplicate_id = dup;
        }


        private void createDictionary(bool stemming)
        {
            MatchCollection matches = Regex.Matches(description, @"[\w\d_]+", RegexOptions.Singleline);
            foreach (Match match in matches)
            {
                if (match.Success)
                {
                    string stemStr;
                    if (stemming)
                    {
                        PorterStemming ps = new PorterStemming();
                        stemStr = ps.stemTerm(match.ToString().ToLower());
                    }
                    else
                    {
                        stemStr = match.ToString().ToLower();
                    }
                    if (wordCount.ContainsKey(stemStr))
                    {
                        wordCount[stemStr]++;
                    }
                    else
                    {
                        wordCount.Add(stemStr, 1);
                    }
                }
            }

        }

        private string getText(string strInput)
        {
            return strInput;
        }
    }

    public class FeatureCollection
    {
        public List<Feature> featureList = new List<Feature>();
        public string bugTag;

        public int TFIDF(string stem)
        {
            int documentCount = 0;
            foreach (Feature ft in featureList)
            {
                if (ft.wordCount.ContainsKey(stem))
                {
                    documentCount++;
                }
            }
            return documentCount;
        }
    }


    public partial class MainForm /*: Form */
    {
        DataSet dsFeatures = new DataSet();
        /////BindingSource bsComments = new BindingSource();
        /////BindingSource bsFeatures = new BindingSource();
        FeatureCollection fc;
        TFIDFMeasure tf;
        public static string logfile;

        //TO CHANGE: choice between MyLyn/Netbeans and Tigris ArgoUML is made here
        //string fileXML = "C:\\Users\\889716\\SkyDrive\\Documents\\EQuA\\FeatureRequests\\MyLyn\\20130408Features.xml";
        //string fileXML = "..\\..\\..\\..\\MyLyn\\20130408Features.xml";
        //string fileXML = "..\\..\\..\\..\\Netbeans\\20130514_enh_2.xml";
        ///// ******string fileXML = "..\\..\\..\\..\\ArgoUML\\20130514_issues.xml";
        string fileXML;
        //string dirPath = "..\\..\\..\\..\\MyLyn";
        //string dirPath = "..\\..\\..\\..\\Netbeans";
        string dirPath = "..\\..\\..\\..\\ArgoUML";
        /////string dirPath;
        int type = 1; //Tigris = 1; Mylyn/Netbeans = 0;
        double simCut = 0.5;
        /* PAY ATTENTION TO TFIDF Cut-off at 0.x SIM for writing cossim file!!!!!*/

        //***** added by jen *****
        int cbMethod_SelectedIndex = 0;
        int cbType_SelectedIndex = 0;
        string lbFile_Text;
        DataTable bsFeatures_DataSource;
        DataTable bsComments_DataSource;
        string tbK_Text;
        string ofXML_FileName;
        public static string lblTDF_Text;
        string lblTerms_Text;
        DataTable dgFeatures_DataSource;
        DataTable dgComments_DataSource;
        // ***** keeping as defaults for now *****
        bool SM = true;
        bool SR = false;
        bool DW = true;
        bool BG = false;
        bool SY = true;
        bool LO = true;
        bool SC = true;
        bool MU = false;
        bool DO = false;
        bool AC = true;

        public MainForm(string inputFile, string outputFile)
        {
            /////InitializeComponent();
            fileXML = inputFile; // **** added **** assign configuration
            dirPath = outputFile; // set location for log file

            //by default select "AllComments"
            ///// cbMethod.SelectedIndex = 0; 
            ///// cbType.SelectedIndex = type;
            // ***** Replacing 
            chooseFile();
            cbType_SelectedIndex = type;
            ///// lbFile.Text = fileXML;
            lbFile_Text = fileXML;
            DateTime d = DateTime.Now;
            logfile = outputFile + '\\' + d.Year.ToString("D4") + d.Month.ToString("D2") + d.Day.ToString("D2") + "_" + d.Hour.ToString("D2") + d.Minute.ToString("D2") + d.Second.ToString("D2") + "_log.txt";

            Console.WriteLine("Do you want to calculate TFIDF? (y/n)");
            //string usrInput = Console.ReadLine();
            string usrInput = "y";
            if (usrInput == "y")
            {
                calculateTFIDF();
            }

        }
        /*
        private void btnFill_Click(object sender, EventArgs e)
        {
            InputXML(fileXML);
            ///// bsFeatures.DataSource = dsFeatures.Tables[fc.bugTag];
            bsFeatures_DataSource = dsFeatures.Tables[fc.bugTag];
            ///// bsComments.DataSource = dsFeatures.Tables["long_desc"];
            bsComments_DataSource = dsFeatures.Tables["long_desc"];
        } */

        /*
    private void btnTest_Click(object sender, EventArgs e)
    {
        DateTime start = DateTime.Now;
        Utilities.LogMessageToFile(MainForm.logfile, DateTime.Now.ToShortTimeString() + " Start LSA");
        InputXML(fileXML);
        Utilities.LogMessageToFile(MainForm.logfile, " Feature requests processed: " + fc.featureList.Count.ToString());
        ////StopWordsHandler swh = new StopWordsHandler(cbSY.Checked);
        StopWordsHandler swh = new StopWordsHandler(true);
        ////TFIDFMeasure tf = new TFIDFMeasure(fc, cbSM.Checked, cbSR.Checked, cbDW.Checked, cbBG.Checked, cbSY.Checked, cbLO.Checked, cbSC.Checked, cbMU.Checked, cbDO.Checked);
        TFIDFMeasure tf = new TFIDFMeasure(fc, true, false, true, false, true, true, true, false, false);
        Utilities.LogMessageToFile(MainForm.logfile, DateTime.Now.ToShortTimeString() + " TFIDF matrix calculated");
        /* //////
        if (tbK.Text != "")
        {
            LSA.MainLSA(fc, tf, Int32.Parse(tbK.Text));
        }
        else
        {
            LSA.MainLSA(fc, tf, 0);
        }
        */ /////
           /*
           Console.WriteLine("Enter tbK: ");
           tbK_Text = Console.ReadLine();
           if (tbK_Text != "")
           {
               LSA.MainLSA(fc, tf, Int32.Parse(tbK_Text));
           }
           else
           {
               LSA.MainLSA(fc, tf, 0);
           }

           ///// Utilities.LogMessageToFile(MainForm.logfile, DateTime.Now.ToShortTimeString() + " LSA matrix calculated for k = " + tbK.Text);
           Utilities.LogMessageToFile(MainForm.logfile, DateTime.Now.ToShortTimeString() + " LSA matrix calculated for k = " + tbK_Text);

           //MessageBox.Show(LSA.GetSimilarity(0, 1).ToString());

           string outputFile;
           outputFile = dirPath + "\\LSA" + getFileEnding();
           outputFile += ".csv";
           System.IO.File.Delete(outputFile);
           System.IO.StreamWriter sw = new System.IO.StreamWriter(outputFile, true);
           sw.WriteLine("Title_i;ID_i;Doc_j;ID_j;Cosine Similarity");
           for (int i = 0; i < fc.featureList.Count; i++)
           {
               Feature f1 = fc.featureList[i];
               for (int j = 0; j < fc.featureList.Count; j++)
               {
                   if (j != i)
                   {
                       float sim = LSA.GetSimilarity(i, j);
                       if (sim > simCut)
                       {
                           Feature f2 = fc.featureList[j];
                           sw.WriteLine(i.ToString() + ";" + f1.id + ";" + j.ToString() + ";" + f2.id + ";" + sim.ToString());
                       }
                   }
               }
           }
           sw.Close();
           //DateTime end2 = DateTime.Now;
           //duration = end2 - end;
           //time = duration.Minutes * 60.0 + duration.Seconds + duration.Milliseconds / 1000.0;
           //MessageBox.Show(String.Format("Total processing time {0:########.00} seconds", time));

           //WORD VECTOR SIMILARITY FOR LSA
           //outputFile = dirPath + "\\LSAterms" + getFileEnding();
           //outputFile += ".csv";
           //System.IO.File.Delete(outputFile);
           //System.IO.StreamWriter sw = new System.IO.StreamWriter(outputFile, true);
           //sw.WriteLine("termID_i;term_i;termID_j;term_j;Cosine Similarity");
           //for (int i = 0; i < tf._terms.Count; i++)
           //{
           //    string term_i = tf._terms[i].ToString();

           //    for (int j = i + 1; j < tf._terms.Count; j++)
           //    {
           //        float sim = LSA.GetWordSimilarity(i, j);
           //        if (sim > 0.5)
           //        {
           //            string term_j = tf._terms[j].ToString();
           //            sw.WriteLine(i.ToString() + ";" + term_i + ";" + j.ToString() + ";" + term_j + ";" + sim.ToString());
           //        }
           //    }
           //}
           //sw.Close();
           //System.Diagnostics.Process.Start(outputFile);

           outputFile = dirPath + "\\duplicatesLSA" + getFileEnding();
           outputFile += ".csv";
           System.IO.File.Delete(outputFile);
           System.IO.StreamWriter sw2 = new System.IO.StreamWriter(outputFile);
           sw2.WriteLine("feature_id; dup_id; rank; sim");

           //voor 1 duplicate top 10/20 vinden
           int cTotal = 0;
           int cTen = 0;
           int cTwenty = 0;
           foreach (Feature f in fc.featureList)
           {
               if (f.duplicate_id != "" && f.duplicate_id != null)
               {

                   //simlist wordt lijst met alle similarities
                   List<KeyValuePair<string, float>> simlist = new List<KeyValuePair<string, float>>();
                   if (f.id == "319295" && f.duplicate_id == "341829")
                   {
                   }
                   else
                   {
                       int v1 = getFeatureIndex(f.id);
                       int d = getFeatureIndex(f.duplicate_id);
                       if (v1 != -1 && d != -1)
                       {
                           cTotal++;
                           foreach (Feature f2 in fc.featureList)
                           {
                               if (f.id != f2.id)
                               {
                                   int v2 = getFeatureIndex(f2.id);
                                   if (v2 != -1)
                                   {
                                       float sim = LSA.GetSimilarity(v1, v2);
                                       KeyValuePair<string, float> kvp = new KeyValuePair<string, float>(f2.id, sim);
                                       simlist.Add(kvp);
                                   }
                               }
                           }

                           //sorteer op similarity
                           simlist = simlist.OrderByDescending(x => x.Value).ToList();
                           //vind ranking
                           f.dupRank = 0;
                           while (simlist.ElementAt(f.dupRank).Key != f.duplicate_id)
                           {
                               f.dupRank++;
                           }
                           f.dupSim = simlist.ElementAt(f.dupRank).Value;
                           f.dupRank += 1;
                           if (f.dupRank <= 10)
                           {
                               cTen++;
                           }

                           if (f.dupRank <= 20)
                           {
                               cTwenty++;
                           }

                           sw2.WriteLine(f.id + ";" + f.duplicate_id + ";" + f.dupRank + ";" + f.dupSim.ToString("F4"));
                           Debug.WriteLine(DateTime.Now.ToShortTimeString() + " " + f.id + ";" + f.duplicate_id + ";" + f.dupRank + ";" + f.dupSim.ToString("F4"));
                       }
                   }
               }
           }
           sw2.Close();
           //System.Diagnostics.Process.Start(outputFile);

           //calculate percentage in top 10 or top 20
           if (cTotal > 0)
           {
               double pTen = (double)cTen / cTotal;
               double pTwenty = (double)cTwenty / cTotal;
               DateTime end = DateTime.Now;
               TimeSpan duration = end - start;
               double time = duration.Days * 24 * 3600.0 + duration.Hours * 3600.0 + duration.Minutes * 60.0 + duration.Seconds + duration.Milliseconds / 1000.0;
               ////MessageBox.Show("top 10: " + pTen.ToString("F2") + "% out of " + cTotal + " \n top 20:" + pTwenty.ToString("F2") + "% out of " + cTotal);
               ////MessageBox.Show(String.Format("Total processing time {0:########.00} seconds", time));
               Utilities.LogMessageToFile(MainForm.logfile, DateTime.Now.ToShortTimeString() + " top 10: " + pTen.ToString("F2") + "% out of " + cTotal + " \n top 20:" + pTwenty.ToString("F2") + "% out of " + cTotal);
               Utilities.LogMessageToFile(MainForm.logfile, DateTime.Now.ToShortTimeString() + String.Format(" Total processing time {0:########.00} seconds", time));

           }
           else
           {
               ////MessageBox.Show("No valid duplicates found");
               Utilities.LogMessageToFile(MainForm.logfile, DateTime.Now.ToShortTimeString() + " No valid duplicates found");
           }
       }
       /*

       //private void btnTest_Click(object sender, EventArgs e)
       //{
       //    DateTime start = DateTime.Now;
       //    InputXML(fileXML);
       //    Utilities.LogMessageToFile(MainForm.logfile, " Feature requests processed: " + fc.featureList.Count.ToString());
       //    StopWordsHandler swh = new StopWordsHandler(true);
       //    TFIDFMeasure tf = new TFIDFMeasure(fc, true, false, true, false, true, true, true, false, false);
       //    Utilities.LogMessageToFile(MainForm.logfile, DateTime.Now.ToShortTimeString() + " TFIDF matrix calculated");
       //    LSA.MainLSALoop(fc, tf); 
       //    ////LSA.MainLSALoopUnweighed(fc, tf); //// ***************** test met ongewogen!!!
       //    int step = 50;
       //    LSA.PrepareDocWordMatrixLoop(0, 1669);
       //    //for (int k = 0; k < fc.featureList.Count; k = k + step)
       //    Utilities.LogMessageToFile(MainForm.logfile, DateTime.Now.ToShortTimeString() + "k;top10;top20");
       //    for (int k = 0; k < 4200; k = k + step)
       //    {
       //        // U * S 
       //        LSA.PrepareDocWordMatrixLoop(k, k + step);
       //        //Utilities.LogMessageToFile(MainForm.logfile, DateTime.Now.ToShortTimeString() + " LSA matrix calculated for k = " + k.ToString());

       //        //voor 1 duplicate top 10/20 vinden
       //        int cTotal = 0;
       //        int cTen = 0;
       //        int cTwenty = 0;
       //        foreach (Feature f in fc.featureList)
       //        {
       //            if (f.duplicate_id != "" && f.duplicate_id != null)
       //            {

       //                //simlist wordt lijst met alle similarities
       //                List<KeyValuePair<string, float>> simlist = new List<KeyValuePair<string, float>>();
       //                if (f.id == "319295" && f.duplicate_id == "341829")
       //                {
       //                }
       //                else
       //                {
       //                    int v1 = getFeatureIndex(f.id);
       //                    int d = getFeatureIndex(f.duplicate_id);
       //                    if (v1 != -1 && d != -1)
       //                    {
       //                        cTotal++;
       //                        foreach (Feature f2 in fc.featureList)
       //                        {
       //                            if (f.id != f2.id)
       //                            {
       //                                int v2 = getFeatureIndex(f2.id);
       //                                if (v2 != -1)
       //                                {
       //                                    float sim = LSA.GetSimilarity(v1, v2);
       //                                    KeyValuePair<string, float> kvp = new KeyValuePair<string, float>(f2.id, sim);
       //                                    simlist.Add(kvp);
       //                                }
       //                            }
       //                        }

       //                        //sorteer op similarity
       //                        simlist = simlist.OrderByDescending(x => x.Value).ToList();
       //                        //vind ranking
       //                        f.dupRank = 0;
       //                        while (simlist.ElementAt(f.dupRank).Key != f.duplicate_id)
       //                        {
       //                            f.dupRank++;
       //                        }
       //                        f.dupSim = simlist.ElementAt(f.dupRank).Value;
       //                        f.dupRank += 1;
       //                        if (f.dupRank <= 10)
       //                        {
       //                            cTen++;
       //                        }

       //                        if (f.dupRank <= 20)
       //                        {
       //                            cTwenty++;
       //                        }

       //                    }
       //                }
       //            }

       //        }

       //        //calculate percentage in top 10 or top 20
       //        if (cTotal > 0)
       //        {
       //            double pTen = (double)cTen / cTotal;
       //            double pTwenty = (double)cTwenty / cTotal;
       //            DateTime end = DateTime.Now;
       //            TimeSpan duration = end - start;
       //            double time = duration.Days * 24 * 3600.0 + duration.Hours * 3600.0 + duration.Minutes * 60.0 + duration.Seconds + duration.Milliseconds / 1000.0;
       //            Utilities.LogMessageToFile(MainForm.logfile, DateTime.Now.ToShortTimeString() + ";" + k.ToString() + ";" + pTen.ToString("F2") + ";" + pTwenty.ToString("F2"));
       //            //Utilities.LogMessageToFile(MainForm.logfile, DateTime.Now.ToShortTimeString() + "k= " + k.ToString() + " top 10: " + pTen.ToString("F2") + "% out of " + cTotal + " \n top 20:" + pTwenty.ToString("F2") + "% out of " + cTotal);
       //            //Utilities.LogMessageToFile(MainForm.logfile, DateTime.Now.ToShortTimeString() + String.Format(" Total processing time {0:########.00} seconds", time));

       //        }
       //        else
       //        {
       //            Utilities.LogMessageToFile(MainForm.logfile, DateTime.Now.ToShortTimeString() + " No valid duplicates found");
       //        }
       //    }
       //}

       private void ofXML_FileOk(object sender, CancelEventArgs e)
       {
           ///// lbFile.Text = ofXML.FileName;
           lbFile_Text = ofXML_FileName;
           ///// fileXML = ofXML.FileName;
           fileXML = ofXML_FileName;
           dirPath = System.IO.Path.GetDirectoryName(fileXML);
           //InputXML(fileXML);
       }

       /* ***** This is for when file is changed, don't worry about yet *****
       private void btnFile_Click(object sender, EventArgs e)
       {
           ///// ofXML.FileName = System.IO.Path.GetFileName(fileXML);
           ofXML_FileName = System.IO.Path.GetFileName(fileXML);
           ofXML.ShowDialog();
       }
       */

        private void InputXML(string fileName)
        {
            dsFeatures = new DataSet();
            //dsFeatures.Clear();
            dsFeatures.ReadXml(fileName);
            ///// dgFeatures.DataSource = bsFeatures;
            /////dgFeatures_DataSource = bsFeatures; 
            ///// dgComments.DataSource = bsComments;
            /////dgComments_DataSource = bsComments;
            processXML();
        }

        /*private void btnTDF_Click(object sender, EventArgs e)
        {
            Utilities.LogMessageToFile(MainForm.logfile, DateTime.Now.ToShortTimeString() + " Start TF-IDF");
            //processXML();
            InputXML(fileXML);
            StopWordsHandler stopword = new StopWordsHandler(cbSY.Checked);
            if (cbMethod.SelectedIndex == 0)
            {
                tf = new TFIDFMeasure(fc, cbSM.Checked, cbSR.Checked, cbDW.Checked, cbBG.Checked, cbSY.Checked, cbLO.Checked, cbSC.Checked, cbMU.Checked, cbDO.Checked);
                Utilities.LogMessageToFile(MainForm.logfile, DateTime.Now.ToShortTimeString() + " End TF-IDF");
                lblTDF.Text = "cosim(0,1) = " + tf.GetSimilarity(17, 259).ToString();
                lblTDF.Text += "; cosim(0,2) = " + tf.GetSimilarity(0, 2).ToString();

                ////word count
                //int wordCount = 0;
                //for (int i = 0; i < tf._termFreq.GetLength(0); i++)
                //{
                //    for (int j = 0; j < tf._termFreq[0].GetLength(0); j++)
                //    {
                //        wordCount += tf._termFreq[i][j];
                //    }
                    
                //}
                //Utilities.LogMessageToFile(logfile, "TOTAL WORD COUNT: " + wordCount);

                //Debug.WriteLine("Start term matrix");
                ////write term count matrix
                //string outputFile = dirPath + "\\termmatrix" + getFileEnding();
                //outputFile += ".csv";
                //System.IO.File.Delete(outputFile);
                //System.IO.StreamWriter sw = new System.IO.StreamWriter(outputFile, true);
                //for (int i = 0; i < tf._termFreq.GetLength(0); i++)
                //{
                //    string fileLine = i.ToString() + ";";
                //    for (int j = 0; j < tf._termFreq[0].GetLength(0); j++)
                //    {
                //        fileLine += tf._termFreq[i][j].ToString() + ";";
                //    }
                //    sw.WriteLine(fileLine);
                //    Debug.WriteLine(fileLine);
                //}
                //sw.Close();
                //Debug.WriteLine("Term matrix written");

                btnSave.Enabled = true;
                btnTerms.Enabled = true;
            }
            else
            {
                string outputFile = dirPath + "\\cossim" + getFileEnding();
                outputFile += "_xref.csv";
                System.IO.File.Delete(outputFile);
                System.IO.StreamWriter sw = new System.IO.StreamWriter(outputFile, true);
                sw.WriteLine("Title_i;ID_i;Doc_j;ID_j;Cosine Similarity");
                for (int i = 0; i < fc.featureList.Count; i++)
                {
                    Feature f1 = fc.featureList[i];
                    //if (f1.id == "224119" || f1.id == "238186" || f1.id == "343755" || f1.id == "344748" ||
                    //   f1.id == "353263" || f1.id == "363984" || f1.id == "364870" || f1.id == "376807" ||
                    //   f1.id == "378528" || f1.id == "394920")
                    //{

                    tf = new TFIDFMeasure(fc, i, cbSM.Checked, cbSR.Checked, cbDW.Checked, cbBG.Checked, cbSY.Checked, cbLO.Checked, cbSC.Checked, cbMU.Checked, cbDO.Checked);
                    for (int j = 0; j < fc.featureList.Count; j++)
                    {
                        if (j != i)
                        {
                            Feature f2 = fc.featureList[j];
                            sw.WriteLine(i.ToString() + ";" + f1.id + ";" + j.ToString() + ";" + f2.id + ";" + tf.GetSimilarity(i, j).ToString());
                        }
                    }
                    lblTDF.Text = i.ToString() + "done!";
                    lblTDF.Refresh();
                    //}
                }
                sw.Close();
            }
        }
        */
        private void calculateTFIDF()
        {
            Utilities.LogMessageToFile(MainForm.logfile, DateTime.Now.ToShortTimeString() + " Start TF-IDF");
            //processXML();
            InputXML(fileXML);
            StopWordsHandler stopword = new StopWordsHandler(SY);
            if (cbMethod_SelectedIndex == 0) /* default */
            {
                tf = new TFIDFMeasure(fc, SM, SR, DW, BG, SY, LO, SC, MU, DO);
                Utilities.LogMessageToFile(MainForm.logfile, DateTime.Now.ToShortTimeString() + " End TF-IDF");
                lblTDF_Text = "cosim(0,1) = " + tf.GetSimilarity(17, 259).ToString();
                lblTDF_Text += "; cosim(0,2) = " + tf.GetSimilarity(0, 2).ToString();
                Console.WriteLine(lblTDF_Text);

                ////word count
                //int wordCount = 0;
                //for (int i = 0; i < tf._termFreq.GetLength(0); i++)
                //{
                //    for (int j = 0; j < tf._termFreq[0].GetLength(0); j++)
                //    {
                //        wordCount += tf._termFreq[i][j];
                //    }

                //}
                //Utilities.LogMessageToFile(logfile, "TOTAL WORD COUNT: " + wordCount);

                //Debug.WriteLine("Start term matrix");
                ////write term count matrix
                //string outputFile = dirPath + "\\termmatrix" + getFileEnding();
                //outputFile += ".csv";
                //System.IO.File.Delete(outputFile);
                //System.IO.StreamWriter sw = new System.IO.StreamWriter(outputFile, true);
                //for (int i = 0; i < tf._termFreq.GetLength(0); i++)
                //{
                //    string fileLine = i.ToString() + ";";
                //    for (int j = 0; j < tf._termFreq[0].GetLength(0); j++)
                //    {
                //        fileLine += tf._termFreq[i][j].ToString() + ";";
                //    }
                //    sw.WriteLine(fileLine);
                //    Debug.WriteLine(fileLine);
                //}
                //sw.Close();
                //Debug.WriteLine("Term matrix written");

                ///// btnSave.Enabled = true;
                ///// btnTerms.Enabled = true;
            }
            else
            {
                string outputFile = dirPath + "\\cossim" + getFileEnding();
                outputFile += "_xref.csv";
                System.IO.File.Delete(outputFile);
                System.IO.StreamWriter sw = new System.IO.StreamWriter(outputFile, true);
                sw.WriteLine("Title_i;ID_i;Doc_j;ID_j;Cosine Similarity");
                for (int i = 0; i < fc.featureList.Count; i++)
                {
                    Feature f1 = fc.featureList[i];
                    //if (f1.id == "224119" || f1.id == "238186" || f1.id == "343755" || f1.id == "344748" ||
                    //   f1.id == "353263" || f1.id == "363984" || f1.id == "364870" || f1.id == "376807" ||
                    //   f1.id == "378528" || f1.id == "394920")
                    //{

                    tf = new TFIDFMeasure(fc, i, SM, SR, DW, BG, SY, LO, SC, MU, DO);
                    for (int j = 0; j < fc.featureList.Count; j++)
                    {
                        if (j != i)
                        {
                            Feature f2 = fc.featureList[j];
                            sw.WriteLine(i.ToString() + ";" + f1.id + ";" + j.ToString() + ";" + f2.id + ";" + tf.GetSimilarity(i, j).ToString());
                        }
                    }
                    lblTDF_Text = i.ToString() + "done!";
                    ///// lblTDF.Refresh();
                    //}
                }
                sw.Close();
            }
        }

        /*
            private void btnTerms_Click(object sender, EventArgs e)
        {
            string outputFile = dirPath + "\\terms" + getFileEnding() + ".txt";
            System.IO.File.Delete(outputFile);
            System.IO.StreamWriter sw1 = new System.IO.StreamWriter(outputFile);
            int cnt = 0;
            foreach (string t in tf._terms)
            {
                sw1.WriteLine(t);
                cnt++;
            }
            sw1.Close();
            lblTerms.Text = "Count: " + cnt.ToString();
            System.Diagnostics.Process.Start(outputFile);
        } */

        /*
    private void btnSave_Click(object sender, EventArgs e)
    {
        if (cbMethod.SelectedIndex == 0)
        {
            string outputFile = dirPath + "\\cossim" + getFileEnding();
            outputFile += ".csv";
            System.IO.File.Delete(outputFile);
            System.IO.StreamWriter sw = new System.IO.StreamWriter(outputFile);
            sw.WriteLine("Doc_i;ID_i;Doc_j;ID_j;Cosine Similarity");
            int i = 0;
            foreach (Feature f1 in fc.featureList)
            {
                Utilities.LogMessageToFile(logfile, "[" + i + "] Feature " + f1.id);
                //if (f1.id == "224119" || f1.id == "238186" || f1.id == "343755" || f1.id == "344748" || 
                //    f1.id == "353263" || f1.id == "363984" || f1.id == "364870" || f1.id == "376807" || 
                //                                              f1.id == "378528" || f1.id == "394920")
                //{
                for (int j = i + 1; j < fc.featureList.Count; j++)
                {
                    float sim = tf.GetSimilarity(i, j);
                    if (sim > simCut) //for Netbeans to reduce file size; see parameter
                    {
                        Feature f2 = fc.featureList[j];
                        sw.WriteLine(i.ToString() + ";" + f1.id + ";" + j.ToString() + ";" + f2.id + ";" + sim.ToString());
                    }
                }
                //}
                i++;
            }
            sw.Close();
            //System.Diagnostics.Process.Start(outputFile);
        }
    } */

        private void processXML()
        {
            //read XML file into Feature Collection with or without 'All comments' and 'Source code'
            if (type == 0)
            {
                fc = new BugzillaFeatureCollection(fileXML, AC, SC, DW);
            }
            else
            {
                fc = new TigrisFeatureCollection(fileXML, AC, SC, DW);
            }
            lblTDF_Text = "";
            lblTerms_Text = "";
            ///// btnSave.Enabled = false;
            ///// btnTerms.Enabled = false;
        }

        /*
        private void dgFeatures_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            int fID = e.RowIndex;
            string selectQuery = "[" + fc.bugTag + "_Id_0] = " + fID.ToString();
            DataRow[] dr = dsFeatures.Tables["long_desc"].Select(selectQuery);
            DataSet ds = new DataSet();
            ds.Merge(dr);
            bsComments.DataSource = ds.Tables["long_desc"];
        }
        */

        private string getFileEnding()
        {
            string strEnd = "";
            /*
            if (cbMethod_SelectedIndex != 0)
            {
                strEnd += "_M" + cbMethod.Text[0];
            }
            foreach (Control cb in this.Controls)
            {
                if (cb.GetType() == typeof(CheckBox))
                {
                    if (((CheckBox)cb).Checked)
                    {
                        strEnd += "_" + cb.Text.Substring(0, 2);
                    }
                }
            } */

            strEnd = "_defaultfornow";
            return strEnd;
        }

        /*
        private void btnVector_Click(object sender, EventArgs e)
        {
            //write two vectors to .xlsx file and open the file
            decimal v1 = nudVector1.Value;
            decimal v2 = nudVector2.Value;
            string outputFile = dirPath + "\\vector_" + v1.ToString() + "_" + v2.ToString() + getFileEnding();
            outputFile += ".csv";
            System.IO.File.Delete(outputFile);
            System.IO.StreamWriter sw = new System.IO.StreamWriter(outputFile);
            sw.WriteLine("i;Term;Vector_" + v1.ToString() + ";Vector_" + v2.ToString());

            for (int i = 0; i < tf._terms.Count; i++)
            {
                sw.WriteLine(i.ToString() + ";" + tf._terms[i] + ";" + tf.GetTermVector((int)v1)[i].ToString() + ";" + tf.GetTermVector((int)v2)[i].ToString());
            }

            sw.Close();
            //System.Diagnostics.Process.Start(outputFile);
        }
        */

        /*
        private void btnCos_Click(object sender, EventArgs e)
    {
        string val1 = nudVectorCos1.Value.ToString();
        string val2 = nudVectorCos2.Value.ToString();
        int v1 = getFeatureIndex(val1);
        int v2 = getFeatureIndex(val2);
        if (v1 == -1 || v2 == -1)
        {
            lblCos.Text = "not in set";
        }
        else
        {
            lblCos.Text = "cosim = " + tf.GetSimilarity(v1, v2).ToString();
        }
    }
    */

        private int getFeatureIndex(string id)
        {
            foreach (Feature ft in fc.featureList)
            {
                if (ft.id == id)
                {
                    return fc.featureList.IndexOf(ft);
                }
            }
            return -1;
        }

        /*
        private void btnDup_Click(object sender, EventArgs e)
        {
            Utilities.LogMessageToFile(logfile, DateTime.Now.ToShortTimeString() + fileXML);
            //DupLoop(false, true, false, false, false, false, false, false);
            //DupLoop(false, false, false, false, false, false, false, false);
            //DupLoop(false, false, false, false, false, true, false, false);
            //DupLoop(false, false, false, false, true, false, false, false);
            //DupLoop(false, false, false, false, false, false, true, false);
            //DupLoop(true, false, false, false, false, false, false, false);
            //DupLoop(false, false, true, false, false, false, false, false);
            //DupLoop(false, false, false, false, false, false, false, true);
            //DupLoop(false, false, false, false, true, false, true, true);
            //DupLoop(false, false, true, false, true, false, true, true);
            //DupLoop(true, false, true, false, true, false, true, true);
            //DupLoop(false, false, true, false, true, true, true, true);
            DupLoop(true, false, true, false, true, true, true, true);
            //DupLoop(true, false, true, false, true, true, true, false);
        }

        private void DupLoop(bool bSM, bool bSR, bool bDW, bool bBG, bool bSY, bool bLO, bool bSC, bool bAC)
        {
            cbSM.Checked = bSM;
            cbSR.Checked = bSR;
            cbDW.Checked = bDW;
            cbBG.Checked = bBG;
            cbSY.Checked = bSY;
            cbLO.Checked = bLO;
            cbSC.Checked = bSC;
            cbAC.Checked = bAC;
            cbMU.Checked = false;
            cbDO.Checked = false;

            //processXML();
            InputXML(fileXML);
            StopWordsHandler stopword = new StopWordsHandler(cbSY.Checked);
            tf = new TFIDFMeasure(fc, cbSM.Checked, cbSR.Checked, cbDW.Checked, cbBG.Checked, cbSY.Checked, cbLO.Checked, cbSC.Checked, cbMU.Checked, cbDO.Checked);
            //tf = new TFIDFMeasure(fc, bSM, bSR, bDW, bBG, bSY, bLO, bSC, false, false);
            
            string outputFile = dirPath + "\\duplicates" + getFileEnding();
            outputFile += ".csv";
            Utilities.LogMessageToFile(MainForm.logfile, DateTime.Now.ToShortTimeString() + getFileEnding());  
            System.IO.File.Delete(outputFile);
            System.IO.StreamWriter sw = new System.IO.StreamWriter(outputFile);
            sw.WriteLine("feature_id; dup_id; rank; sim");

            //voor 1 duplicate top 10/20 vinden
            int cTotal = 0;
            int cTen = 0;
            int cTwenty = 0;
            foreach (Feature f in fc.featureList)
            {
                if (f.duplicate_id != "" && f.duplicate_id != null)
                {

                    //simlist wordt lijst met alle similarities
                    List<KeyValuePair<string, float>> simlist = new List<KeyValuePair<string, float>>();
                    if (f.id == "319295" && f.duplicate_id == "341829")
                    {
                    }
                    else
                    {
                        int v1 = getFeatureIndex(f.id);
                        int d = getFeatureIndex(f.duplicate_id);
                        if (v1 != -1 && d != -1)
                        {
                            cTotal++;
                            foreach (Feature f2 in fc.featureList)
                            {
                                if (f.id != f2.id)
                                {
                                    int v2 = getFeatureIndex(f2.id);
                                    if (v2 != -1)
                                    {
                                        float sim = tf.GetSimilarity(v1, v2);
                                        KeyValuePair<string, float> kvp = new KeyValuePair<string, float>(f2.id, sim);
                                        simlist.Add(kvp);
                                    }
                                }
                            }

                            //sorteer op similarity
                            simlist = simlist.OrderByDescending(x => x.Value).ToList();
                            //vind ranking
                            f.dupRank = 0;
                            while (simlist.ElementAt(f.dupRank).Key != f.duplicate_id)
                            {
                                f.dupRank++;
                            }
                            f.dupSim = simlist.ElementAt(f.dupRank).Value;
                            f.dupRank += 1;
                            if (f.dupRank <= 10)
                            {
                                cTen++;
                            }

                            if (f.dupRank <= 20)
                            {
                                cTwenty++;
                            }

                           sw.WriteLine(f.id + ";" + f.duplicate_id + ";" + f.dupRank + ";" + f.dupSim.ToString("F4"));
                           // Debug.WriteLine("[" + cTotal.ToString() + "] " + f.id + ";" + f.duplicate_id + ";" + f.dupRank + ";" + f.dupSim.ToString("F4") + "; T10=" + cTen.ToString() + "; T20=" + cTwenty.ToString());
                        }
                    }
                }

            }
            sw.Close();
            //System.Diagnostics.Process.Start(outputFile);

            //calculate percentage in top 10 or top 20
            if (cTotal > 0)
            {
                double pTen = (double)cTen / cTotal;
                double pTwenty = (double)cTwenty / cTotal;
                Utilities.LogMessageToFile(MainForm.logfile, DateTime.Now.ToShortTimeString() + " top 10: " + pTen.ToString("F2") + "% out of " + cTotal + " \n top 20:" + pTwenty.ToString("F2") + "% out of " + cTotal);
                //MessageBox.Show("top 10: " + pTen.ToString("F2") + "% out of " + cTotal + " \n top 20:" + pTwenty.ToString("F2") + "% out of " + cTotal);
            }
            else
            {
                Utilities.LogMessageToFile(MainForm.logfile, DateTime.Now.ToShortTimeString() + "No valid duplicates found");
                //MessageBox.Show("No valid duplicates found");
            }



            /*string outputFileXref = dirPath + "\\duplicates_xref_" + getFileEnding();
            outputFileXref += ".csv";
            System.IO.File.Delete(outputFileXref);
            System.IO.StreamWriter sw2 = new System.IO.StreamWriter(outputFileXref);
            sw2.WriteLine("feature_id; dup_id; rank; sim");

            //voor 1 duplicate top 10/20 vinden
            int cTotalX = 0;
            int cTenX = 0;
            int cTwentyX = 0;
            foreach (Feature f in fc.featureList)
            {
                if (f.duplicate_id != "" && f.duplicate_id != null)
                {

                    //simlist wordt lijst met alle similarities
                    List<KeyValuePair<string, float>> simlist = new List<KeyValuePair<string, float>>();
                    int i = getFeatureIndex(f.id);
                    int d = getFeatureIndex(f.duplicate_id);
                    if (i != -1 && d != -1)
                    {
                        cTotalX++;
                        tf = new TFIDFMeasure(fc, i, cbSM.Checked, cbSR.Checked, cbDW.Checked, cbBG.Checked, cbSY.Checked, cbLO.Checked, cbSC.Checked);
                        for (int j = 0; j < fc.featureList.Count; j++)
                        {
                            if (j != i)
                            {
                                Feature f2 = fc.featureList[j];
                                float sim = tf.GetSimilarity(i, j);
                                KeyValuePair<string, float> kvp = new KeyValuePair<string, float>(f2.id, sim);
                                int a = 0;
                                foreach (KeyValuePair<string, float> k in simlist)
                                {
                                    if (k.Value < sim)
                                    {
                                        a = simlist.IndexOf(k);
                                        break;
                                    }
                                    a = simlist.Count() - 1;
                                }
                                simlist.Insert(a, kvp);
                            }
                        }
                       
                        //sorteer op similarity
                        //simlist = simlist.OrderByDescending(x => x.Value).ToList();
                        //vind ranking
                        f.dupRank = 0;
                        while (simlist.ElementAt(f.dupRank).Key != f.duplicate_id)
                        {
                            f.dupRank++;
                        }
                        f.dupSim = simlist.ElementAt(f.dupRank).Value;
                        f.dupRank += 1;
                        if (f.dupRank <= 10)
                        {
                            cTenX++;
                        }

                        if (f.dupRank <= 20)
                        {
                            cTwentyX++;
                        }

                        sw2.WriteLine(f.id + ";" + f.duplicate_id + ";" + f.dupRank + ";" + f.dupSim.ToString("F4"));
                    }
                }

            }
            sw2.Close();
            System.Diagnostics.Process.Start(outputFileXref);

            //calculate percentage in top 10 or top 20
            if (cTotalX > 0)
            {
                double pTenX = (double)cTenX / cTotalX;
                double pTwentyX = (double)cTwentyX / cTotalX;
                MessageBox.Show("top 10 xref: " + pTenX.ToString("F2") + "% out of " + cTotalX + " \n top 20 xref:" + pTwentyX.ToString("F2") + "% out of " + cTotalX);
            }
            else
            {
                MessageBox.Show("No valid duplicates found");
            }
            
        } */

        /*
    private void tbK_TextChanged(object sender, EventArgs e)
    {

    }

    /// <summary>
    /// Main test program for Infer.NET LDA models
    /// </summary>
    /// <param name="args"></param>
    private void LDAMain(TFIDFMeasure tf, int k)
    {


        Rand.Restart(5);
        Dictionary<int, string> vocabulary = new Dictionary<int,string>();

        //TODO Dit vervangen door onze eigen woorden!
        int nrDocs = tf._termFreq[0].Length;
        Dictionary<int, int>[] allWords = new Dictionary<int, int>[nrDocs]; 
        //for all documents
        for (int i = 0; i < nrDocs; i++)
        {
            allWords[i] = new Dictionary<int,int>();
            //for all terms
            int len2 = tf._termFreq.GetLength(0);
            for (int j = 0; j < len2 ; j++)
            {
                allWords[i].Add(j, tf._termFreq[j][i]);
                if (i==0)
                {
                    vocabulary.Add(j, tf._terms[j].ToString());
                }
            }
        }

        int sizeVocab = Utilities.GetVocabularySize(allWords);
        int numTopics = k;
        int numTrainDocs = allWords.Length;
        Utilities.LogMessageToFile(logfile, "************************************");
        Utilities.LogMessageToFile(logfile, "Vocabulary size = " + sizeVocab);
        Utilities.LogMessageToFile(logfile, "Number of documents = " + numTrainDocs);
        Utilities.LogMessageToFile(logfile, "Number of topics = " + numTopics);
        Utilities.LogMessageToFile(logfile, "************************************");
        double alpha = 150 / numTopics;
        double beta = 0.1;
        /*
                    int numTopics = 5;
                    int sizeVocab = 1000;
                    int numTrainDocs = 500;
                    int averageDocumentLength = 100;
                    int averageWordsPerTopic = 10;
                    int numTestDocs = numTopics * 5;
                    int numDocs = numTrainDocs + numTestDocs;

                    // Create the true model
                    Dirichlet[] trueTheta, truePhi;
                    Utilities.CreateTrueThetaAndPhi(
                        sizeVocab, numTopics, numDocs, averageDocumentLength, averageWordsPerTopic,
                        out trueTheta, out truePhi);

                    // Split the documents between a train and test set
                    Dirichlet[] trueThetaTrain = new Dirichlet[numTrainDocs];
                    Dirichlet[] trueThetaTest = new Dirichlet[numTestDocs];
                    int docx = 0;
                    for (int i = 0; i < numTrainDocs; i++) trueThetaTrain[i] = trueTheta[docx++];
                    for (int i = 0; i < numTestDocs; i++) trueThetaTest[i] = trueTheta[docx++];

                    // Generate training and test data for the training documents
                    Dictionary<int, int>[] trainWordsInTrainDoc = Utilities.GenerateLDAData(trueThetaTrain, truePhi, (int)(0.9 * averageDocumentLength));
                    Dictionary<int, int>[] testWordsInTrainDoc = Utilities.GenerateLDAData(trueThetaTrain, truePhi, (int)(0.1 * averageDocumentLength));
                    Dictionary<int, int>[] wordsInTestDoc = Utilities.GenerateLDAData(trueThetaTest, truePhi, averageDocumentLength);
                    Debug.WriteLine("************************************");
                    Debug.WriteLine("Vocabulary size = " + sizeVocab);
                    Debug.WriteLine("Number of topics = " + numTopics);
                    Debug.WriteLine("True average words per topic = " + averageWordsPerTopic);
                    Debug.WriteLine("Number of training documents = " + numTrainDocs);
                    Debug.WriteLine("Number of test documents = " + numTestDocs);
                    Debug.WriteLine("************************************");
                    double alpha = 1.0;
                    double beta = 0.1;
        */ /*

        //for (int i = 0; i < 2; i++)
        //{
        //    bool shared = i == 0;
            // if (!shared) continue; // Comment out this line to see full LDA models
            LDA.RunTest(
                sizeVocab,
                numTopics,
                allWords,
                alpha,
                beta,
                true,
                vocabulary);
        //}

    } */

        /*
    private void btnLDA_Click(object sender, EventArgs e)
    {
        for (int k = 100; k <= 200; k = k + 10)
        {
            Utilities.LogMessageToFile(logfile, "LDA run started");
            Utilities.LogMessageToFile(logfile, "Input file: " + fileXML);
            Utilities.LogMessageToFile(logfile, "Options: " + getFileEnding());
            Utilities.LogMessageToFile(logfile, "Nr Topics = " + k.ToString());
            InputXML(fileXML);
            Utilities.LogMessageToFile(logfile, "Feature requests processed: " + fc.featureList.Count.ToString());
            StopWordsHandler swh = new StopWordsHandler(cbSY.Checked);
            TFIDFMeasure tf = new TFIDFMeasure(fc, cbSM.Checked, cbSR.Checked, cbDW.Checked, cbBG.Checked, cbSY.Checked, cbLO.Checked, cbSC.Checked, cbMU.Checked, cbDO.Checked);
            Utilities.LogMessageToFile(logfile, "TFIDF matrix calculated");

            LDAMain(tf, k);

            //string outputFile = dirPath + "\\duplicatesLDA" + getFileEnding();
            //outputFile += ".csv";
            //System.IO.File.Delete(outputFile);
            //System.IO.StreamWriter sw2 = new System.IO.StreamWriter(outputFile);
            //sw2.WriteLine("feature_id; dup_id; rank; sim");

            //voor 1 duplicate top 10/20 vinden
            int cTotal = 0;
            int cTen = 0;
            int cTwenty = 0;
            foreach (Feature f in fc.featureList)
            {
                if (f.duplicate_id != "" && f.duplicate_id != null)
                {

                    //simlist wordt lijst met alle similarities
                    List<KeyValuePair<string, float>> simlist = new List<KeyValuePair<string, float>>();
                    if (f.id == "319295" && f.duplicate_id == "341829")
                    {
                    }
                    else
                    {
                        int v1 = getFeatureIndex(f.id);
                        int d = getFeatureIndex(f.duplicate_id);
                        if (v1 != -1 && d != -1)
                        {
                            cTotal++;
                            foreach (Feature f2 in fc.featureList)
                            {
                                if (f.id != f2.id)
                                {
                                    int v2 = getFeatureIndex(f2.id);
                                    if (v2 != -1)
                                    {
                                        float sim = LDA.GetSimilarity(v1, v2);
                                        KeyValuePair<string, float> kvp = new KeyValuePair<string, float>(f2.id, sim);
                                        simlist.Add(kvp);
                                    }
                                }
                            }

                            //sorteer op similarity
                            simlist = simlist.OrderByDescending(x => x.Value).ToList();
                            //vind ranking
                            f.dupRank = 0;
                            while (simlist.ElementAt(f.dupRank).Key != f.duplicate_id)
                            {
                                f.dupRank++;
                            }
                            f.dupSim = simlist.ElementAt(f.dupRank).Value;
                            f.dupRank += 1;
                            if (f.dupRank <= 10)
                            {
                                cTen++;
                            }

                            if (f.dupRank <= 20)
                            {
                                cTwenty++;
                            }

                            //sw2.WriteLine(f.id + ";" + f.duplicate_id + ";" + f.dupRank + ";" + f.dupSim.ToString("F4"));
                            Utilities.LogMessageToFile(logfile, f.id + ";" + f.duplicate_id + ";" + f.dupRank + ";" + f.dupSim.ToString("F4"));
                        }
                    }
                }

            }
            //sw2.Close();
            //System.Diagnostics.Process.Start(outputFile);

            //calculate percentage in top 10 or top 20
            if (cTotal > 0)
            {
                double pTen = (double)cTen / cTotal;
                double pTwenty = (double)cTwenty / cTotal;
                //MessageBox.Show("top 10: " + pTen.ToString("F2") + "% out of " + cTotal + " \n top 20:" + pTwenty.ToString("F2") + "% out of " + cTotal);
                Utilities.LogMessageToFile(logfile, "[k=" + k.ToString() + "] top 10: " + pTen.ToString("F2") + "% out of " + cTotal + " \n top 20:" + pTwenty.ToString("F2") + "% out of " + cTotal);
            }
            else
            {
                Utilities.LogMessageToFile(logfile, "No valid duplicates found");
            }
        }
    }
    */

        /*private void cbType_SelectedIndexChanged(object sender, EventArgs e)
        {
            type = cbType.SelectedIndex;
            //InputXML(fileXML);
        }
        */
        private void chooseFile()
        {
            Console.WriteLine("Choose file:\n(1) Mylyn / Netbeans \n(2) ArgoUML");
            //string usrInput = Console.ReadLine();
            //type = Int32.Parse(usrInput);
            type = 1;
        }
    }

    /*
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            //Application.EnableVisualStyles();
            //Application.SetCompatibleTextRenderingDefault(false);
            //Application.Run(new MainForm());
            new MainForm();
        }
    } */

}