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
        }
        #endregion events


        public StatisticsWindow()
        {
            InitializeComponent();
            PointLabel = chartPoint => $"{chartPoint.Y} ({chartPoint.Participation:P})";

            SeriesCollection = new SeriesCollection
            {
                new StackedAreaSeries
                {
                    Title = "Blond",
                     Values = new ChartValues<int>(){0}
                },
                new StackedAreaSeries
                {
                    Title = "Brown",
                     Values = new ChartValues<int>(){0}
                },
                new StackedAreaSeries
                {
                    Title = "Black",
                     Values = new ChartValues<int>(){0}
                },
                new StackedAreaSeries
                {
                    Title = "Gray",
                     Values = new ChartValues<int>(){0}
                },
                new StackedAreaSeries
                {
                    Title = "Red",
                     Values = new ChartValues<int>(){0}
                },
                new StackedAreaSeries
                {
                    Title = "White",
                     Values = new ChartValues<int>(){0}
                },
                new StackedAreaSeries
                {
                    Title = "Other",
                     Values = new ChartValues<int>(){0}
                }
            };

            //Values = new ChartValues<DateTimePoint>
            //{
            //    new DateTimePoint(new DateTime(1950, 1, 1), .228),
            //    new DateTimePoint(new DateTime(1960, 1, 1), .285),
            //    new DateTimePoint(new DateTime(1970, 1, 1), .366),
            //    new DateTimePoint(new DateTime(1980, 1, 1), .478),
            //    new DateTimePoint(new DateTime(1990, 1, 1), .629),
            //    new DateTimePoint(new DateTime(2000, 1, 1), .808),
            //    new DateTimePoint(new DateTime(2010, 1, 1), 1.031),
            //    new DateTimePoint(new DateTime(2013, 1, 1), 1.110)
            //},
            //LineSmoothness = 0

            DataContext = this;
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

            if (hairColourString.Equals("blond"))
            {
                if (SeriesCollection[0].Values == null)
                    Helper.ConsoleLog("is null");
                else
                    Helper.ConsoleLog("not null");



            }
        }

        /// <summary> Sets the happiness gauge in realtime</summary>
        public void SetHappinessGauge(double happiness) => HappinessGauge.Dispatcher.Invoke(
                () => HappinessGauge.Value = happiness * 100);
    }
}