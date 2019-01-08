using System.Windows;
using FacialRecognition_Oxford.Data;
using System;
using System.ComponentModel;
using System.Windows.Controls;
using FacialRecognition_Oxford.Misc;
using LiveCharts;
using LiveCharts.Wpf;
using LiveCharts.Defaults;
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
        public Func<ChartPoint, string> PointLabel { get; set; }
        public new bool IsLoaded = false;

        public SeriesCollection SeriesCollection { get; set; }
        public Func<double, string> XFormatter { get; set; }
        public Func<double, string> YFormatter { get; set; }


        public StatisticsWindow()
        {
            InitializeComponent();
            PointLabel = chartPoint => $"{chartPoint.Y} ({chartPoint.Participation:P})";

            SeriesCollection = new SeriesCollection
            {
                new StackedAreaSeries
                {
                    Title = "Blond"
                },
                new StackedAreaSeries
                {
                    Title = "Brown"
                },
                new StackedAreaSeries
                {
                    Title = "Black"
                },
                new StackedAreaSeries
                {
                    Title = "Gray"
                },
                new StackedAreaSeries
                {
                    Title = "Red"
                },
                new StackedAreaSeries
                {
                    Title = "White"
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
        }

        /// <summary> Sets the happiness gauge in realtime </summary>
        public void SetHappinessGauge(double happiness) => HappinessGauge.Dispatcher.Invoke(
                () => HappinessGauge.Value = happiness * 100);

        private void Window_Loaded(object sender, RoutedEventArgs e) => IsLoaded = true;

        private void Reset_Click(object sender, RoutedEventArgs e) => MainWindow.StatisticsData = new StatisticsData();
    }
}