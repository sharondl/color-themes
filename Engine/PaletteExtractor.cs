using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using Engine;
using System.Drawing.Drawing2D;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;

namespace Engine
{

    //Cache to keep track of palettes and their scores
    public class PaletteScoreCache
    {
        private Dictionary<String, double> cache;
        private double capacity;

        public PaletteScoreCache(int limit=-1)
        {
            cache = new Dictionary<String, double>();
            if (limit >= 0)
                capacity = limit;
            else
                capacity = Double.PositiveInfinity;
        }

        //order doesn't matter
        private String GetKey(List<CIELAB> lab)
        {
            var sorted = lab.OrderBy(c => c.L).ThenBy(c => c.A).ThenBy(c => c.B);
            return String.Join(",",sorted.Select(c => c.ToString()));
        }

        public bool ContainsKey(List<CIELAB> lab)
        {
            return cache.ContainsKey(GetKey(lab));
        }

        public double GetScore(List<CIELAB> lab)
        {
            return cache[GetKey(lab)];
        }

        public void SetScore(List<CIELAB> lab, double score)
        {
            if (cache.Keys.Count() >= capacity)
                return;
            cache[GetKey(lab)] = score;
        }


    }

    public enum Features
    {
        //recoloring error
        //pixel
        ErrorUnweighted, SqErrorUnweighted, NError, NSqError, SoftError, NSoftError,
        ErrorWeightedSaliency, SqErrorWeightedSaliency, NErrorWeightedSaliency, NSqErrorW, SoftWError, NSoftWError,
        //segment
        ErrorUnweightedSegment, SqErrorSeg, NErrorSeg, NSqErrorSeg, SoftErrorSeg, NSoftErrorSeg,
        ErrorWeightedSegment, SqErrorWSeg, NErrorWSeg, NSqErrorWSeg, SoftErrorWSeg, NSoftErrorWSeg,
        ErrorSegSD, SqErrorSegSD, NErrorSegSD, NSqErrorSegSD, SoftErrorSegSD, NSoftErrorSegSD,
        //segmentmean
        MeanSegError, NMeanSegError,
        MeanSegErrorWeighted, NMeanSegErrorW,
        MeanSegErrorDensity, NMeanSegErrorDensity,
        //range coverage
        LCov, ACov, BCov, SatCov,
        //segment uniqueness
        ColorsPerSeg, ColorsPerSegWeighted,

        //diversity
        DMin, DMax, DAvg, DClosestAvg, DMinR, DMaxR, DAvgR, DClosestAvgR,
        NDiffMin, NDiffMax, NDiffAvg, NClosestAvg, NDiffMinR, NDiffMaxR, NDiffAvgR, NClosestAvgR,

        //nameability
        NSaliencyMin, NSaliencyMax, NSaliencyAvg,
        NSaliencyMinR, NSaliencyMaxR, NSaliencyAvgR,

        //impurity
        PurityMin, PurityMax, PurityAvg,
        PurityMinR, PurityMaxR, PurityAvgR,

        //saliency
        SaliencyTotal, SaliencyDensMin,
        SaliencyDensMax, SaliencyDensAvg, SaliencyDensClustMin, SaliencyDensClustMax,
        SaliencyDensClustAvg,

        //cluster statistics
        WithinVar, BetweenVar
    }

    public class FeatureNamePair
    {
        public String Name;
        public Features Feature;
        public FeatureNamePair(Features f, String n)
        {
            Name = n;
            Feature = f;
        }
    }

    public class FeatureParams
    {
        public SortedSet<Features> included;
        public CIELAB[,] imageLAB;
        public double[,] map;
        public double[] colorToSaliency;
        public int[] colorToCounts;
        public double totalSaliency;
        public ColorNames colorNames;
        public double factor;
        public double[] colorToCNSaliency;
        public double minE;
        public double maxE;
        public Dictionary<CIELAB, int> swatchIndexOf;
        public Dictionary<Tuple<int, int>, double> imageSwatchDist;
        public double afactor;
        public double[] swatchToPurity;
        public Dictionary<int, double> pToScore;
        public double maxN;
        public double avgN;
        public double avgE;
        public double[] segmentToSaliency;
        public SortedSet<Features> CoverageFeatures;
        public SortedSet<Features> NameCovFeatures;
        public SortedSet<Features> SaliencyDensFeatures;
        public SortedSet<Features> SegmentFeatures;
        public Segmentation seg;
        public double Lspan;
        public double Aspan;
        public double Bspan;
        public double Satspan;
        public int[,] binAssignments;
        public int[] swatchIdxToBin;
        public double totalCapturableSaliency;
        public double[,] pixelToDists;
        public double[,] pixelToNDists;
        public double totalSD;
        public double[] segmentToSD;



        public FeatureParams()
        {
            CoverageFeatures = new SortedSet<Features>(new Features[] {
                    Features.ErrorUnweighted,
                    Features.ErrorWeightedSaliency,
                    Features.ErrorUnweighted,
                    Features.ErrorWeightedSegment,
                    Features.ErrorUnweightedSegment,
                    Features.BetweenVar, 
                    Features.WithinVar,
                    Features.SqErrorUnweighted, 
                    Features.SqErrorWeightedSaliency, 
                    Features.SqErrorWSeg, 
                    Features.SqErrorSeg,

                    Features.SoftError, 
                    Features.SoftWError, 
                    Features.SoftErrorSeg,
                    Features.SoftErrorWSeg,
                    Features.SoftErrorSegSD,
                    
                    Features.SqErrorSegSD,
                    Features.ErrorSegSD
                
                
    
                });
            NameCovFeatures = new SortedSet<Features>(new Features[] { 
                    Features.NError,
                    Features.NErrorWeightedSaliency,
                    Features.NSqError,
                    Features.NSqErrorSeg,
                    Features.NSqErrorW,
                    Features.NSqErrorWSeg,

                    Features.NSqErrorSegSD,
                    Features.NErrorSegSD,
                   
                                
                    Features.NSoftError, 
                    Features.NSoftWError});
            SaliencyDensFeatures = new SortedSet<Features>(new Features[] {
                    Features.SaliencyDensClustAvg,
                    Features.SaliencyDensClustMax,
                    Features.SaliencyDensClustMin,
                    Features.SaliencyDensAvg,
                    Features.SaliencyDensMax,
                    Features.SaliencyDensMin
                });

            SegmentFeatures = new SortedSet<Features>(new Features[]{
                    Features.ColorsPerSeg,
                    Features.ColorsPerSegWeighted,
                    Features.MeanSegError,
                    Features.MeanSegErrorWeighted,
                    Features.NMeanSegError,
                    Features.NMeanSegErrorW,
                    
                    Features.SoftErrorSeg,
                    Features.SoftErrorWSeg,
                    Features.SoftErrorSegSD,
                    Features.SoftErrorSegSD,
                    
                    Features.SqErrorSegSD,
                    Features.ErrorSegSD
                });

            included = new SortedSet<Features>();
            swatchIndexOf = new Dictionary<CIELAB, int>();
            imageSwatchDist = new Dictionary<Tuple<int, int>, double>();
            pToScore = new Dictionary<int, double>();
        }


    }

    public class FeatureName
    {
        private Dictionary<Features, String> featureToName;
        private Dictionary<String, Features> nameToFeature;

