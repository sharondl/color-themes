using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;

using Engine;

namespace Extract
{
    public partial class Extract : Form
    {
        public class EvalItem
        {
            public String Name;
            public String Path;
            public bool Filter;

            public EvalItem(String n, String p, bool f)
            {
                Name = n;
                Path = p;
                Filter = f;
            }
        }


        //Directories of interest
        String config = "../../localconfig.txt";
        String dir;
        String json;
        String weightsDir;
        String evalDir;
        String trainInDir;
        String trainOutDir;


        //Parameters for evaluating themes
        List<EvalItem> compare;
        List<EvalItem> reference;
        String refkeys;

        //Parameters for outputting features for training
        EvalItem trainReference;

        //Parameters for diagramming themes
        List<EvalItem> diagramLeft;
        List<EvalItem> diagramRight;

        bool debug = true;
        bool debugLogs = false;

        FeatureName featureName;

        public Extract()
        {
            InitializeComponent();
        
            reference = new List<EvalItem>();
            compare = new List<EvalItem>();
            featureName = new FeatureName();

            diagramLeft = new List<EvalItem>();
            diagramRight = new List<EvalItem>();

            //load the config file
            String[] lines = File.ReadAllLines(config);
            foreach (String l in lines)
            {
                String[] fields = l.Split('>');
                String param = fields.First().Trim();
                String[] subfields;

                switch (param)
                {
                    case "dir":
                        dir = fields.Last().Trim();
                        break;
                    case "json":
                        json = fields.Last().Trim();
                        break;
                    case "weightsDir":
                        weightsDir = fields.Last().Trim();
                        break;

                    case "trainInDir":
                        trainInDir = fields.Last().Trim();
                        break;

                    case "trainOutDir":
                        trainOutDir = fields.Last().Trim();
                        break;

                    case "trainRef":
                        subfields = fields.Last().Split('^');
                        if (subfields.Length > 1)
                            trainReference = new EvalItem("Palettes", subfields[0].Trim(), subfields[1].Trim().ToLower() == "filter");
                        else
                            trainReference = new EvalItem("Palettes", subfields[0].Trim(), false);
                        break;

                    case "evalInOutDir":
                        evalDir = fields.Last().Trim();
                        break;

                    case "ref":
                        subfields = fields.Last().Split('^');
                        if (subfields.Length > 2)
                            reference.Add(new EvalItem(subfields[0].Trim(), subfields[1].Trim(), subfields[2].Trim().ToLower()=="filter"));
                        else
                            reference.Add(new EvalItem(subfields[0].Trim(), subfields[1].Trim(), false));

                        break;
                    case "compare":
                        subfields = fields.Last().Split('^');
                        if (subfields.Length > 2)
                            compare.Add(new EvalItem(subfields[0].Trim(), subfields[1].Trim(), subfields[2].Trim().ToLower() == "filter"));
                        else
                            compare.Add(new EvalItem(subfields[0].Trim(), subfields[1].Trim(), false));

                        break;
                    case "refkeys":
                        refkeys = fields.Last().Trim();
                        break;

                    case "diagramLeft":
                        subfields = fields.Last().Split('^');
                        if (subfields.Length > 2)
                            diagramLeft.Add(new EvalItem(subfields[0].Trim(), subfields[1].Trim(), subfields[2].Trim().ToLower() == "filter"));
                        else
                            diagramLeft.Add(new EvalItem(subfields[0].Trim(), subfields[1].Trim(), false));
                        break;
                    case "diagramRight":
                        subfields = fields.Last().Split('^');
                        if (subfields.Length > 2)
                            diagramRight.Add(new EvalItem(subfields[0].Trim(), subfields[1].Trim(), subfields[2].Trim().ToLower() == "filter"));
                        else
                            diagramRight.Add(new EvalItem(subfields[0].Trim(), subfields[1].Trim(), false));
                        break;
                    default:
                        break;
                }

            }

            //resolve relative paths
            refkeys = Path.Combine(evalDir, refkeys);
            trainReference.Path = Path.Combine(trainInDir, trainReference.Path);
            ResolveRelativePaths(compare, evalDir);
            ResolveRelativePaths(reference, evalDir);
            ResolveRelativePaths(diagramLeft, evalDir);
            ResolveRelativePaths(diagramRight, evalDir);

        }

        private void ResolveRelativePaths(List<EvalItem> items, String baseDir)
        {
            foreach (EvalItem item in items)
                item.Path = Path.Combine(baseDir, item.Path);
        }


        private BackgroundWorker MakeBackgroundWorker(DoWorkEventHandler handler)
        {
            BackgroundWorker bw = new BackgroundWorker();

            bw.ProgressChanged += updateProgress;
            bw.RunWorkerCompleted += delegate(object sender, RunWorkerCompletedEventArgs e)
            {
                progressBar.Value = 100;
                if (e.Error == null)
                    statusBox.Text = "Done!";
                else statusBox.Text = "Error: " + e.Error.Message;
                UseWaitCursor = false;
            };
            bw.WorkerReportsProgress = true;
            bw.DoWork += handler;

            return bw;
        }


        /**
         * Get image files (jpg, gif, and png) in the directory
         */
        private String[] GetImageFiles(String directory)
        {
            return Directory.GetFiles(directory, "*.*").Where(file => file.ToLower().EndsWith("jpg") || file.ToLower().EndsWith("gif") || file.ToLower().EndsWith("png")).ToArray<String>();
        }

      
        private void updateProgress(object sender, ProgressChangedEventArgs e)
        {
            if (e.ProgressPercentage >= 0)
                progressBar.Value = e.ProgressPercentage;

            if (e.UserState != null && e.UserState.ToString() != "")
                statusBox.Text = e.UserState.ToString();
        }

        /**
         * Extract themes from images in the image directory
         */ 
        private void extractTheme_Click(object sender, EventArgs e)
        {
            if (backgroundWorker.IsBusy)
            {
                statusBox.Text = "Please wait...";
                return;
            }
           

            backgroundWorker =  MakeBackgroundWorker(delegate
            {
                backgroundWorker.ReportProgress(0, "Extracting Themes from Image Dir");
                //Use the Palette Extractor to extract the theme
                PaletteExtractor extractor = new PaletteExtractor(dir, weightsDir, json);

                String[] files = GetImageFiles(dir);
                String outfile = Path.Combine(dir, "optimized.tsv");
                String headers = "pid\tid\timage\tcolors\tnumColors\tlog\n";
                File.WriteAllText(outfile, "");
                File.AppendAllText(outfile, headers);
                int count = 0;
                foreach (String f in files)
                {
                    count++;
                    String basename = new FileInfo(f).Name;

                    //The saliency pattern "_Judd" is just an additional annotation after the image filename if it exists
                    //i.e. if the image filename is A.png, the saliency map filename is A_Judd.png
                    PaletteData data = extractor.HillClimbPalette(basename, "_Judd", debug);

                    //save to file
                    String colorString = data.ToString();
                    File.AppendAllText(outfile, count + "\t-1\t" + basename + "\t" + colorString + "\t5\t\n");
                    backgroundWorker.ReportProgress(100 * count / files.Count(),"Finished " + f);

                }
            });

            RunWorker();

        }

