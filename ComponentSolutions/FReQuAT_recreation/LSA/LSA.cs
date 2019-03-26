using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Text;
using System.Diagnostics;

/* From TFIDFMeasure */
using System.Collections;

/* From Utilities */
using System.IO;

/* From LSA */
using System.Reflection;

// Located in c:\Program Files (x86)\COEST\TraceLab\lib\TraceLabSDK.dll
using TraceLabSDK;

namespace LSA
{
    [Component(Name = "LSA",
                Description = "Component to calculate the LSA of a feature request file",
                Author = "Jen Lee at University of Kentucky: Based on contributions from Dr. Petra Heck and Dr. Andy Zaidman",
                Version = "1.0",
                ConfigurationType = typeof(LSA_configuration))]

    // Get inputs from "Input XML" component
    [IOSpec(IOType = IOSpecType.Input, Name = "inputFile", DataType = typeof(string))]
    [IOSpec(IOType = IOSpecType.Input, Name = "outputDirectory", DataType = typeof(string))]
    [IOSpec(IOType = IOSpecType.Input, Name = "AC", DataType = typeof(bool))]
    [IOSpec(IOType = IOSpecType.Input, Name = "SC", DataType = typeof(bool))]
    [IOSpec(IOType = IOSpecType.Input, Name = "SM", DataType = typeof(bool))]
    [IOSpec(IOType = IOSpecType.Input, Name = "SR", DataType = typeof(bool))]
    [IOSpec(IOType = IOSpecType.Input, Name = "DW", DataType = typeof(bool))]
    [IOSpec(IOType = IOSpecType.Input, Name = "BG", DataType = typeof(bool))]
    [IOSpec(IOType = IOSpecType.Input, Name = "SY", DataType = typeof(bool))]
    [IOSpec(IOType = IOSpecType.Input, Name = "LO", DataType = typeof(bool))]
    [IOSpec(IOType = IOSpecType.Input, Name = "MU", DataType = typeof(bool))]
    [IOSpec(IOType = IOSpecType.Input, Name = "DO", DataType = typeof(bool))]
    [IOSpec(IOType = IOSpecType.Input, Name = "fc_type", DataType = typeof(int))]

    public class LSA : BaseComponent
    {
        // global variables used in LSA
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
        double simCut = 0.5;

        // globals from TraceLab configurations
        string inputFile;
        string outputDirectory;
        bool AC; bool SC; bool SM; bool SR; bool DW; bool BG; bool SY; bool LO; bool MU; bool DO;
        int fc_type;

        // access to classes
        FeatureCollection fc;
        setFiles sF;

        // connect to configuration file
        public LSA(ComponentLogger log) : base(log)
        {
            this.Configuration = new LSA_configuration();
        }
        public new LSA_configuration Configuration
        {
            get => base.Configuration as LSA_configuration;
            set => base.Configuration = value;
        }

        public override void Compute()
        {
            // load data from Worspace
            inputFile = (string)Workspace.Load("inputFile");
            outputDirectory = (string)Workspace.Load("outputDirectory");
            AC = (bool)Workspace.Load("AC");
            SC = (bool)Workspace.Load("SC");
            SM = (bool)Workspace.Load("SM");
            SR = (bool)Workspace.Load("SR");
            DW = (bool)Workspace.Load("DW");
            BG = (bool)Workspace.Load("BG");
            SY = (bool)Workspace.Load("SY");
            LO = (bool)Workspace.Load("LO");
            MU = (bool)Workspace.Load("MU");
            DO = (bool)Workspace.Load("DO");
            fc_type = (int)Workspace.Load("fc_type");

            // Tokenize the XML File
            // *** Note: Ideallythis would happen in a seperate Tokenizing Component
            // *** Hurdle: FeatureCollection is not serilalizable to be stored in Workspace
            sF = new setFiles(inputFile, outputDirectory, AC, SC, DW);
            fc = sF.InputXML(inputFile, fc_type);

            // get k value from configuration file
            string k = (string)this.Configuration.k;

            calculateLSA(k);
        }

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
                Utilities.LogMessageToFile(setFiles.logfile + "_LSA.txt", "File: " + file + ", words: " + dictionary.GetNumberOfWords() + ", size: " + dictionary.GetDictionarySize());
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

