/* ----------------------------------------------- 
 * Copyright © 2013 Esri Inc. All Rights Reserved. 
 * ----------------------------------------------- */

using Esri.ArcGISRuntime.Controls;
using Esri.ArcGISRuntime.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using Windows.Data.Json;
using Windows.Foundation;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;

namespace ESRI.PrototypeLab.ProximityMap {
    public class MapInkManager : BindableBase {
        //
        // Properties
        //
        public List<MapInkLine> Lines { get; private set; }
        public bool IsEnabled { get; set; }
        private readonly Map _map = null;
        private readonly Canvas _canvas = null;
        //
        // Constructor
        //
        public MapInkManager(Map map, Canvas canvas) {
            this._map = map;
            this._canvas = canvas;
            this.Lines = new List<MapInkLine>();
            this.IsEnabled = false;
            this._map.ExtentChanged += (s, e) => {
                this.Render();
            };
            this._map.PointerPressed += (s, e) => {
                this.AddPoint(e);
            };
            this._map.PointerMoved += (s, e) => {
                if (!e.Pointer.IsInContact) { return; }
                this.AddPoint(e);
                if (this.IsEnabled) {
                    this.Render();
                }
            };
            this._map.PointerReleased += (s, e) => {
                this.CloseLine(e);
                if (this.IsEnabled) {
                    this.Render();
                }
            };
        }
        //
        // EVENTS
        //
        public event EventHandler<MapInkLineEventArgs> LineChanged;
        public event EventHandler<MapInkLineEventArgs> LineClosed;
        //
        // METHODS
        //
        protected void OnLineChanged(MapInkLineEventArgs e) {
            if (this.LineChanged != null) {
                this.LineChanged(this, e);
            }
        }
        protected void OnLineClosed(MapInkLineEventArgs e) {
            if (this.LineClosed != null) {
                this.LineClosed(this, e);
            }
        }
        private void AddPoint(PointerRoutedEventArgs e) {
            if (!this.IsEnabled) { return; }
            MapInkLine line = this.Lines.FirstOrDefault(l => l.Id == e.Pointer.PointerId && !l.IsClosed);
            if (line == null) {
                line = new MapInkLine(e.Pointer.PointerId, this.InkColor);
                this.Lines.Add(line);
            }
            line.Points.Add(this._map.ScreenToMap(e.GetCurrentPoint(this._map).Position));
            this.OnLineChanged(new MapInkLineEventArgs(line));
        }
        private void CloseLine(PointerRoutedEventArgs e) {
            MapInkLine line = this.Lines.FirstOrDefault(l => l.Id == e.Pointer.PointerId && !l.IsClosed);
            if (line != null) {
                line.Close();
                this.OnLineClosed(new MapInkLineEventArgs(line));
            }
        }
        public void Render() {
            const double FLARE_LEN = 200d;
            const double MAX_WIDTH = 30d;
            const double MIN_WIDTH = 10d;

            this._canvas.Children.Clear();
            foreach (var line in this.Lines) {
                if (line.Points.Count < 2) { continue; }
                if (line.IsClosed) {
                    PathSegmentCollection pathSegments = new PathSegmentCollection();
                    for (int i = 1; i < line.Points.Count - 2; i = i + 2) {
                        Point point1 = this._map.MapToScreen(line.Points[i]);
                        Point point2 = this._map.MapToScreen(line.Points[i + 1]);
                        QuadraticBezierSegment bezier = new QuadraticBezierSegment() {
                            Point1 = point1,
                            Point2 = point2
                        };
                        pathSegments.Add(bezier);
                    }
                    PathFigure pathFigure = new PathFigure() {
                        StartPoint = this._map.MapToScreen(line.Points[0]),
                        Segments = pathSegments
                    };
                    PathFigureCollection pathFigures = new PathFigureCollection();
                    pathFigures.Add(pathFigure);
                    PathGeometry pathGeometry = new PathGeometry() {
                        Figures = pathFigures
                    };
                    Path p = new Path() {
                        Stroke = new SolidColorBrush() {
                            Color = line.Color
                        },
                        StrokeStartLineCap = PenLineCap.Round,
                        StrokeEndLineCap = PenLineCap.Round,
                        StrokeLineJoin = PenLineJoin.Round,
                        StrokeThickness = MIN_WIDTH,
                        IsHitTestVisible = false,
                        Data = pathGeometry
                    };
                    this._canvas.Children.Add(p);
                }
                else {
                    Point? curr = null;
                    Point? prev = null;
                    double len = 0;
                    foreach (var point in line.Points.Reverse<MapPoint>()) {
                        curr = this._map.MapToScreen(point);
                        if (prev != null) {
                            len += Math.Sqrt(Math.Pow(prev.Value.X - curr.Value.X, 2) + Math.Pow(prev.Value.Y - curr.Value.Y, 2));
                            double size = MIN_WIDTH;
                            if (len <= FLARE_LEN) {
                                size = len * (MIN_WIDTH - MAX_WIDTH) / FLARE_LEN + MAX_WIDTH;
                            }
                            this._canvas.Children.Add(
                                new Line() {
                                    X1 = curr.Value.X,
                                    Y1 = curr.Value.Y,
                                    X2 = prev.Value.X,
                                    Y2 = prev.Value.Y,
                                    Stroke = new SolidColorBrush() {
                                        Color = Colors.Orange
                                    },
                                    StrokeStartLineCap = PenLineCap.Round,
                                    StrokeEndLineCap = PenLineCap.Round,
                                    StrokeLineJoin = PenLineJoin.Round,
                                    StrokeThickness = size,
                                    IsHitTestVisible = false
                                }
                            );
                        }
                        prev = curr;
                    }
                }
            }
        }
        public static readonly DependencyProperty InkColorProperty = DependencyProperty.Register(
            "InkColor",
            typeof(Color),
            typeof(MapInkManager),
            new PropertyMetadata(Colors.Red)
        );
        public Color InkColor {
            get { return (Color)this.GetValue(MapInkManager.InkColorProperty); }
            set {
                this.SetValue(MapInkManager.InkColorProperty, value);
                this.OnPropertyChanged("InkColor");
            }
        }
    }