        public FeatureName()
        {
            //add the features to names here
            FeatureNamePair[] map = new FeatureNamePair[]{
                    new FeatureNamePair(Features.ErrorWeightedSaliency, "error-pixel-hard-saliency-lab"),
                    new FeatureNamePair(Features.ErrorUnweighted, "error-pixel-hard-uniform-lab"),
                    new FeatureNamePair(Features.NError, "error-pixel-hard-uniform-names"),
                    new FeatureNamePair(Features.NErrorWeightedSaliency, "error-pixel-hard-saliency-names"),
                    new FeatureNamePair(Features.DMin, "diversity-lab-normalizeMax-min"),
                    new FeatureNamePair(Features.DMax, "diversity-lab-normalizeMax-max"),
                    new FeatureNamePair(Features.DAvg, "diversity-lab-normalizeMax-mean"),
                    new FeatureNamePair(Features.DClosestAvg, "diversity-lab-closest"),
                    new FeatureNamePair(Features.NDiffMin, "diversity-names-normalizeMax-min"),
                    new FeatureNamePair(Features.NDiffMax, "diversity-names-normalizeMax-max"),
                    new FeatureNamePair(Features.NDiffAvg, "diversity-names-normalizeMax-mean"),
                    new FeatureNamePair(Features.NClosestAvg, "diversity-names-mean"),
                    new FeatureNamePair(Features.NSaliencyAvg, "nameability-normalizeMax-mean"),
                    new FeatureNamePair(Features.NSaliencyMin, "nameability-normalizeMax-min"),
                    new FeatureNamePair(Features.NSaliencyMax, "nameability-normalizeMax-max"),
                    new FeatureNamePair(Features.SaliencyDensMin, "saliency-swatch-min"),
                    new FeatureNamePair(Features.SaliencyDensMax, "saliency-swatch-max"),
                    new FeatureNamePair(Features.SaliencyDensAvg, "saliency-swatch-mean"),
                    new FeatureNamePair(Features.SaliencyTotal, "saliency-swatch-total"),
                    new FeatureNamePair(Features.SaliencyDensClustMin, "saliency-theme-min"),
                    new FeatureNamePair(Features.SaliencyDensClustMax, "saliency-theme-max"),
                    new FeatureNamePair(Features.SaliencyDensClustAvg, "saliency-theme-mean"),
                    new FeatureNamePair(Features.DMaxR, "diversity-lab-normalizeMean-max"),
                    new FeatureNamePair(Features.DMinR, "diversity-lab-normalizeMean-min"),
                    new FeatureNamePair(Features.DAvgR, "diversity-lab-normalizeMean-mean"),
                    new FeatureNamePair(Features.DClosestAvgR, "diversity-lab-normalizeMean-closest"),
                    new FeatureNamePair(Features.PurityMin, "impurity-normalizeMax-min"),
                    new FeatureNamePair(Features.PurityMax, "impurity-normalizeMax-max"),
                    new FeatureNamePair(Features.PurityAvg, "impurity-normalizeMax-mean"),
                    new FeatureNamePair(Features.PurityAvgR, "impurity-normalizeMean-mean"),
                    new FeatureNamePair(Features.PurityMinR, "impurity-normalizeMean-min"),
                    new FeatureNamePair(Features.PurityMaxR, "impurity-normalizeMean-max"),
                    new FeatureNamePair(Features.NDiffMinR, "diversity-names-normalizeMean-min"),
                    new FeatureNamePair(Features.NDiffMaxR, "diversity-names-normalizeMean-max"),
                    new FeatureNamePair(Features.NDiffAvgR, "diversity-names-normalizeMean-mean"),
                    new FeatureNamePair(Features.NClosestAvgR, "diversity-names-normalizeMean-closest"),
                    new FeatureNamePair(Features.NSaliencyAvgR, "nameability-normalizeMean-mean"),
                    new FeatureNamePair(Features.NSaliencyMaxR, "nameability-normalizeMean-max"),
                    new FeatureNamePair(Features.NSaliencyMinR, "nameability-normalizeMean-min"),
                    new FeatureNamePair(Features.ErrorWeightedSegment, "error-segment-hard-saliency-lab"),
                    new FeatureNamePair(Features.ErrorUnweightedSegment, "error-segment-hard-uniform-lab"),
                    new FeatureNamePair(Features.ColorsPerSeg, "segmentunique-uniform"),
                    new FeatureNamePair(Features.ColorsPerSegWeighted, "segmentunique-saliency"),
                    new FeatureNamePair(Features.MeanSegError, "error-segmentmean-hard-uniform-lab"),
                    new FeatureNamePair(Features.MeanSegErrorWeighted, "error-segmentmean-hard-saliency-lab"),
                    new FeatureNamePair(Features.LCov, "rangecov-l"),
                    new FeatureNamePair(Features.ACov, "rangecov-a"),
                    new FeatureNamePair(Features.BCov, "rangecov-b"),
                    new FeatureNamePair(Features.SatCov, "rangecov-saturation"),

                    new FeatureNamePair(Features.BetweenVar, "clusterstats-betweenvar"),
                    new FeatureNamePair(Features.WithinVar, "clusterstats-withinvar"),
                    new FeatureNamePair(Features.SqErrorUnweighted, "error-pixel-hard-uniform-sqlab"),
                    new FeatureNamePair(Features.SqErrorWeightedSaliency, "error-pixel-hard-saliency-sqlab"),
                    new FeatureNamePair(Features.SqErrorWSeg, "error-segment-hard-saliency-sqlab"),
                    new FeatureNamePair(Features.SqErrorSeg, "error-segment-hard-uniform-sqlab"),

                    new FeatureNamePair(Features.NErrorSeg, "error-segment-hard-uniform-names"),
                    new FeatureNamePair(Features.NErrorWSeg, "error-segment-hard-saliency-names"),
                    
                    new FeatureNamePair(Features.NSqErrorSeg, "error-segment-hard-uniform-sqnames"),
                    new FeatureNamePair(Features.NSqErrorWSeg, "error-segment-hard-saliency-sqnames"),
                    new FeatureNamePair(Features.NSqError, "error-pixel-hard-uniform-sqnames"),
                    new FeatureNamePair(Features.NSqErrorW, "error-pixel-hard-saliency-sqnames"),

                    new FeatureNamePair(Features.SoftError, "error-pixel-soft-uniform-lab"),
                    new FeatureNamePair(Features.SoftWError, "error-pixel-soft-saliency-lab"),
                    new FeatureNamePair(Features.NSoftError, "error-pixel-soft-uniform-names"),
                    new FeatureNamePair(Features.NSoftWError, "error-pixel-soft-saliency-names"),
                    new FeatureNamePair(Features.SoftErrorSeg, "error-segment-soft-uniform-lab"),
                    new FeatureNamePair(Features.SoftErrorWSeg, "error-segment-soft-saliency-lab"),
                    new FeatureNamePair(Features.NSoftErrorSeg, "error-segment-soft-uniform-names"),
                    new FeatureNamePair(Features.NSoftErrorWSeg, "error-segment-soft-saliency-names"),
                    new FeatureNamePair(Features.NMeanSegError, "error-segmentmean-hard-uniform-names"),
                    new FeatureNamePair(Features.NMeanSegErrorW, "error-segmentmean-hard-saliency-names"),
                    new FeatureNamePair(Features.NMeanSegErrorDensity, "error-segmentmean-hard-saliencydens-names"),
                    new FeatureNamePair(Features.MeanSegErrorDensity, "error-segmentmean-hard-saliencydens-lab"),

                    new FeatureNamePair(Features.SoftErrorSegSD, "error-segment-soft-saliencydens-lab"),
                    new FeatureNamePair(Features.NSoftErrorSegSD, "error-segment-soft-saliencydens-names"),
                    new FeatureNamePair(Features.SqErrorSegSD, "error-segment-hard-saliencydens-sqlab"),
                    new FeatureNamePair(Features.ErrorSegSD, "error-segment-hard-saliencydens-lab"),
                    new FeatureNamePair(Features.NErrorSegSD, "error-segment-hard-saliencydens-names"),
                    new FeatureNamePair(Features.NSqErrorSegSD, "error-segment-hard-saliencydens-sqnames"),
                };

            featureToName = new Dictionary<Features, String>();
            nameToFeature = new Dictionary<String, Features>();

            //populate the dictionaries
            foreach (FeatureNamePair pair in map)
            {
                featureToName.Add(pair.Feature, pair.Name);
                nameToFeature.Add(pair.Name, pair.Feature);
            }

        }

        public String Name(Features f)
        {
            return featureToName[f];
        }

        public Features Feature(String s)
        {
            return nameToFeature[s];
        }
    }

    public class Segmentation
    {
        public int[,] assignments;
        public int numSegments; 
        public int[] counts;
        public CIELAB[] segToMeanColor;


        public Segmentation()
        {
        }

        public Segmentation(int n, int width, int height)
        {
            numSegments = n;
            counts = new int[n];
            segToMeanColor = new CIELAB[n];
            assignments = new int[width, height];
        }

        public void LoadFromFile(String imageFile, String segFile)
        {
            Bitmap image = new Bitmap(imageFile);
            Bitmap map = new Bitmap(segFile);

            int width = image.Width;
            int height = image.Height;
            
            Bitmap resized = Util.ResizeBitmapNearest(map, width, height);

            assignments = new int[width, height];

            List<Color> unique = new List<Color>();
            for (int i = 0; i < width; i++)
            {
                for (int j = 0; j < height; j++)
                {
                    Color c = resized.GetPixel(i, j);
                    if (!unique.Contains(c))
                        unique.Add(c);

                    int idx = unique.IndexOf(c);
                    assignments[i, j] = idx;
                }
            }

            numSegments = unique.Count();
            counts = new int[numSegments];
            segToMeanColor = new CIELAB[numSegments];
            for (int i = 0; i < numSegments; i++)
            {
                segToMeanColor[i] = new CIELAB();
            }
            for (int i = 0; i < width; i++)
            {
                for (int j = 0; j < height; j++)
                {

                    int idx = assignments[i, j];
                    counts[idx]++;
                    segToMeanColor[idx] += Util.RGBtoLAB(image.GetPixel(i, j));
                }
            }

            for (int i = 0; i < numSegments; i++)
            {
                segToMeanColor[i] /= counts[i];
            }

        }
    }

    public class PaletteData
    {
        public List<Color> colors;
        public List<CIELAB> lab;
        public List<Point> points;
        public int workerNum;
        public int seconds;
        public int id;
        public String log;
        public String workerName;

