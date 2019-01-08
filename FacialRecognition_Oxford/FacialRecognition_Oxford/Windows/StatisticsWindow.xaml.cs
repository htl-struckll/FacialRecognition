using System.Windows;
using FacialRecognition_Oxford.Data;
using System;
using System.ComponentModel;
using System.Windows.Controls;
using FacialRecognition_Oxford.Misc;
using LiveCharts;
using LiveCharts.Wpf;
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

        public StatisticsWindow()
        {
            InitializeComponent();
            PointLabel = chartPoint => $"{chartPoint.Y} ({chartPoint.Participation:P})";

            DataContext = this;
        }


        public void SetMaleFemmalePieChart(StatisticsData data)
        {
            Helper.ConsoleLog("Setting: " + data);

            MalePieSeries.Dispatcher.Invoke(
                () => MalePieSeries.Values = new ChartValues<int>() { data.AmountMale });
            FemalePieSeries.Dispatcher.Invoke(
                () => FemalePieSeries.Values = new ChartValues<int>() { data.AmountFemale });
        }

        /// <summary> Sets the happiness gauge in realtime</summary>
        public void SetHappinessGauge(double happiness) => HappinessGauge.Dispatcher.Invoke(
                () => HappinessGauge.Value = happiness * 100);

        private void Window_Loaded(object sender, RoutedEventArgs e) => IsLoaded = true;

        private void Reset_Click(object sender, RoutedEventArgs e) => MainWindow.StatisticsData = new StatisticsData();
    }
}