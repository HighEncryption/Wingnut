namespace Wingnut.UI.ViewModels
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.Linq;
    using System.Windows.Media;

    using LiveCharts;
    using LiveCharts.Configurations;
    using LiveCharts.Wpf;

    using Wingnut.Data.Models;
    using Wingnut.UI.Framework;

    public class DataSeriesViewModel : ViewModelBase
    {
        private bool isEnabled;

        public bool IsEnabled
        {
            get => this.isEnabled;
            set => this.SetProperty(ref this.isEnabled, value);
        }

        public string DisplayName { get; }

        public string VariableName { get; }

        public Brush Brush { get; }

        public LineSeries LineSeries { get; }

        public DataSeriesViewModel(
            string variableName,
            string displayName,
            Brush brush)
        {
            this.VariableName = variableName;
            this.DisplayName = displayName;
            this.Brush = brush;

            this.LineSeries = new LineSeries()
            {
                PointGeometry = null,
                LineSmoothness = 1,
                StrokeThickness = 2,
                Stroke = this.Brush,
                Fill = Brushes.Transparent,
                Values = new ChartValues<MeasureModel>()
            };
        }
    }

    public class EnergyUsagePageViewModel : PageViewModel
    {
        private readonly List<Brush> seriesBrushes = new List<Brush>()
        {
            Brushes.Red,
            Brushes.Green,
            Brushes.Blue,
            Brushes.DarkCyan,
            Brushes.Aquamarine,
            Brushes.Brown,
            Brushes.DarkOrange,
            Brushes.CornflowerBlue,
            Brushes.Fuchsia,
            Brushes.Gray,
            Brushes.Lime,
        };

        private int lastSeriesBrushIndex = 0;

        private ObservableCollection<DataSeriesViewModel> seriesViewModels;

        public ObservableCollection<DataSeriesViewModel> SeriesViewModels =>
            this.seriesViewModels ?? (this.seriesViewModels = new ObservableCollection<DataSeriesViewModel>());

        private UpsDeviceViewModel currentDevice;

        public EnergyUsagePageViewModel()
        {
            this.NavigationHeader = "Energy Usage";
            this.PageHeader = "Energy Usage";
            this.Glyph = "\uE9D2";

            var mapper = Mappers.Xy<MeasureModel>()
                .X(model => model.DateTime.Ticks)   // Use DateTime.Ticks as X
                .Y(model => model.Value);           // Use the value property as Y

            // Save the mapper globally.
            Charting.For<MeasureModel>(mapper);

            //lets set how to display the X Labels
            DateTimeFormatter = value => new DateTime((long)value).ToString("hh:mm:ss");

            //AxisStep forces the distance between each separator in the X axis
            AxisStep = TimeSpan.FromSeconds(10).Ticks;

            //AxisUnit forces lets the axis know that we are plotting seconds
            //this is not always necessary, but it can prevent wrong labeling
            AxisUnit = TimeSpan.TicksPerSecond;

            SetAxisLimits(DateTime.Now);

            this.PropertyChanged += HandlePropertyChange;

            DataSeries = new SeriesCollection();
        }

        private void HandlePropertyChange(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ActiveDevice))
            {
                if (this.currentDevice != null)
                {
                    this.currentDevice.NewMetrics -= HandleNewMetrics;
                }

                // Check for null in case device is being removed
                if (this.ActiveDevice != null)
                {
                    this.ActiveDevice.NewMetrics += HandleNewMetrics;
                }

                this.currentDevice = this.ActiveDevice;
                this.lastSeriesBrushIndex = 0;
                this.DataSeries.Clear();
            }
        }

        private void HandleNewMetrics(object sender, NewMetricsEventArgs e)
        {
            foreach (MetricMeasurement metric in e.Metrics)
            {
                DataSeriesViewModel seriesViewModel =
                    this.SeriesViewModels.FirstOrDefault(
                        s => s.VariableName == metric.VariableName);

                if (seriesViewModel == null)
                {
                    App.DispatcherInvoke(() =>
                    {
                        seriesViewModel = new DataSeriesViewModel(
                            metric.VariableName,
                            MetricToDisplayName(metric.VariableName),
                            this.seriesBrushes[this.lastSeriesBrushIndex % this.seriesBrushes.Count]);

                        seriesViewModel.PropertyChanged += HandleSeriesViewModelPropertyChanged;

                        this.lastSeriesBrushIndex++;

                        this.SeriesViewModels.Add(seriesViewModel);
                    });
                }

                seriesViewModel.LineSeries.Values.Add(new MeasureModel()
                {
                    DateTime = metric.Timestamp.ToLocalTime(),
                    Value = metric.Value
                });
            }

            SetAxisLimits(DateTime.Now);
        }

        private void HandleSeriesViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(DataSeriesViewModel.IsEnabled))
            {
                return;
            }

            DataSeriesViewModel viewModel = (DataSeriesViewModel) sender;

            if (viewModel.IsEnabled)
            {
                this.DataSeries.Add(viewModel.LineSeries);
            }
            else
            {
                this.DataSeries.Remove(viewModel.LineSeries);
            }
        }

        private string MetricToDisplayName(string variableName)
        {
            var nameParts = variableName.Split('.');
            List<string> parts = new List<string>(nameParts.Length);

            foreach (string part in nameParts)
            {
                parts.Add(part.First().ToString().ToUpper() + part.Substring(1));
            }

            return string.Join(" ", parts);
        }

        public void Foo()
        {

            ActiveDevice.NewMetrics += (sender, args) =>
            {
                var metric =
                    args.Metrics.First(m => m.VariableName == "battery.voltage");

                var values = (ChartValues<MeasureModel>) DataSeries.First().Values;

                values.Add(new MeasureModel()
                    {
                        DateTime = metric.Timestamp.ToLocalTime(),
                        Value = metric.Value
                    });

                SetAxisLimits(DateTime.Now);
            };

        }

        private SeriesCollection dataSeries;

        public SeriesCollection DataSeries
        {
            get => this.dataSeries;
            set => this.SetProperty(ref this.dataSeries, value);
        }

        public Func<double, string> DateTimeFormatter { get; set; }
        public double AxisStep { get; set; }
        public double AxisUnit { get; set; }

        private double axisMax;

        public double AxisMax
        {
            get => this.axisMax;
            set => this.SetProperty(ref this.axisMax, value);
        }

        private double axisMin;

        public double AxisMin
        {
            get => this.axisMin;
            set => this.SetProperty(ref this.axisMin, value);
        }

        private void SetAxisLimits(DateTime now)
        {
            AxisMax = now.Ticks + TimeSpan.FromSeconds(1).Ticks; // lets force the axis to be 1 second ahead
            AxisMin = now.Ticks - TimeSpan.FromSeconds(60).Ticks; // and 8 seconds behind
        }
    }


    public class MeasureModel
    {
        public DateTime DateTime { get; set; }
        public double Value { get; set; }
    }
}