        public PaletteData()
        {
            colors = new List<Color>();
            lab = new List<CIELAB>();
            points = new List<Point>();
            workerNum = -1;
            seconds = 0;
        }

        public PaletteData(PaletteData other)
        {
            colors = new List<Color>(other.colors);
            lab = new List<CIELAB>(other.lab);
            id = other.id;
        }

        public override String ToString()
        {
            //get the list of rgb colors in a string
            String[] colorString = new String[colors.Count()];
            for (int i = 0; i < colors.Count(); i++)
            {
                String[] fields = new String[] { colors[i].R.ToString(), colors[i].G.ToString(), colors[i].B.ToString() };
                colorString[i] = String.Join(",", fields);
            }
            return String.Join(" ", colorString);
        }
    }




    public class PaletteExtractor
    {
        double[,] cnCache;
        object[] cacheLocks;

        ColorNames colorNames;
        String dir; //image directory
        FeatureName featureName;
        String weightsDir; //weights directory
        bool debug;

        /**
         * Set the base directory (where all the images are), weights directory (wDir), and pass in the filepath
         * for the color names json data
         */
        public PaletteExtractor(String directory, String wDir, String json, bool debugLogs=false)
        {
            debug = debugLogs;
            dir = directory;
            weightsDir = wDir;

            colorNames = new ColorNames(json);

            int numBins = colorNames.map.Keys.Count();
            cnCache = new double[numBins, numBins];
            cacheLocks = new object[256];

            for (int i = 0; i < numBins; i++)
            {
                for (int j = 0; j < numBins; j++)
                {
                    cnCache[i, j] = -1;                  
                }
            }
            for (int i = 0; i < cacheLocks.Length; i++)
                cacheLocks[i] = new object();

            featureName = new FeatureName();

            //create base subdirectories
            Directory.CreateDirectory(Path.Combine(dir, "swatches"));
            Directory.CreateDirectory(Path.Combine(dir, "out"));
            Directory.CreateDirectory(Path.Combine(dir, "saliency"));
            Directory.CreateDirectory(Path.Combine(dir, "segments"));
            
        }



        /**
         * Load or create the candidate swatches
         */
        public PaletteData GetPaletteSwatches(string key, int maxIters=50)
        {
            PaletteData global = new PaletteData();

            //load all the candidate colors (the json file)
            String jsonFile = Path.Combine(dir,"swatches", Util.ConvertFileName(key,"",".json"));

            //check if it exists, if not, create the swatches
            if (!File.Exists(jsonFile))
                GenerateCandidates(key, maxIters);

            String rawText = System.IO.File.ReadAllText(jsonFile);

            //Get the rgb strings
            String cleanedText = rawText.Replace("},{", "^");
            cleanedText = cleanedText.Replace("[{", "");
            cleanedText = cleanedText.Replace("}]", "");

            String[] rgbStrings = cleanedText.Split('^');
            foreach (String rgb in rgbStrings)
            {
                String[] fields = rgb.Split(',');

                //assume that the first three fields are r,g,b
                Color color = Color.FromArgb(Int32.Parse(fields[0].Split(':')[1]),
                                             Int32.Parse(fields[1].Split(':')[1]),
                                             Int32.Parse(fields[2].Split(':')[1]));
                CIELAB lab = Util.RGBtoLAB(color);
                global.lab.Add(lab);
                global.colors.Add(color);
            }
            return global;
        }



        /**
         * Load the segmentation file for a particular image
         */
        private Segmentation LoadSegAssignments(String key, int width, int height, CIELAB[,] imageLAB)
        {
            Bitmap map = new Bitmap(Path.Combine(dir,"segments",key));
            Bitmap resized = Util.ResizeBitmapNearest(map, width, height);
            Color[,] resizedData = Util.BitmapToArray(resized);

            Segmentation s = new Segmentation();
            s.assignments = new int[width, height];

            List<Color> unique = new List<Color>();
            for (int i = 0; i < width; i++)
            {
                for (int j = 0; j < height; j++)
                {
                    Color c = resizedData[i, j];
                    if (!unique.Contains(c))
                        unique.Add(c);

                    int idx = unique.IndexOf(c);
                    s.assignments[i, j] = idx;

                }
            }

            s.numSegments = unique.Count();
            s.counts = new int[s.numSegments];
            s.segToMeanColor = new CIELAB[s.numSegments];
            for (int i = 0; i < s.numSegments; i++)
            {
                s.segToMeanColor[i] = new CIELAB();
            }
            for (int i = 0; i < width; i++)
            {
                for (int j = 0; j < height; j++)
                {

                    int idx = s.assignments[i, j];
                    s.counts[idx]++;
                    s.segToMeanColor[idx] += imageLAB[i, j];
                }
            }

            for (int i = 0; i < s.numSegments; i++)
            {
                s.segToMeanColor[i] /= s.counts[i];
            }

            return s;
        }

