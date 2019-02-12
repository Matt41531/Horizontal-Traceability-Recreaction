//This is converted to c# library SVDLIBC http://tedlab.mit.edu/~dr/SVDLIBC/
//The program operates via hard drive. It reads saved sparse data matrix, performs SVD and saves 
//result to correspondent files. The files format match SVDLIBC. 
//The current code is just recently finished tested on large randomly generated matrices.
//The bugs may be reported to andrewpolar@bellsouth.net. 09/10/2011.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;


namespace FeatureTool
{
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
                    svd_dcopy(i - l, bet, l+1, 1, wrk, l+1, -1);
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
}
