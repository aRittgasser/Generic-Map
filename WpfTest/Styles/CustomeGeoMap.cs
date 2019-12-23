using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Shapes;
using System.Xml;
using LiveCharts.Wpf;
using Path = System.Windows.Shapes.Path;

namespace WpfTest.Styles
{
    /// <summary>
    /// 
    /// </summary>
    /// <seealso cref="System.Windows.Controls.UserControl" />
    public class CustomeGeoMap : UserControl
    {

        LvcMap lvcMap;
        #region Constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="CustomeGeoMap"/> class.
        /// </summary>
        public CustomeGeoMap()
        {
            Canvas = new Canvas();
            Map = new Canvas();
            Canvas.ClipToBounds = true;
            Map.ClipToBounds = true;
            Canvas.Children.Add(Map);
            Content = Canvas;

            Canvas.SetBinding(WidthProperty,
                new Binding { Path = new PropertyPath(ActualWidthProperty), Source = this });
            Canvas.SetBinding(HeightProperty,
                new Binding { Path = new PropertyPath(ActualHeightProperty), Source = this });

            Lands = new Dictionary<string, MapData>();

            SetCurrentValue(DefaultLandFillProperty, new SolidColorBrush(Color.FromArgb(200, 255, 255, 255)));
            SetCurrentValue(LandStrokeProperty, new SolidColorBrush(Color.FromArgb(30, 55, 55, 55)));
            SetCurrentValue(LandStrokeThicknessProperty, 1.3d);
            SetCurrentValue(AnimationsSpeedProperty, TimeSpan.FromMilliseconds(500));
            SetCurrentValue(BackgroundProperty, new SolidColorBrush(Color.FromArgb(150, 96, 125, 138)));
            SetCurrentValue(GradientStopCollectionProperty, new GradientStopCollection
            {
                new GradientStop(Color.FromArgb(100,2,119,188), 0d),
                new GradientStop(Color.FromRgb(2,119,188), 1d),
            });
            SetCurrentValue(HeatMapProperty, new Dictionary<string, double>());


            SetCurrentValue(GeoMapTooltipProperty, new DefaultGeoMapTooltip { Visibility = Visibility.Hidden });
            Canvas.Children.Add(GeoMapTooltip);

            SizeChanged += (sender, e) =>
            {
                Draw();
            };

            MouseWheel += (sender, e) =>
            {
                if (!EnableZoomingAndPanning) return;

                e.Handled = true;
                var rt = Map.RenderTransform as ScaleTransform;
                var p = rt == null ? 1 : rt.ScaleX;
                p += e.Delta > 0 ? .05 : -.05;
                p = p < 1 ? 1 : p;
                var o = e.GetPosition(this);
                if (e.Delta > 0) Map.RenderTransformOrigin = new Point(o.X / ActualWidth, o.Y / ActualHeight);
                Map.RenderTransform = new ScaleTransform(p, p);
            };

            MouseDown += (sender, e) =>
            {
                if (!EnableZoomingAndPanning) return;

                DragOrigin = e.GetPosition(this);
            };

            MouseUp += (sender, e) =>
            {
                if (!EnableZoomingAndPanning) return;

                var end = e.GetPosition(this);
                var delta = new Point(DragOrigin.X - end.X, DragOrigin.Y - end.Y);

                var l = Canvas.GetLeft(Map) - delta.X;
                var t = Canvas.GetTop(Map) - delta.Y;

                if (DisableAnimations)
                {
                    Canvas.SetLeft(Map, l);
                    Canvas.SetTop(Map, t);
                }
                else
                {
                    Map.BeginAnimation(Canvas.LeftProperty, new DoubleAnimation(l, AnimationsSpeed));
                    Map.BeginAnimation(Canvas.TopProperty, new DoubleAnimation(t, AnimationsSpeed));
                }


            };

            Loaded += (sender, e) =>
            {
                lvcMap = MapResolver.Get(Source);
                Draw();
            };

        }

        #endregion

        #region Events

        /// <summary>
        /// Occurs when [land click].
        /// </summary>
        public event Action<object, MapData> LandClick;

        #endregion

        #region Properties

