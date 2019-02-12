using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Runtime.InteropServices;
using System.Reflection;

static class MatrixHelper
{
    public static void MatrixProduct(float[,] result, float[,] Left, float[,] Right, int nRows, int nCommon, int nCols)
    {
        for (int i = 0; i < nRows; ++i)
        {
            for (int j = 0; j < nCols; ++j)
            {
                result[i, j] = (float)(0.0);
                for (int k = 0; k < nCommon; ++k)
                {
                    result[i, j] += Left[i, k] * Right[k, j];
                }
            }
        }
    }

    public static void PrintMatrix(string name, float[,] Matrix, int nRows, int nCols)
    {
        Console.WriteLine(name);
        for (int i = 0; i < nRows; ++i)
        {
            for (int j = 0; j < nCols; ++j)
            {
                Console.Write(" {0:###0.00} ", Matrix[i, j]);
            }
            Console.WriteLine();
        }
        Console.WriteLine();
    }
}

namespace LSAtest
{
    static class Test
    {
        public static void testMain()
        {
            //These are file names for data
            string path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string dataFileName = Path.Combine(path, "matrix");
            string resultBlockName = Path.Combine(path, "result");
            const double kappa = 1e-6;

            /*           const int nRows = 4;
                       const int nCols = 6;
                       float[,] matrix = new float[nRows, nCols]{
                           {0.0f, 1.1f, -2.5f, 2.8f, 0.0f, 0.0f},
                           {1.2f, 0.0f, 2.3f, 0.0f, 1.3f, 1.2f},
                           {3.3f, 1.8f, 0.0f, 0.0f, 8.0f, -2.0f},
                           {0.0f, 1.4f, 2.8f, -1.6f, 0.0f, -0.5f}
                       };*/

            /*          const int nRows = 7;
                      const int nCols = 5;
                      float[,] matrix = new float[nRows, nCols] {
                          {0.0f, 1.1f, 2.5f, 2.8f, 0.0f},
                          {1.2f, 0.0f, 2.3f, 0.0f, 1.6f},
                          {3.3f, -1.7f, 0.0f, 0.0f, 0.0f},
                          {0.0f, 1.4f, 2.8f, -1.6f, 0.0f},
                          {1.0f, 0.0f, 0.0f, -2.6f, 7.8f},
                          {0.0f, 0.0f, 1.0f, -3.6f, 0.8f},
                          {1.0f, 1.0f, 0.0f, 0.0f, 0.8f},
                      };*/

            /*         const int nRows = 7;
                     const int nCols = 6;
                     float[,] matrix = new float[nRows, nCols] {
                         {4.0f, 2.0f, 0.0f, 0.0f, 0.0f, 0.0f},
                         {3.0f, 8.0f, 0.0f, 0.0f, 0.0f, 0.0f},
                         {1.0f, 1.0f, 0.0f, 0.0f, 0.0f, 0.0f},
                         {1.0f, 1.0f, 2.0f, 1.0f, 0.0f, 0.0f},
                         {0.0f, 0.0f, 1.0f, 2.0f, 3.0f, 2.0f},
                         {0.0f, 0.0f, 0.0f, 1.0f, 12.0f, 5.0f},
                         {0.0f, 0.0f, 0.0f, 0.0f, 11.0f, 1.0f},
                     };*/

            const int nRows = 10;
            const int nCols = 9;
            float[,] matrix = new float[nRows, nCols] {
	            {4.0f, 2.0f, 2.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f},
	            {3.0f, 8.0f, 8.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f},
	            {1.0f, 1.0f, 1.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f},
	            {1.0f, 1.0f, 1.0f, 2.0f, 1.0f, 0.0f, 0.0f, 0.0f, 0.0f},
	            {0.0f, 0.0f, 0.0f, 1.0f, 2.0f, 3.0f, 0.0f, 0.0f, 2.0f},
	            {0.0f, 0.0f, 0.0f, 0.0f, 1.0f, 8.0f, 0.0f, 0.0f, 5.0f},
	            {0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 9.0f, 0.0f, 0.0f, 1.0f},
                {1.0f, 2.0f, 2.0f, 0.9f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f},
                {0.0f, 0.0f, 0.0f, 0.0f, 8.0f, -7.0f, 0.0f, 0.0f, 0.0f},
                {0.1f, 0.1f, 0.1f, 0.1f, 0.0f, 0.1f, 0.0f, 0.0f, -1.0f},
	        };

            MatrixHelper.PrintMatrix("Original Matrix", matrix, nRows, nCols);

            int nNonZeros = 0;
            int[] nNonZerosInCols = new int[nCols];
            for (int i = 0; i < nCols; i++)
            {
                nNonZerosInCols[i] = 0;
            }
            for (int j = 0; j < nCols; ++j)
            {
                for (int i = 0; i < nRows; ++i)
                {
                    if (Math.Abs(matrix[i, j]) > kappa)
                    {
                        ++nNonZerosInCols[j];
                        ++nNonZeros;
                    }
                }
            }

            using (FileStream stream = new FileStream(dataFileName, FileMode.Create))
            {
                using (BinaryWriter writer = new BinaryWriter(stream))
                {
                    writer.Write(System.Net.IPAddress.HostToNetworkOrder(nRows));
                    writer.Write(System.Net.IPAddress.HostToNetworkOrder(nCols));
                    writer.Write(System.Net.IPAddress.HostToNetworkOrder(nNonZeros));
                    for (int k = 0; k < nCols; ++k)
                    {
                        writer.Write(System.Net.IPAddress.HostToNetworkOrder(nNonZerosInCols[k]));
                        for (int j = 0; j < nRows; ++j)
                        {
                            if (Math.Abs(matrix[j, k]) > kappa)
                            {
                                writer.Write(System.Net.IPAddress.HostToNetworkOrder(j));
                                byte[] b = BitConverter.GetBytes(matrix[j, k]);
                                int x = BitConverter.ToInt32(b, 0);
                                writer.Write(System.Net.IPAddress.HostToNetworkOrder(x));
                            }
                        }
                    }
                    writer.Flush();
                    writer.Close();
                }
                stream.Close();
            }

            //////////////////////////////////////////////////////////////////////////////////////////////////////////
            //At this point the data matrix is prepared and saved to file. Next is computatinal part.
            //////////////////////////////////////////////////////////////////////////////////////////////////////////

            //This is C# version of converted C code.
            SVD.ProcessData(dataFileName, resultBlockName, false);
            ///////////////////////////////////////////////

            ////////////////////////////////////////////////////////////////////////////////////////
            //At this point computational part is completed. Next is verification of result.
            ////////////////////////////////////////////////////////////////////////////////////////

            //Read and make matrix with singular values. It is diagonal.
            float[,] S = new float[nCols, nCols];
            for (int i = 0; i < nCols; ++i)
            {
                for (int j = 0; j < nCols; ++j)
                {
                    S[i, j] = (float)(0.0);
                }
            }
            int rank = nCols;

            string singularValues = resultBlockName + "-S";
            string line = string.Empty;
            System.IO.StreamReader file = new System.IO.StreamReader(singularValues);
            line = file.ReadLine();
            if (line == null)
            {
                Console.WriteLine("Misformatted file: {0}", singularValues);
                Environment.Exit(1);
            }
            try
            {
                rank = Convert.ToInt32(line);
            }
            catch (Exception)
            {
                Console.WriteLine("Misformatted file: {0}", singularValues);
                Environment.Exit(1);
            }

            try
            {
                int cnt = 0;
                while ((line = file.ReadLine()) != null)
                {
                    S[cnt, cnt] = (float)(Convert.ToDouble(line));
                    ++cnt;
                }
            }
            catch (Exception)
            {
                Console.WriteLine("Misformatted file: {0}", singularValues);
                Environment.Exit(1);
            }
            file.Close();
            MatrixHelper.PrintMatrix("Singular values", S, rank, rank);
            //finished with singular values matrix

            //Read and make UT matrix, it is transposed U in U*S*VT
            float[,] UT = new float[nCols, nRows];
            for (int i = 0; i < nCols; ++i)
            {
                for (int j = 0; j < nRows; ++j)
                {
                    UT[i, j] = (float)(0.0);
                }
            }
            string fUTfileName = resultBlockName + "-Ut";
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
                            UT[i, j] = BitConverter.ToSingle(b, 0);
                        }
                    }
                    reader.Close();
                }
                stream.Close();
            }
            //end reading

            //check validity of UT matrix
            float[,] I = new float[nCols, nCols];
            for (int i = 0; i < rank; ++i)
            {
                for (int j = 0; j < rank; ++j)
                {
                    I[i, j] = (float)(0.0);
                    for (int k = 0; k < nRows; ++k)
                    {
                        I[i, j] += UT[i, k] * UT[j, k];
                    }
                }
            }
            MatrixHelper.PrintMatrix("\nUT*U", I, rank, rank);
            //finished	

            //Read and make VT matrix
            float[,] VT = new float[nCols, nCols];
            for (int i = 0; i < nCols; ++i)
            {
                for (int j = 0; j < nCols; ++j)
                {
                    VT[i, j] = (float)(0.0);
                }
            }
            string fVTfileName = resultBlockName + "-Vt";
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
                            VT[i, j] = BitConverter.ToSingle(b, 0);
                        }
                    }
                    reader.Close();
                }
                stream.Close();
            }
            //end reading

            //check validity of VT matrix
            for (int i = 0; i < rank; ++i)
            {
                for (int j = 0; j < rank; ++j)
                {
                    I[i, j] = (float)(0.0);
                    for (int k = 0; k < nCols; ++k)
                    {
                        I[i, j] += VT[i, k] * VT[j, k];
                    }
                }
            }
            MatrixHelper.PrintMatrix("\nVT*V", I, rank, rank);
            //finished	

            //We multiply all 3 matrices for the test
            float[,] U = new float[nRows, nCols];
            for (int i = 0; i < nRows; ++i)
            {
                for (int j = 0; j < nCols; ++j)
                {
                    U[i, j] = UT[j, i];
                }
            }

            float[,] testData = new float[nRows, nCols];
            MatrixHelper.MatrixProduct(testData, U, S, nRows, nCols, nCols);
            float[,] testData2 = new float[nRows, nCols];
            MatrixHelper.MatrixProduct(testData2, testData, VT, nRows, nCols, nCols);
            MatrixHelper.PrintMatrix("\nRestored original", testData2, nRows, nCols);
            //finished;
        }
    }
}