        /**
         * Precompute palette and image information
         * */
        public FeatureParams SetupFeatureParams(SortedSet<Features> included, String key, String saliencyPattern, bool debug = false, int paletteSize=5, int maxIters=50)
        {
            FeatureParams options = new FeatureParams();
            

            //find the normalization factor for swatches
            double factor = 0;
            double afactor = 0;

            PaletteData swatches = GetPaletteSwatches(key, maxIters);
            int scount = swatches.lab.Count();
            for (int i = 0; i < swatches.lab.Count(); i++)
            {
                for (int j = i + 1; j < swatches.lab.Count(); j++)
                {
                    double dist = swatches.lab[i].SqDist(swatches.lab[j]);
                    factor = Math.Max(factor, dist);
                    afactor += Math.Sqrt(dist);
                }
            }
            factor = Math.Sqrt(factor);
            afactor /= ((scount - 1) * scount / 2);

            Bitmap image = Util.GetImage(Path.Combine(dir, key), debug);
            int width = image.Width;
            int height = image.Height;

            CIELAB[,] imageLAB = Util.Map(Util.BitmapToArray(image), c => Util.RGBtoLAB(c));


            double[,] pixelToDists = new double[width * height, scount];
            double[,] pixelToNDists = new double[width * height, scount];

            //calculate the assignment of a pixel to a swatch
            //for (int i = 0; i < width; i++)
            //{
            Parallel.For(0, width, i =>
            {
                for (int j = 0; j < height; j++)
                {
                    int idx = j * width + i;

                    for (int s = 0; s < scount; s++)
                    {
                        pixelToDists[idx, s] = imageLAB[i, j].SqDist(swatches.lab[s]);


                        int a = colorNames.GetBin(imageLAB[i, j]);
                        int b = colorNames.GetBin(swatches.lab[s]);

                        double ndist = GetCNDist(a, b);
                        pixelToNDists[idx, s] = ndist * ndist;
                    }
                }
            });


            //calculate the unnormalized purity of each swatch
            double[] purityAvg = new double[scount];
            int purityCount = (int)Math.Round(width * height / 20.0);
            //for each swatch, find the top 5% pixels closest to it (used by Donovan)
            Parallel.For(0, scount, s =>
            {
                CIELAB swatch = swatches.lab[s];
                List<double> dists = new List<double>(width * height);

                for (int i = 0; i < width; i++)
                {
                    for (int j = 0; j < height; j++)
                    {
                        dists.Add(imageLAB[i, j].SqDist(swatch));
                    }
                }

                dists.Sort();

                //now pop off the top
                for (int i = 0; i < purityCount; i++)
                {
                    purityAvg[s] += Math.Sqrt(dists[i]);
                }
                purityAvg[s] /= purityCount;

            });



            //read the saliency map, and calculate saliency for each swatch
            String mapPath = Util.ConvertFileName(key, saliencyPattern);//"gbvs_" + key;
            Bitmap map = new Bitmap(Image.FromFile(Path.Combine(dir,"saliency",mapPath)), width, height);

            double[,] mapData = Util.Map(Util.BitmapToArray(map), c => c.G / 255.0);

            Dictionary<CIELAB, int> swatchIndexOf = new Dictionary<CIELAB, int>();
            for (int i = 0; i < swatches.lab.Count(); i++)
            {
                swatchIndexOf.Add(swatches.lab[i], i);
            }

            //for each cluster, figure out it's total/average saliency
            double[] colorToSaliency = new double[swatches.lab.Count()];
            int[] colorToCounts = new int[swatches.lab.Count()];

            double thresh = Double.PositiveInfinity;
            double totalSaliency = 0;

            for (int i = 0; i < map.Width; i++)
            {
                for (int j = 0; j < map.Height; j++)
                {
                    //find the image color, and its closest swatch, within some max threshold
                    CIELAB c = imageLAB[i, j];
                    double bestDist = thresh;
                    int bestSwatch = -1;
                    for (int s = 0; s < swatches.lab.Count(); s++)
                    {
                        if (swatches.lab[s].SqDist(c) < bestDist)
                        {
                            bestDist = swatches.lab[s].SqDist(c);
                            bestSwatch = s;
                        }
                    }

                    double value = mapData[i, j];//map.GetPixel(i, j).G / 255.0;

                    if (bestSwatch >= 0)
                    {
                        colorToSaliency[bestSwatch] += value;
                        colorToCounts[bestSwatch]++;
                    }

                    totalSaliency += value;
                }
            }

            List<double> sorted = colorToSaliency.ToList<double>();
            sorted.Sort();
            sorted.Reverse();
            double totalCapturable = 0;
            for (int i = 0; i < paletteSize; i++)
            {
                totalCapturable += sorted[i];
            }

            double minE = Double.PositiveInfinity;
            double maxE = Double.NegativeInfinity;
            double[] colorToCNSaliency = new double[swatches.lab.Count()];
            double avgE = 0;
            for (int i = 0; i < swatches.lab.Count(); i++)
            {
                CIELAB s = swatches.lab[i];
                double sal = colorNames.Saliency(s);
                minE = Math.Min(minE, sal);
                maxE = Math.Max(maxE, sal);
                avgE += sal;
                colorToCNSaliency[i] = sal;
            }
            avgE /= swatches.lab.Count();
            avgE = (avgE - minE) / (maxE - minE);

            //find the avg and max color name distance between swatches
            double maxN = Double.NegativeInfinity;
            double avgN = 0;
            for (int i = 0; i < scount; i++)
            {
                for (int j = i + 1; j < scount; j++)
                {
                    double val = 1 - colorNames.CosineDistance(swatches.lab[i], swatches.lab[j]);
                    avgN += val;
                    maxN = Math.Max(maxN, val);
                }
            }
            avgN /= ((scount - 1) * scount / 2);


            //cache the distance between image colors and swatches
            Dictionary<Tuple<int, int>, double> imageSwatchDist = new Dictionary<Tuple<int, int>, double>();
            int[,] binAssignments = new int[width, height];
            int[] swatchIdxToBin = new int[swatches.colors.Count()];

            if (included.Overlaps(options.NameCovFeatures))
            {
                for (int i = 0; i < width; i++)
                {
                    for (int j = 0; j < height; j++)
                    {
                        CIELAB c = imageLAB[i, j];
                        int a = colorNames.GetBin(imageLAB[i, j]);
                        binAssignments[i, j] = a;
                    }
                }
            }

            for (int s = 0; s < swatches.lab.Count(); s++)
            {                        
                int b = colorNames.GetBin(swatches.lab[s]);                      
                swatchIdxToBin[s] = b;
            }

            //segment to total saliency
            Segmentation seg = LoadSegAssignments(key, width, height, imageLAB);
            double[] segmentToSaliency = new double[seg.numSegments];
            double[] segmentToSD = new double[seg.numSegments];

            for (int i = 0; i < width; i++)
            {
                for (int j = 0; j < height; j++)
                {
                    int id = seg.assignments[i, j];
                    segmentToSaliency[id] += mapData[i, j];
                    segmentToSD[id] += mapData[i, j];
                }
            }

            double totalSD = 0;
            for (int i = 0; i < seg.numSegments; i++)
            {
                segmentToSD[i] /= seg.counts[i];
                totalSD += segmentToSD[i];
            }



            //find the l,a,b, sat bounds
            double[] Lbounds = new double[] { double.PositiveInfinity, double.NegativeInfinity };
            double[] Abounds = new double[] { double.PositiveInfinity, double.NegativeInfinity };
            double[] Bbounds = new double[] { double.PositiveInfinity, double.NegativeInfinity };
            double[] Satbounds = new double[] { double.PositiveInfinity, double.NegativeInfinity };

            for (int i = 0; i < swatches.lab.Count(); i++)
            {
                CIELAB lab = swatches.lab[i];
                Color rgb = swatches.colors[i];
                Lbounds[0] = Math.Min(Lbounds[0], lab.L);
                Lbounds[1] = Math.Max(Lbounds[1], lab.L);

                Abounds[0] = Math.Min(Abounds[0], lab.A);
                Abounds[1] = Math.Max(Abounds[1], lab.A);

                Bbounds[0] = Math.Min(Bbounds[0], lab.B);
                Bbounds[1] = Math.Max(Bbounds[1], lab.B);

                double s = rgb.GetSaturation();
                Satbounds[0] = Math.Min(Satbounds[0], s);
                Satbounds[1] = Math.Max(Satbounds[1], s);
            }


            //pack up

            options.included = included;
            options.imageLAB = imageLAB;
            options.map = mapData;
            options.colorToSaliency = colorToSaliency;
            options.colorToCounts = colorToCounts;
            options.totalSaliency = totalSaliency;
            options.colorNames = colorNames;
            options.factor = factor;
            options.colorToCNSaliency = colorToCNSaliency;
            options.minE = minE;
            options.maxE = maxE;
            options.swatchIndexOf = swatchIndexOf;
            options.imageSwatchDist = imageSwatchDist;
            options.afactor = afactor;
            options.swatchToPurity = purityAvg;
            options.maxN = maxN;
            options.avgN = avgN;
            options.avgE = avgE;
            options.segmentToSaliency = segmentToSaliency;
            options.seg = seg;

            options.Lspan = Lbounds[1] - Lbounds[0];
            options.Aspan = Abounds[1] - Abounds[0];
            options.Bspan = Bbounds[1] - Bbounds[0];
            options.Satspan = Satbounds[1] - Satbounds[0];

            options.swatchIdxToBin = swatchIdxToBin;
            options.binAssignments = binAssignments;

            options.totalCapturableSaliency = totalCapturable;
            options.pixelToDists = pixelToDists;

            options.pixelToNDists = pixelToNDists;

            options.totalSD = totalSD;
            options.segmentToSD = segmentToSD;

            image.Dispose();
            map.Dispose();
            


            return options;
        }

        private double GetCNDist(int i, int j)
        {
            if (cnCache[i, j] < 0)
            {
                double dist = 1 - colorNames.CosineDistance(i, j);

                int numBins = colorNames.map.Keys.Count();
                lock (cacheLocks[(Math.Min(i,j)*numBins+Math.Max(i,j)) % cacheLocks.Length])
                {
                    cnCache[i, j] = dist;
                    cnCache[j, i] = dist;
                }
            }
            return cnCache[i, j];
        }


        private void ClearLog(String log)
        {
            File.WriteAllText(log, "");
        }

        private void Log(String log, String text)
        {
            if (debug)
                File.AppendAllText(log, text + "\n");
        }

