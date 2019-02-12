/*
Copyright 2011, Andrew Polar

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

using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.IO;
using System.Reflection;
using System.Diagnostics;

namespace FeatureTool
{
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
}
