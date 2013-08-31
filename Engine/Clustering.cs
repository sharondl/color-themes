using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Diagnostics;
using System.IO;

namespace Engine
{
    /**
     * For K-means Clustering
     **/
    public class Cluster
    {
        public int id;
        public CIELAB lab;
        private CIELAB sumlab;
        public double count;

        public Cluster()
        {
            lab = new CIELAB();
            sumlab = new CIELAB();
            count = 0;
            id = -1;
        }

        public CIELAB MeanColor()
        {
            if (count == 0)
                return lab;
            return new CIELAB(sumlab.L / (double)count, sumlab.A / (double)count, sumlab.B / (double)count);
        }

        public void AddColor(CIELAB color, double weight=1)
        {
            sumlab.L += color.L*weight;
            sumlab.A += color.A*weight;
            sumlab.B += color.B*weight;
            count += weight;
        }
        public void Reset()
        {
            lab = MeanColor();
            count = 0;
            sumlab = new CIELAB(0, 0, 0);
        }
    }

   
    public class Clustering
    {
        public static List<Cluster> InitializePictureSeeds(List<CIELAB> colors, int k)
        {
            //initialize k seeds, randomly choose colors in LAB space
            //find extents
            List<Cluster> seeds = new List<Cluster>();
            Random random = new Random();

            //sample colors in LAB bounding box
            double Lmin = double.PositiveInfinity;
            double Lmax = double.NegativeInfinity;
            double Amin = double.PositiveInfinity;
            double Amax = double.NegativeInfinity;
            double Bmin = double.PositiveInfinity;
            double Bmax = double.NegativeInfinity;

            for (int i = 0; i < colors.Count(); i++)
            {
                CIELAB lab = colors[i];
                Lmin = Math.Min(Lmin, lab.L);
                Lmax = Math.Max(Lmax, lab.L);
                Amin = Math.Min(Amin, lab.A);
                Amax = Math.Max(Amax, lab.A);
                Bmin = Math.Min(Bmin, lab.B);
                Bmax = Math.Max(Bmax, lab.B);
            }



            //initialize the seeds (stratified) randomly
            //within the bounding box
            if (k <= 10)
            {
                for (int i = 0; i < k; i++)
                {
                    double L = random.NextDouble() * (Lmax - Lmin) + Lmin;
                    double A = random.NextDouble() * (Amax - Amin) + Amin;
                    double B = random.NextDouble() * (Bmax - Bmin) + Bmin;
                    CIELAB seed = new CIELAB(L, A, B);
                    Cluster cluster = new Cluster();
                    cluster.id = i;
                    cluster.lab = seed;
                    seeds.Add(cluster);
                }
            }
            else
            {
                //stratified
                //find closest floor perfect square. 
                //TODO: need to generalize this better, doesn't work for non-perfect squares
                int sideLength = 2;

                int numSamples = (int)Math.Floor(k / (double)(sideLength*sideLength*sideLength));
                int i = 0;

                for (int l = 0; l < sideLength; l++)
                {
                    double dLmax = (Lmax - Lmin) / sideLength * (l+1) + Lmin;
                    double dLmin = (Lmax - Lmin) / sideLength * l + Lmin;

                    for (int a = 0; a < sideLength; a++)
                    {
                        double dAmax = (Amax - Amin) / sideLength * (a + 1) + Amin;
                        double dAmin = (Amax - Amin) / sideLength * a + Amin;

                        for (int b = 0; b < sideLength; b++)
                        {
                            double dBmax = (Bmax - Bmin) / sideLength * (b + 1) + Bmin;
                            double dBmin = (Bmax - Bmin) / sideLength * b + Bmin;

                            int dSamples = numSamples;

                            if (b == sideLength - 1 && a == sideLength - 1 && l == sideLength - 1)
                            {
                                //figure out leftovers
                                dSamples = k - numSamples*(sideLength * sideLength * sideLength - 1);

                            }
                            for (int s = 0; s < dSamples; s++)
                            {
                                double L = random.NextDouble() * (dLmax - dLmin) + dLmin;
                                double A = random.NextDouble() * (dAmax - dAmin) + dAmin;
                                double B = random.NextDouble() * (dBmax - dBmin) + dBmin;
                                CIELAB seed = new CIELAB(L, A, B);
                                Cluster cluster = new Cluster();
                                cluster.id = i;
                                cluster.lab = seed;
                                seeds.Add(cluster);
                                i++;
                            }

                        }
                    }
                }


            }


            return seeds;
        }