        private void RunWorker()
        {
            UseWaitCursor = true;
            backgroundWorker.RunWorkerAsync();
        }


        /**
         * Render themes in the image directory
         */ 
        private void RenderThemes_Click(object sender, EventArgs e)
        { 
            
            if (backgroundWorker.IsBusy)
            {
                statusBox.Text = "Please wait...";
                return;
            }
            backgroundWorker.WorkerReportsProgress = true;

            backgroundWorker =  MakeBackgroundWorker( delegate
            {
                Directory.CreateDirectory(Path.Combine(dir, "renders"));

                String infile = Path.Combine(dir, "optimized.tsv");
                Dictionary<String, List<PaletteData>> palettes = LoadFilePalettes(infile);

                int count = 0;
                foreach (String key in palettes.Keys)
                {                   
                    List<PaletteData> list = palettes[key];
                    
                    int idx = 0;
                    foreach (PaletteData data in list)
                    {
                        String filename = Path.Combine(dir, "renders", Util.ConvertFileName(key, "_" + idx));
                        SavePaletteToImage(dir, key, filename, data);
                        idx++;
                    }
                    count++;
                    backgroundWorker.ReportProgress(count * 100 / palettes.Keys.Count(), "Rendering themes..");
                }
            });

            RunWorker();
        }

        /**
         * Return a theme with colors snapped to the given swatches
         */ 
        private PaletteData SnapColors(PaletteData data, PaletteData swatches)
        {
            PaletteData result = new PaletteData();
            List<CIELAB> matchedColors = new List<CIELAB>();
            //match each color to closest swatch color
            foreach (CIELAB kc in data.lab)
            {
                double bestDist = Double.PositiveInfinity;
                CIELAB best = new CIELAB();
                foreach (CIELAB s in swatches.lab)
                {
                    double dist = s.SqDist(kc);
                    if (dist < bestDist)
                    {
                        bestDist = dist;
                        best = s;
                    }
                }
                matchedColors.Add(best);
            }
            result.lab = matchedColors;
            foreach (CIELAB l in data.lab)
                result.colors.Add(Util.LABtoRGB(l));
            return result;
        }

        private Dictionary<String, Dictionary<String, List<PaletteData>>> ItemsToDictionary(List<EvalItem> items)
        {
            Dictionary<String, Dictionary<String, List<PaletteData>>> result = new Dictionary<String, Dictionary<String, List<PaletteData>>>();
            foreach (var t in items)
            {
                String name = t.Name;
                String path = t.Path;
                bool filter = t.Filter;

                var palettes = LoadFilePalettes(path);
                var keys = palettes.Keys.ToArray<String>();
                if (filter)
                    foreach (String key in keys)
                        palettes[key] = removeOutliers(palettes[key]);

                result.Add(name, palettes);
            }
            return result;
        }

        /**
         * Calculate average distance between themes from different sources, and average overlap over different thresholds
         */ 
        private void compareThemes_Click(object sender, EventArgs e)
        {
            if (backgroundWorker.IsBusy)
            {
                statusBox.Text = "Please wait...";
                return;
            }
            backgroundWorker.WorkerReportsProgress = true;

            backgroundWorker =  MakeBackgroundWorker(delegate
            {
                    backgroundWorker.ReportProgress(0, "Comparing themes...");
                    //load the reference palettes
                    //load the palettes to compare against
                    Dictionary<String, Dictionary<String, List<PaletteData>>> referenceP = ItemsToDictionary(reference);
                    Dictionary<String, Dictionary<String, List<PaletteData>>> compareP = ItemsToDictionary(compare);
                    Dictionary<String, List<PaletteData>> testKeys = LoadFilePalettes(refkeys);

                    //compare each source in compare to each reference
                    List<String> desc = new List<String>();
                    List<double> dists = new List<double>();

                    foreach (var r in reference)
                    {
                        foreach (var c in compare)
                        {
                            desc.Add(c.Name + "-" + r.Name);
                            dists.Add(0);
                        }
                    }

  
                    int progress = 0;
                    foreach (String key in testKeys.Keys)
                    {
                        int idx = 0;
                        foreach (var r in reference)
                        {
                            foreach (var c in compare)
                            {
                                //count number of keys in common between reference, compare, and test set
                                if (referenceP[r.Name].ContainsKey(key) && compareP[c.Name].ContainsKey(key))
                                {
                                    double factor = referenceP[r.Name].Keys.Intersect(compareP[c.Name].Keys).Intersect(testKeys.Keys).Count();
                                    dists[idx] += GetAvgDist(referenceP[r.Name][key], compareP[c.Name][key], c.Path == r.Path) / factor;
                                }
                                idx++;
                            }
                        }

                        progress++;
                        backgroundWorker.ReportProgress(50 * progress/testKeys.Keys.Count(), "Comparing themes...Calculating distance...");
                    }

                    //Write out the average distances
                    List<String> lines = new List<String>();
                    lines.Add(String.Join(",", desc.ToArray<String>()));
                    lines.Add(String.Join(",", dists.Select(i => i.ToString())));
                    File.WriteAllLines(Path.Combine(evalDir, "avgDists.csv"), lines.ToArray<String>());

                    backgroundWorker.ReportProgress(50, "Comparing themes...Calculating overlap...");
                    //Output csv file for the overlap graph
                    //clamp each palette to the closest color swatch
                    //when computing overlap for the graph
                    PaletteExtractor extractor = new PaletteExtractor(evalDir, weightsDir, json);
                    foreach (String key in testKeys.Keys)
                    {
                        PaletteData swatches = extractor.GetPaletteSwatches(key);
                        foreach (var r in reference)
                        {
                            if (!referenceP[r.Name].ContainsKey(key))
                                continue;
                            var palettes = referenceP[r.Name][key];
                            for (int i = 0; i < palettes.Count(); i++)
                                palettes[i] = SnapColors(palettes[i], swatches);
                        }
                        foreach (var c in compare)
                        {
                            if (!compareP[c.Name].ContainsKey(key))
                                continue;
                            var palettes = compareP[c.Name][key];
                            for (int i = 0; i < palettes.Count(); i++)
                                palettes[i] = SnapColors(palettes[i], swatches);
                               
                        }
                    }

                    lines.Clear();

                    //compute the overlaps for different thresholds
                    lines.Add("thresh,type,overlap");

                    for (int t = 1; t < 100; t += 2)
                    {
                        double[] overlaps = new double[reference.Count() * compare.Count()];
                        int idx = 0;
                        foreach (var r in reference)
                        {
                            foreach (var c in compare)
                            {                       
                                foreach (String key in testKeys.Keys)
                                    if (compareP[c.Name].ContainsKey(key) && referenceP[r.Name].ContainsKey(key))
                                        overlaps[idx] += GetAvgOverlap(compareP[c.Name][key], referenceP[r.Name][key], t, c.Path == r.Path);

                                double factor = referenceP[r.Name].Keys.Intersect(compareP[c.Name].Keys).Intersect(testKeys.Keys).Count();
                                lines.Add(t + "," + desc[idx] + "," + overlaps[idx] / factor);
                                idx++;
                            }
                        }
                        backgroundWorker.ReportProgress(t/2+50, String.Format("Comparing themes...Calculating overlap for thresh {0}/{1}...",t,100));

                    }
                    File.WriteAllLines(Path.Combine(evalDir, "overlaps.csv"), lines.ToArray<String>());
            });

            RunWorker();
            
        }

