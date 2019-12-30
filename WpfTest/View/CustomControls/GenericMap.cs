using System;
using System.Collections.Generic;
using System.ComponentModel;
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


namespace WpfTest.View.CustomControls
{
    public class GenericMap : UserControl
    {
        private readonly Canvas _canvasMap;
        private readonly Dictionary<string, MapCountry> _countries;


        private Map _map;
        private Canvas _canvas;


        private bool _isDrawn;
        private bool _isWidthDominant;


        private Point _dragOrigin;
        private Point _originalPosition;


        public GenericMap()
        {
            _canvas = new Canvas();
            _canvasMap = new Canvas();
            _canvas.ClipToBounds = true;
            _canvasMap.ClipToBounds = true;
            _canvas.Children.Add(_canvasMap);
            Content = _canvas;

            //_canvas.SetBinding(WidthProperty,
            //    new Binding { Path = new PropertyPath(ActualWidthProperty), Source = this });
            //_canvas.SetBinding(HeightProperty,
            //    new Binding { Path = new PropertyPath(ActualHeightProperty), Source = this });

            _countries = new Dictionary<string, MapCountry>();

            SetCurrentValue(DefaultCountryFillProperty, new SolidColorBrush(Color.FromArgb(200, 255, 255, 255)));
            SetCurrentValue(CountryBorderBrushProperty, new SolidColorBrush(Color.FromArgb(30, 55, 55, 55)));
            SetCurrentValue(CountryBorderThicknessProperty, 1.3d);
            SetCurrentValue(AnimationsSpeedProperty, TimeSpan.FromMilliseconds(500));
            SetCurrentValue(BackgroundProperty, new SolidColorBrush(Color.FromArgb(150, 96, 125, 138)));
            SetCurrentValue(GradientStopCollectionProperty, new GradientStopCollection
            {
                new GradientStop(Color.FromArgb(100,2,119,188), 0d),
                new GradientStop(Color.FromRgb(2,119,188), 1d),
            });
            SetCurrentValue(HeatMapProperty, new Dictionary<string, double>());


            //SetCurrentValue(GeoMapTooltipProperty, new DefaultGeoMapTooltip { Visibility = Visibility.Hidden });
            //_canvas.Children.Add(GeoMapTooltip);

            SizeChanged += (sender, e) =>
            {
                Draw();
            };

            MouseWheel += (sender, e) =>
            {
                if (!EnableZoomingAndPanning) return;

                e.Handled = true;
                var rt = _canvasMap.RenderTransform as ScaleTransform;
                var p = rt == null ? 1 : rt.ScaleX;
                p += e.Delta > 0 ? .05 : -.05;
                p = p < 1 ? 1 : p;
                var o = e.GetPosition(this);
                if (e.Delta > 0) _canvasMap.RenderTransformOrigin = new Point(o.X / ActualWidth, o.Y / ActualHeight);
                _canvasMap.RenderTransform = new ScaleTransform(p, p);
            };

            MouseDown += (sender, e) =>
            {
                if (!EnableZoomingAndPanning) return;

                _dragOrigin = e.GetPosition(this);
            };

            MouseUp += (sender, e) =>
            {
                if (!EnableZoomingAndPanning) return;

                var end = e.GetPosition(this);
                var delta = new Point(_dragOrigin.X - end.X, _dragOrigin.Y - end.Y);

                var l = Canvas.GetLeft(_canvasMap) - delta.X;
                var t = Canvas.GetTop(_canvasMap) - delta.Y;

                if (DisableAnimations)
                {
                    Canvas.SetLeft(_canvasMap, l);
                    Canvas.SetTop(_canvasMap, t);
                }
                else
                {
                    _canvasMap.BeginAnimation(Canvas.LeftProperty, new DoubleAnimation(l, AnimationsSpeed));
                    _canvasMap.BeginAnimation(Canvas.TopProperty, new DoubleAnimation(t, AnimationsSpeed));
                }


            };

            Loaded += (sender, e) =>
            {
                _map = GetMap(Source);
                Draw();
            };

        }

        #region Events

