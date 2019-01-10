using System.Windows;
using FacialRecognition_Oxford.Data;
using System;
using System.ComponentModel;
using System.Windows.Controls;
using FacialRecognition_Oxford.Misc;
using LiveCharts;
using LiveCharts.Wpf;
using LiveCharts.Defaults;
using Microsoft.ProjectOxford.Face.Contract;
using System.Collections;
using System.Collections.Generic;
using LiveCharts.Configurations;
using System.Threading;
using System.Diagnostics;

/*
* Install-Package LiveCharts.Wpf
*/

namespace FacialRecognition_Oxford.Windows
{
    /// <summary>
    /// Interaction logic for StatisticsWindow.xaml
    /// </summary>
    public partial class StatisticsWindow : Window
    {

        #region var
        public Func<ChartPoint, string> PointLabel { get; set; }
        public new bool IsLoaded = false;
        public ChartValues<double> RamValues { get; set; }

        #region StackedAreaValues
        public ChartValues<double> BlondValues { get; set; }
        public ChartValues<double> BrownValues { get; set; }
        public ChartValues<double> BlackValues { get; set; }
        public ChartValues<double> GrayValues { get; set; }
        public ChartValues<double> RedValues { get; set; }
        public ChartValues<double> WhiteValues { get; set; }
        public ChartValues<double> OtherValues { get; set; }
        private int blondCnt = 0, brownCnt = 0, blackCnt = 0, grayCnt = 0, redCnt = 0, whiteCnt = 0, otherCnt = 0;
        #endregion StackedAreaValues

        public SeriesCollection SeriesCollection { get; set; }
        public Func<double, string> XFormatter { get; set; }
        public Func<double, string> YFormatter { get; set; }
        #endregion var

        #region events
        private void Window_Loaded(object sender, RoutedEventArgs e) => IsLoaded = true;

        private void Reset_Click(object sender, RoutedEventArgs e) => MainWindow.StatisticsData = new StatisticsData();


        private void Resize_Click(object sender, RoutedEventArgs e) => ResizingWindow();
        private void Resize_SizeChanged(object sender, SizeChangedEventArgs e) => ResizingWindow();

        /// <summary>
        /// Resizing the windows
        /// </summary>
        private void ResizingWindow()
        {
            double width = this.Width, height = this.Height, topMargin = ToolBar.Height + 2, leftMargin = 2, bottomMargin = 2, rightMargin = 2;

            PieChartGender.Margin = new Thickness(leftMargin, topMargin, width / 2, height / 2);
            HappinessGauge.Margin = new Thickness(width / 2, topMargin, rightMargin, height / 2);
            HairColourLineChart.Margin = new Thickness(leftMargin, height / 2, width / 2, bottomMargin);
            RamChart.Margin = new Thickness(width/2, height/2,rightMargin,bottomMargin+5);
        }
        #endregion events


        public StatisticsWindow()
        {
            InitializeComponent();
            PointLabel = chartPoint => $"{chartPoint.Y} ({chartPoint.Participation:P})";
            RamValues = new ChartValues<double> { 0 };
            BlondValues = new ChartValues<double> { 0 };
            BrownValues = new ChartValues<double> { 0 };
            BlackValues = new ChartValues<double> { 0 };
            GrayValues = new ChartValues<double> { 0 };
            RedValues = new ChartValues<double> { 0 };
            WhiteValues = new ChartValues<double> { 0 };
            OtherValues = new ChartValues<double> { 0 };

            SeriesCollection = new SeriesCollection
            {
                new StackedAreaSeries
                {
                    Title = "Blond",
                     Values =  BlondValues
                },
                new StackedAreaSeries
                {
                    Title = "Brown",
                     Values = BrownValues
                },
                new StackedAreaSeries
                {
                    Title = "Black",
                     Values = BlackValues
                },
                new StackedAreaSeries
                {
                    Title = "Gray",
                     Values = GrayValues
                },
                new StackedAreaSeries
                {
                    Title = "Red",
                     Values = RedValues
                },
                new StackedAreaSeries
                {
                    Title = "White",
                     Values = WhiteValues
                },
                new StackedAreaSeries
                {
                    Title = "Other",
                     Values = OtherValues
                }
            };

            Thread ramThread = new Thread(RamThread);
            ramThread.Start();

            DataContext = this;
        }

        private void RamThread()
        {
            Helper.ConsoleLog("started ram");
            while (true)
            {
                PerformanceCounter pCounter = new PerformanceCounter("Memory", "Committed Bytes");
                double ramMem = pCounter.RawValue / 10000;

                RamValues.Add(ramMem);
                Thread.Sleep(100);

                if (RamValues.Count > 50)
                    RamValues.RemoveAt(0);
            }
        }

        /// <summary> Sets the statistic </summary>
        public void SetStatistics(StatisticsData data)
        {
            MalePieSeries.Dispatcher.Invoke(
                () => MalePieSeries.Values = new ChartValues<int>() { data.AmountMale });
            FemalePieSeries.Dispatcher.Invoke(
                () => FemalePieSeries.Values = new ChartValues<int>() { data.AmountFemale });

            AddNewestHairColour(data);
        }