        /**
         * Output features for training a model
         */ 
        private void OutputFeatures_Click(object sender, EventArgs e)
        {
            if (backgroundWorker.IsBusy)
            {
                statusBox.Text = "Please wait...";
                return;
            }
            backgroundWorker.WorkerReportsProgress = true;

            backgroundWorker = MakeBackgroundWorker(delegate { CalculateFeatures(); });
            RunWorker();
        }

        /**
         * Extract k-means, c-means, model, and oracle themes for comparison
         */ 
        private void extractThemesToCompare_Click(object sender, EventArgs e)
        {
            if (backgroundWorker.IsBusy)
            {
                statusBox.Text = "Please wait...";
                return;
            }
            backgroundWorker.WorkerReportsProgress = true;

            backgroundWorker = MakeBackgroundWorker(delegate
            {
                backgroundWorker.ReportProgress(0, "Extracting themes to compare...");

                PaletteExtractor extractor = new PaletteExtractor(evalDir, weightsDir, json);
                Dictionary<String, Dictionary<String, List<PaletteData>>> referenceP = ItemsToDictionary(reference);


                String[] files = GetImageFiles(evalDir);
                String headers = "pid\tid\timage\tcolors\tnumColors\tlog\n";


                backgroundWorker.ReportProgress(0, "Extracting random themes...");
                //extract random
                String randomFile = Path.Combine(evalDir, "random.tsv");
                if (!File.Exists(randomFile) || File.ReadLines(randomFile).Count() != (40 * files.Count()) + 1)
                {
                    int count = 0;
                    File.WriteAllText(randomFile, "");
                    File.AppendAllText(randomFile, headers);

                    foreach (String f in files)
                    {
                        String basename = new FileInfo(f).Name;
                        PaletteData swatches = extractor.GetPaletteSwatches(basename);

                        List<PaletteData> random = GenerateRandomPalettesNonBinned(40, swatches);

                        foreach (PaletteData data in random)
                        {
                            count++;
                            String colorString = data.ToString();
                            File.AppendAllText(randomFile, count + "\t-1\t" + basename + "\t" + colorString + "\t5\t\n");
                        }

                    }
                }
                else
                {
                    Console.WriteLine("Random palettes file already exists");
                    backgroundWorker.ReportProgress(100, "Random themes file already exist...skipping");
                    Thread.Sleep(1000);
                }

                backgroundWorker.ReportProgress(0, "Extracting K-means themes...");

                //extract kmeans
                String kmeansFile = Path.Combine(evalDir, "kmeans.tsv");
                if (!File.Exists(kmeansFile) || File.ReadLines(kmeansFile).Count() != files.Count() + 1)
                {
                    File.WriteAllText(kmeansFile, "");
                    File.AppendAllText(kmeansFile, headers);
                    int count = 0;
                    foreach (String f in files)
                    {
                        count++;
                        String basename = new FileInfo(f).Name;

                        PaletteData data = KMeansPalette(f);

                        //save to file
                        String colorString = data.ToString();
                        File.AppendAllText(kmeansFile, count + "\t-1\t" + basename + "\t" + colorString + "\t5\t\n");
                        backgroundWorker.ReportProgress(100 * count / files.Count(), "");
                    }
                }
                else
                {
                    Console.WriteLine("Kmeans palettes file already exists");
                    backgroundWorker.ReportProgress(100, "K-means themes already exist...skipping");
                    Thread.Sleep(1000);
                }



                //extract cmeans
                backgroundWorker.ReportProgress(0, "Extracting C-means themes...");
                String cmeansFile = Path.Combine(evalDir, "cmeans.tsv");
                if (!File.Exists(cmeansFile) || File.ReadLines(cmeansFile).Count() != files.Count() + 1)
                {
                    File.WriteAllText(cmeansFile, "");
                    File.AppendAllText(cmeansFile, headers);
                    int count = 0;
                    foreach (String f in files)
                    {
                        count++;
                        String basename = new FileInfo(f).Name;

                        PaletteData data = CMeansPalette(f);

                        //save to file
                        String colorString = data.ToString();
                        File.AppendAllText(cmeansFile, count + "\t-1\t" + basename + "\t" + colorString + "\t5\t\n");
                        backgroundWorker.ReportProgress(100 * count / files.Count(), "");
                    }
                }
                else
                {
                    Console.WriteLine("Cmeans palettes file already exists");
                    backgroundWorker.ReportProgress(100, "C-means themes already exist...skipping");
                    Thread.Sleep(1000);
                }


                //extract model
                backgroundWorker.ReportProgress(0, "Extracting Optimized themes...");
                String modelFile = Path.Combine(evalDir, "optimized.tsv");
                if (!File.Exists(modelFile) || File.ReadLines(modelFile).Count() != files.Count() + 1)
                {
                    File.WriteAllText(modelFile, "");
                    File.AppendAllText(modelFile, headers);
                    int count = 0;
                    foreach (String f in files)
                    {
                        count++;
                        String basename = new FileInfo(f).Name;

                        //The saliency pattern "_Judd" is just an additional annotation after the image filename if it exists
                        //i.e. if the image filename is A.png, the saliency map filename is A_Judd.png
                        PaletteData data = extractor.HillClimbPalette(basename, "_Judd", debug);

                        //save to file
                        String colorString = data.ToString();
                        File.AppendAllText(modelFile, count + "\t-1\t" + basename + "\t" + colorString + "\t5\t\n");
                        backgroundWorker.ReportProgress(100 * count / files.Count(), "");
                    }
                }
                else
                {
                    Console.WriteLine("Optimized palettes file already exists");
                    backgroundWorker.ReportProgress(100, "Optimized themes already exist...skipping");
                    Thread.Sleep(1000);
                }

                //extract oracle
                foreach (String name in referenceP.Keys)
                {
                    backgroundWorker.ReportProgress(0, "Extracting Oracle themes for..." + name);
                    String oracleFile = Path.Combine(evalDir, "oracle-" + name + ".tsv");
                    if (!File.Exists(oracleFile) || File.ReadLines(oracleFile).Count() != referenceP[name].Keys.Count() + 1)
                    {
                        File.WriteAllText(oracleFile, "");
                        File.AppendAllText(oracleFile, headers);
                        int count = 0;
                        foreach (String f in files)
                        {

                            String basename = new FileInfo(f).Name;
                            if (!referenceP[name].ContainsKey(basename))
                                continue;

                            count++;
                            PaletteData data = GetOracleTheme(basename, referenceP[name]);

                            //save to file
                            String colorString = data.ToString();
                            File.AppendAllText(oracleFile, count + "\t-1\t" + basename + "\t" + colorString + "\t5\t\n");
                            backgroundWorker.ReportProgress(100 * count / files.Count(), "");
                        }
                    }
                    else
                    {
                        Console.WriteLine("Oracle palettes file already exists");
                        backgroundWorker.ReportProgress(100, "Oracle themes already exist...skipping");
                        Thread.Sleep(1000);
                    }
                }



            });

            RunWorker();

        }


