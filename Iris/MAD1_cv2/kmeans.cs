﻿using MAD.Data;
using MAD.Helpers;
using OxyPlot;
using OxyPlot.Series;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MAD
{
    public class KMeans
    {
        private ElucidanDistance ElucDist = new ElucidanDistance();

        private List<MeansPoint> _rawDataToCluster = new List<MeansPoint>();
        private List<MeansPoint> _normalizedDataToCluster = new List<MeansPoint>();
        private List<MeansPoint> _clusters = new List<MeansPoint>();
        private int _numberOfClusters = 0;
        private PlotModel graf = new PlotModel();

        private double sse = new double();
        private bool print = new bool();

        //Grafování rozložení x,y
        public void AddToGraph(List<double> x, List<double> y, int ClusterNum, byte r, byte g, byte b)
        {
            var scatterSeries = new ScatterSeries { MarkerType = MarkerType.Cross, MarkerStroke = OxyColor.FromRgb(r, g, b) };

            for (int i = 0; i < x.Count; i++)
            {
                scatterSeries.Points.Add(new ScatterPoint(x[i], y[i]));
            }

            graf.Series.Add(scatterSeries);
        }

        public void GenerateGraph()
        {
            var scatterSeries2 = new ScatterSeries { MarkerType = MarkerType.Diamond, MarkerStroke = OxyColor.FromRgb(0, 0, 0) };

            for (int i = 0; i < _clusters.Count; i++)
            {
                scatterSeries2.Points.Add(new ScatterPoint(_clusters[i].Width, _clusters[i].Length));
            }

            graf.Series.Add(scatterSeries2);

            using (var stream = File.Create("output/kmeans-" + _clusters.Count + ".pdf"))
            {
                var pdfExporter = new PdfExporter { Width = 1000, Height = 400 };
                pdfExporter.Export(graf, stream);
            }
        }

        public byte[] GetRandomColor()
        {
            System.Threading.Thread.Sleep(50);
            Random r = new Random(DateTime.UtcNow.Millisecond);

            return new byte[] { (byte)r.Next(0, 255), (byte)r.Next(0, 255), (byte)r.Next(0, 255) };
        }

        //Přidat data (Width X Lenght) do k-means
        public void InitData(List<double> Width, List<double> Lenght)
        {
            for (int i = 0; i < Lenght.Count(); i++)
            {
                _rawDataToCluster.Add(new MeansPoint(Width[i], Lenght[i]));
            }
        }

        //Normalizace bodů (Je třeba ?)
        private void NormalizeData()
        {
            double widthSum = 0.0;
            double lengthSum = 0.0;

            foreach (MeansPoint dataPoint in _rawDataToCluster)
            {
                widthSum += dataPoint.Width;
                lengthSum += dataPoint.Length;
            }

            double widthMean = widthSum / _rawDataToCluster.Count;
            double lengthMean = lengthSum / _rawDataToCluster.Count;

            double sumWidth = 0.0;
            double sumLength = 0.0;
            foreach (MeansPoint dataPoint in _rawDataToCluster)
            {
                sumWidth += Math.Pow(dataPoint.Width - widthMean, 2);
                sumLength += Math.Pow(dataPoint.Length - lengthMean, 2);
            }

            double widthSD = sumWidth / _rawDataToCluster.Count;
            double lengthSD = sumLength / _rawDataToCluster.Count;
            foreach (MeansPoint dataPoint in _rawDataToCluster)
            {
                _normalizedDataToCluster.Add(new MeansPoint()
                {
                    Width = (dataPoint.Width - widthMean) / widthSD,
                    Length = (dataPoint.Length - lengthMean) / lengthSD
                }
                );
            }

            //??
            _normalizedDataToCluster = _rawDataToCluster;
        }

        //Nastavit počet clusetru
        private void SetClusters(int num)
        {
            _numberOfClusters = num;
        }

        //Helper.: Test zda má cluster alespoň jeden přiřazený bod
        private bool IsClusterEmpty(List<MeansPoint> data)
        {
            var emptyCluster =
            data.GroupBy(s => s.Cluster).OrderBy(s => s.Key).Select(g => new { Cluster = g.Key, Count = g.Count() });

            foreach (var item in emptyCluster)
            {
                if (item.Count == 0)
                {
                    return true;
                }
            }
            return false;
        }

        //Inicializovat centroidy náhodně
        private void InitializeCentroids()
        {
            Random random = new Random(_numberOfClusters);

            //Prvních x budou centroidy clusterů
            for (int i = 0; i < _numberOfClusters; ++i)
            {
                _normalizedDataToCluster[i].Cluster = _rawDataToCluster[i].Cluster = i;
            }

            //Zbyty datapointu přiřadima do nahodných clusterů
            //Nějaké "aribtary clusters" u kterých dojde k přehodnocení
            for (int i = _numberOfClusters; i < _normalizedDataToCluster.Count; i++)
            {
                _normalizedDataToCluster[i].Cluster = _rawDataToCluster[i].Cluster = random.Next(0, _numberOfClusters);
            }
        }

        //Nalezení nejvhodnějšího centroidu
        //Počítáme průměr pro cluster
        private bool UpdateDataPointMeans()
        {
            if (IsClusterEmpty(_normalizedDataToCluster)) return false;

            var groupToComputeMeans = _normalizedDataToCluster.GroupBy(s => s.Cluster).OrderBy(s => s.Key);

            int clusterIndex = 0;
            double width = 0.0;
            double length = 0.0;

            foreach (var item in groupToComputeMeans)
            {
                foreach (var value in item)
                {
                    width += value.Width;
                    length += value.Length;
                }

                _clusters[clusterIndex].Width = width / item.Count();
                _clusters[clusterIndex].Length = length / item.Count();

                clusterIndex++;
                width = 0.0;
                length = 0.0;
            }
            return true;
        }

        //Přesunout MeansPointy do správných clusterů
        private bool UpdateClusterMembership()
        {
            MinIndex min = new MinIndex();

            bool changed = false;

            double[] distances = new double[_numberOfClusters];

            double[] sse_count = new double[_numberOfClusters];

            double result = 0;

            for (int i = 0; i < _normalizedDataToCluster.Count; ++i)
            {
                for (int k = 0; k < _numberOfClusters; ++k)
                {
                    distances[k] = ElucDist.Get(_normalizedDataToCluster[i], _clusters[k]);
                }

                int newClusterId = min.Get(distances);

                if (newClusterId != _normalizedDataToCluster[i].Cluster)
                {
                    changed = true;
                    _normalizedDataToCluster[i].Cluster = _rawDataToCluster[i].Cluster = newClusterId;

                    if (print)
                    {
                        Console.WriteLine("Width: " + _rawDataToCluster[i].Width + ", Lenght: " +
                        _rawDataToCluster[i].Length + " ----> Cluster " + newClusterId);
                    }
                }
                //SSE pro cluster
                sse_count[newClusterId] = sse_count[newClusterId] + distances.Min();
            }

            //SSE celkové
            for (int o = 0; o < sse_count.Length; o++)
            { result = result + sse_count[o]; }
            sse = result;

            if (changed == false)
                return false;
            if (IsClusterEmpty(_normalizedDataToCluster)) return false;
            return true;
        }

        public void SSE()
        {
            double result = 0;
            double result_cluster = 0;

            for (int i = 0; i < _clusters.Count; i++)
            {
                for (int z = 0; z < _rawDataToCluster.Count; z++)
                {
                    if (_rawDataToCluster[z].Cluster == _clusters[i].Cluster)
                    {
                        result_cluster = result + ElucDist.Get(_rawDataToCluster[z], _clusters[i]);
                    }
                }

                result = result + result_cluster;
            }

            Console.WriteLine("SSE: " + result.ToString());
        }

        public void Execute(int NumberOfClusters, bool print)
        {
            this.print = print;

            SetClusters(NumberOfClusters);

            for (int i = 0; i < _numberOfClusters; i++)
            {
                _clusters.Add(new MeansPoint() { Cluster = i });
            }

            bool _changed = true;
            bool _success = true;

            NormalizeData();
            InitializeCentroids();

            int maxIteration = _normalizedDataToCluster.Count * 1000000;
            int _threshold = 0;

            while (_success == true && _changed == true && _threshold < maxIteration)
            {
                ++_threshold;
                _success = UpdateDataPointMeans();
                _changed = UpdateClusterMembership();
            }

            var group = _rawDataToCluster.GroupBy(s => s.Cluster).OrderBy(s => s.Key);

            foreach (var g in group)
            {
                if (print)
                {
                    Console.WriteLine("Cluster " + g.Key + ":");
                }

                List<double> Wid = new List<double>();
                List<double> Len = new List<double>();

                foreach (var value in g)
                {
                    if (print)
                        Console.WriteLine(value.ToString());

                    Wid.Add(value.Width);
                    Len.Add(value.Length);
                }

                RandomColor random = new RandomColor();
                byte[] color = random.Get();

                AddToGraph(Wid, Len, g.Key, color[0], color[1], color[2]);

                if (print)
                {
                    Console.WriteLine("------------------------------");
                }
            }

            GenerateGraph();
            Console.WriteLine("SSE pro k = " + _clusters.Count + " je " + sse);
        }

        /// <summary>
        /// Run k-means algorithm for multiple cluster values and generate SSE value.
        /// </summary>
        public void GenerateSSE(List<double> petalwid_list, List<double> petallen_list, List<double> sepalwid_list, List<double> sepallen_list)
        {
            KMeans k_means = new KMeans();
            //k_means.InitData(petalwid_list, petallen_list);
            k_means.InitData(sepalwid_list, sepallen_list);
            k_means.Execute(1, false);

            KMeans k_means2 = new KMeans();
            //k_means2.InitData(petalwid_list, petallen_list);
            k_means2.InitData(sepalwid_list, sepallen_list);
            k_means2.Execute(2, false);

            KMeans k_means3 = new KMeans();
            //k_means3.InitData(petalwid_list, petallen_list);
            k_means3.InitData(sepalwid_list, sepallen_list);
            k_means3.Execute(3, false);

            KMeans k_means4 = new KMeans();
            //k_means4.InitData(petalwid_list, petallen_list);
            k_means4.InitData(sepalwid_list, sepallen_list);
            k_means4.Execute(4, false);

            KMeans k_means5 = new KMeans();
            //k_means5.InitData(petalwid_list, petallen_list);
            k_means5.InitData(sepalwid_list, sepallen_list);
            k_means5.Execute(5, false);

            KMeans k_means6 = new KMeans();
            //k_means6.InitData(petalwid_list, petallen_list);
            k_means6.InitData(sepalwid_list, sepallen_list);
            k_means6.Execute(6, false);
        }
    }
}