        private void AddNewestHairColour(StatisticsData data)
        {
            string hairColourString = data.HairColors[data.HairColors.Count - 1].ToString().ToLower();
            Helper.ConsoleLog(hairColourString);
            switch (hairColourString)
            {
                case "blond":
                    BlondValues.Remove(BlondValues.Count);
                    blondCnt++;
                    BlondValues.Add(blondCnt);

                    BrownValues.Remove(BrownValues.Count);
                    BrownValues.Add(brownCnt);
                    BlackValues.Remove(BlackValues.Count);
                    BlackValues.Add(blackCnt);
                    GrayValues.Remove(GrayValues.Count);
                    GrayValues.Add(grayCnt);
                    RedValues.Remove(RedValues.Count);
                    RedValues.Add(redCnt);
                    WhiteValues.Remove(WhiteValues.Count);
                    WhiteValues.Add(whiteCnt);
                    OtherValues.Remove(OtherValues.Count);
                    OtherValues.Add(otherCnt);
                    break;
                case "brown":
                    BrownValues.Remove(BrownValues.Count);
                    brownCnt++;
                    BrownValues.Add(brownCnt);
           

                   
                    BlondValues.Remove(BlondValues.Count);
                    BlondValues.Add(blondCnt);
                    BlackValues.Remove(BlackValues.Count);
                    BlackValues.Add(blackCnt);
                    GrayValues.Remove(GrayValues.Count);
                    GrayValues.Add(grayCnt);
                    RedValues.Remove(RedValues.Count);
                    RedValues.Add(redCnt);
                    WhiteValues.Remove(WhiteValues.Count);
                    WhiteValues.Add(whiteCnt);
                    OtherValues.Remove(OtherValues.Count);
                    OtherValues.Add(otherCnt);
                    break;
                case "black":
                    BlackValues.Remove(BlackValues.Count);
                    blackCnt++;
                    BlackValues.Add(blackCnt);



                    BlondValues.Remove(BlondValues.Count);
                    BlondValues.Add(blondCnt);
                    BrownValues.Remove(BrownValues.Count);
                    BrownValues.Add(brownCnt);
                    GrayValues.Remove(GrayValues.Count);
                    GrayValues.Add(grayCnt);
                    RedValues.Remove(RedValues.Count);
                    RedValues.Add(redCnt);
                    WhiteValues.Remove(WhiteValues.Count);
                    WhiteValues.Add(whiteCnt);
                    OtherValues.Remove(OtherValues.Count);
                    OtherValues.Add(otherCnt);
                    break;
                case "gray":
                    GrayValues.Remove(GrayValues.Count);
                    grayCnt++;
                    GrayValues.Add(grayCnt);



                    BlondValues.Remove(BlondValues.Count);
                    BlondValues.Add(blondCnt);
                    BlackValues.Remove(BlackValues.Count);
                    BlackValues.Add(blackCnt);
                    BrownValues.Remove(BrownValues.Count);
                    BrownValues.Add(brownCnt);
                    RedValues.Remove(RedValues.Count);
                    RedValues.Add(redCnt);
                    WhiteValues.Remove(WhiteValues.Count);
                    WhiteValues.Add(whiteCnt);
                    OtherValues.Remove(OtherValues.Count);
                    OtherValues.Add(otherCnt);
                    break;
                case "red":
                    RedValues.Remove(RedValues.Count);
                    redCnt++;
                    RedValues.Add(redCnt);


                    BlondValues.Remove(BlondValues.Count);
                    BlondValues.Add(blondCnt);
                    BlackValues.Remove(BlackValues.Count);
                    BlackValues.Add(blackCnt);
                    GrayValues.Remove(GrayValues.Count);
                    GrayValues.Add(grayCnt);
                    BrownValues.Remove(BrownValues.Count);
                    BrownValues.Add(brownCnt);
                    WhiteValues.Remove(WhiteValues.Count);
                    WhiteValues.Add(whiteCnt);
                    OtherValues.Remove(OtherValues.Count);
                    OtherValues.Add(otherCnt);
                    break;
                case "white":
                    WhiteValues.Remove(WhiteValues.Count);
                    whiteCnt++;
                    WhiteValues.Add(whiteCnt);



                    BlondValues.Remove(BlondValues.Count);
                    BlondValues.Add(blondCnt);
                    BlackValues.Remove(BlackValues.Count);
                    BlackValues.Add(blackCnt);
                    GrayValues.Remove(GrayValues.Count);
                    GrayValues.Add(grayCnt);
                    RedValues.Remove(RedValues.Count);
                    RedValues.Add(redCnt);
                    BrownValues.Remove(BrownValues.Count);
                    BrownValues.Add(brownCnt);
                    OtherValues.Remove(OtherValues.Count);
                    OtherValues.Add(otherCnt);
                    break;
                default:
                    OtherValues.Remove(OtherValues.Count);
                    otherCnt++;
                    OtherValues.Add(otherCnt);



                    BlondValues.Remove(BlondValues.Count);
                    BlondValues.Add(blondCnt);
                    BlackValues.Remove(BlackValues.Count);
                    BlackValues.Add(blackCnt);
                    GrayValues.Remove(GrayValues.Count);
                    GrayValues.Add(grayCnt);
                    RedValues.Remove(RedValues.Count);
                    RedValues.Add(redCnt);
                    WhiteValues.Remove(WhiteValues.Count);
                    WhiteValues.Add(whiteCnt);
                    BrownValues.Remove(BrownValues.Count);
                    BrownValues.Add(brownCnt);
                    break;
            }

        }

        /// <summary> Sets the happiness gauge in realtime</summary>
        public void SetHappinessGauge(double happiness) => HappinessGauge.Dispatcher.Invoke(
                () => HappinessGauge.Value = happiness * 100);
    }
}