        /// <summary>
        /// Occurs when [land click].
        /// </summary>
        public event Action<object, MapCountry> LandClick;



        #endregion

        #region Dependency Properties


        /*
         private static readonly DependencyProperty GeoMapTooltipProperty = DependencyProperty.Register(
             "GeoMapTooltip", typeof(DefaultGeoMapTooltip), typeof(GenericMap), new PropertyMetadata(default(DefaultGeoMapTooltip)));
         private DefaultGeoMapTooltip GeoMapTooltip
         {
             get { return (DefaultGeoMapTooltip)GetValue(GeoMapTooltipProperty); }
             set => SetValue(GeoMapTooltipProperty, value);
         }

         */

        /*
         /// <summary>
         /// The language pack property
         /// </summary>
         public static readonly DependencyProperty LanguagePackProperty = DependencyProperty.Register(
             "LanguagePack", typeof(Dictionary<string, string>), typeof(GenericMap), new PropertyMetadata(default(Dictionary<string, string>)));
         /// <summary>
         /// Gets or sets the language dictionary
         /// </summary>
         public Dictionary<string, string> LanguagePack
         {
             get { return (Dictionary<string, string>)GetValue(LanguagePackProperty); }
             set { SetValue(LanguagePackProperty, value); }
         }


         */


        /// <summary>
        /// The Default Country Fill Property
        /// </summary>
        public static readonly DependencyProperty DefaultCountryFillProperty = DependencyProperty.Register(
            "DefaultCountryFill", typeof(Brush), typeof(GenericMap), new PropertyMetadata(default(Brush)));

        /// <summary>
        /// Gets or sets default all Countries background colo
        /// </summary>
        public Brush DefaultCountryFill
        {
            get { return (Brush)GetValue(DefaultCountryFillProperty); }
            set
            {
                SetValue(DefaultCountryFillProperty, value);
            }
        }



        /// <summary>
        /// The Country Border Thickness Property
        /// </summary>
        public static readonly DependencyProperty CountryBorderThicknessProperty = DependencyProperty.Register(
            "CountryBorderThickness", typeof(double), typeof(GenericMap), new PropertyMetadata(default(double)));

        /// <summary>
        /// Gets or sets all Countries border thickness property
        /// </summary>
        public double CountryBorderThickness
        {
            get { return (double)GetValue(CountryBorderThicknessProperty); }
            set { SetValue(CountryBorderThicknessProperty, value); }
        }



        /// <summary>
        /// The Country Border Brush Property
        /// </summary>
        public static readonly DependencyProperty CountryBorderBrushProperty = DependencyProperty.Register(
            "CountryBorderBrush", typeof(Brush), typeof(GenericMap), new PropertyMetadata(default(Brush)));

        /// <summary>
        /// Gets or sets all Countries border brush
        /// </summary>
        public Brush CountryBorderBrush
        {
            get { return (Brush)GetValue(CountryBorderBrushProperty); }
            set { SetValue(CountryBorderBrushProperty, value); }
        }




        /// <summary>
        /// The disable animations property todo Disable Animations
        /// </summary>
        public static readonly DependencyProperty DisableAnimationsProperty = DependencyProperty.Register(
            "DisableAnimations", typeof(bool), typeof(GenericMap), new PropertyMetadata(default(bool)));
        /// <summary>
        /// Gets or sets whether the chart is animated
        /// </summary>
        public bool DisableAnimations
        {
            get { return (bool)GetValue(DisableAnimationsProperty); }
            set { SetValue(DisableAnimationsProperty, value); }
        }