        private Dictionary<String, List<PaletteData>> LoadFilePalettes(String file)
        {
            //load art palettes
            var lines = File.ReadLines(file);

            Dictionary<String, List<PaletteData>> plist = new Dictionary<String, List<PaletteData>>();

            int count = 0;
            List<String> headers = new List<String>();

            foreach (String line in lines)
            {
                if (count == 0)
                {
                    count++;
                    headers = line.Replace("\"", "").Split('\t').ToList<String>();
                    continue;
                }

                String[] fields = line.Replace("\"", "").Split('\t');
                PaletteData data = new PaletteData();
                data.id = Int32.Parse(fields[headers.IndexOf("pid")]);
                data.workerNum = Int32.Parse(fields[headers.IndexOf("id")]);
                String key = fields[headers.IndexOf("image")];
                String[] colors = fields[headers.IndexOf("colors")].Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries);
                if (headers.IndexOf("log") >= 0)
                    data.log = fields[headers.IndexOf("log")];

                foreach (String s in colors)
                {
                    String[] comp = s.Split(',');
                    Color c = Color.FromArgb(Int32.Parse(comp[0]), Int32.Parse(comp[1]), Int32.Parse(comp[2]));
                    CIELAB l = Util.RGBtoLAB(c);
                    data.colors.Add(c);
                    data.lab.Add(l);
                }
                if (!plist.ContainsKey(key))
                    plist.Add(key, new List<PaletteData>());
                plist[key].Add(data);
            }
            return plist;
        }

        private void SavePaletteToImage(String dir, String key, String filename, PaletteData data)
        {
            int colorSize = 100;
            int numColors = data.colors.Count();
            int gridWidth = 10;
            int padding = 0;

            int imageSize = 500;

            Bitmap image = new Bitmap(Path.Combine(dir, key));

            int imageWidth = imageSize;
            int imageHeight = imageSize;

            if (image.Width > image.Height)
                imageHeight = (int)Math.Round(imageSize / (double)image.Width * image.Height);
            else
                imageWidth = (int)Math.Round(imageSize / (double)image.Height * image.Width);

            int width = Math.Max(colorSize * Math.Min(gridWidth, numColors), imageSize) + 2 * padding;
            int height = imageHeight + 3 * padding + colorSize * (int)(Math.Ceiling(numColors / (double)gridWidth));

            Bitmap bitmap = new Bitmap(width, height);
            Graphics g = Graphics.FromImage(bitmap);


            //fill with black
            g.FillRectangle(new SolidBrush(Color.Black), 0, 0, bitmap.Width, bitmap.Height);


            //draw image
            g.DrawImage(image, padding, padding, imageWidth, imageHeight);

            //draw out the clusters
            for (int i = 0; i < numColors; i++)
            {
                int row = (int)Math.Floor(i / (double)gridWidth);
                int col = i - row * gridWidth;
                Pen pen = new Pen(data.colors[i]);
                g.FillRectangle(pen.Brush, col * colorSize + padding, imageHeight + 2 * padding + row * colorSize, colorSize - padding, colorSize - padding);

                double brightness = pen.Color.GetBrightness();
                Brush brush = new SolidBrush(Color.White);
                if (brightness > 0.5)
                    brush = new SolidBrush(Color.Black);

            }

            bitmap.Save(filename);

        }