        /**
         * Calculate all the palette and image features here. 
         * options is the precomputed information from SetupFeatureParams
         * */
        public Dictionary<Features, double> CalculateFeatures(PaletteData data, FeatureParams options)
        {
            String log = Path.Combine(dir,"out","timelog.txt");
            Stopwatch watch = new Stopwatch();

            //unpack options
            SortedSet<Features> included = options.included;
            CIELAB[,] imageLAB = options.imageLAB;
            double[,] map = options.map;
            double[] colorToSaliency = options.colorToSaliency;
            int[] colorToCounts = options.colorToCounts;
            double totalSaliency = options.totalSaliency;
            ColorNames colorNames = options.colorNames;
            double factor = options.factor;
            double[] colorToCNSaliency = options.colorToCNSaliency;
            double minE = options.minE;
            double maxE = options.maxE;
            Dictionary<CIELAB, int> swatchIndexOf = options.swatchIndexOf;
            Dictionary<Tuple<int, int>, double> imageSwatchDist = options.imageSwatchDist;
            double afactor = options.afactor;
            double[] swatchToPurity = options.swatchToPurity;
            double maxN = options.maxN;
            double avgN = options.avgN;
            double avgE = options.avgE;
            double[] segToSaliency = options.segmentToSaliency;
            Segmentation seg = options.seg;
            int[] swatchIdxToBin = options.swatchIdxToBin;
            int[,] binAssignments = options.binAssignments;
            double totalCapturable = options.totalCapturableSaliency;
            double[,] pixelToDists = options.pixelToDists;
            double[,] pixelToCNDists = options.pixelToNDists;

            double[] segmentToSD = options.segmentToSD;
            double totalSD = options.totalSD;

            //calculate the palette features
            Dictionary<Features, double> features = new Dictionary<Features, double>();

            double sdmin2 = Double.PositiveInfinity;
            double sdmax2 = 0;
            double sdavg2 = 0;

            int width = options.imageLAB.GetLength(0);
            int height = options.imageLAB.GetLength(1);

            int n = data.lab.Count();

            double[] elementToSaliency = new double[n];
            int[] elementToCount = new int[n];

            //Calculate the error of recoloring the image, each pixel weighted the same, and subtract from 1
            double error = 0;
            //Coverage weighted by saliency of pixel
            double errorWeighted = 0;

            //error of recoloring the image, distance is the names distance metric
            double nerror = 0;
            double nerrorWeighted = 0;

            double errorSeg = 0;
            double errorSegSal = 0;

            double colorsPSeg = 0;
            double colorsPSegW = 0;

            double withinVariance = 0;
            double betweenVariance = 0;

            double sqerror = 0;
            double sqerrorW = 0;
            double sqerrorSeg = 0;
            double sqerrorSegSal = 0;

            double nsqerrorSeg = 0;
            double nsqerrorSegSal = 0;


            double nerrorSeg = 0;
            double nerrorSegSal = 0;

            double nsqerror = 0;
            double nsqerrorW = 0;

            double softerror = 0;
            double softwerror = 0;
            double softerrorseg = 0;
            double softerrorwseg = 0;
            double nsofterror = 0;
            double nsoftwerror = 0;
            double nsofterrorseg = 0;
            double nsofterrorwseg = 0;

            double nmeansegerror = 0;
            double nmeansegwerror = 0;

            double softerrorsegsd = 0;
            double nsofterrorsegsd = 0;
            double errorsegsd = 0;
            double nerrorsegsd = 0;
            double nsqerrorsegsd = 0;
            double sqerrorsegsd = 0;
            double meansegsd = 0;
            double nmeansegsd = 0;

            double epsilon = 0.0001;

            watch.Start();

            int numPixels = width * height;

            //compute the memberships
            double[,] memberships = new double[numPixels, n];
            double[,] nmemberships = new double[numPixels, n];


            //LAB, and SAT coverage
            double[] Lbounds = new double[] { double.PositiveInfinity, double.NegativeInfinity };
            double[] Abounds = new double[] { double.PositiveInfinity, double.NegativeInfinity };
            double[] Bbounds = new double[] { double.PositiveInfinity, double.NegativeInfinity };
            double[] Satbounds = new double[] { double.PositiveInfinity, double.NegativeInfinity };

            for (int i = 0; i < n; i++)
            {
                CIELAB lab = data.lab[i];
                Color c = data.colors[i];
                double s = c.GetSaturation();

                Lbounds[0] = Math.Min(Lbounds[0], lab.L);
                Lbounds[1] = Math.Max(Lbounds[1], lab.L);

                Abounds[0] = Math.Min(Abounds[0], lab.A);
                Abounds[1] = Math.Max(Abounds[1], lab.A);

                Bbounds[0] = Math.Min(Bbounds[0], lab.B);
                Bbounds[1] = Math.Max(Bbounds[1], lab.B);

                Satbounds[0] = Math.Min(Satbounds[0], s);
                Satbounds[1] = Math.Max(Satbounds[1], s);

            }
            double Lspan = Lbounds[1] - Lbounds[0];
            double Aspan = Abounds[1] - Abounds[0];
            double Bspan = Bbounds[1] - Bbounds[0];
            double Satspan = Satbounds[1] - Satbounds[0];

            double Lcov = Math.Min(Lspan, options.Lspan) / Math.Max(Math.Max(Lspan, options.Lspan), 1);
            double Acov = Math.Min(Aspan, options.Aspan) / Math.Max(Math.Max(Aspan, options.Aspan), 1);
            double Bcov = Math.Min(Bspan, options.Bspan) / Math.Max(Math.Max(Bspan, options.Bspan), 1);
            double Scov = Math.Min(Satspan, options.Satspan) / Math.Max(Math.Max(Satspan, options.Satspan), epsilon);



            double meansegError = 0;
            double meansegErrorWeighted = 0;


            double[,] colorSegDist = new double[n, seg.numSegments];

            double[] segToDist = new double[seg.numSegments];

            for (int j = 0; j < seg.numSegments; j++)
            {
                //find the closest color for each segment
                double bestDist = Double.PositiveInfinity;
                for (int i = 0; i < n; i++)
                {
                    colorSegDist[i, j] = Math.Sqrt(data.lab[i].SqDist(seg.segToMeanColor[j]));
                    segToDist[j] += colorSegDist[i, j];

                    bestDist = Math.Min(colorSegDist[i, j], bestDist);
                }

                double nbestDist = Double.PositiveInfinity;
                for (int i = 0; i < n; i++)
                {
                    double ndist = GetCNDist(colorNames.GetBin(data.lab[i]), colorNames.GetBin(seg.segToMeanColor[j]));
                    nbestDist = Math.Min(ndist, nbestDist);

                }

                meansegError += bestDist;
                meansegErrorWeighted += bestDist * segToSaliency[j];

                nmeansegerror += nbestDist;
                nmeansegwerror += nbestDist * segToSaliency[j];

                meansegsd += bestDist * segmentToSD[j];
                nmeansegsd += nbestDist * segmentToSD[j];

            }

            meansegError /= seg.numSegments * factor;
            meansegErrorWeighted /= totalSaliency * factor;

            nmeansegerror /= seg.numSegments;
            nmeansegwerror /= totalSaliency;

            meansegsd /= totalSD * factor;
            nmeansegsd /= totalSD;

            //now calculate entropy per segment
            //probability is 1-(dist/totaldist)   
            double[] segEntropy = new double[seg.numSegments];
            for (int i = 0; i < seg.numSegments; i++)
            {
                for (int j = 0; j < n; j++)
                {
                    double prob = 1 - colorSegDist[j, i] / segToDist[i];
                    segEntropy[i] += prob * Math.Log(prob);
                }
            }

            //average and/or weight by saliency of segment
            for (int i = 0; i < seg.numSegments; i++)
            {
                colorsPSeg += segEntropy[i];
                colorsPSegW += segEntropy[i] * segToSaliency[i];
            }

            colorsPSeg /= seg.numSegments;
            colorsPSegW /= totalSaliency;



            double[,] nsubs = new double[width, height];
            double[,] subs = new double[width, height];

            if (included.Overlaps(options.NameCovFeatures) ||
                included.Overlaps(options.CoverageFeatures) ||
                included.Overlaps(options.SaliencyDensFeatures))
            {

                double[,] errors = new double[width, height];
                int[,] assignment = new int[width, height]; //the pixel to the nearest palette color

                double[,] nerrors = new double[width, height];
                double[,] nerrorsWeighted = new double[width, height];

                double[, ,] clusterSqErrorTemp = new double[width, height, n];
                double[] clusterSqError = new double[n];

                int[,] nassignment = new int[width, height];

                Parallel.For(0, width, i =>
               {
                   for (int j = 0; j < height; j++)
                   {
                       double bestDist = Double.PositiveInfinity;
                       int bestIdx = -1;
                       for (int s = 0; s < data.lab.Count(); s++)
                       {
                           double dist = data.lab[s].SqDist(options.imageLAB[i, j]);
                           if (dist < bestDist)
                               bestIdx = s;
                           bestDist = Math.Min(dist, bestDist);

                       }
                       assignment[i, j] = bestIdx;
                       double sqrtBestDist = Math.Sqrt(bestDist);
                       errors[i, j] = sqrtBestDist;
                       //clusterSqError[bestIdx] += bestDist;
                       clusterSqErrorTemp[i, j, bestIdx] = bestDist;
                   }


                   if (included.Overlaps(options.NameCovFeatures))
                   {
                       for (int j = 0; j < height; j++)
                       {
                           int bestIdx = -1;
                           double bestDist = Double.PositiveInfinity;
                           for (int s = 0; s < data.lab.Count(); s++)
                           {
                               int sIdx = swatchIndexOf[data.lab[s]];
                               double dist = GetCNDist(binAssignments[i, j], swatchIdxToBin[sIdx]);

                               if (dist < bestDist)
                                   bestIdx = s;

                               bestDist = Math.Min(dist, bestDist);
                           }
                           double val = map[i, j];
                           nerrors[i, j] = bestDist;
                           nerrorsWeighted[i, j] = bestDist * val;
                           nassignment[i, j] = bestIdx;
                       }
                   }



                   for (int j = 0; j < height; j++)
                   {
                       //calculate the memberships
                       double[] dists = new double[n];
                       double[] ndists = new double[n];

                       double subtotal = 0;
                       double nsubtotal = 0;

                       int idx = j * width + i;

                       for (int k = 0; k < n; k++)
                       {
                           int sidx = swatchIndexOf[data.lab[k]];

                           dists[k] = Math.Max(pixelToDists[idx, sidx], epsilon);
                           ndists[k] = Math.Max(pixelToCNDists[idx, sidx], epsilon);

                           subtotal += (1.0 / dists[k]);
                           nsubtotal += (1.0 / ndists[k]);
                       }
                       for (int k = 0; k < n; k++)
                       {
                           memberships[idx, k] = 1.0 / (dists[k] * subtotal);
                           nmemberships[idx, k] = 1.0 / (ndists[k] * nsubtotal);
                       }

                       for (int k = 0; k < n; k++)
                       {
                           int sidx = swatchIndexOf[data.lab[k]];

                           double u = memberships[idx, k];
                           subs[i, j] += u * u * Math.Max(pixelToDists[idx, sidx], epsilon);

                           double nu = nmemberships[idx, k];
                           nsubs[i, j] += nu * nu * Math.Max(pixelToCNDists[idx, sidx], epsilon);
                       }
                   }


               });



                //find J

                double[] segToJ = new double[seg.numSegments];
                double[] nsegToJ = new double[seg.numSegments];

                for (int i = 0; i < width; i++)
                {
                    for (int j = 0; j < height; j++)
                    {
                        int idx = j * width + i;

                        double sub = subs[i, j];
                        double nsub = nsubs[i, j];


                        softerror += sub;
                        nsofterror += nsub;

                        softwerror += sub * map[i, j];
                        nsoftwerror += nsub * map[i, j];

                        int segIdx = seg.assignments[i, j];
                        segToJ[segIdx] += sub;
                        nsegToJ[segIdx] += nsub;
                    }
                }


                softerror /= numPixels * factor * factor;
                nsofterror /= numPixels;

                softwerror /= totalSaliency * factor * factor;
                nsoftwerror /= totalSaliency;

                for (int s = 0; s < seg.numSegments; s++)
                {
                    softerrorseg += segToJ[s] / seg.counts[s];
                    softerrorwseg += segToSaliency[s] * segToJ[s] / seg.counts[s];


                    nsofterrorseg += nsegToJ[s] / seg.counts[s];
                    nsofterrorwseg += segToSaliency[s] * nsegToJ[s] / seg.counts[s];


                    softerrorsegsd += segmentToSD[s] * segToJ[s] / seg.counts[s];
                    nsofterrorsegsd += segmentToSD[s] * nsegToJ[s] / seg.counts[s];

                }
                softerrorseg /= seg.numSegments * factor * factor;
                softerrorwseg /= totalSaliency * factor * factor;

                nsofterrorseg /= seg.numSegments;
                nsofterrorwseg /= totalSaliency;

                softerrorsegsd /= totalSD * factor * factor;
                nsofterrorsegsd /= totalSD;


                //calculate within and between palette cluster variance
                //between variance
                CIELAB betweenMean = new CIELAB();

                for (int j = 0; j < n; j++)
                {
                    betweenMean = betweenMean + data.lab[j];
                }
                betweenMean = betweenMean / (double)n;
                for (int j = 0; j < n; j++)
                {
                    betweenVariance += betweenMean.SqDist(data.lab[j]);
                }
                betweenVariance /= (double)n * factor * factor;

                double[] counts = new double[n];

                for (int i = 0; i < width; i++)
                {
                    for (int j = 0; j < height; j++)
                    {
                        counts[assignment[i, j]]++;
                    }
                }

                //aggregate cluster sq error
                for (int c = 0; c < n; c++)
                    for (int i = 0; i < width; i++)
                        for (int j = 0; j < height; j++)
                            clusterSqError[c] += clusterSqErrorTemp[i, j, c];

                for (int i = 0; i < n; i++)
                {
                    withinVariance += clusterSqError[i] / Math.Max(1, counts[i]);
                }
                withinVariance /= n * (factor * factor);


                double[] segmentToError = new double[segToSaliency.Count()];
                double[] segToSqError = new double[segToSaliency.Count()];
                Dictionary<int, SortedSet<int>> segToColors = new Dictionary<int, SortedSet<int>>();

                //now aggregate
                for (int i = 0; i < width; i++)
                {
                    for (int j = 0; j < height; j++)
                    {
                        double sqError = errors[i, j] * errors[i, j];
                        double val = map[i, j];
                        errorWeighted += val * errors[i, j];
                        sqerrorW += val * sqError;
                        error += errors[i, j];
                        sqerror += sqError;

                        int idx = assignment[i, j];
                        if (idx >= 0)
                        {
                            elementToSaliency[idx] += val;
                            elementToCount[idx]++;
                        }
                        int segIdx = seg.assignments[i, j];
                        segmentToError[segIdx] += errors[i, j] / seg.counts[segIdx];
                        segToSqError[segIdx] += sqError / seg.counts[segIdx];

                    }
                }



                for (int i = 0; i < seg.numSegments; i++)
                {

                    errorSeg += segmentToError[i];
                    errorSegSal += segmentToError[i] * segToSaliency[i];

                    sqerrorSeg += segToSqError[i];
                    sqerrorSegSal += segToSqError[i] * segToSaliency[i]; //TODO: this is weighted by segment percent salient density

                    errorsegsd += segmentToError[i] * segmentToSD[i];
                    sqerrorsegsd += segToSqError[i] * segmentToSD[i];

                }

                errorSeg /= seg.numSegments * factor;
                errorSegSal /= totalSaliency * factor;

                sqerrorSeg /= seg.numSegments * factor * factor;
                sqerrorSegSal /= totalSaliency * factor * factor;


                errorWeighted /= totalSaliency * factor;
                error /= width * height * factor;

                sqerrorW /= totalSaliency * factor * factor;
                sqerror /= width * height * factor * factor;

                errorsegsd /= totalSD * factor;
                sqerrorsegsd /= totalSD * factor * factor;



                //now sum it all up
                for (int i = 0; i < width; i++)
                {
                    for (int j = 0; j < height; j++)
                    {
                        nerrorWeighted += nerrorsWeighted[i, j];
                        nerror += nerrors[i, j];
                    }
                }


                nerrorWeighted /= totalSaliency;
                nerror /= width * height;


                double[] nsegmentToError = new double[segToSaliency.Count()];
                double[] nsegToSqError = new double[segToSaliency.Count()];

                //now aggregate
                for (int i = 0; i < width; i++)
                {
                    for (int j = 0; j < height; j++)
                    {
                        double sqError = nerrors[i, j] * nerrors[i, j];
                        double val = map[i, j];
                        nerrorWeighted += val * nerrors[i, j];
                        nsqerrorW += val * sqError;
                        nerror += nerrors[i, j];
                        nsqerror += sqError;

                        int idx = nassignment[i, j];
                        if (idx >= 0)
                        {
                            elementToSaliency[idx] += val;
                            elementToCount[idx]++;
                        }
                        int segIdx = seg.assignments[i, j];
                        nsegmentToError[segIdx] += nerrors[i, j] / seg.counts[segIdx];
                        nsegToSqError[segIdx] += sqError / seg.counts[segIdx];

                    }
                }



                for (int i = 0; i < seg.numSegments; i++)
                {

                    nerrorSeg += nsegmentToError[i];
                    nerrorSegSal += nsegmentToError[i] * segToSaliency[i];

                    nsqerrorSeg += nsegToSqError[i];
                    nsqerrorSegSal += nsegToSqError[i] * segToSaliency[i]; //TODO: this is weighted by segment percent salient density

                    nerrorsegsd += nsegmentToError[i] * segmentToSD[i];
                    nsqerrorsegsd += nsegToSqError[i] * segmentToSD[i];

                }


                nerrorSeg /= seg.numSegments;
                nerrorSegSal /= totalSaliency;

                nsqerrorSeg /= seg.numSegments;
                nsqerrorSegSal /= totalSaliency;


                nerrorWeighted /= totalSaliency;
                nerror /= width * height;

                nsqerrorW /= totalSaliency;
                nsqerror /= width * height;

                nerrorsegsd /= totalSD;
                nsqerrorsegsd /= totalSD;


            }
            Log(log, "Coverage and Name Coverage" + watch.ElapsedMilliseconds);
            watch.Restart();


            for (int i = 0; i < n; i++)
            {
                double dens = elementToSaliency[i] / Math.Max(1, elementToCount[i]);
                sdmin2 = Math.Min(dens, sdmin2);
                sdmax2 = Math.Max(dens, sdmax2);
                sdavg2 += dens;
            }
            sdavg2 /= data.lab.Count();


            Log(log, "SaliencyDensClust " + watch.ElapsedMilliseconds);
            watch.Restart();

            //total captured salience (salience sum/salience of the image)
            double stotal = 0;

            //average salience density of each swatch (and min/max)
            double sdmin = Double.PositiveInfinity;
            double sdmax = 0;
            double sdavg = 0;


            foreach (CIELAB c in data.lab)
            {
                double val = colorToSaliency[swatchIndexOf[c]] / Math.Max(1, colorToCounts[swatchIndexOf[c]]);
                sdmin = Math.Min(val, sdmin);
                sdmax = Math.Max(val, sdavg);
                sdavg += val;
                stotal += colorToSaliency[swatchIndexOf[c]];
            }
            sdavg /= data.lab.Count();
            stotal /= totalCapturable;

            Log(log, "SaliencyDens " + watch.ElapsedMilliseconds);
            watch.Restart();


            double cnmin = Double.PositiveInfinity;
            double cnmax = 0;
            double cnavg = 0;


            for (int i = 0; i < n; i++)
            {
                for (int j = i + 1; j < n; j++)
                {
                    double dist = 1 - colorNames.CosineDistance(data.lab[i], data.lab[j]);
                    cnavg += dist;
                    cnmin = Math.Min(cnmin, dist);
                    cnmax = Math.Max(cnmax, dist);
                }
            }
            cnavg /= n * (n - 1) / 2;


            Log(log, "NamesDiff " + watch.ElapsedMilliseconds);
            watch.Restart();

            //Calculate the color name diversity, avg color name distance to closest neighbor
            double cndiv = 0;

            for (int c = 0; c < n; c++)
            {
                double bestDist = Double.PositiveInfinity;
                for (int i = 0; i < n; i++)
                {
                    if (c == i)
                        continue;
                    double curDist = 1 - colorNames.CosineDistance(data.lab[i], data.lab[c]);
                    if (curDist < bestDist)
                        bestDist = curDist;
                }

                cndiv += bestDist / n;
            }


            Log(log, "NamesClosestAvg " + watch.ElapsedMilliseconds);
            watch.Restart();


            //calculate color name salience (average, min, max)
            double csmin = Double.PositiveInfinity;
            double csmax = 0;
            double csavg = 0;

            for (int i = 0; i < n; i++)
            {
                double salience = (colorToCNSaliency[swatchIndexOf[data.lab[i]]] - minE) / (maxE - minE);//colorNames.NormalizedSaliency(data.lab[i], minE, maxE);
                csavg += salience;
                csmin = Math.Min(csmin, salience);
                csmax = Math.Max(csmax, salience);
            }
            csavg /= n;


            Log(log, "NameSalience " + watch.ElapsedMilliseconds);
            watch.Restart();

            //Calculate the diversity, avg distance to closest neighbor
            double div = 0;
            double divr = 0;

            for (int c = 0; c < n; c++)
            {
                double bestDist = Double.PositiveInfinity;
                for (int i = 0; i < n; i++)
                {
                    if (c == i)
                        continue;
                    double curDist = data.lab[c].SqDist(data.lab[i]);
                    if (curDist < bestDist)
                        bestDist = curDist;
                }

                div += Math.Sqrt(bestDist); // (n * factor);
            }
            double temp = div;
            div = temp / (n * factor);
            divr = temp / (n * afactor);


            Log(log, "DiffClosestAvg " + watch.ElapsedMilliseconds);
            watch.Restart();

            //calculate average distance of one color to the rest of the colors
            double dmin = Double.PositiveInfinity;
            double dmax = 0;
            double davg = 0;

            double dminr = Double.PositiveInfinity;
            double dmaxr = 0;
            double davgr = 0;

            for (int i = 0; i < n; i++)
            {
                for (int j = i + 1; j < n; j++)
                {
                    double dist = Math.Sqrt(data.lab[i].SqDist(data.lab[j]));
                    davg += dist;
                    dmin = Math.Min(dmin, dist);
                    dmax = Math.Max(dmax, dist);
                }
            }


            double dtemp = dmin;
            dmin = dtemp / factor;
            dminr = dtemp / afactor;
            dtemp = dmax;
            dmax = dtemp / factor;
            dmaxr = dtemp / afactor;

            davg /= n * (n - 1) / 2;

            dtemp = davg;
            davg = dtemp / factor;
            davgr = dtemp / afactor;

            Log(log, "LABDiff " + watch.ElapsedMilliseconds);
            watch.Restart();

            double puritymin = double.PositiveInfinity;
            double puritymax = double.NegativeInfinity;
            double purityavg = 0;

            double purityminr = puritymin;
            double puritymaxr = puritymax;
            double purityavgr = 0;

            foreach (CIELAB c in data.lab)
            {
                double val = swatchToPurity[swatchIndexOf[c]];
                puritymin = Math.Min(puritymin, val);
                puritymax = Math.Max(puritymax, val);
                purityavg += val;
            }
            double ptemp = puritymin;
            puritymin = ptemp / factor;
            purityminr = ptemp / factor;

            ptemp = puritymax;
            puritymax = ptemp / factor;
            puritymaxr = ptemp / afactor;

            ptemp = purityavg / data.lab.Count(); 
            purityavg = ptemp / factor;
            purityavgr = ptemp / afactor;




            //Record all the features
            features.Add(Features.ErrorUnweighted, error);
            features.Add(Features.ErrorWeightedSaliency, errorWeighted);
            features.Add(Features.NError, nerror);
            features.Add(Features.NErrorWeightedSaliency, nerrorWeighted);
            features.Add(Features.DAvg, davg);
            features.Add(Features.DClosestAvg, div);
            features.Add(Features.DMin, dmin);
            features.Add(Features.DMax, dmax);

            features.Add(Features.NClosestAvg, cndiv / maxN);
            features.Add(Features.NDiffAvg, cnavg / maxN);
            features.Add(Features.NDiffMin, cnmin / maxN);
            features.Add(Features.NDiffMax, cnmax / maxN);
            features.Add(Features.NSaliencyMin, csmin);
            features.Add(Features.NSaliencyMax, csmax);
            features.Add(Features.NSaliencyAvg, csavg);
            features.Add(Features.SaliencyDensMin, sdmin);
            features.Add(Features.SaliencyDensMax, sdmax);
            features.Add(Features.SaliencyDensAvg, sdavg);
            features.Add(Features.SaliencyTotal, stotal);
            features.Add(Features.SaliencyDensClustMin, sdmin2);
            features.Add(Features.SaliencyDensClustMax, sdmax2);
            features.Add(Features.SaliencyDensClustAvg, sdavg2);
            features.Add(Features.DMinR, dminr);
            features.Add(Features.DMaxR, dmaxr);
            features.Add(Features.DAvgR, davgr);
            features.Add(Features.DClosestAvgR, divr);
            features.Add(Features.PurityMin, puritymin);
            features.Add(Features.PurityMax, puritymax);
            features.Add(Features.PurityAvg, purityavg);
            features.Add(Features.PurityMinR, purityminr);
            features.Add(Features.PurityMaxR, puritymaxr);
            features.Add(Features.PurityAvgR, purityavgr);

            features.Add(Features.NDiffMinR, cnmin / avgN);
            features.Add(Features.NDiffMaxR, cnmax / avgN);
            features.Add(Features.NDiffAvgR, cnavg / avgN);
            features.Add(Features.NClosestAvgR, cndiv / avgN);

            features.Add(Features.NSaliencyAvgR, csavg / avgE);
            features.Add(Features.NSaliencyMaxR, csmax / avgE);
            features.Add(Features.NSaliencyMinR, csmin / avgE);
            

            features.Add(Features.ErrorWeightedSegment, errorSegSal);
            features.Add(Features.ErrorUnweightedSegment, errorSeg);

            features.Add(Features.ColorsPerSeg, colorsPSeg);
            features.Add(Features.ColorsPerSegWeighted, colorsPSegW);

            features.Add(Features.MeanSegError, meansegError);
            features.Add(Features.MeanSegErrorWeighted, meansegErrorWeighted);

            features.Add(Features.LCov, Lcov);
            features.Add(Features.ACov, Acov);
            features.Add(Features.BCov, Bcov);
            features.Add(Features.SatCov, Scov);

            features.Add(Features.BetweenVar, betweenVariance);
            features.Add(Features.WithinVar, withinVariance);
            features.Add(Features.SqErrorUnweighted, sqerror);
            features.Add(Features.SqErrorWeightedSaliency, sqerrorW);
            features.Add(Features.SqErrorWSeg, sqerrorSegSal);
            features.Add(Features.SqErrorSeg, sqerrorSeg);

            features.Add(Features.NSqError, nsqerror);
            features.Add(Features.NSqErrorW, nsqerrorW);
            features.Add(Features.NSqErrorSeg, nsqerrorSeg);
            features.Add(Features.NSqErrorWSeg, nsqerrorSegSal);
            features.Add(Features.NErrorSeg, nerrorSeg);
            features.Add(Features.NErrorWSeg, nerrorSegSal);


            features.Add(Features.SoftError, softerror);
            features.Add(Features.SoftWError, softwerror);
            features.Add(Features.SoftErrorSeg, softerrorseg);
            features.Add(Features.SoftErrorWSeg, softerrorwseg);


            features.Add(Features.NSoftError, nsofterror);
            features.Add(Features.NSoftWError, nsoftwerror);
            features.Add(Features.NSoftErrorSeg, nsofterrorseg);
            features.Add(Features.NSoftErrorWSeg, nsofterrorwseg);

            features.Add(Features.NMeanSegError, nmeansegerror);
            features.Add(Features.NMeanSegErrorW, nmeansegwerror);

            features.Add(Features.MeanSegErrorDensity, meansegsd);
            features.Add(Features.NMeanSegErrorDensity, nmeansegsd);

            features.Add(Features.ErrorSegSD, errorsegsd);
            features.Add(Features.NErrorSegSD, nerrorsegsd);
            features.Add(Features.SqErrorSegSD, sqerrorsegsd);
            features.Add(Features.NSqErrorSegSD, nsqerrorsegsd);
            features.Add(Features.SoftErrorSegSD, softerrorsegsd);
            features.Add(Features.NSoftErrorSegSD, nsofterrorsegsd);



            return features;

        }