    public class MapInkLine {
        //
        // PROPERTIES
        //
        public Guid UniqueId { get; private set; }
        public uint Id { get; private set; }
        public List<MapPoint> Points { get; private set; }
        public bool IsClosed { get; private set; }
        public Color Color { get; private set; }
        //
        // CONSTRUCTOR
        //
        public MapInkLine(uint id, Color color) {
            this.UniqueId = Guid.NewGuid();
            this.Id = id;
            this.Points = new List<MapPoint>();
            this.IsClosed = false;
            this.Color = color;
        }
        //
        // METHODS
        //
        public void Close() {
            this.IsClosed = true;
        }
        public string ToJson() {
            JsonArray j = new JsonArray();
            foreach (var point in this.Points) {
                JsonObject c = new JsonObject();
                c["X"] = JsonValue.CreateNumberValue(point.X);
                c["Y"] = JsonValue.CreateNumberValue(point.Y);
                j.Add(c);
            }

            JsonObject color = new JsonObject();
            color["A"] = JsonValue.CreateNumberValue(this.Color.A);
            color["R"] = JsonValue.CreateNumberValue(this.Color.R);
            color["G"] = JsonValue.CreateNumberValue(this.Color.G);
            color["B"] = JsonValue.CreateNumberValue(this.Color.B);

            JsonObject jsonObject = new JsonObject();
            jsonObject["UniqueId"] = JsonValue.CreateStringValue(this.UniqueId.ToString());
            jsonObject["Id"] = JsonValue.CreateNumberValue(this.Id);
            jsonObject["IsClosed"] = JsonValue.CreateBooleanValue(this.IsClosed);
            jsonObject["Points"] = j;
            jsonObject["Color"] = color;

            return jsonObject.Stringify();
        }
        public static MapInkLine FromJson(string json) {
            JsonValue jsonValue = JsonValue.Parse(json);
            Guid guid = Guid.Parse(jsonValue.GetObject().GetNamedString("UniqueId"));
            uint id = (uint)jsonValue.GetObject().GetNamedNumber("Id");
            bool isclosed = jsonValue.GetObject().GetNamedBoolean("IsClosed");
            JsonObject color = jsonValue.GetObject()["Color"].GetObject();
            Color c = new Color() {
                A = (byte)color.GetNamedNumber("A"),
                R = (byte)color.GetNamedNumber("R"),
                G = (byte)color.GetNamedNumber("G"),
                B = (byte)color.GetNamedNumber("B")
            };

            var x = jsonValue.GetObject().GetNamedArray("Points").Select(
                p => {
                    return new MapPoint() {
                        X = p.GetObject().GetNamedNumber("X"),
                        Y = p.GetObject().GetNamedNumber("Y")
                    };
                }
            );
            return new MapInkLine(id, c) {
                UniqueId = guid,
                IsClosed = isclosed,
                Points = x.ToList()
            };
        }
    }

    public class MapInkLineEventArgs : EventArgs {
        public MapInkLine MapInkLine { get; private set; }
        public MapInkLineEventArgs(MapInkLine line) {
            this.MapInkLine = line;
        }
    }
}