        private List<PaletteData> GenerateRandomPalettes(int count, List<PaletteData> palettes, PaletteData swatches)
        {
            PaletteScoreCache cache = new PaletteScoreCache(1000000);
            int k = 5;
            List<PaletteData> data = new List<PaletteData>();

            Random random = new Random();
            List<int> shuffled = new List<int>();
            for (int i = 0; i < swatches.lab.Count(); i++)
                shuffled.Add(i);


            //find the max scoring human palette
            double maxScore = Double.NegativeInfinity;

            foreach (PaletteData p in palettes)
            {
                double score = 1 - GetAvgDist(new List<PaletteData> { p }, palettes);
                maxScore = Math.Max(maxScore, score);
            }

            //generate random palettes to find the min score
            double minScore = Double.PositiveInfinity;
            for (int i = 0; i < 10000; i++)
            {

                PaletteData result = new PaletteData();
                result.id = -1;

                //pick k random colors. First shuffle the colors and pick the top k
                for (int j = shuffled.Count() - 1; j >= 0; j--)
                {
                    int idx = random.Next(j + 1);
                    int temp = shuffled[j];
                    shuffled[j] = shuffled[idx];
                    shuffled[idx] = temp;
                }

                for (int c = 0; c < k; c++)
                {
                    result.colors.Add(swatches.colors[shuffled[c]]);
                    result.lab.Add(swatches.lab[shuffled[c]]);
                }

                double score = 1 - GetAvgDist(new List<PaletteData>{result}, palettes);
                cache.SetScore(result.lab, score);

                minScore = Math.Min(minScore, score);
                maxScore = Math.Max(maxScore, score);
            }

            int numbins = 10;
            int[] counts = new int[numbins];
            int totalCount = 0;

            int thresh = count / numbins;
            int tries = 0;

            //generate random palettes, given swatches
            while (totalCount < count)
            {

                if (tries % 1000 == 0)
                {
                    String log = Path.Combine(trainOutDir, "randlog.txt");
                    Log(log, tries + "-" + totalCount + " binned ");
                    Log(log, " bins --- " + counts[0] + "," + counts[1] + "," + counts[2] + "," + counts[3] + "," + counts[4] + "," + counts[5] + "," + counts[6] + "," + counts[7] + "," + counts[8] + "," + counts[9]);
 
                    backgroundWorker.ReportProgress(-1,"bin counts " + String.Join(",",counts));
                }

                tries++;

                PaletteData result = new PaletteData();
                result.id = -1;

                //pick k random colors. First shuffle the colors and pick the top k
                for (int j = shuffled.Count() - 1; j >= 0; j--)
                {
                    int idx = random.Next(j + 1);
                    int temp = shuffled[j];
                    shuffled[j] = shuffled[idx];
                    shuffled[idx] = temp;
                }

                for (int c = 0; c < k; c++)
                {
                    result.colors.Add(swatches.colors[shuffled[c]]);
                    result.lab.Add(swatches.lab[shuffled[c]]);
                }

                double score = 0;
                if (cache.ContainsKey(result.lab))
                    score = cache.GetScore(result.lab);
                else
                {
                    score = 1 - GetAvgDist(new List<PaletteData> { result }, palettes);
                    cache.SetScore(result.lab, score);
                }

                score = (score - minScore) / (maxScore - minScore);

                int bin = (int)Math.Min(Math.Max(0, Math.Floor(score * numbins)), numbins - 1);


                if (counts[bin] >= thresh)
                    continue;
                totalCount++;
                counts[bin]++;

                data.Add(result);
            }
            return data;
        }

        /**
         * We're doing rejection sampling here, so this is pretty slow for getting palettes that are scored very highly or very lowly.
         * */
        private void SaveRandomPalettes(String filename, Dictionary<String, List<PaletteData>> palettes, PaletteExtractor extractor)
        {
            int numPalettes = 1000;
            if (File.Exists(filename) && File.ReadLines(filename).Count()==(palettes.Keys.Count()*numPalettes)+1)
            {
                Console.WriteLine("Binned random palettes already generated...skipping");
                backgroundWorker.ReportProgress(100, "Random palettes already generated from before...skipping");
                Thread.Sleep(1000);
                return;
            }

            int counter = -1;
            //for each key, generate random palettes and save them
            //pid, id, image, colors
            List<String> outLines = new List<String>();
            outLines.Add("pid\tid\timage\tcolors\tnumColors\tlog");
            int progress = 0;
            foreach (String key in palettes.Keys)
            {
                backgroundWorker.ReportProgress(-1, "Generating random palettes for " + key);
                List<PaletteData> rand = GenerateRandomPalettes(numPalettes, palettes[key], extractor.GetPaletteSwatches(key));
                for (int i = 0; i < rand.Count(); i++)
                {
                    List<String> colors = new List<String>();
                    foreach (Color c in rand[i].colors)
                    {
                        colors.Add(c.R + "," + c.G + "," + c.B);
                    }
                    outLines.Add(counter + "\t-1\t" + key + "\t" + String.Join(" ", colors.ToArray<String>()) + "\t5\t");
                    counter--;
                }
                progress++;
                backgroundWorker.ReportProgress(progress * 100 / palettes.Keys.Count(), "Generated Random Palettes for Image..."+progress+" of "+palettes.Keys.Count());

                Log(Path.Combine(trainOutDir, "randlog.txt"), key + "-" + DateTime.Now.ToString());
            }

            File.WriteAllLines(filename, outLines.ToArray<String>());
        }

        private void ClearLog(String log)
        {
            File.WriteAllText(log, "");
        }

        private void Log(String log, String text)
        {
            if (debugLogs)
                File.AppendAllText(log, text + "\n");
        }

        private void CalculateFeatures()
        {
            backgroundWorker.ReportProgress(0, "First generating binned random palettes...checking for existing file");

            PaletteExtractor extractor = new PaletteExtractor(trainInDir, weightsDir, json);

            Dictionary<String, List<PaletteData>> palettes = LoadFilePalettes(trainReference.Path);
            if (trainReference.Filter)
            {
                var keys = palettes.Keys.ToArray<String>();
                foreach (String key in keys)
                    palettes[key] = removeOutliers(palettes[key]);
            }

            SaveRandomPalettes(Path.Combine(trainOutDir, "random-binned.tsv"), palettes, extractor);
            Dictionary<String, List<PaletteData>> randomP = LoadFilePalettes(Path.Combine(trainOutDir, "random-binned.tsv"));

            ClearLog(Path.Combine(trainOutDir, "timelog.txt"));

            Features[] included = Enum.GetValues(typeof(Features)).Cast<Features>().ToArray<Features>();

            SortedSet<Features> headers = new SortedSet<Features>(included);
            String saliencyPattern = "_Judd";
            String imageNameFile = Path.Combine(trainOutDir, "imageNames.txt");
            String outFile = Path.Combine(trainOutDir, "features.csv");

            File.WriteAllText(outFile, ""); //clear the file
            File.WriteAllText(imageNameFile, "");

            //write the headings in a separate file
            File.WriteAllText(Path.Combine(trainOutDir, "featurenames.txt"), String.Join("\n", headers.Select<Features, String>((a) => featureName.Name(a))));
            String log = Path.Combine(trainOutDir,"log.txt");
            Log(log, "Start");

            backgroundWorker.ReportProgress(0, "Calculating features...");
            int progress = 0;
            foreach (String key in palettes.Keys)
            {
                List<String> sources = new List<String>();
                List<String> allFeatures = new List<String>();
                List<double> scores = new List<double>();

                List<double> tempScores = new List<double>();
                double maxScore = Double.NegativeInfinity;
                double minScore = Double.PositiveInfinity;

                List<PaletteData> all = new List<PaletteData>(palettes[key]);
                foreach (PaletteData r in randomP[key])
                    all.Add(r);

                FeatureParams fp = extractor.SetupFeatureParams(headers, key, saliencyPattern, debug);

                foreach (PaletteData data in all)
                {
                    //calculate the score
                    double rep = 1 - GetAvgDist(new List<PaletteData>{data}, palettes[key]);

                    tempScores.Add(rep);
                    maxScore = Math.Max(rep, maxScore);
                    minScore = Math.Min(rep, minScore);


                    Dictionary<Features, double> features = extractor.CalculateFeatures(data, fp);
                   
                    List<String> featureString = new List<String>();
                    foreach (Features f in headers)
                        featureString.Add(features[f].ToString());
                    allFeatures.Add(String.Join(",", featureString.ToArray<String>()));
                    sources.Add(data.id.ToString());
                }

                //normalize the scores
                foreach (double score in tempScores)
                    scores.Add((score - minScore) / (maxScore - minScore));

                List<String> outLines = new List<String>();
                //write the scores thus far
                for (int i = 0; i < scores.Count(); i++)
                {
                    outLines.Add(sources[i] + "," + scores[i] + "," + allFeatures[i]);
                }
                File.AppendAllLines(outFile, outLines.ToArray<String>());

                Log(log, key + " - " + DateTime.Now.ToString() + "\n");
                File.AppendAllText(imageNameFile, key + "\n");

                progress++;
                backgroundWorker.ReportProgress(100*progress/palettes.Keys.Count(), String.Format("Calculated features for {0} ({1}/{2})",key,progress,palettes.Keys.Count()));
            }
         

        }


