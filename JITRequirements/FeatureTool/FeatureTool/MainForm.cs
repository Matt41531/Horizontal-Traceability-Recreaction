using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Diagnostics;
using MicrosoftResearch.Infer.Distributions;
using MicrosoftResearch.Infer.Maths;


namespace FeatureTool
{
    public partial class MainForm : Form
    {
        DataSet dsFeatures = new DataSet();
        BindingSource bsComments = new BindingSource();
        BindingSource bsFeatures = new BindingSource();
        FeatureCollection fc;
        TFIDFMeasure tf;
        public static string logfile;

        //TO CHANGE: choice between MyLyn/Netbeans and Tigris ArgoUML is made here
        //string fileXML = "C:\\Users\\889716\\SkyDrive\\Documents\\EQuA\\FeatureRequests\\MyLyn\\20130408Features.xml";
        //string fileXML = "..\\..\\..\\..\\MyLyn\\20130408Features.xml";
        //string fileXML = "..\\..\\..\\..\\Netbeans\\20130514_enh_2.xml";
        string fileXML = "..\\..\\..\\..\\ArgoUML\\20130514_issues.xml";
        //string dirPath = "..\\..\\..\\..\\MyLyn";
        //string dirPath = "..\\..\\..\\..\\Netbeans";
        string dirPath = "..\\..\\..\\..\\ArgoUML";
        int type = 1; //Tigris = 1; Mylyn/Netbeans = 0;
        double simCut = 0.5;
        /* PAY ATTENTION TO TFIDF Cut-off at 0.x SIM for writing cossim file!!!!!*/


        public MainForm()
        {
            InitializeComponent();

            //by default select "AllComments"
            cbMethod.SelectedIndex = 0;
            cbType.SelectedIndex = type;
            lbFile.Text = fileXML;
            DateTime d = DateTime.Now;
            logfile = d.Year.ToString("D4") + d.Month.ToString("D2") + d.Day.ToString("D2") + "_" + d.Hour.ToString("D2") + d.Minute.ToString("D2") + d.Second.ToString("D2") + "_log.txt";

        }

        private void btnFill_Click(object sender, EventArgs e)
        {
            InputXML(fileXML);
            bsFeatures.DataSource = dsFeatures.Tables[fc.bugTag];
            bsComments.DataSource = dsFeatures.Tables["long_desc"];
        }