        // *** Are files processed differently for LSA????
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
                Utilities.LogMessageToFile(setFiles.logfile + "_LSA.txt", "Matrix data file not found");
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
            Utilities.LogMessageToFile(setFiles.logfile + "_LSA.txt", "");

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
            Utilities.LogMessageToFile(setFiles.logfile + "_LSA.txt", String.Format("\nTotal correct categories in LSA {0} out of {1}, ratio {2}\n", m_totalCorrectCategories,
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
            Utilities.LogMessageToFile(setFiles.logfile + "_LSA.txt", String.Format("\nTotal correct categories in cosine comparison {0} out of {1}, ratio {2:#0.#####}\n", m_totalCorrectCategories,
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
            Utilities.LogMessageToFile(setFiles.logfile + "_LSA.txt", DateTime.Now.ToString() + " SVD calculated and files read into U, V and S");
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
            Utilities.LogMessageToFile(setFiles.logfile + "_LSA.txt", DateTime.Now.ToString() + " SVD calculated and files read into U, V and S");
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

        private void calculateLSA(string k)
        {
            DateTime start = DateTime.Now;
            Utilities.LogMessageToFile(setFiles.logfile + "_LSA.txt", DateTime.Now.ToShortTimeString() + " Start LSA");

            Utilities.LogMessageToFile(setFiles.logfile + "_LSA.txt", " Feature requests processed: " + fc.featureList.Count.ToString());
            StopWordsHandler swh = new StopWordsHandler(SY);
            TFIDFMeasure tf = new TFIDFMeasure(fc, SM, SR, DW, BG, SY, LO, SC, MU, DO);
            Utilities.LogMessageToFile(setFiles.logfile + "_LSA.txt", DateTime.Now.ToShortTimeString() + " TFIDF matrix calculated");

            // k is grabbed from the configuration file
            if (k != "")
            {
                LSA.MainLSA(fc, tf, Int32.Parse(k));
            }
            else
            {
                k = "0";
                LSA.MainLSA(fc, tf, 0);
            }

            Utilities.LogMessageToFile(setFiles.logfile + "_LSA.txt", DateTime.Now.ToShortTimeString() + " LSA matrix calculated for k = " + k);

            //MessageBox.Show(LSA.GetSimilarity(0, 1).ToString());

            string outputFile;
            outputFile = outputDirectory + "\\LSA" + sF.getFileEnding(AC, SC, SM, SR, DW, BG, SY, LO, MU, DO);
            outputFile += ".csv";
            System.IO.File.Delete(outputFile);
            System.IO.StreamWriter sw = new System.IO.StreamWriter(outputFile, true);
            sw.WriteLine("Title_i, ID_i, Doc_j, ID_j, Cosine Similarity");
            for (int i = 0; i < fc.featureList.Count; i++)
            {
                Feature f1 = fc.featureList[i];
                for (int j = i + 1; j < fc.featureList.Count; j++) // updated from j = 0 to imrove speed
                {
                    if (j != i) // *** don't think this if is necessary, keepng for now
                    {
                        float sim = LSA.GetSimilarity(i, j);
                        if (sim > simCut) // if cosSim > 0.5
                        {
                            Feature f2 = fc.featureList[j];
                            sw.WriteLine(i.ToString() + "," + f1.id + "," + j.ToString() + "," + f2.id + "," + sim.ToString());
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

            outputFile = outputDirectory + "\\duplicatesLSA" + sF.getFileEnding(AC, SC, SM, SR, DW, BG, SY, LO, MU, DO);
            outputFile += ".csv";
            System.IO.File.Delete(outputFile);
            System.IO.StreamWriter sw2 = new System.IO.StreamWriter(outputFile);
            sw2.WriteLine("feature_id, dup_id, rank, sim");

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
                        int v1 = sF.getFeatureIndex(f.id);
                        int d = sF.getFeatureIndex(f.duplicate_id);
                        if (v1 != -1 && d != -1)
                        {
                            cTotal++;
                            foreach (Feature f2 in fc.featureList)
                            {
                                if (f.id != f2.id)
                                {
                                    int v2 = sF.getFeatureIndex(f2.id);
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

                            sw2.WriteLine(f.id + "," + f.duplicate_id + "," + f.dupRank + "," + f.dupSim.ToString("F4"));
                            Debug.WriteLine(DateTime.Now.ToShortTimeString() + " " + f.id + "," + f.duplicate_id + "," + f.dupRank + "," + f.dupSim.ToString("F4"));
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
                Utilities.LogMessageToFile(setFiles.logfile + "_LSA.txt", DateTime.Now.ToShortTimeString() + " top 10: " + pTen.ToString("F2") + "% out of " + cTotal + " \n top 20:" + pTwenty.ToString("F2") + "% out of " + cTotal);
                Utilities.LogMessageToFile(setFiles.logfile + "_LSA.txt", DateTime.Now.ToShortTimeString() + String.Format(" Total processing time {0:########.00} seconds", time));

            }
            else
            {
                ////MessageBox.Show("No valid duplicates found");
                Utilities.LogMessageToFile(setFiles.logfile + "_LSA.txt", DateTime.Now.ToShortTimeString() + " No valid duplicates found");
            }
        }

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
                Utilities.LogMessageToFile(setFiles.logfile + "_LSA.txt", e.Message);
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
                Utilities.LogMessageToFile(setFiles.logfile + "_LSA.txt", e.Message);
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
            if (error_index > 0) Utilities.LogMessageToFile(setFiles.logfile + "_LSA.txt", "svdLAS2 parameter error: %s\n");
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
                        Utilities.LogMessageToFile(setFiles.logfile + "_LSA.txt", String.Format("svdLAS2: store (RETRQ) called on index {0} (not allocated) ", (j + MAXLL)));
                    }
                    svd_dcopy(n, LanStore[j + MAXLL], 0, 1, s, 0, 1);
                    break;
                case (int)storeVals.STORP:
                    if (j >= MAXLL)
                    {
                        Utilities.LogMessageToFile(setFiles.logfile + "_LSA.txt", "svdLAS2: store (STORP) called with j >= MAXLL");
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
                        Utilities.LogMessageToFile(setFiles.logfile + "_LSA.txt", "svdLAS2: store (RETRP) called with j >= MAXLL");
                        break;
                    }
                    if (LanStore[j] == null)
                    {
                        Utilities.LogMessageToFile(setFiles.logfile + "_LSA.txt", "svdLAS2: store (RETRP) called on index %d (not allocated) " + j.ToString());
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
                        Utilities.LogMessageToFile(setFiles.logfile + "_LSA.txt", String.Format("Fatal error: {0}", ierr));
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
                        Utilities.LogMessageToFile(setFiles.logfile + "_LSA.txt", String.Format("svdLAS2: imtqlb failed to converge {0}", ierr));
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
                Utilities.LogMessageToFile(setFiles.logfile + "_LSA.txt", "TRANSPOSING THE MATRIX FOR SPEED\n");
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
            Utilities.LogMessageToFile(setFiles.logfile + "_LSA.txt", String.Format("Machine precision {0} {1} {2} {3} {4}", ibeta, it, irnd, machep, negep));

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

            Utilities.LogMessageToFile(setFiles.logfile + "_LSA.txt", String.Format("NUMBER OF LANCZOS STEPS {0}", steps + 1));
            Utilities.LogMessageToFile(setFiles.logfile + "_LSA.txt", String.Format("RITZ VALUES STABILIZED = RANK {0}", neig));

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
                Utilities.LogMessageToFile(setFiles.logfile + "_LSA.txt", "Error reading matrix");
                Environment.Exit(1);
            }
            Utilities.LogMessageToFile(setFiles.logfile + "_LSA.txt", String.Format("NUMBER OF ROWS {0}", A.rows));
            Utilities.LogMessageToFile(setFiles.logfile + "_LSA.txt", String.Format("NUMBER OF COLS {0}", A.cols));

            if (doTranspose)
            {
                A = svdTransposeS(A);
            }

            Utilities.LogMessageToFile(setFiles.logfile + "_LSA.txt", "Computing the SVD...\n");
            SVDRec R = svdLAS2(A);
            if (R == null)
            {
                Utilities.LogMessageToFile(setFiles.logfile + "_LSA.txt", "Error SVD matrix");
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
            Utilities.LogMessageToFile(setFiles.logfile + "_LSA.txt", String.Format("Time for encoding: {0:########.00} seconds", time));
        }

        public static void ProcessData(string datafile, string resultnameblock, bool doTranspose)
        {
            DateTime start = DateTime.Now;

            SMat A = svdLoadSparseMatrix(datafile);
            if (A == null)
            {
                Utilities.LogMessageToFile(setFiles.logfile + "_LSA.txt", "Error reading matrix");
                Environment.Exit(1);
            }
            Utilities.LogMessageToFile(setFiles.logfile + "_LSA.txt", String.Format("NUMBER OF ROWS {0}", A.rows));
            Utilities.LogMessageToFile(setFiles.logfile + "_LSA.txt", String.Format("NUMBER OF COLS {0}", A.cols));

            if (doTranspose)
            {
                A = svdTransposeS(A);
            }

            Utilities.LogMessageToFile(setFiles.logfile + "_LSA.txt", "Computing the SVD...\n");
            SVDRec R = svdLAS2(A);
            if (R == null)
            {
                Utilities.LogMessageToFile(setFiles.logfile + "_LSA.txt", "Error SVD matrix");
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
            Utilities.LogMessageToFile(setFiles.logfile + "_LSA.txt", String.Format("Time for encoding: {0:########.00} seconds", time));
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
}