        /**
         * Return filtered themes with farthest themes removed
         */ 
        private List<PaletteData> removeOutliers(List<PaletteData> orig)
        {
            //Remove the bottom 25% palettes from each image
            int n = (int)(0.25*orig.Count);
            List<PaletteData> data = SortByDist(orig);
            return data.Take(data.Count() - n).OrderBy(p => p.id).ToList<PaletteData>();
        }

        /**
         * Return sorted list of themes by average distance to each other
         */ 
        private List<PaletteData> SortByDist(List<PaletteData> orig)
        {
            List<PaletteData> list = new List<PaletteData>();
            foreach (PaletteData d in orig)
                list.Add(new PaletteData(d));
            Dictionary<int, double> pToScore = new Dictionary<int, double>();
            foreach (PaletteData data in list)
            {
                double dist = GetAvgDist(new List<PaletteData> { data }, list);
                pToScore.Add(data.id, dist);
            }
            list.Sort((a, b) => Math.Sign(pToScore[a.id] - pToScore[b.id]));
            return list;
        }

        /**
         * Return sorted list of themes by average overlap to each other
         */
        private List<PaletteData> SortByOverlap(List<PaletteData> orig)
        {
            List<PaletteData> list = new List<PaletteData>();
            foreach (PaletteData d in orig)
                list.Add(new PaletteData(d));
            Dictionary<int, double> pToScore = new Dictionary<int, double>();
            foreach (PaletteData data in list)
            {
                double dist = GetAvgOverlap(new List<PaletteData> { data }, list, 1);
                pToScore.Add(data.id, dist);
            }
            list.Sort((a, b) => -1*Math.Sign(pToScore[a.id] - pToScore[b.id]));
            return list;
        }

        /**
         * Compute average distance from one list of themes to another list of themes
         * same - indicates if the lists are the same (and in the same order), so themes should not be compared to themselves
         */ 
        private double GetAvgDist(List<PaletteData> from, List<PaletteData> to, bool same=false)
        {
            double overlap = 0;
            int count = 0;
            for (int i = 0; i < from.Count(); i++)
            {
                for (int j = 0; j < to.Count(); j++)
                {
                    if (!same || (same && i != j)) //this is to prevent double-counting. Was not here for the published version of the paper
                    {
                        count++;
                        int[] matching = new int[Math.Min(from[i].colors.Count(), to[j].colors.Count())];
                        double dist = PaletteDistanceHungarian(from[i], to[j], ref matching, false);
                        overlap += dist;
                    }
                }
            }

            if (count == 0)
                return Double.PositiveInfinity;

            overlap /= count;
            return 1000*overlap; //re-scale to LAB units
        }


        private double PaletteDistanceHungarian(PaletteData a, PaletteData b, ref int[] matching, bool print, int thresh = 1000)
        {
            double score = 0;
            int maxDist = thresh;

            //Make a the smaller palette
            if (a.colors.Count() > b.colors.Count())
            {
                //swap
                PaletteData temp = a;
                a = b;
                b = temp;
            }

            //do the matching
            List<int> foundMatching = HungarianAlgorithmColors(a.lab, b.lab, maxDist, false, print);
            matching = foundMatching.ToArray<int>();

            Debug.Assert(foundMatching.Count() == a.colors.Count());

            //find the score
            for (int i = 0; i < foundMatching.Count(); i++)
            {
                int idx = foundMatching[i];
                if (idx < b.lab.Count())
                {

                    double dist = Math.Sqrt(a.lab[i].SqDist(b.lab[idx]));
                    score += dist;
                }
                else
                {
                    score += maxDist;
                    matching[i] = -1;
                }
            }

            int nullDistances = (b.colors.Count() - a.colors.Count()) * maxDist;

            //normalize between 0 and 1
            return ((score + nullDistances) / b.lab.Count()) / maxDist;
        }

        //Implement the Hungarian Algorithm, the Matrix version, on colors
        //From the description on: http://www.wikihow.com/Use-the-Hungarian-Algorithm
        private List<int> HungarianAlgorithmColors(List<CIELAB> a, List<CIELAB> b, double nullScore, bool test = false, bool print = false)
        {
            List<int> assignments = new List<int>();
            int n = a.Count() + b.Count();

            //fill the matrix
            double[,] matrix = new double[n, n];
            for (int i = 0; i < n; i++)
            {
                for (int j = 0; j < n; j++)
                {
                    if (i < a.Count() && j < b.Count())
                    {
                        matrix[i, j] = Math.Sqrt(a[i].SqDist(b[j]));
                    }
                    else
                    {
                        matrix[i, j] = nullScore;
                    }
                }
            }

            assignments = HungarianAlgorithm.Solve(matrix, test, print);

            return assignments.Take<int>(a.Count()).ToList<int>();
        }

        
        /**
         * Compute average overlap from one list of themes to another list of themes
         * same - indicates if the lists are the same (and in the same order), so themes should not be compared to themselves
         */
        private double GetAvgOverlap(List<PaletteData> from, List<PaletteData> to, int thresh, bool same=false)
        {
            double overlap = 0;
            int count = 0;
            for (int i = 0; i < from.Count(); i++)
            {
                for (int j = 0; j < to.Count(); j++)
                {
                    if (!same || (same && i != j)) //this is to prevent double-counting. Was not here in the published version
                    {
                        count++;
                        int[] matching = new int[Math.Min(from[i].colors.Count(), to[j].colors.Count())];

                        double dist = PaletteDistanceHungarian(from[i], to[j], ref matching, false, thresh);
                        overlap += PaletteOverlap(from[i], to[j], matching);
                    }
                }
            }
            overlap /= count;
            return overlap;
        }