        public static double CMeansPicture(List<CIELAB> colors, List<Cluster> seeds, int m=2)
        {
            //cluster colors given seeds
            //return score
            List<double> weights = new List<double>();
            double[,] memberships = new double[colors.Count(), seeds.Count()]; //pixel to cluster
            int numSeeds = seeds.Count();
            int numColors = colors.Count();

            int maxIters = 50;//100;
            double epsilon = 0.0001;

            double J = Double.PositiveInfinity;

            int changes = 0;
            for (int t = 0; t < maxIters; t++)
            {
                changes = 0;
                for (int i = 0; i < colors.Count(); i++)
                {
                    //calculate the memberships
                    double[] dists = new double[numSeeds];
                    double factor = 0;
                    for (int k = 0; k < numSeeds; k++)
                    {
                        dists[k] = Math.Max(epsilon, Math.Pow(Math.Sqrt(colors[i].SqDist(seeds[k].lab)), 2.0/(m-1)));
                        factor += (1.0 / dists[k]);
                    }
                    for (int k = 0; k < numSeeds; k++)
                    {
                        double oldval = memberships[i, k];
                        memberships[i, k] = 1.0 / (dists[k] * factor);
                        if (oldval != memberships[i, k])
                            changes++;
                    }        
                }

                //update the centers
                for (int k = 0; k < numSeeds; k++)
                {
                    CIELAB center = new CIELAB();
                    double total = 0;
                    for (int i = 0; i < numColors; i++)
                    {
                        double u = Math.Pow(memberships[i, k], m);
                        center += colors[i]*u;
                        total += u;
                    }
                    center = center / total;
                    seeds[k].lab = center;
                }

                //find J
                double thisJ = 0;
                for (int i = 0; i < numColors; i++)
                {
                    for (int k = 0; k < numSeeds; k++)
                    {
                        double u = memberships[i, k];
                        thisJ += Math.Pow(u, m)* Math.Max(epsilon, seeds[k].lab.SqDist(colors[i]));
                    }
                }

                if (thisJ >= J)
                    break;

                J = thisJ;

                if (changes == 0)
                    break;
            }



            return J;

        }


        public static double KMeansPicture(List<CIELAB> colors, List<Cluster> seeds, int maxIters=50, List<double> inWeights=null)
        {
            //cluster colors given seeds
            //return score
            List<double> weights = new List<double>();
            if (inWeights == null)
            {
                for (int i = 0; i < colors.Count(); i++)
                    weights.Add(1);
            }
            else
            {
                weights = new List<double>(inWeights);
            }

            Random r = new Random();

            //go through colors
            int[] assignments = new int[colors.Count()];
            int changes = 1;

            for (int t = 0; t < maxIters; t++)
            {
                changes = 0;
                for (int i = 0; i < colors.Count(); i++)
                {
                    double bestDist = double.PositiveInfinity;
                    int bestSeed = -1;

                    //go through seeds and pick best one
                    foreach (Cluster seed in seeds)
                    {
                        double dist = seed.lab.SqDist(colors[i]);
                        if (dist < bestDist)
                        {
                            bestDist = dist;
                            bestSeed = seed.id;
                        }
                    }

                    //check the assignment
                    if (assignments[i] != bestSeed)
                        changes++;
                    assignments[i] = bestSeed;
                    seeds[bestSeed].AddColor(colors[i],weights[i]);
                }

                //update means
                for (int i = 0; i < seeds.Count(); i++)
                {
                    //if seed is starved, try again, just pick a random color
                    if (seeds[i].count == 0)
                    {
                        seeds[i].lab = colors[r.Next(colors.Count)];
                        changes++;
                    }
                    //return double.PositiveInfinity;
                    else
                        seeds[i].Reset();
                }

                if (changes == 0)
                    break;
            }

            //find the sq error
            double score = 0;
            for (int i = 0; i < assignments.Count(); i++)
            {
                int seed = assignments[i];
                score += weights[i]*colors[i].SqDist(seeds[seed].lab);
            }

            return score;

        }
    }



}