        private Canvas Canvas { get; set; }
        private Canvas Map { get; set; }
        private Point DragOrigin { get; set; }
        private Point OriginalPosition { get; set; }
        private bool IsDrawn { get; set; }
        private bool IsWidthDominant { get; set; }
        private Dictionary<string, MapData> Lands { get; set; }

        private static readonly DependencyProperty GeoMapTooltipProperty = DependencyProperty.Register(
            "GeoMapTooltip", typeof(DefaultGeoMapTooltip), typeof(CustomeGeoMap), new PropertyMetadata(default(DefaultGeoMapTooltip)));
        private DefaultGeoMapTooltip GeoMapTooltip
        {
            get { return (DefaultGeoMapTooltip)GetValue(GeoMapTooltipProperty); }
            set => SetValue(GeoMapTooltipProperty, value);
        }

        /// <summary>
        /// The language pack property
        /// </summary>
        public static readonly DependencyProperty LanguagePackProperty = DependencyProperty.Register(
            "LanguagePack", typeof(Dictionary<string, string>), typeof(CustomeGeoMap), new PropertyMetadata(default(Dictionary<string, string>)));
        /// <summary>
        /// Gets or sets the language dictionary
        /// </summary>
        public Dictionary<string, string> LanguagePack
        {
            get { return (Dictionary<string, string>)GetValue(LanguagePackProperty); }
            set { SetValue(LanguagePackProperty, value); }
        }

        /// <summary>
        /// The default land fill property
        /// </summary>
        public static readonly DependencyProperty DefaultLandFillProperty = DependencyProperty.Register(
            "DefaultLandFill", typeof(Brush), typeof(CustomeGeoMap), new PropertyMetadata(default(Brush)));
        /// <summary>
        /// Gets or sets default land fill
        /// </summary>
        public Brush DefaultLandFill
        {
            get { return (Brush)GetValue(DefaultLandFillProperty); }
            set
            {
                SetValue(DefaultLandFillProperty, value);
            }
        }

        /// <summary>
        /// The land stroke thickness property
        /// </summary>
        public static readonly DependencyProperty LandStrokeThicknessProperty = DependencyProperty.Register(
            "LandStrokeThickness", typeof(double), typeof(CustomeGeoMap), new PropertyMetadata(default(double)));
        /// <summary>
        /// Gets or sets every land stroke thickness property
        /// </summary>
        public double LandStrokeThickness
        {
            get { return (double)GetValue(LandStrokeThicknessProperty); }
            set { SetValue(LandStrokeThicknessProperty, value); }
        }

        /// <summary>
        /// The land stroke property
        /// </summary>
        public static readonly DependencyProperty LandStrokeProperty = DependencyProperty.Register(
            "LandStroke", typeof(Brush), typeof(CustomeGeoMap), new PropertyMetadata(default(Brush)));
        /// <summary>
        /// Gets or sets every land stroke
        /// </summary>
        public Brush LandStroke
        {
            get { return (Brush)GetValue(LandStrokeProperty); }
            set { SetValue(LandStrokeProperty, value); }
        }

        /// <summary>
        /// The disable animations property
        /// </summary>
        public static readonly DependencyProperty DisableAnimationsProperty = DependencyProperty.Register(
            "DisableAnimations", typeof(bool), typeof(CustomeGeoMap), new PropertyMetadata(default(bool)));
        /// <summary>
        /// Gets or sets whether the chart is animated
        /// </summary>
        public bool DisableAnimations
        {
            get { return (bool)GetValue(DisableAnimationsProperty); }
            set { SetValue(DisableAnimationsProperty, value); }
        }

        /// <summary>
        /// The animations speed property
        /// </summary>
        public static readonly DependencyProperty AnimationsSpeedProperty = DependencyProperty.Register(
            "AnimationsSpeed", typeof(TimeSpan), typeof(CustomeGeoMap), new PropertyMetadata(default(TimeSpan)));
        /// <summary>
        /// Gets or sets animations speed
        /// </summary>
        public TimeSpan AnimationsSpeed
        {
            get { return (TimeSpan)GetValue(AnimationsSpeedProperty); }
            set { SetValue(AnimationsSpeedProperty, value); }
        }