        private double PaletteOverlap(PaletteData a, PaletteData b, int[] matching)
        {
            //return number of colors in common over the number of colors in the larger palette
            int common = 0;
            foreach (int idx in matching)
                if (idx != -1)
                    common++;
            return common / (double)(Math.Max(a.colors.Count(), b.colors.Count()));
        }

      
        /**
         * Run c-means clustering on the given image file, and return the theme
         */ 
        private PaletteData CMeansPalette(String file, int k=5)
        {
            PaletteData result = new PaletteData();
            List<CIELAB> colors = Util.BitmapTo1DArray(Util.GetImage(file,debug)).Select(c => Util.RGBtoLAB(c)).ToList<CIELAB>();
            List<Cluster> final = null;
            double bestScore = Double.PositiveInfinity;

            int trials = 5;
            for (int t = 0; t < trials; t++)
            {
                List<Cluster> seeds = Clustering.InitializePictureSeeds(colors, k);
                double score = Clustering.CMeansPicture(colors, seeds);
                if (score < bestScore)
                {
                    final = seeds;
                    bestScore = score;
                }
            }

            foreach (Cluster c in final)
            {
                result.colors.Add(Util.LABtoRGB(c.lab));
                result.lab.Add(c.lab);
            }

            return result;
        }


        /**
         * Run k-means clustering on the given image file, and return the theme
         */ 
        private PaletteData KMeansPalette(String file, int k=5)
        {
            PaletteData result = new PaletteData();
            List<CIELAB> colors = Util.BitmapTo1DArray(Util.GetImage(file, debug)).Select(c => Util.RGBtoLAB(c)).ToList<CIELAB>();
            List<Cluster> final = null;
            double bestScore = Double.PositiveInfinity;

            int trials = 5;
            for (int t = 0; t < trials; t++)
            {
                List<Cluster> seeds = Clustering.InitializePictureSeeds(colors, k);
                double score = Clustering.KMeansPicture(colors, seeds);
                if (score < bestScore)
                {
                    final = seeds;
                    bestScore = score;
                }
            }

            foreach (Cluster c in final)
            {
                result.colors.Add(Util.LABtoRGB(c.lab));
                result.lab.Add(c.lab);
            }

            return result;
        }


        /**
         * Randomly pick image swatches, and return the theme
         */ 
        private List<PaletteData> GenerateRandomPalettesNonBinned(int count, PaletteData swatches)
        {
            int k = 5;
            List<PaletteData> data = new List<PaletteData>();

            Random random = new Random();
            List<int> shuffled = new List<int>();
            for (int i = 0; i < swatches.lab.Count(); i++)
                shuffled.Add(i);

            for (int i = 0; i < count; i++)
            {

                PaletteData result = new PaletteData();
                result.id = -1;

                //pick k random colors. First shuffle the colors and pick the top k
                for (int j = shuffled.Count() - 1; j >= 0; j--)
                {
                    int idx = random.Next(j + 1);
                    int temp = shuffled[j];
                    shuffled[j] = shuffled[idx];
                    shuffled[idx] = temp;
                }

                for (int c = 0; c < k; c++)
                {
                    result.colors.Add(swatches.colors[shuffled[c]]);
                    result.lab.Add(swatches.lab[shuffled[c]]);
                }
                data.Add(result);
            }
            return data;


        }

        private void debugBox_CheckedChanged(object sender, EventArgs e)
        {
            debug = this.debugBox.Checked;
        }


        /**
         * Find the theme closest on average to a given set of themes (compare[key])
         */ 
        private PaletteData GetOracleTheme(String key, Dictionary<String, List<PaletteData>> compare)
        {
            //find the palette that has the lowest distance to all other palettes
            int trials = 20;

            PaletteExtractor extractor = new PaletteExtractor(evalDir, weightsDir, json);
            PaletteData swatches = extractor.GetPaletteSwatches(key);

            List<CIELAB> shuffled = new List<CIELAB>();
            foreach (CIELAB c in swatches.lab)
                shuffled.Add(c);

            Random random = new Random();

            PaletteData best = new PaletteData();
            double bestScore = Double.NegativeInfinity;


            Stopwatch watch = new Stopwatch();

            //Generate all the random starts first
            List<PaletteData> starts = new List<PaletteData>();
            PaletteData[] allOptions = new PaletteData[trials];
            double[] allScores = new double[trials];


            for (int t = 0; t < trials; t++)
            {
                //setup
                PaletteData option = new PaletteData();

                //pick k random colors. First shuffle the colors and pick the top k
                for (int j = shuffled.Count() - 1; j >= 0; j--)
                {
                    int idx = random.Next(j + 1);
                    CIELAB temp = shuffled[j];
                    shuffled[j] = shuffled[idx];
                    shuffled[idx] = temp;
                }

                for (int i = 0; i < 5; i++)
                {
                    option.lab.Add(shuffled[i]);
                    option.colors.Add(Util.LABtoRGB(shuffled[i]));
                }
                starts.Add(option);
                allOptions[t] = new PaletteData(option);
                double optionScore = 1 - GetAvgDist(new List<PaletteData> { option }, compare[key]);
                allScores[t] = optionScore;
            }

            watch.Restart();

            Parallel.For(0, trials, t =>
            {
                //setup
                PaletteData option = new PaletteData(starts[t]);

                double optionScore = allScores[t];


                //Now hill climb, for each swatch, consider replacing it with a better swatch
                //Pick the best replacement, and continue until we reach the top of a hill
                int changes = 1;
                int iters = 0;

                watch.Restart();
                while (changes > 0)
                {
                    changes = 0;

                    for (int i = 0; i < option.lab.Count(); i++)
                    {
                        //find the best swatch replacement for this color
                        double bestTempScore = optionScore;
                        CIELAB bestRep = option.lab[i];

                        double[] scores = new double[swatches.lab.Count()];

                        for (int s = 0; s < swatches.lab.Count(); s++)
                        {
                            CIELAB r = swatches.lab[s];

                            PaletteData temp = new PaletteData(option);
                            if (!temp.lab.Contains(r))
                            {

                                temp.lab[i] = r;
                                temp.colors[i] = swatches.colors[s];

                                double tempScore = 1 - GetAvgDist(new List<PaletteData> { temp }, compare[key]);
                                scores[s] = tempScore;
                            }
                            else
                            {
                                scores[s] = Double.NegativeInfinity;
                            }
                        }

                        //aggregate results
                        for (int s = 0; s < scores.Count(); s++)
                        {
                            if (scores[s] > bestTempScore)
                            {
                                bestTempScore = scores[s];
                                bestRep = swatches.lab[s];
                            }
                        }


                        if (!option.lab[i].Equals(bestRep))
                        {
                            option.lab[i] = bestRep;
                            optionScore = bestTempScore;
                            changes++;
                        }
                    }

                    iters++;
                }



                if (optionScore > allScores[t])
                {
                    allOptions[t] = option;
                    allScores[t] = optionScore;
                } 
            });

            //aggregate scores
            for (int i = 0; i < allScores.Count(); i++)
            {
                if (allScores[i] > bestScore)
                {
                    bestScore = allScores[i];
                    best = allOptions[i];
                }
            }

            //convert best lab to rgb
            best.colors = new List<Color>();
            foreach (CIELAB l in best.lab)
                best.colors.Add(Util.LABtoRGB(l));

            return best;


        }

