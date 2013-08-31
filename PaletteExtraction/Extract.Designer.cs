namespace Extract
{
    partial class Extract
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Extract));
            this.toolStrip1 = new System.Windows.Forms.ToolStrip();
            this.labelRunExtractor = new System.Windows.Forms.ToolStripLabel();
            this.extractTheme = new System.Windows.Forms.ToolStripButton();
            this.renderThemes = new System.Windows.Forms.ToolStripButton();
            this.toolStrip2 = new System.Windows.Forms.ToolStrip();
            this.labelTrain = new System.Windows.Forms.ToolStripLabel();
            this.OutputFeatures = new System.Windows.Forms.ToolStripButton();
            this.toolStrip3 = new System.Windows.Forms.ToolStrip();
            this.labelEvaluate = new System.Windows.Forms.ToolStripLabel();
            this.CompareThemes = new System.Windows.Forms.ToolStripButton();
            this.extractThemesToCompare = new System.Windows.Forms.ToolStripButton();
            this.debugBox = new System.Windows.Forms.CheckBox();
            this.progressBar = new System.Windows.Forms.ProgressBar();
            this.backgroundWorker = new System.ComponentModel.BackgroundWorker();
            this.statusBox = new System.Windows.Forms.TextBox();
            this.diagramThemes = new System.Windows.Forms.ToolStripButton();
            this.toolStrip1.SuspendLayout();
            this.toolStrip2.SuspendLayout();
            this.toolStrip3.SuspendLayout();
            this.SuspendLayout();
            // 
            // toolStrip1
            // 
            this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.labelRunExtractor,
            this.extractTheme,
            this.renderThemes});
            this.toolStrip1.Location = new System.Drawing.Point(0, 0);
            this.toolStrip1.Name = "toolStrip1";
            this.toolStrip1.Size = new System.Drawing.Size(497, 25);
            this.toolStrip1.TabIndex = 1;
            this.toolStrip1.Text = "toolStrip1";
            // 
            // labelRunExtractor
            // 
            this.labelRunExtractor.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.labelRunExtractor.Name = "labelRunExtractor";
            this.labelRunExtractor.Size = new System.Drawing.Size(149, 22);
            this.labelRunExtractor.Text = "Run the Palette Extractor";
            // 
            // extractTheme
            // 
            this.extractTheme.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.extractTheme.Image = ((System.Drawing.Image)(resources.GetObject("extractTheme.Image")));
            this.extractTheme.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.extractTheme.Name = "extractTheme";
            this.extractTheme.Size = new System.Drawing.Size(88, 22);
            this.extractTheme.Text = "extractThemes";
            this.extractTheme.Click += new System.EventHandler(this.extractTheme_Click);
            // 
            // renderThemes
            // 
            this.renderThemes.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.renderThemes.Image = ((System.Drawing.Image)(resources.GetObject("renderThemes.Image")));
            this.renderThemes.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.renderThemes.Name = "renderThemes";
            this.renderThemes.Size = new System.Drawing.Size(87, 22);
            this.renderThemes.Text = "renderThemes";
            this.renderThemes.Click += new System.EventHandler(this.RenderThemes_Click);
            // 
            // toolStrip2
            // 
            this.toolStrip2.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.labelTrain,
            this.OutputFeatures});
            this.toolStrip2.Location = new System.Drawing.Point(0, 25);
            this.toolStrip2.Name = "toolStrip2";
            this.toolStrip2.Size = new System.Drawing.Size(497, 25);
            this.toolStrip2.TabIndex = 2;
            this.toolStrip2.Text = "toolStrip2";
            // 
            // labelTrain
            // 
            this.labelTrain.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.labelTrain.Name = "labelTrain";
            this.labelTrain.Size = new System.Drawing.Size(99, 22);
            this.labelTrain.Text = "Training a Model";
            // 
            // OutputFeatures
            // 
            this.OutputFeatures.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.OutputFeatures.Image = ((System.Drawing.Image)(resources.GetObject("OutputFeatures.Image")));
            this.OutputFeatures.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.OutputFeatures.Name = "OutputFeatures";
            this.OutputFeatures.Size = new System.Drawing.Size(91, 22);
            this.OutputFeatures.Text = "outputFeatures";
            this.OutputFeatures.Click += new System.EventHandler(this.OutputFeatures_Click);
            // 
            // toolStrip3
            // 
            this.toolStrip3.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.labelEvaluate,
            this.CompareThemes,
            this.extractThemesToCompare,
            this.diagramThemes});
            this.toolStrip3.Location = new System.Drawing.Point(0, 50);
            this.toolStrip3.Name = "toolStrip3";
            this.toolStrip3.Size = new System.Drawing.Size(497, 25);
            this.toolStrip3.TabIndex = 3;
            this.toolStrip3.Text = "toolStrip3";
            // 
            // labelEvaluate
            // 
            this.labelEvaluate.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.labelEvaluate.Name = "labelEvaluate";
            this.labelEvaluate.Size = new System.Drawing.Size(101, 22);
            this.labelEvaluate.Text = "Evaluate Themes";
            // 
            // CompareThemes
            // 
            this.CompareThemes.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.CompareThemes.Image = ((System.Drawing.Image)(resources.GetObject("CompareThemes.Image")));
            this.CompareThemes.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.CompareThemes.Name = "CompareThemes";
            this.CompareThemes.Size = new System.Drawing.Size(100, 22);
            this.CompareThemes.Text = "compareThemes";
            this.CompareThemes.Click += new System.EventHandler(this.compareThemes_Click);
            // 
            // extractThemesToCompare
            // 
            this.extractThemesToCompare.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.extractThemesToCompare.Image = ((System.Drawing.Image)(resources.GetObject("extractThemesToCompare.Image")));
            this.extractThemesToCompare.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.extractThemesToCompare.Name = "extractThemesToCompare";
            this.extractThemesToCompare.Size = new System.Drawing.Size(151, 22);
            this.extractThemesToCompare.Text = "extractThemesToCompare";
            this.extractThemesToCompare.Click += new System.EventHandler(this.extractThemesToCompare_Click);
            // 
            // debugBox
            // 
            this.debugBox.AutoSize = true;
            this.debugBox.Checked = true;
            this.debugBox.CheckState = System.Windows.Forms.CheckState.Checked;
            this.debugBox.Location = new System.Drawing.Point(12, 90);
            this.debugBox.Name = "debugBox";
            this.debugBox.Size = new System.Drawing.Size(135, 17);
            this.debugBox.TabIndex = 4;
            this.debugBox.Text = "Debug (Resize images)";
            this.debugBox.UseVisualStyleBackColor = true;
            this.debugBox.CheckedChanged += new System.EventHandler(this.debugBox_CheckedChanged);
            // 
            // progressBar
            // 
            this.progressBar.Location = new System.Drawing.Point(0, 150);
            this.progressBar.Name = "progressBar";
            this.progressBar.Size = new System.Drawing.Size(497, 18);
            this.progressBar.TabIndex = 5;
            // 
            // statusBox
            // 
            this.statusBox.BackColor = System.Drawing.SystemColors.Control;
            this.statusBox.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.statusBox.Location = new System.Drawing.Point(0, 131);
            this.statusBox.Name = "statusBox";
            this.statusBox.Size = new System.Drawing.Size(455, 13);
            this.statusBox.TabIndex = 6;
            // 
            // diagramThemes
            // 
            this.diagramThemes.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.diagramThemes.Image = ((System.Drawing.Image)(resources.GetObject("diagramThemes.Image")));
            this.diagramThemes.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.diagramThemes.Name = "diagramThemes";
            this.diagramThemes.Size = new System.Drawing.Size(97, 22);
            this.diagramThemes.Text = "diagramThemes";
            this.diagramThemes.Click += new System.EventHandler(this.diagramThemes_Click);
            // 
            // Extract
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(497, 169);
            this.Controls.Add(this.statusBox);
            this.Controls.Add(this.progressBar);
            this.Controls.Add(this.debugBox);
            this.Controls.Add(this.toolStrip3);
            this.Controls.Add(this.toolStrip2);
            this.Controls.Add(this.toolStrip1);
            this.Name = "Extract";
            this.Text = "PaletteExtraction";
            this.toolStrip1.ResumeLayout(false);
            this.toolStrip1.PerformLayout();
            this.toolStrip2.ResumeLayout(false);
            this.toolStrip2.PerformLayout();
            this.toolStrip3.ResumeLayout(false);
            this.toolStrip3.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ToolStrip toolStrip1;
        private System.Windows.Forms.ToolStripButton extractTheme;
        private System.Windows.Forms.ToolStripButton renderThemes;
        private System.Windows.Forms.ToolStripLabel labelRunExtractor;
        private System.Windows.Forms.ToolStrip toolStrip2;
        private System.Windows.Forms.ToolStripLabel labelTrain;
        private System.Windows.Forms.ToolStripButton OutputFeatures;
        private System.Windows.Forms.ToolStrip toolStrip3;
        private System.Windows.Forms.ToolStripLabel labelEvaluate;
        private System.Windows.Forms.ToolStripButton CompareThemes;
        private System.Windows.Forms.ToolStripButton extractThemesToCompare;
        private System.Windows.Forms.CheckBox debugBox;
        private System.Windows.Forms.ProgressBar progressBar;
        private System.ComponentModel.BackgroundWorker backgroundWorker;
        private System.Windows.Forms.TextBox statusBox;
        private System.Windows.Forms.ToolStripButton diagramThemes;
    }
}