        /// <summary>
        /// The hoverable property
        /// </summary>
        public static readonly DependencyProperty HoverableProperty = DependencyProperty.Register(
            "Hoverable", typeof(bool), typeof(CustomeGeoMap), new PropertyMetadata(default(bool)));
        /// <summary>
        /// Gets or sets whether the chart reacts when a user moves the mouse over a land
        /// </summary>
        public bool Hoverable
        {
            get { return (bool)GetValue(HoverableProperty); }
            set { SetValue(HoverableProperty, value); }
        }

        /// <summary>
        /// The heat map property
        /// </summary>
        public static readonly DependencyProperty HeatMapProperty = DependencyProperty.Register(
            "HeatMap",
            typeof(Dictionary<string, double>),
            typeof(CustomeGeoMap),
            new PropertyMetadata(default(Dictionary<string, double>), OnHeapMapChanged));

        /// <summary>
        /// Gets or sets the current heat map
        /// </summary>
        public Dictionary<string, double> HeatMap
        {
            get { return (Dictionary<string, double>)GetValue(HeatMapProperty); }
            set
            {
                SetValue(HeatMapProperty, value);

            }
        }

        //=====================================================================================================================

        /// <summary>
        /// The Test property
        /// </summary>
        public static readonly DependencyProperty MapValuesProperty = DependencyProperty.Register(
            "MapValues",
            typeof(Dictionary<string, double>),
            typeof(CustomeGeoMap),
            new PropertyMetadata(default(Dictionary<string, double>), OnMapValuesChanged)
            );

        /// <summary>
        /// Gets or sets the current Test
        /// </summary>
        public Dictionary<string, double> MapValues
        {
            get => (Dictionary<string, double>)GetValue(MapValuesProperty);
            set => SetValue(MapValuesProperty, value);
        }

        private static void OnMapValuesChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var geoMap = (CustomeGeoMap)d;

            if (geoMap.HeatMap != null)
            {
                geoMap.HeatMap.Clear();

                foreach (var map in geoMap.MapValues)
                {
                    geoMap.HeatMap.Add(map.Key, map.Value);
                }
            }
            geoMap.ShowMeSomeHeat();
        }



        public static DependencyProperty SelectedLendCommandProperty
            = DependencyProperty.Register(
                "SelectedLendCommand",
                typeof(ICommand),
                typeof(CustomeGeoMap));

        public ICommand SelectedLendCommand
        {
            get => (ICommand)GetValue(SelectedLendCommandProperty);

            set => SetValue(SelectedLendCommandProperty, value);
        }



        public static DependencyProperty SelectedLandBackgroundProperty
            = DependencyProperty.Register(
                "SelectedLandBackground",
                typeof(Brush),
                typeof(CustomeGeoMap));

        public Brush SelectedLandBackground
        {
            get => (Brush)GetValue(SelectedLandBackgroundProperty);

            set => SetValue(SelectedLandBackgroundProperty, value);
        }


        public static DependencyProperty SelectedCountryProperty
            = DependencyProperty.Register(
                "SelectedCountry",
                typeof(string),
                typeof(CustomeGeoMap),
                new PropertyMetadata(default(string), OnSelectedCountryChanged));

        public string SelectedCountry
        {
            get => (string)GetValue(SelectedCountryProperty);

            set => SetValue(SelectedCountryProperty, value);
        }

        private static void OnSelectedCountryChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var geoMap = (CustomeGeoMap)d;

            if (geoMap.HeatMap != null && geoMap.SelectedCountry != null)
            {
                geoMap.HeatMap.Clear();

                geoMap.HeatMap.Add(geoMap.SelectedCountry.ToUpper(), 100);

            }

            if (geoMap.HeatMap != null && geoMap.SelectedCountry == null)
            {
                geoMap.HeatMap.Clear();
            }

