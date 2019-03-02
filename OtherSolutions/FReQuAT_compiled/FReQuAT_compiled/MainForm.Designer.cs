namespace FReQuAT_compiled
{
    partial class MainForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.btnFill = new System.Windows.Forms.Button();
            this.dgFeatures = new System.Windows.Forms.DataGridView();
            this.dgComments = new System.Windows.Forms.DataGridView();
            this.btnTest = new System.Windows.Forms.Button();
            this.ofXML = new System.Windows.Forms.OpenFileDialog();
            this.lbFile = new System.Windows.Forms.Label();
            this.btnFile = new System.Windows.Forms.Button();
            this.btnTDF = new System.Windows.Forms.Button();
            this.lblTDF = new System.Windows.Forms.Label();
            this.btnTerms = new System.Windows.Forms.Button();
            this.btnSave = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.cbMethod = new System.Windows.Forms.ComboBox();
            this.lblTerms = new System.Windows.Forms.Label();
            this.cbAC = new System.Windows.Forms.CheckBox();
            this.cbSC = new System.Windows.Forms.CheckBox();
            this.cbSM = new System.Windows.Forms.CheckBox();
            this.cbSR = new System.Windows.Forms.CheckBox();
            this.cbDW = new System.Windows.Forms.CheckBox();
            this.cbBG = new System.Windows.Forms.CheckBox();
            this.cbSY = new System.Windows.Forms.CheckBox();
            this.cbLO = new System.Windows.Forms.CheckBox();
            this.nudVector1 = new System.Windows.Forms.NumericUpDown();
            this.nudVector2 = new System.Windows.Forms.NumericUpDown();
            this.btnVector = new System.Windows.Forms.Button();
            this.nudVectorCos1 = new System.Windows.Forms.NumericUpDown();
            this.nudVectorCos2 = new System.Windows.Forms.NumericUpDown();
            this.btnCos = new System.Windows.Forms.Button();
            this.lblCos = new System.Windows.Forms.Label();
            this.btnDup = new System.Windows.Forms.Button();
            this.tbK = new System.Windows.Forms.TextBox();
            this.cbMU = new System.Windows.Forms.CheckBox();
            this.cbDO = new System.Windows.Forms.CheckBox();
            this.btnLDA = new System.Windows.Forms.Button();
            this.tbLDA = new System.Windows.Forms.TextBox();
            this.cbType = new System.Windows.Forms.ComboBox();
            ((System.ComponentModel.ISupportInitialize)(this.dgFeatures)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.dgComments)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudVector1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudVector2)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudVectorCos1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudVectorCos2)).BeginInit();
            this.SuspendLayout();
            // 
            // btnFill
            // 
            this.btnFill.Location = new System.Drawing.Point(12, 41);
            this.btnFill.Name = "btnFill";
            this.btnFill.Size = new System.Drawing.Size(75, 23);
            this.btnFill.TabIndex = 1;
            this.btnFill.Text = "Fill";
            this.btnFill.UseVisualStyleBackColor = true;
            this.btnFill.Click += new System.EventHandler(this.btnFill_Click);
            // 
            // dgFeatures
            // 
            this.dgFeatures.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgFeatures.Location = new System.Drawing.Point(12, 338);
            this.dgFeatures.Name = "dgFeatures";
            this.dgFeatures.Size = new System.Drawing.Size(689, 150);
            this.dgFeatures.TabIndex = 2;
            this.dgFeatures.CellClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.dgFeatures_CellClick);
            // 
            // dgComments
            // 
            this.dgComments.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgComments.Location = new System.Drawing.Point(12, 494);
            this.dgComments.Name = "dgComments";
            this.dgComments.Size = new System.Drawing.Size(689, 150);
            this.dgComments.TabIndex = 3;
            // 
            // btnTest
            // 
            this.btnTest.Location = new System.Drawing.Point(630, 248);
            this.btnTest.Name = "btnTest";
            this.btnTest.Size = new System.Drawing.Size(75, 23);
            this.btnTest.TabIndex = 7;
            this.btnTest.Text = "LSA";
            this.btnTest.UseVisualStyleBackColor = true;
            this.btnTest.Click += new System.EventHandler(this.btnTest_Click);
            // 
            // ofXML
            // 
            this.ofXML.Filter = "XML-files|*.xml";
            this.ofXML.InitialDirectory = "C:\\Users\\889716\\SkyDrive\\Documents\\EQuA\\FeatureRequests\\MyLyn";
            this.ofXML.Title = "Input XML";
            this.ofXML.FileOk += new System.ComponentModel.CancelEventHandler(this.ofXML_FileOk);
            // 
            // lbFile
            // 
            this.lbFile.AutoSize = true;
            this.lbFile.Location = new System.Drawing.Point(235, 20);
            this.lbFile.Name = "lbFile";
            this.lbFile.Size = new System.Drawing.Size(66, 13);
            this.lbFile.TabIndex = 8;
            this.lbFile.Text = "Current File: ";
            // 
            // btnFile
            // 
            this.btnFile.Location = new System.Drawing.Point(12, 15);
            this.btnFile.Name = "btnFile";
            this.btnFile.Size = new System.Drawing.Size(75, 23);
            this.btnFile.TabIndex = 9;
            this.btnFile.Text = "Change File";
            this.btnFile.UseVisualStyleBackColor = true;
            this.btnFile.Click += new System.EventHandler(this.btnFile_Click);
            // 
            // btnTDF
            // 
            this.btnTDF.Location = new System.Drawing.Point(235, 95);
            this.btnTDF.Name = "btnTDF";
            this.btnTDF.Size = new System.Drawing.Size(109, 24);
            this.btnTDF.TabIndex = 10;
            this.btnTDF.Text = "Calculate TFIDF";
            this.btnTDF.UseVisualStyleBackColor = true;
            this.btnTDF.Click += new System.EventHandler(this.btnTDF_Click);
            // 
            // lblTDF
            // 
            this.lblTDF.AutoSize = true;
            this.lblTDF.Location = new System.Drawing.Point(352, 101);
            this.lblTDF.Name = "lblTDF";
            this.lblTDF.Size = new System.Drawing.Size(42, 13);
            this.lblTDF.TabIndex = 11;
            this.lblTDF.Text = "TF/IDF";
            // 
            // btnTerms
            // 
            this.btnTerms.Enabled = false;
            this.btnTerms.Location = new System.Drawing.Point(235, 128);
            this.btnTerms.Name = "btnTerms";
            this.btnTerms.Size = new System.Drawing.Size(109, 24);
            this.btnTerms.TabIndex = 12;
            this.btnTerms.Text = "Show Term List";
            this.btnTerms.UseVisualStyleBackColor = true;
            this.btnTerms.Click += new System.EventHandler(this.btnTerms_Click);
            // 
            // btnSave
            // 
            this.btnSave.Enabled = false;
            this.btnSave.Location = new System.Drawing.Point(236, 161);
            this.btnSave.Name = "btnSave";
            this.btnSave.Size = new System.Drawing.Size(75, 23);
            this.btnSave.TabIndex = 13;
            this.btnSave.Text = "Save TFIDF";
            this.btnSave.UseVisualStyleBackColor = true;
            this.btnSave.Click += new System.EventHandler(this.btnSave_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(13, 101);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(46, 13);
            this.label1.TabIndex = 15;
            this.label1.Text = "Method:";
            // 
            // cbMethod
            // 
            this.cbMethod.FormattingEnabled = true;
            this.cbMethod.Items.AddRange(new object[] {
            "1: TFIDF",
            "2: Title vs description"});
            this.cbMethod.Location = new System.Drawing.Point(65, 97);
            this.cbMethod.Name = "cbMethod";
            this.cbMethod.Size = new System.Drawing.Size(146, 21);
            this.cbMethod.TabIndex = 16;
            // 
            // lblTerms
            // 
            this.lblTerms.AutoSize = true;
            this.lblTerms.Location = new System.Drawing.Point(352, 134);
            this.lblTerms.Name = "lblTerms";
            this.lblTerms.Size = new System.Drawing.Size(41, 13);
            this.lblTerms.TabIndex = 17;
            this.lblTerms.Text = "Count: ";
            // 
            // cbAC
            // 
            this.cbAC.AutoSize = true;
            this.cbAC.Checked = true;
            this.cbAC.CheckState = System.Windows.Forms.CheckState.Checked;
            this.cbAC.Location = new System.Drawing.Point(16, 122);
            this.cbAC.Name = "cbAC";
            this.cbAC.Size = new System.Drawing.Size(108, 17);
            this.cbAC.TabIndex = 19;
            this.cbAC.Text = "AC: All comments";
            this.cbAC.UseVisualStyleBackColor = true;
            // 
            // cbSC
            // 
            this.cbSC.AutoSize = true;
            this.cbSC.Checked = true;
            this.cbSC.CheckState = System.Windows.Forms.CheckState.Checked;
            this.cbSC.Location = new System.Drawing.Point(16, 140);
            this.cbSC.Name = "cbSC";
            this.cbSC.Size = new System.Drawing.Size(147, 17);
            this.cbSC.TabIndex = 20;
            this.cbSC.Text = "SC: Source code removal";
            this.cbSC.UseVisualStyleBackColor = true;
            // 
            // cbSM
            // 
            this.cbSM.AutoSize = true;
            this.cbSM.Checked = true;
            this.cbSM.CheckState = System.Windows.Forms.CheckState.Checked;
            this.cbSM.Location = new System.Drawing.Point(16, 158);
            this.cbSM.Name = "cbSM";
            this.cbSM.Size = new System.Drawing.Size(94, 17);
            this.cbSM.TabIndex = 21;
            this.cbSM.Text = "SM: Stemming";
            this.cbSM.UseVisualStyleBackColor = true;
            // 
            // cbSR
            // 
            this.cbSR.AutoSize = true;
            this.cbSR.Location = new System.Drawing.Point(16, 176);
            this.cbSR.Name = "cbSR";
            this.cbSR.Size = new System.Drawing.Size(135, 17);
            this.cbSR.TabIndex = 22;
            this.cbSR.Text = "SR: Stop word removal";
            this.cbSR.UseVisualStyleBackColor = true;
            // 
            // cbDW
            // 
            this.cbDW.AutoSize = true;
            this.cbDW.Checked = true;
            this.cbDW.CheckState = System.Windows.Forms.CheckState.Checked;
            this.cbDW.Location = new System.Drawing.Point(16, 194);
            this.cbDW.Name = "cbDW";
            this.cbDW.Size = new System.Drawing.Size(138, 17);
            this.cbDW.TabIndex = 23;
            this.cbDW.Text = "DW: Double weight title";
            this.cbDW.UseVisualStyleBackColor = true;
            // 
            // cbBG
            // 
            this.cbBG.AutoSize = true;
            this.cbBG.Location = new System.Drawing.Point(16, 212);
            this.cbBG.Name = "cbBG";
            this.cbBG.Size = new System.Drawing.Size(82, 17);
            this.cbBG.TabIndex = 24;
            this.cbBG.Text = "BG: Bi-gram";
            this.cbBG.UseVisualStyleBackColor = true;
            // 
            // cbSY
            // 
            this.cbSY.AutoSize = true;
            this.cbSY.Checked = true;
            this.cbSY.CheckState = System.Windows.Forms.CheckState.Checked;
            this.cbSY.Location = new System.Drawing.Point(16, 230);
            this.cbSY.Name = "cbSY";
            this.cbSY.Size = new System.Drawing.Size(220, 17);
            this.cbSY.TabIndex = 25;
            this.cbSY.Text = "SY: Remove synonyms, spelling mistakes";
            this.cbSY.UseVisualStyleBackColor = true;
            // 
            // cbLO
            // 
            this.cbLO.AutoSize = true;
            this.cbLO.Checked = true;
            this.cbLO.CheckState = System.Windows.Forms.CheckState.Checked;
            this.cbLO.Location = new System.Drawing.Point(16, 248);
            this.cbLO.Name = "cbLO";
            this.cbLO.Size = new System.Drawing.Size(84, 17);
            this.cbLO.TabIndex = 26;
            this.cbLO.Text = "LO: toLower";
            this.cbLO.UseVisualStyleBackColor = true;
            // 
            // nudVector1
            // 
            this.nudVector1.Location = new System.Drawing.Point(236, 194);
            this.nudVector1.Maximum = new decimal(new int[] {
            10000,
            0,
            0,
            0});
            this.nudVector1.Name = "nudVector1";
            this.nudVector1.Size = new System.Drawing.Size(65, 20);
            this.nudVector1.TabIndex = 27;
            // 
            // nudVector2
            // 
            this.nudVector2.Location = new System.Drawing.Point(320, 194);
            this.nudVector2.Maximum = new decimal(new int[] {
            10000,
            0,
            0,
            0});
            this.nudVector2.Name = "nudVector2";
            this.nudVector2.Size = new System.Drawing.Size(73, 20);
            this.nudVector2.TabIndex = 28;
            // 
            // btnVector
            // 
            this.btnVector.Location = new System.Drawing.Point(410, 191);
            this.btnVector.Name = "btnVector";
            this.btnVector.Size = new System.Drawing.Size(109, 23);
            this.btnVector.TabIndex = 29;
            this.btnVector.Text = "Save Term Vectors";
            this.btnVector.UseVisualStyleBackColor = true;
            this.btnVector.Click += new System.EventHandler(this.btnVector_Click);
            // 
            // nudVectorCos1
            // 
            this.nudVectorCos1.Location = new System.Drawing.Point(235, 229);
            this.nudVectorCos1.Maximum = new decimal(new int[] {
            1000000,
            0,
            0,
            0});
            this.nudVectorCos1.Name = "nudVectorCos1";
            this.nudVectorCos1.Size = new System.Drawing.Size(65, 20);
            this.nudVectorCos1.TabIndex = 30;
            // 
            // nudVectorCos2
            // 
            this.nudVectorCos2.Location = new System.Drawing.Point(321, 229);
            this.nudVectorCos2.Maximum = new decimal(new int[] {
            1000000,
            0,
            0,
            0});
            this.nudVectorCos2.Name = "nudVectorCos2";
            this.nudVectorCos2.Size = new System.Drawing.Size(73, 20);
            this.nudVectorCos2.TabIndex = 31;
            // 
            // btnCos
            // 
            this.btnCos.Location = new System.Drawing.Point(410, 226);
            this.btnCos.Name = "btnCos";
            this.btnCos.Size = new System.Drawing.Size(52, 23);
            this.btnCos.TabIndex = 32;
            this.btnCos.Text = "CoSim";
            this.btnCos.UseVisualStyleBackColor = true;
            this.btnCos.Click += new System.EventHandler(this.btnCos_Click);
            // 
            // lblCos
            // 
            this.lblCos.AutoSize = true;
            this.lblCos.Location = new System.Drawing.Point(468, 231);
            this.lblCos.Name = "lblCos";
            this.lblCos.Size = new System.Drawing.Size(37, 13);
            this.lblCos.TabIndex = 33;
            this.lblCos.Text = "CoSim";
            // 
            // btnDup
            // 
            this.btnDup.Location = new System.Drawing.Point(12, 67);
            this.btnDup.Name = "btnDup";
            this.btnDup.Size = new System.Drawing.Size(112, 23);
            this.btnDup.TabIndex = 34;
            this.btnDup.Text = "Read Duplicates";
            this.btnDup.UseVisualStyleBackColor = true;
            this.btnDup.Click += new System.EventHandler(this.btnDup_Click);
            // 
            // tbK
            // 
            this.tbK.Location = new System.Drawing.Point(524, 250);
            this.tbK.Name = "tbK";
            this.tbK.Size = new System.Drawing.Size(100, 20);
            this.tbK.TabIndex = 35;
            this.tbK.TextChanged += new System.EventHandler(this.tbK_TextChanged);
            // 
            // cbMU
            // 
            this.cbMU.AutoSize = true;
            this.cbMU.Location = new System.Drawing.Point(16, 266);
            this.cbMU.Name = "cbMU";
            this.cbMU.Size = new System.Drawing.Size(181, 17);
            this.cbMU.TabIndex = 36;
            this.cbMU.Text = "MU: Remove words with count 1";
            this.cbMU.UseVisualStyleBackColor = true;
            // 
            // cbDO
            // 
            this.cbDO.AutoSize = true;
            this.cbDO.Location = new System.Drawing.Point(16, 285);
            this.cbDO.Name = "cbDO";
            this.cbDO.Size = new System.Drawing.Size(211, 17);
            this.cbDO.TabIndex = 37;
            this.cbDO.Text = "DO: Remove words in only 1 document";
            this.cbDO.UseVisualStyleBackColor = true;
            // 
            // btnLDA
            // 
            this.btnLDA.Location = new System.Drawing.Point(630, 285);
            this.btnLDA.Name = "btnLDA";
            this.btnLDA.Size = new System.Drawing.Size(75, 23);
            this.btnLDA.TabIndex = 38;
            this.btnLDA.Text = "LDA";
            this.btnLDA.UseVisualStyleBackColor = true;
            this.btnLDA.Click += new System.EventHandler(this.btnLDA_Click);
            // 
            // tbLDA
            // 
            this.tbLDA.Location = new System.Drawing.Point(524, 287);
            this.tbLDA.Name = "tbLDA";
            this.tbLDA.Size = new System.Drawing.Size(100, 20);
            this.tbLDA.TabIndex = 39;
            this.tbLDA.Text = "10";
            // 
            // cbType
            // 
            this.cbType.FormattingEnabled = true;
            this.cbType.Items.AddRange(new object[] {
            "Mylyn/Netbeans",
            "ArgoUML"});
            this.cbType.Location = new System.Drawing.Point(106, 17);
            this.cbType.Name = "cbType";
            this.cbType.Size = new System.Drawing.Size(121, 21);
            this.cbType.TabIndex = 40;
            this.cbType.SelectedIndexChanged += new System.EventHandler(this.cbType_SelectedIndexChanged);
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(723, 656);
            this.Controls.Add(this.cbType);
            this.Controls.Add(this.tbLDA);
            this.Controls.Add(this.btnLDA);
            this.Controls.Add(this.cbDO);
            this.Controls.Add(this.cbMU);
            this.Controls.Add(this.tbK);
            this.Controls.Add(this.btnDup);
            this.Controls.Add(this.lblCos);
            this.Controls.Add(this.btnCos);
            this.Controls.Add(this.nudVectorCos2);
            this.Controls.Add(this.nudVectorCos1);
            this.Controls.Add(this.btnVector);
            this.Controls.Add(this.nudVector2);
            this.Controls.Add(this.nudVector1);
            this.Controls.Add(this.cbLO);
            this.Controls.Add(this.cbSY);
            this.Controls.Add(this.cbBG);
            this.Controls.Add(this.cbDW);
            this.Controls.Add(this.cbSR);
            this.Controls.Add(this.cbSM);
            this.Controls.Add(this.cbSC);
            this.Controls.Add(this.cbAC);
            this.Controls.Add(this.lblTerms);
            this.Controls.Add(this.cbMethod);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.btnSave);
            this.Controls.Add(this.btnTerms);
            this.Controls.Add(this.lblTDF);
            this.Controls.Add(this.btnTDF);
            this.Controls.Add(this.btnFile);
            this.Controls.Add(this.lbFile);
            this.Controls.Add(this.btnTest);
            this.Controls.Add(this.dgComments);
            this.Controls.Add(this.dgFeatures);
            this.Controls.Add(this.btnFill);
            this.Name = "MainForm";
            this.Text = "FRequAT - Feature Request Analysis Tool";
            ((System.ComponentModel.ISupportInitialize)(this.dgFeatures)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.dgComments)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudVector1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudVector2)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudVectorCos1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudVectorCos2)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button btnFill;
        private System.Windows.Forms.DataGridView dgFeatures;
        private System.Windows.Forms.DataGridView dgComments;
        private System.Windows.Forms.Button btnTest;
        private System.Windows.Forms.OpenFileDialog ofXML;
        private System.Windows.Forms.Label lbFile;
        private System.Windows.Forms.Button btnFile;
        private System.Windows.Forms.Button btnTDF;
        private System.Windows.Forms.Label lblTDF;
        private System.Windows.Forms.Button btnTerms;
        private System.Windows.Forms.Button btnSave;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.ComboBox cbMethod;
        private System.Windows.Forms.Label lblTerms;
        private System.Windows.Forms.CheckBox cbAC;
        private System.Windows.Forms.CheckBox cbSC;
        private System.Windows.Forms.CheckBox cbSM;
        private System.Windows.Forms.CheckBox cbSR;
        private System.Windows.Forms.CheckBox cbDW;
        private System.Windows.Forms.CheckBox cbBG;
        private System.Windows.Forms.CheckBox cbSY;
        private System.Windows.Forms.CheckBox cbLO;
        private System.Windows.Forms.NumericUpDown nudVector1;
        private System.Windows.Forms.NumericUpDown nudVector2;
        private System.Windows.Forms.Button btnVector;
        private System.Windows.Forms.NumericUpDown nudVectorCos1;
        private System.Windows.Forms.NumericUpDown nudVectorCos2;
        private System.Windows.Forms.Button btnCos;
        private System.Windows.Forms.Label lblCos;
        private System.Windows.Forms.Button btnDup;
        private System.Windows.Forms.TextBox tbK;
        private System.Windows.Forms.CheckBox cbMU;
        private System.Windows.Forms.CheckBox cbDO;
        private System.Windows.Forms.Button btnLDA;
        private System.Windows.Forms.TextBox tbLDA;
        private System.Windows.Forms.ComboBox cbType;
    }
}