        private double ScorePalette(Dictionary<Features, double> weights, Dictionary<Features, double> features)
        {
            double score = 0;
            foreach (Features f in weights.Keys)
            {
                score += weights[f] * features[f];
            }
            return score;
        }

        private Dictionary<Features, double> LoadWeights(String file, String nameFile)
        {

            Dictionary<Features, double> weights = new Dictionary<Features, double>();
            String[] lines = File.ReadAllLines(file);
            String[] nameLines = File.ReadAllLines(nameFile);

            String[] names = nameLines;//.First().Split(',');

            for (int i = 0; i < lines.Count(); i++)
            {
                String line = lines[i];
                double val = double.Parse(line);
                String name = names[i];
                Features f = featureName.Feature(name);

                if (Math.Abs(val) != 0.0)
                    weights.Add(f, val);
            }

            return weights;
        }

        /**
         * Extract a palette by hill climbing on the given image 
         * key - image name in the base directory (directory)
         * saliencyPattern - the label appended to the image name, to find the saliency map filename (i.e. "_Judd" for "[imagename]_Judd.[imagename extension]")
         * debug - if images should be resized smaller
         * paletteSize - the size of the palette to be extracted
         */ 
        public PaletteData HillClimbPalette(String key, String saliencyPattern, bool debug = false, int paletteSize=5)
        {
            //Now initialize a palette and start hill climbing
            int trials = 5;

            PaletteScoreCache cache = new PaletteScoreCache(1000000);

            PaletteData swatches = GetPaletteSwatches(key);

            List<CIELAB> shuffled = new List<CIELAB>();
            foreach (CIELAB c in swatches.lab)
                shuffled.Add(c);

            Random random = new Random();

            //weights
            Dictionary<Features, double> weights = new Dictionary<Features, double>();

            weights = LoadWeights(Path.Combine(weightsDir,"weights.csv"), Path.Combine(weightsDir,"featurenames.txt"));


            PaletteData best = new PaletteData();
            double bestScore = Double.NegativeInfinity;

            SortedSet<Features> included = new SortedSet<Features>(weights.Keys);

            Stopwatch watch = new Stopwatch();
            watch.Start();

            FeatureParams fparams = SetupFeatureParams(included, key, saliencyPattern, debug);

            Log(Path.Combine(dir, "out", "convergelog.txt"), "Setup feature params " + watch.ElapsedMilliseconds);

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

                for (int i = 0; i < paletteSize; i++)
                {
                    option.lab.Add(shuffled[i]);
                    option.colors.Add(Util.LABtoRGB(shuffled[i]));
                }
                starts.Add(option);
                allOptions[t] = new PaletteData(option);
                double optionScore = ScorePalette(weights, CalculateFeatures(option, fparams));
                allScores[t] = optionScore;

                cache.SetScore(option.lab, optionScore);
            }