            geoMap.ShowMeSomeHeat();

        }


        public static DependencyProperty EnableCountryHighlightProperty
            = DependencyProperty.Register(
                "EnableCountryHighlight",
                typeof(bool),
                typeof(CustomeGeoMap));

        public bool EnableCountryHighlight
        {
            get => (bool)GetValue(EnableCountryHighlightProperty);

            set => SetValue(EnableCountryHighlightProperty, value);
        }

        //=====================================================================================================================

        /// <summary>
        /// The gradient stop collection property
        /// </summary>
        public static readonly DependencyProperty GradientStopCollectionProperty = DependencyProperty.Register(
            "GradientStopCollection", typeof(GradientStopCollection), typeof(CustomeGeoMap), new PropertyMetadata(default(GradientStopCollection)));
        /// <summary>
        /// Gets or sets the gradient stop collection, use every gradient offset and color properties to define your gradient.
        /// </summary>
        public GradientStopCollection GradientStopCollection
        {
            get { return (GradientStopCollection)GetValue(GradientStopCollectionProperty); }
            set { SetValue(GradientStopCollectionProperty, value); }
        }

        /// <summary>
        /// The source property
        /// </summary>
        public static readonly DependencyProperty SourceProperty = DependencyProperty.Register(
            "Source", typeof(string), typeof(CustomeGeoMap), new PropertyMetadata(default(string)));
        /// <summary>
        /// Gets or sets the map source
        /// </summary>
        public string Source
        {
            get { return (string)GetValue(SourceProperty); }
            set { SetValue(SourceProperty, value); }
        }

        /// <summary>
        /// The enable zooming and panning property
        /// </summary>
        public static readonly DependencyProperty EnableZoomingAndPanningProperty = DependencyProperty.Register(
            "EnableZoomingAndPanning", typeof(bool), typeof(CustomeGeoMap), new PropertyMetadata(default(bool)));
        /// <summary>
        /// Gets or sets whether the map allows zooming and panning
        /// </summary>
        public bool EnableZoomingAndPanning
        {
            get { return (bool)GetValue(EnableZoomingAndPanningProperty); }
            set { SetValue(EnableZoomingAndPanningProperty, value); }
        }

        #endregion

        #region Publics

        ///// <summary>
        ///// Moves the map to a specif area
        ///// </summary>
        ///// <param name="data">target area</param>
        //public void MoveTo(MapData data)
        //{
        //    var s = (Path) data.Shape;
        //    var area = s.Data.Bounds;
        //    var t = (ScaleTransform) s.RenderTransform;



        //    //if (DisableAnimations)
        //    //{

        //    double scale;
        //    double cx = 0;
        //    double cy = 0;

        //    if (IsWidthDominant)
        //    {
        //        scale = data.LvcMap.DesiredWidth / area.Width;
        //    }
        //    else
        //    {
        //        scale = data.LvcMap.DesiredHeight / area.Height;
        //    }

        //    Map.RenderTransformOrigin = new Point(0, 0);
        //    Map.RenderTransform = new ScaleTransform(scale, scale);

        //    Canvas.SetLeft(Map, -area.X*t.ScaleX*scale + cx);
        //    Canvas.SetTop(Map, -area.Y*t.ScaleY*scale + cy);

        //    //}
        //    //else
        //    //{
        //    //    Map.BeginAnimation(Canvas.LeftProperty, new DoubleAnimation(-area.X, AnimationsSpeed));
        //    //    //Map.BeginAnimation(Canvas.TopProperty, new DoubleAnimation(area.Y, AnimationsSpeed));
        //    //}
        //}

        /// <summary>
        /// Restarts the current map view
        /// </summary>
        public void Restart()
        {
            Map.RenderTransform = new ScaleTransform(1, 1);
            if (DisableAnimations)
            {
                Canvas.SetLeft(Map, OriginalPosition.X);
                Canvas.SetTop(Map, OriginalPosition.Y);
            }
            else
            {
                Map.BeginAnimation(Canvas.LeftProperty, new DoubleAnimation(OriginalPosition.X, TimeSpan.FromMilliseconds(1)));
                Map.BeginAnimation(Canvas.TopProperty, new DoubleAnimation(OriginalPosition.Y, TimeSpan.FromMilliseconds(1)));
            }
        }

        /// <summary>
        /// Sets a heat map value with a given key, then updates every land heat color
        /// </summary>
        /// <param name="key">key</param>
        /// <param name="value">new value</param>
        public void UpdateKey(string key, double value)
        {
            HeatMap[key] = value;
            ShowMeSomeHeat();
        }

        #endregion

        #region Privates

        private void Draw()
        {
            IsDrawn = true;

            Map.Children.Clear();

            if (DesignerProperties.GetIsInDesignMode(this))
            {
                Map.Children.Add(new TextBlock
                {
                    Text = "Designer preview is not currently available",
                    Foreground = Brushes.White,
                    FontWeight = FontWeights.Bold,
                    FontSize = 12,
                    Effect = new DropShadowEffect
                    {
                        ShadowDepth = 2,
                        RenderingBias = RenderingBias.Performance
                    }
                });
                return;
            }

            //lvcMap = MapResolver.Get(Source);

            if (lvcMap == null) return;

            var desiredSize = new Size(lvcMap.DesiredWidth, lvcMap.DesiredHeight);
            var r = desiredSize.Width / desiredSize.Height;

            var wr = ActualWidth / desiredSize.Width;
            var hr = ActualHeight / desiredSize.Height;
            double s;

            if (wr < hr)
            {
                IsWidthDominant = true;
                Map.Width = ActualWidth;
                Map.Height = Map.Width / r;
                s = wr;
                OriginalPosition = new Point(0, (ActualHeight - Map.Height) * .5);
                Canvas.SetLeft(Map, OriginalPosition.X);
                Canvas.SetTop(Map, OriginalPosition.Y);
            }
            else
            {
                IsWidthDominant = false;
                Map.Height = ActualHeight;
                Map.Width = r * ActualHeight;
                s = hr;
                OriginalPosition = new Point((ActualWidth - Map.Width) * .5, 0d);
                Canvas.SetLeft(Map, OriginalPosition.X);
                Canvas.SetTop(Map, OriginalPosition.Y);
            }

            var t = new ScaleTransform(s, s);

            foreach (var land in lvcMap.Data)
            {
                var p = new Path
                {
                    Data = Geometry.Parse(land.Data),
                    RenderTransform = t
                };

                land.Shape = p;
                Lands[land.Id] = land;
                Map.Children.Add(p);

                p.MouseEnter += POnMouseEnter;
                p.MouseLeave += POnMouseLeave;
                p.MouseMove += POnMouseMove;
                p.MouseDown += POnMouseDown;

                p.SetBinding(Shape.StrokeProperty,
                    new Binding { Path = new PropertyPath(LandStrokeProperty), Source = this });
                p.SetBinding(Shape.StrokeThicknessProperty,
                    new MultiBinding
                    {
                        Converter = new ScaleStrokeConverter(),
                        Bindings =
                        {
                            new Binding("LandStrokeThickness") {Source = this},
                            new Binding("ScaleX") {Source = t}
                        }
                    });

            }

            ShowMeSomeHeat();
        }


        private void ShowMeSomeHeat()
        {
            var max = double.MinValue;
            var min = double.MaxValue;

            foreach (var i in HeatMap.Values)
            {
                max = i > max ? i : max;
                min = i < min ? i : min;
            }

            foreach (var land in Lands)
            {
                double temperature;
                var shape = ((Shape)land.Value.Shape);

                shape.SetBinding(Shape.FillProperty,
                    new Binding { Path = new PropertyPath(DefaultLandFillProperty), Source = this });

                if (!HeatMap.TryGetValue(land.Key, out temperature)) continue;

                temperature = LinealInterpolation(0, 1, min, max, temperature);


                if (DisableAnimations)
                {
                    shape.Fill = SelectedLandBackground ?? new SolidColorBrush(ColorInterpolation(temperature));

                }
                else if (!DisableAnimations && SelectedLandBackground is SolidColorBrush)
                {
                    shape.Fill = new SolidColorBrush();
                    ((SolidColorBrush)shape.Fill).BeginAnimation(SolidColorBrush.ColorProperty,
                        new ColorAnimation(SelectedLandBackground != null ? ((SolidColorBrush)SelectedLandBackground).Color : ColorInterpolation(temperature), AnimationsSpeed));
                }
                else if (!DisableAnimations && SelectedLandBackground is LinearGradientBrush)
                {
                    shape.Fill = new SolidColorBrush(ColorInterpolation(temperature));
                }
                else
                {
                    shape.Fill = SelectedLandBackground ?? new SolidColorBrush(ColorInterpolation(temperature));
                }

            }
        }


        private void POnMouseDown(object sender, MouseButtonEventArgs mouseButtonEventArgs)
        {
            var land = Lands.Values.FirstOrDefault(x => x.Shape == sender);
            if (land == null) return;

            if (LandClick != null)
            {
                LandClick.Invoke(sender, land);
            }


            SelectedLendCommand?.Execute(land);
        }

        private void POnMouseLeave(object sender, MouseEventArgs mouseEventArgs)
        {
            if (EnableCountryHighlight)
            {
                var path = (Path)sender;
                path.Opacity = 1;
            }

            GeoMapTooltip.Visibility = Visibility.Hidden;
        }

        private void POnMouseEnter(object sender, MouseEventArgs mouseEventArgs)
        {
            if (EnableCountryHighlight)
            {
                var path = (Path)sender;
                path.Opacity = .8;
            }


            var land = Lands.Values.FirstOrDefault(x => x.Shape == sender);
            if (land == null) return;

            double value;

            //todo fix this 'if' bug (it's try to find Land in heatmap to show 
            if (!HeatMap.TryGetValue(land.Id, out value)) return;
            if (!Hoverable) return;

            GeoMapTooltip.Visibility = Visibility.Visible;

            string name = null;

            if (LanguagePack != null) LanguagePack.TryGetValue(land.Id, out name);

            GeoMapTooltip.GeoData = new GeoData
            {
                Name = name ?? land.Name,
                Value = value
            };
        }

        private void POnMouseMove(object sender, MouseEventArgs mouseEventArgs)
        {
            var location = mouseEventArgs.GetPosition(this);
            Canvas.SetTop(GeoMapTooltip, location.Y + 5);
            Canvas.SetLeft(GeoMapTooltip, location.X + 5);
        }

        private Color ColorInterpolation(double weight)
        {
            Color from = Color.FromRgb(0, 0, 0), to = Color.FromRgb(0, 0, 0);
            double fromOffset = 0, toOffset = 0;

            for (var i = 0; i < GradientStopCollection.Count - 1; i++)
            {
                // ReSharper disable once InvertIf
                if (GradientStopCollection[i].Offset <= weight && GradientStopCollection[i + 1].Offset >= weight)
                {
                    from = GradientStopCollection[i].Color;
                    to = GradientStopCollection[i + 1].Color;

                    fromOffset = GradientStopCollection[i].Offset;
                    toOffset = GradientStopCollection[i + 1].Offset;

                    break;
                }
            }

            return Color.FromArgb(
                (byte)LinealInterpolation(from.A, to.A, fromOffset, toOffset, weight),
                (byte)LinealInterpolation(from.R, to.R, fromOffset, toOffset, weight),
                (byte)LinealInterpolation(from.G, to.G, fromOffset, toOffset, weight),
                (byte)LinealInterpolation(from.B, to.B, fromOffset, toOffset, weight));
        }

        private static double LinealInterpolation(double fromComponent, double toComponent,
            double fromOffset, double toOffset, double value)
        {
            var p1 = new Point(fromOffset, fromComponent);
            var p2 = new Point(toOffset, toComponent);

            var deltaX = p2.X - p1.X;
            // ReSharper disable once CompareOfFloatsByEqualityOperator
            var m = (p2.Y - p1.Y) / (deltaX == 0 ? double.MinValue : deltaX);

            return m * (value - p1.X) + p1.Y;
        }

        private static void OnHeapMapChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            var geoMap = (CustomeGeoMap)o;

            if (!geoMap.IsDrawn) return;

            geoMap.ShowMeSomeHeat();
        }

        #endregion
    }

    /// <summary>
    /// 
    /// </summary>
    /// <seealso cref="System.Windows.Data.IMultiValueConverter" />
    public class ScaleStrokeConverter : IMultiValueConverter
    {
        /// <summary>
        /// Converts source values to a value for the binding target. The data binding engine calls this method when it propagates the values from source bindings to the binding target.
        /// </summary>
        /// <param name="values">The array of values that the source bindings in the <see cref="T:System.Windows.Data.MultiBinding" /> produces. The value <see cref="F:System.Windows.DependencyProperty.UnsetValue" /> indicates that the source binding has no value to provide for conversion.</param>
        /// <param name="targetType">The type of the binding target property.</param>
        /// <param name="parameter">The converter parameter to use.</param>
        /// <param name="culture">The culture to use in the converter.</param>
        /// <returns>
        /// A converted value.If the method returns null, the valid null value is used.A return value of <see cref="T:System.Windows.DependencyProperty" />.<see cref="F:System.Windows.DependencyProperty.UnsetValue" /> indicates that the converter did not produce a value, and that the binding will use the <see cref="P:System.Windows.Data.BindingBase.FallbackValue" /> if it is available, or else will use the default value.A return value of <see cref="T:System.Windows.Data.Binding" />.<see cref="F:System.Windows.Data.Binding.DoNothing" /> indicates that the binding does not transfer the value or use the <see cref="P:System.Windows.Data.BindingBase.FallbackValue" /> or the default value.
        /// </returns>
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            return (double)values[0] / (double)values[1];
        }

        /// <summary>
        /// Converts a binding target value to the source binding values.
        /// </summary>
        /// <param name="value">The value that the binding target produces.</param>
        /// <param name="targetTypes">The array of types to convert to. The array length indicates the number and types of values that are suggested for the method to return.</param>
        /// <param name="parameter">The converter parameter to use.</param>
        /// <param name="culture">The culture to use in the converter.</param>
        /// <returns>
        /// An array of values that have been converted from the target value back to the source values.
        /// </returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    internal static class MapResolver
    {
        public static LvcMap Get(string file)
        {

            var isFromResource = false;

            var svgMap = new LvcMap
            {
                DesiredHeight = 600,
                DesiredWidth = 800,
                Data = new List<MapData>()
            };

            if (File.Exists(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, file)))
            {

                file = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, file);
            }
            else
            {
                isFromResource = true;
                file = System.Diagnostics.Process.GetCurrentProcess().ProcessName.Replace(@"\", "")
                       + file.Replace("/", ".").Replace(@"\", ".");
            }



            using (var stream = isFromResource
                                  ? Assembly.GetExecutingAssembly().GetManifestResourceStream(file)
                                  : File.Open(file, FileMode.Open))
            {
                if (stream == null) return null;

                using (var reader = XmlReader.Create(stream))
                {

                    while (reader.Read())
                    {
                        if (reader.Name == "Height") svgMap.DesiredHeight = double.Parse(reader.ReadInnerXml());
                        if (reader.Name == "Width") svgMap.DesiredWidth = double.Parse(reader.ReadInnerXml());
                        
                        if (reader.Name != "MapShape") continue;

                        var p = new MapData
                        {
                            LvcMap = svgMap
                        };
                        reader.Read();
                        while (reader.NodeType != XmlNodeType.EndElement)
                        {
                            if (reader.NodeType != XmlNodeType.Element) reader.Read();
                            if (reader.Name == "Id") p.Id = reader.ReadInnerXml();
                            if (reader.Name == "Name") p.Name = reader.ReadInnerXml();
                            if (reader.Name == "Path") p.Data = reader.ReadInnerXml();
                        }
                        svgMap.Data.Add(p);
                    }
                }

            }



            return svgMap;
        }
    }

    public class MapData
    {
        /// <summary>Gets or sets the identifier.</summary>
        /// <value>The identifier.</value>
        public string Id { get; set; }

        /// <summary>Gets or sets the name.</summary>
        /// <value>The name.</value>
        public string Name { get; set; }

        /// <summary>Gets or sets the data.</summary>
        /// <value>The data.</value>
        public string Data { get; set; }

        /// <summary>Gets or sets the shape.</summary>
        /// <value>The shape.</value>
        public object Shape { get; set; }

        /// <summary>Gets or sets the LVC map.</summary>
        /// <value>The LVC map.</value>
        public LvcMap LvcMap { get; set; }
    }

    public class LvcMap
    {
        /// <summary>Gets or sets the width of the desired.</summary>
        /// <value>The width of the desired.</value>
        public double DesiredWidth { get; set; }

        /// <summary>Gets or sets the height of the desired.</summary>
        /// <value>The height of the desired.</value>
        public double DesiredHeight { get; set; }

        /// <summary>Gets or sets the data.</summary>
        /// <value>The data.</value>
        public List<MapData> Data { get; set; }
    }
}