        private void btnTest_Click(object sender, EventArgs e)
        {
            DateTime start = DateTime.Now;
            Utilities.LogMessageToFile(MainForm.logfile, DateTime.Now.ToShortTimeString() + " Start LSA");
            Utilities.LogMessageToFile(MainForm.logfile, DateTime.Now.ToShortTimeString() + " Configuration: " + getFileEnding());
            InputXML(fileXML);
            Utilities.LogMessageToFile(MainForm.logfile, " Feature requests processed: " + fc.featureList.Count.ToString());
            ////StopWordsHandler swh = new StopWordsHandler(cbSY.Checked);
            StopWordsHandler swh = new StopWordsHandler(true);
            ////TFIDFMeasure tf = new TFIDFMeasure(fc, cbSM.Checked, cbSR.Checked, cbDW.Checked, cbBG.Checked, cbSY.Checked, cbLO.Checked, cbSC.Checked, cbMU.Checked, cbDO.Checked);
            TFIDFMeasure tf = new TFIDFMeasure(fc, true, false, true, false, true, true, true, false, false);
            Utilities.LogMessageToFile(MainForm.logfile, DateTime.Now.ToShortTimeString() + " TFIDF matrix calculated");
            if (tbK.Text != "")
            {
                LSA.MainLSA(fc, tf, Int32.Parse(tbK.Text));
            }
            else
            {
                LSA.MainLSA(fc, tf, 0);
            }
            Utilities.LogMessageToFile(MainForm.logfile, DateTime.Now.ToShortTimeString() + " LSA matrix calculated for k = " + tbK.Text);

            //MessageBox.Show(LSA.GetSimilarity(0, 1).ToString());

            string outputFile;
            outputFile = dirPath + "\\LSA" + getFileEnding();
            outputFile += ".csv";
            System.IO.File.Delete(outputFile);
            System.IO.StreamWriter sw = new System.IO.StreamWriter(outputFile, true);
            sw.WriteLine("Title_i,ID_i,Doc_j,ID_j,Cosine Similarity");
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

            outputFile = dirPath + "\\duplicatesLSA" + getFileEnding();
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
                Utilities.LogMessageToFile(MainForm.logfile, DateTime.Now.ToShortTimeString() + " top 10: " + pTen.ToString("F2") + "% out of " + cTotal + " \n top 20:" + pTwenty.ToString("F2") + "% out of " + cTotal);
                Utilities.LogMessageToFile(MainForm.logfile, DateTime.Now.ToShortTimeString() + String.Format(" Total processing time {0:########.00} seconds", time));

            }
            else
            {
                ////MessageBox.Show("No valid duplicates found");
                Utilities.LogMessageToFile(MainForm.logfile, DateTime.Now.ToShortTimeString() + " No valid duplicates found");
            }
        }

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
            lbFile.Text = ofXML.FileName;
            fileXML = ofXML.FileName;
            dirPath = System.IO.Path.GetDirectoryName(fileXML);
            //InputXML(fileXML);
        }

        private void btnFile_Click(object sender, EventArgs e)
        {
            ofXML.FileName = System.IO.Path.GetFileName(fileXML);
            ofXML.ShowDialog();
        }

        private void InputXML(string fileName)
        {
            dsFeatures = new DataSet();
            //dsFeatures.Clear();
            dsFeatures.ReadXml(fileName);
            dgFeatures.DataSource = bsFeatures;
            dgComments.DataSource = bsComments;
            processXML();
        }

        private void btnTDF_Click(object sender, EventArgs e)
        {
            Utilities.LogMessageToFile(MainForm.logfile, DateTime.Now.ToShortTimeString() + " Start TF-IDF");
            Utilities.LogMessageToFile(MainForm.logfile, DateTime.Now.ToShortTimeString() + " Configuration: " + getFileEnding());
            //processXML();
            InputXML(fileXML);
            StopWordsHandler stopword = new StopWordsHandler(cbSY.Checked);
            if (cbMethod.SelectedIndex == 0)
            {
                tf = new TFIDFMeasure(fc, cbSM.Checked, cbSR.Checked, cbDW.Checked, cbBG.Checked, cbSY.Checked, cbLO.Checked, cbSC.Checked, cbMU.Checked, cbDO.Checked);
                Utilities.LogMessageToFile(MainForm.logfile, DateTime.Now.ToShortTimeString() + " End TF-IDF");
                lblTDF.Text = "cosim(0,1) = " + tf.GetSimilarity(17, 259).ToString();
                lblTDF.Text += "; cosim(0,2) = " + tf.GetSimilarity(0, 2).ToString();
                Utilities.LogMessageToFile(MainForm.logfile, DateTime.Now.ToShortTimeString() + " " + lblTDF.Text);

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
                sw.WriteLine("Title_i,ID_i,Doc_j,ID_j,Cosine Similarity");
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
                            sw.WriteLine(i.ToString() + "," + f1.id + "," + j.ToString() + "," + f2.id + "," + tf.GetSimilarity(i, j).ToString());
                        }
                    }
                    lblTDF.Text = i.ToString() + "done!";
                    lblTDF.Refresh();
                    //}
                }
                sw.Close();
            }


        }

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
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            if (cbMethod.SelectedIndex == 0)
            {
                string outputFile = dirPath + "\\cossim" + getFileEnding();
                outputFile += ".csv";
                System.IO.File.Delete(outputFile);
                System.IO.StreamWriter sw = new System.IO.StreamWriter(outputFile);
                sw.WriteLine("Doc_i,ID_i,Doc_j,ID_j,Cosine Similarity");
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
                            sw.WriteLine(i.ToString() + "," + f1.id + "," + j.ToString() + "," + f2.id + "," + sim.ToString());
                        }
                    }
                    //}
                    i++;
                }
                sw.Close();
                //System.Diagnostics.Process.Start(outputFile);
            }
        }

        private void processXML()
        {
            //read XML file into Feature Collection with or without 'All comments' and 'Source code'
            if (type == 0)
            {
                fc = new BugzillaFeatureCollection(fileXML, cbAC.Checked, cbSC.Checked, cbDW.Checked);
            }
            else
            {
                fc = new TigrisFeatureCollection(fileXML, cbAC.Checked, cbSC.Checked, cbDW.Checked);
            }
            lblTDF.Text = "";
            lblTerms.Text = "";
            btnSave.Enabled = false;
            btnTerms.Enabled = false;
        }

        private void dgFeatures_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            int fID = e.RowIndex;
            string selectQuery = "[" + fc.bugTag + "_Id_0] = " + fID.ToString();
            DataRow[] dr = dsFeatures.Tables["long_desc"].Select(selectQuery);
            DataSet ds = new DataSet();
            ds.Merge(dr);
            bsComments.DataSource = ds.Tables["long_desc"];
        }

        public string getFileEnding()
        {
            string strEnd = "";
            if (cbMethod.SelectedIndex != 0)
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
            }
            return strEnd;
        }

        private void btnVector_Click(object sender, EventArgs e)
        {
            //write two vectors to .xlsx file and open the file
            decimal v1 = nudVector1.Value;
            decimal v2 = nudVector2.Value;
            string outputFile = dirPath + "\\vector_" + v1.ToString() + "_" + v2.ToString() + getFileEnding();
            outputFile += ".csv";
            System.IO.File.Delete(outputFile);
            System.IO.StreamWriter sw = new System.IO.StreamWriter(outputFile);
            sw.WriteLine("i,Term,Vector_" + v1.ToString() + ",Vector_" + v2.ToString());

            for (int i = 0; i < tf._terms.Count; i++)
            {
                sw.WriteLine(i.ToString() + "," + tf._terms[i] + "," + tf.GetTermVector((int)v1)[i].ToString() + "," + tf.GetTermVector((int)v2)[i].ToString());
            }

            sw.Close();
            //System.Diagnostics.Process.Start(outputFile);
        }

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
            sw.WriteLine("feature_id, dup_id, rank, sim");

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

                           sw.WriteLine(f.id + "," + f.duplicate_id + "," + f.dupRank + "," + f.dupSim.ToString("F4"));
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
            */
        }

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
            */

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

        }

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

        private void cbType_SelectedIndexChanged(object sender, EventArgs e)
        {
            type = cbType.SelectedIndex;
            //InputXML(fileXML);
        }
    }
}