        /**
         * Create diagrams of the themes showing source image, swatches, and aligned themes
         */ 
        private void diagramThemes_Click(object sender, EventArgs e)
        {
            if (backgroundWorker.IsBusy)
            {
                statusBox.Text = "Please wait...";
                return;
            }

            backgroundWorker = MakeBackgroundWorker(delegate
            {
                backgroundWorker.ReportProgress(0, "Diagramming themes...");

                //create directory
                String diagramDir = Path.Combine(evalDir, "diagrams");
                Directory.CreateDirectory(diagramDir);

                Dictionary<String, Dictionary<String, List<PaletteData>>> leftP = ItemsToDictionary(diagramLeft);
                Dictionary<String, Dictionary<String, List<PaletteData>>> rightP = ItemsToDictionary(diagramRight);

                int imageWidth = 200;
                int colorSize = 20;
                int padding = 5;
                int gutter = 50;
                int labelTop = 100;

                PaletteExtractor extractor = new PaletteExtractor(evalDir, weightsDir, json);

                SortedSet<String> keys = new SortedSet<String>();
                foreach (String key in leftP.Keys)
                    keys.UnionWith(leftP[key].Keys);
                foreach (String key in rightP.Keys)
                    keys.UnionWith(rightP[key].Keys);

                int progress = 0;
                foreach (String key in keys)
                {
                    
                    //check total number of themes to draw
                    int totalThemes = 0;
                    PaletteData swatches = extractor.GetPaletteSwatches(key);

                    //Count total number of themes, and sort the themes by average distance
                    foreach (String n in leftP.Keys)
                    {
                        if (!leftP[n].ContainsKey(key))
                            continue;
                        totalThemes += leftP[n][key].Count();
                        leftP[n][key] = SortByDist(leftP[n][key]);
                    }

                    foreach (String n in rightP.Keys)
                    {
                        if (!rightP[n].ContainsKey(key))
                            continue;
                        totalThemes += rightP[n][key].Count();
                        rightP[n][key] = SortByDist(rightP[n][key]);
                    }

                    int width = imageWidth + (leftP.Keys.Count() + rightP.Keys.Count() + 2) * gutter + colorSize * totalThemes;
                    int height = labelTop + swatches.colors.Count()*colorSize;

                    Bitmap result = new Bitmap(width, height);
                    Graphics g = Graphics.FromImage(result);
                    Bitmap image = new Bitmap(Path.Combine(evalDir, key));

                    //draw the source image
                    g.DrawImage(image, 0, labelTop, imageWidth, imageWidth*image.Height / image.Width);


                    int startX = imageWidth+gutter;
                    
                    //draw the themes to the left of the swatches
                    foreach (EvalItem item in diagramLeft)
                    {
                        String name = item.Name;

                        if (!leftP[name].ContainsKey(key))
                            continue;

                        DrawThemes(g, name, leftP[name][key], swatches, startX, labelTop, colorSize, padding);
                        startX += gutter + leftP[name][key].Count()*colorSize;
                    }

                    //draw the swatches
                    for (int c = 0; c < swatches.colors.Count; c++)
                    {
                        int y = c * colorSize+labelTop;
                        g.FillRectangle(new SolidBrush(swatches.colors[c]), startX, y, colorSize - padding, colorSize - padding);
                        g.DrawRectangle(new Pen(Color.Black), startX, y, colorSize - padding, colorSize - padding);
                    }

                    startX += colorSize+gutter;
                    //draw the themes to the right of the swatches
                    foreach (EvalItem item in diagramRight)
                    {
                        String name = item.Name;
                        if (!rightP[name].ContainsKey(key))
                            continue;
                        DrawThemes(g, name, rightP[name][key], swatches, startX, labelTop, colorSize, padding);
                        startX += gutter+ rightP[name][key].Count()*colorSize;
                    }

                    result.Save(Path.Combine(diagramDir, key));
                    progress++;

                    backgroundWorker.ReportProgress(100*progress / keys.Count, "Diagrammed " + key);

                }
 
            });

            RunWorker();

        }

        /**
         * Draw the themes aligned to the swatches, assume themes are already sorted in the right order
         */ 
        private void DrawThemes(Graphics g, String name, List<PaletteData> themes, PaletteData swatches, int startX, int labelTop, int colorSize, int padding)
        {
            g.DrawString(name, new Font("Arial", 12.0f), new SolidBrush(Color.Black), startX+(colorSize*(themes.Count()-1)/2) - colorSize/2, labelTop/2);

            int x = startX;
            int y = labelTop;

            for (int i = 0; i < themes.Count(); i++)
            {
                int[] matching = new int[themes[i].colors.Count];
                PaletteDistanceHungarian(themes[i], swatches, ref matching, false);

                for (int c = 0; c < themes[i].colors.Count; c++)
                {
                    if (matching[c] < 0)
                    {
                        backgroundWorker.ReportProgress(-1, "Error: Some elements not matched");
                        continue;
                    }

                    y = labelTop + matching[c] * colorSize;
                    g.FillRectangle(new SolidBrush(themes[i].colors[c]), x, y, colorSize - padding, colorSize - padding);
                    g.DrawRectangle(new Pen(Color.Black), x, y, colorSize - padding, colorSize - padding);
                }
                x = startX + colorSize * (i+1);
            }

        }



    }
}