        /// <summary>
        /// The animations speed property todo Disable Animations speed
        /// </summary>
        public static readonly DependencyProperty AnimationsSpeedProperty = DependencyProperty.Register(
            "AnimationsSpeed", typeof(TimeSpan), typeof(GenericMap), new PropertyMetadata(default(TimeSpan)));
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
            "Hoverable", typeof(bool), typeof(GenericMap), new PropertyMetadata(default(bool)));
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
            typeof(GenericMap),
            new PropertyMetadata(default(Dictionary<string, double>), OnHeapMapChanged));



        private static void OnHeapMapChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            var geoMap = (GenericMap)o;

            if (!geoMap._isDrawn) return;

            geoMap.ShowMeSomeHeat();
        }


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




        /// <summary>
        /// The Test property
        /// </summary>
        public static readonly DependencyProperty MapValuesProperty = DependencyProperty.Register(
            "MapValues",
            typeof(Dictionary<string, double>),
            typeof(GenericMap),
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
            var geoMap = (GenericMap)d;

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
                typeof(GenericMap));

        public ICommand SelectedLendCommand
        {
            get => (ICommand)GetValue(SelectedLendCommandProperty);

            set => SetValue(SelectedLendCommandProperty, value);
        }



        public static DependencyProperty SelectedLandBackgroundProperty
            = DependencyProperty.Register(
                "SelectedLandBackground",
                typeof(Brush),
                typeof(GenericMap));

        public Brush SelectedLandBackground
        {
            get => (Brush)GetValue(SelectedLandBackgroundProperty);

            set => SetValue(SelectedLandBackgroundProperty, value);
        }


        public static DependencyProperty SelectedCountryProperty
            = DependencyProperty.Register(
                "SelectedCountry",
                typeof(string),
                typeof(GenericMap),
                new PropertyMetadata(default(string), OnSelectedCountryChanged));

        public string SelectedCountry
        {
            get => (string)GetValue(SelectedCountryProperty);

            set => SetValue(SelectedCountryProperty, value);
        }

        private static void OnSelectedCountryChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var geoMap = (GenericMap)d;

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
                typeof(GenericMap));

        public bool EnableCountryHighlight
        {
            get => (bool)GetValue(EnableCountryHighlightProperty);

            set => SetValue(EnableCountryHighlightProperty, value);
        }


        /// <summary>
        /// The gradient stop collection property
        /// </summary>
        public static readonly DependencyProperty GradientStopCollectionProperty = DependencyProperty.Register(
            "GradientStopCollection", typeof(GradientStopCollection), typeof(GenericMap), new PropertyMetadata(default(GradientStopCollection)));
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
            "Source", typeof(string), typeof(GenericMap), new PropertyMetadata(default(string)));
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
            "EnableZoomingAndPanning", typeof(bool), typeof(GenericMap), new PropertyMetadata(default(bool)));


        /// <summary>
        /// Gets or sets whether the map allows zooming and panning
        /// </summary>
        public bool EnableZoomingAndPanning
        {
            get { return (bool)GetValue(EnableZoomingAndPanningProperty); }
            set { SetValue(EnableZoomingAndPanningProperty, value); }
        }


        #endregion End of "Dependency Properties"








        #region Private Methods

        private void Draw()
        {
            _isDrawn = true;

            _canvasMap.Children.Clear();

            if (DesignerProperties.GetIsInDesignMode(this))
            {
                _canvasMap.Children.Add(new TextBlock
                {
                    Text = "Designer preview is not currently available",
                    Foreground = Brushes.Red,
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

            if (_map == null) return;

            var desiredSize = new Size(_map.DesiredWidth, _map.DesiredHeight);
            var r = desiredSize.Width / desiredSize.Height;

            var wr = ActualWidth / desiredSize.Width;
            var hr = ActualHeight / desiredSize.Height;
            double s;

            if (wr < hr)
            {
               // _isWidthDominant = true;
                _canvasMap.Width = ActualWidth;
                _canvasMap.Height = _canvasMap.Width / r;
                s = wr;
                _originalPosition = new Point(0, (ActualHeight - _canvasMap.Height) * .5);
                Canvas.SetLeft(_canvasMap, _originalPosition.X);
                Canvas.SetTop(_canvasMap, _originalPosition.Y);
            }
            else
            {
              //  _isWidthDominant = false;
                _canvasMap.Height = ActualHeight;
                _canvasMap.Width = r * ActualHeight;
                s = hr;
                _originalPosition = new Point((ActualWidth - _canvasMap.Width) * .5, 0d);
                Canvas.SetLeft(_canvasMap, _originalPosition.X);
                Canvas.SetTop(_canvasMap, _originalPosition.Y);
            }

            var t = new ScaleTransform(s, s);

            foreach (var land in _map.MapCountries)
            {
                var countryPath = new System.Windows.Shapes.Path
                {
                    Data = Geometry.Parse(land.Data),
                    RenderTransform = t
                };

                land.Shape = countryPath;
                _countries[land.Id] = land;
                 _canvasMap.Children.Add(countryPath);

                //p.MouseEnter += POnMouseEnter;
                //p.MouseLeave += POnMouseLeave;
                //p.MouseMove += POnMouseMove;
                //p.MouseDown += POnMouseDown;

                countryPath.SetBinding(Shape.StrokeProperty,
                    new Binding { Path = new PropertyPath(CountryBorderBrushProperty), Source = this });
                countryPath.SetBinding(Shape.StrokeThicknessProperty,
                    new MultiBinding
                    {
                        Converter = new Styles.ScaleStrokeConverter(),
                        Bindings =
                        {
                            new Binding("CountryBorderThickness") {Source = this},
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

            foreach (var land in _countries)
            {
                double temperature;
                var shape = ((Shape)land.Value.Shape);

                shape.SetBinding(Shape.FillProperty,
                    new Binding { Path = new PropertyPath(DefaultCountryFillProperty), Source = this });

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
            var land = _countries.Values.FirstOrDefault(x => x.Shape == sender);
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
                var path = (System.Windows.Shapes.Path)sender;
                path.Opacity = 1;
            }

            //GeoMapTooltip.Visibility = Visibility.Hidden;
        }

        private void POnMouseEnter(object sender, MouseEventArgs mouseEventArgs)
        {
            if (EnableCountryHighlight)
            {
                var path = (System.Windows.Shapes.Path)sender;
                path.Opacity = .8;
            }


            var land = _countries.Values.FirstOrDefault(x => x.Shape == sender);
            if (land == null) return;

            double value;

            //todo fix this 'if' bug (it's try to find Land in heatmap to show 
            if (!HeatMap.TryGetValue(land.Id, out value)) return;
            if (!Hoverable) return;

            //GeoMapTooltip.Visibility = Visibility.Visible;

            string name = null;

            // if (LanguagePack != null) LanguagePack.TryGetValue(land.Id, out name);

            /*GeoMapTooltip.GeoData = new GeoData
            {
                Name = name ?? land.Name,
                Value = value
            };*/
        }

        private void POnMouseMove(object sender, MouseEventArgs mouseEventArgs)
        {
            var location = mouseEventArgs.GetPosition(this);
            // Canvas.SetTop(GeoMapTooltip, location.Y + 5);
            // Canvas.SetLeft(GeoMapTooltip, location.X + 5);
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



        private Map GetMap(string file)
        {

            var isFromResource = false;

            var svgMap = new Map
            {
                DesiredHeight = 600,
                DesiredWidth = 800,
                MapCountries = new List<MapCountry>()
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

                        var mapCountry = new MapCountry();

                        reader.Read();
                        while (reader.NodeType != XmlNodeType.EndElement)
                        {
                            if (reader.NodeType != XmlNodeType.Element) reader.Read();
                            if (reader.Name == "Id") mapCountry.Id = reader.ReadInnerXml();
                            if (reader.Name == "Name") mapCountry.Name = reader.ReadInnerXml();
                            if (reader.Name == "Path") mapCountry.Data = reader.ReadInnerXml();
                        }

                        svgMap.MapCountries.Add(mapCountry);
                    }
                }

            }



            return svgMap;
        }

        #endregion End o "Private Methods"
    }


    //=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-


    public class MapCountry
    {
        public string Id { get; set; }


        public string Name { get; set; }


        public string Data { get; set; }


        public object Shape { get; set; }

    }

    public class Map
    {

        public double DesiredWidth { get; set; }


        public double DesiredHeight { get; set; }


        public List<MapCountry> MapCountries { get; set; }
    }

    //=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-
}