            watch.Restart();

            for (int t = 0; t < trials; t++)
            {
                //setup
                PaletteData option = new PaletteData(starts[t]);

                double optionScore = allScores[t];


                //Now hill climb, for each swatch, consider replacing it with a better swatch
                //Pick the best replacement, and continue until we reach the top of a hill
                int changes = 1;
                int iters = 0;
                int reuse = 0;

                watch.Restart();

                while (changes > 0)
                {
                    changes = 0;

                    for (int i = 0; i < option.lab.Count(); i++)
                    {
                        //find the best swatch replacement for this color
                        double bestTempScore = optionScore;
                        CIELAB bestRep = option.lab[i];
                        Color bestRGB = option.colors[i];


                        double[] scores = new double[swatches.lab.Count()];

                        for (int s = 0; s < swatches.lab.Count(); s++)
                        {
                            CIELAB r = swatches.lab[s];

                            PaletteData temp = new PaletteData(option);
                            if (!temp.lab.Contains(r))
                            {

                                temp.lab[i] = r;
                                temp.colors[i] = swatches.colors[s];

                                if (!cache.ContainsKey(temp.lab))
                                {
                                    double tempScore = ScorePalette(weights, CalculateFeatures(temp, fparams));
                                    scores[s] = tempScore;
                                    cache.SetScore(temp.lab, tempScore);
                                }
                                else
                                {
                                    scores[s] = cache.GetScore(temp.lab);
                                    reuse++;
                                }
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
                                bestRGB = swatches.colors[s];
                            }
                        }


                        if (!option.lab[i].Equals(bestRep))
                        {
                            option.lab[i] = bestRep;
                            option.colors[i] = bestRGB;
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
                Log(Path.Combine(dir, "out", "convergelog.txt"), "Trial " + t + " Key " + key + " BestScore: " + allScores[t] + " Steps: " + iters + " Time: " + watch.ElapsedMilliseconds);
                Log(Path.Combine(dir, "out", "convergelog.txt"), "Reused " + reuse);

            }

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
         * Generate candidate swatches for the given image filename (key) using k-means clustering
         * Save to a json file, sorted by hue, value, and saturation
         */ 
        private void GenerateCandidates(String key, int maxIters=50)
        {
            //generate candidate colors for this image
            //open image
            Bitmap image = new Bitmap(Path.Combine(dir, key));
            Directory.CreateDirectory(Path.Combine(dir, "swatches"));
            String filename = Path.Combine(dir,"swatches", Util.ConvertFileName(key,"",".json"));

            int numSeeds = 40;

            List<CIELAB> colors = Util.BitmapTo1DArray(image).Select(c=>Util.RGBtoLAB(c)).ToList<CIELAB>();

            //Get rid of duplicates
            colors = colors.Distinct<CIELAB>().ToList<CIELAB>();

            //a few trials
            int trials = 1;
            List<Cluster> clusters = new List<Cluster>();
            double bestScore = Double.PositiveInfinity;

            for (int t = 0; t < trials; t++)
            {
                List<Cluster> seeds = Clustering.InitializePictureSeeds(colors, numSeeds);
                double score = Clustering.KMeansPicture(colors, seeds, maxIters, null);
                if (score < bestScore)
                {
                    clusters = seeds;
                    bestScore = score;
                }
            }

            //Go through the image and pick the closest pixel to each swatch
            //This is pretty inefficient
            Dictionary<int, CIELAB> clusterToSwatch = new Dictionary<int, CIELAB>();
            foreach (CIELAB pixel in colors)
            {
                foreach (Cluster c in clusters)
                {
                    int id = c.id;
                    if (clusterToSwatch.ContainsKey(id))
                    {
                        //compare distances
                        if (c.lab.SqDist(pixel) < c.lab.SqDist(clusterToSwatch[id]))
                        {
                            clusterToSwatch[id] = pixel;
                        }
                    }
                    else
                    {
                        clusterToSwatch.Add(id, pixel);
                    }
                }
            }

            //Might not be guaranteed to pick unique colors
            //so, eliminate duplicates, then add to get back to right number
            List<CIELAB> swatches = new List<CIELAB>();
            foreach (int id in clusterToSwatch.Keys)
            {
                swatches.Add(clusterToSwatch[id]);
            }
            swatches = swatches.Distinct<CIELAB>().ToList<CIELAB>();

            if (swatches.Count() < numSeeds)
            {
                //go through colors and add until we reach numSeeds
                foreach (CIELAB c in colors)
                {
                    if (!swatches.Contains(c))
                        swatches.Add(c);
                    if (swatches.Count() == numSeeds)
                        break;
                }
            }


            List<Color> rgbSwatches = new List<Color>();
            foreach (CIELAB l in swatches)
            {
                rgbSwatches.Add(Util.LABtoRGB(l));
            }
       
            rgbSwatches.Sort(delegate(Color a, Color b)
            {
                double dH = a.GetHue() - b.GetHue();
                double dV = b.GetBrightness() - a.GetBrightness();
                double dS = b.GetSaturation() - a.GetSaturation();

                if (Math.Abs(dH) < 1.0)
                {
                    if (Math.Abs(dV) < 0.01)
                    {
                        if (Math.Abs(dS) < 0.01)
                            return 0;
                        return Math.Sign(dS);
                    }
                    return Math.Sign(dV);
                }
                return Math.Sign(dH);
            });


            List<String> lines = new List<String>();

            //Write out to JSON
            foreach (Color c in rgbSwatches)
            {
                lines.Add("{\"r\":" + c.R + ", \"g\":" + c.G + ", \"b\":" + c.B + ", \"h\":" + c.GetHue() + ", \"s\":" + c.GetSaturation() + ", \"v\":" + c.GetBrightness() + "}");
            }
            String bigLine = "[" + String.Join(",", lines) + "]";

            System.IO.File.WriteAllText(filename, bigLine);

        }


    }
}
