using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using AxInterop.MapWinGIS;
using Interop.MapWinGIS;
using Hue_travel.Services.Online;
using Hue_travel.Services.Routing;
using Hue_travel.Theme;

namespace Hue_travel.MapEngine
{
    /// <summary>
    /// Draws a trip on the MapWinGIS map and drives a toy car along it: every leg is a coloured line (with a white edge) in its
    /// own layer below the places; the start and the stops are numbered badges and the car is a polygon, both in MapWinGIS
    /// drawing layers (so they lie above the data and are redrawn cheaply while the car moves).
    /// </summary>
    internal sealed class RouteDrawing : IDisposable
    {
        private const double MinSpeedMetresPerSecond = 120;     // a short trip is not a blink
        private const double TripSeconds = 26;                  // a long trip is shown in about this time
        private const int FrameMs = 33;
        private const double DwellSeconds = 0.9;                // the car waits a moment at every stop
        private const int BadgeRadiusPixels = 14;
        private const double FollowMargin = 0.18;               // the map follows the car once it is this close to an edge

        private readonly AxMap map;
        private readonly Func<int> lowestPlacePosition;
        private readonly Timer frame = new Timer { Interval = FrameMs };
        private readonly Stopwatch clock = new Stopwatch();
        private readonly List<int> legLayers = new List<int>();
        private int badgeDrawing = -1, carDrawing = -1;

        private TripPlan plan;
        private List<MapXY> points = new List<MapXY>();
        private double[] cumulative = new double[0];     // distance along the whole line at every point
        private double[] legEnds = new double[0];        // distance at the end of every leg
        private double total;
        private double travelled;
        private double speed;
        private double dwellLeft;
        private int legIndex;
        private bool playing;
        private double lastMetresPerPixel, lastBadgeMetresPerPixel;
        private long lastTicks;

        /// <summary>Raised when the car enters another leg (the index of the leg).</summary>
        public event EventHandler<int> LegChanged;

        /// <summary>Raised when the car stops or starts moving.</summary>
        public event EventHandler PlayStateChanged;

        /// <summary>Raised when the car reaches the end of the trip.</summary>
        public event EventHandler Finished;

        public RouteDrawing(AxMap map, Func<int> lowestPlacePosition)
        {
            this.map = map;
            this.lowestPlacePosition = lowestPlacePosition;
            frame.Tick += (s, e) => Step();
        }

        public bool HasTrip => plan != null;

        public bool IsPlaying => playing;

        public bool Follow { get; set; } = true;

        public int CurrentLeg => legIndex;

        public void Dispose()
        {
            frame.Dispose();
        }

        // ------------------------------------------------------------------ showing and clearing

        /// <summary>Draws the trip, zooms to it and, with <paramref name="play"/>, sets the car off from the start.</summary>
        public void Show(TripPlan trip, bool play)
        {
            Clear();
            plan = trip;
            BuildLine();
            DrawLegs();
            ZoomToTrip();
            lastBadgeMetresPerPixel = 0;
            DrawBadges();
            frame.Start();
            clock.Restart();
            lastTicks = 0;
            Restart();
            if (play) Play();
        }

        public void Clear()
        {
            frame.Stop();
            clock.Reset();
            bool wasPlaying = playing;
            playing = false;
            foreach (int handle in legLayers) map.RemoveLayer(handle);
            legLayers.Clear();
            if (badgeDrawing >= 0) map.ClearDrawing(badgeDrawing);
            if (carDrawing >= 0) map.ClearDrawing(carDrawing);
            badgeDrawing = carDrawing = -1;
            plan = null;
            points = new List<MapXY>();
            cumulative = new double[0];
            legEnds = new double[0];
            total = travelled = 0;
            if (wasPlaying) PlayStateChanged?.Invoke(this, EventArgs.Empty);
            map.Redraw();
        }

        // The legs joined end to end, with the distance along the line at every point and at the end of every leg.
        private void BuildLine()
        {
            points = new List<MapXY>();
            var ends = new List<int>();
            foreach (TripLeg leg in plan.Legs)
            {
                foreach (MapXY p in leg.Path)
                {
                    if (points.Count > 0 && Distance(points[points.Count - 1], p) < 0.01) continue;
                    points.Add(p);
                }
                ends.Add(points.Count - 1);
            }
            cumulative = new double[points.Count];
            for (int i = 1; i < points.Count; i++) cumulative[i] = cumulative[i - 1] + Distance(points[i - 1], points[i]);
            total = points.Count == 0 ? 0 : cumulative[points.Count - 1];
            legEnds = ends.Select(i => i >= 0 && i < cumulative.Length ? cumulative[i] : 0).ToArray();
            speed = Math.Max(MinSpeedMetresPerSecond, total / TripSeconds);
        }

        private void DrawLegs()
        {
            int firstPosition = lowestPlacePosition();
            int edge = Ui.S(9), line = Ui.S(5);
            var created = new List<int>();
            for (int pass = 0; pass < 2; pass++)
            {
                for (int i = 0; i < plan.Legs.Count; i++)
                {
                    IReadOnlyList<MapXY> path = plan.Legs[i].Path;
                    if (path == null || path.Count < 2) continue;
                    int handle = AddLine(path, pass == 0 ? Color.White : RoutePalette.ColorOf(i), pass == 0 ? edge : line, plan.IsEstimate && pass == 1);
                    if (handle >= 0) created.Add(handle);
                }
            }
            // Below the places: the markers stay on top of the line.
            int position = Math.Max(0, firstPosition);
            foreach (int handle in created)
            {
                map.MoveLayer(map.get_LayerPosition(handle), position++);
            }
            legLayers.AddRange(created);
        }

        private int AddLine(IReadOnlyList<MapXY> path, Color color, int width, bool dashed)
        {
            var layer = new Shapefile();
            if (!layer.CreateNew(string.Empty, ShpfileType.SHP_POLYLINE)) return -1;
            var shape = new Shape();
            int part = 0;
            if (!shape.Create(ShpfileType.SHP_POLYLINE) || !shape.InsertPart(0, ref part)) return -1;
            foreach (MapXY p in path) shape.AddPoint(p.X, p.Y);
            layer.EditAddShape(shape);
            ShapeDrawingOptions style = layer.DefaultDrawingOptions;
            style.LineVisible = true;
            style.LineColor = Ole(color);
            style.LineWidth = width;
            if (dashed) style.LineStipple = tkDashStyle.dsDash;
            return map.AddLayer(layer, true);
        }

        // The start (green) and the stops (numbered, in the colour of the leg that leads to them).
        private void DrawBadges()
        {
            if (plan == null) return;
            double mpp = MetresPerPixel();
            if (mpp <= 0) return;
            // Circles are polygons sized in pixels (the circle call takes map units), so they are redrawn when the zoom changes.
            int previous = badgeDrawing;
            badgeDrawing = map.NewDrawing(tkDrawReferenceList.dlSpatiallyReferencedList);
            if (previous >= 0) map.ClearDrawing(previous);
            Labels labels = map.get_DrawingLabels(badgeDrawing);
            labels.FontName = "Segoe UI";
            labels.FontSize = 10;
            labels.FontBold = true;
            labels.FontColor = Ole(Color.White);
            labels.Alignment = tkLabelAlignment.laCenter;
            labels.FrameVisible = false;
            labels.HaloVisible = false;
            map.set_DrawingLabelsVisible(badgeDrawing, true);

            Badge(badgeDrawing, plan.StartPoint, Color.FromArgb(22, 163, 74), "BĐ", true, mpp);
            for (int i = 0; i < plan.Stops.Count; i++)
            {
                Badge(badgeDrawing, new MapXY(plan.Stops[i].X, plan.Stops[i].Y), RoutePalette.ColorOf(i), (i + 1).ToString(), false, mpp);
            }
            lastBadgeMetresPerPixel = mpp;
        }

        private void Badge(int layer, MapXY at, Color color, string text, bool start, double mpp)
        {
            int radius = Ui.S(BadgeRadiusPixels) + (start ? Ui.S(2) : 0);
            Polygon(layer, Circle(radius + Ui.S(3)), at.X, at.Y, 0, mpp, Color.FromArgb(40, 15, 23, 42), 90);
            Polygon(layer, Circle(radius + Ui.S(2)), at.X, at.Y, 0, mpp, Color.White, 255);
            Polygon(layer, Circle(radius), at.X, at.Y, 0, mpp, color, 255);
            map.DrawLabelEx(layer, text, at.X, at.Y, 0);
        }

        private static PointF[] Circle(double radius)
        {
            var points = new PointF[28];
            for (int i = 0; i < points.Length; i++)
            {
                double angle = i * 2 * Math.PI / points.Length;
                points[i] = new PointF((float)(radius * Math.Cos(angle)), (float)(radius * Math.Sin(angle)));
            }
            return points;
        }

        // ------------------------------------------------------------------ zooming

        public void ZoomToTrip()
        {
            if (points.Count == 0) return;
            ZoomToPoints(points);
        }

        public void ZoomToLeg(int index)
        {
            if (plan == null || index < 0 || index >= plan.Legs.Count) return;
            IReadOnlyList<MapXY> path = plan.Legs[index].Path;
            if (path != null && path.Count > 0) ZoomToPoints(path);
        }

        private void ZoomToPoints(IEnumerable<MapXY> source)
        {
            double minX = double.MaxValue, minY = double.MaxValue, maxX = double.MinValue, maxY = double.MinValue;
            foreach (MapXY p in source)
            {
                minX = Math.Min(minX, p.X); maxX = Math.Max(maxX, p.X);
                minY = Math.Min(minY, p.Y); maxY = Math.Max(maxY, p.Y);
            }
            double padX = Math.Max((maxX - minX) * 0.18, 300), padY = Math.Max((maxY - minY) * 0.18, 300);
            var view = new Extents();
            view.SetBounds(minX - padX, minY - padY, 0, maxX + padX, maxY + padY, 0);
            map.Extents = view;
        }

        // ------------------------------------------------------------------ the car

        public void Play()
        {
            if (plan == null || total <= 0) return;
            if (travelled >= total) Restart();
            SetPlaying(true);
        }

        public void Pause() => SetPlaying(false);

        public void Replay()
        {
            if (plan == null) return;
            Restart();
            SetPlaying(true);
        }

        private void Restart()
        {
            travelled = 0;
            dwellLeft = 0;
            legIndex = 0;
            lastMetresPerPixel = 0;
            DrawCar();
            map.Redraw2(tkRedrawType.RedrawSkipDataLayers);
            LegChanged?.Invoke(this, legIndex);
        }

        private void SetPlaying(bool value)
        {
            if (playing == value) return;
            playing = value;
            lastTicks = clock.ElapsedTicks;
            PlayStateChanged?.Invoke(this, EventArgs.Empty);
        }

        private void Step()
        {
            long now = clock.ElapsedTicks;
            double dt = Math.Min(0.1, (now - lastTicks) / (double)Stopwatch.Frequency);
            lastTicks = now;
            if (plan == null) return;

            bool moved = false;
            if (playing)
            {
                if (dwellLeft > 0)
                {
                    dwellLeft -= dt;
                }
                else
                {
                    double before = travelled;
                    travelled = Math.Min(total, travelled + speed * dt);
                    moved = travelled != before;
                    int arrived = Array.FindIndex(legEnds, end => before < end - 0.001 && travelled >= end - 0.001);
                    if (travelled >= total)
                    {
                        SetPlaying(false);
                        Finished?.Invoke(this, EventArgs.Empty);
                    }
                    else if (arrived >= 0)
                    {
                        dwellLeft = DwellSeconds;
                    }
                    UpdateLeg();
                }
            }

            double mpp = MetresPerPixel();
            if (moved || Math.Abs(mpp - lastMetresPerPixel) > 1e-6)
            {
                if (Math.Abs(mpp - lastBadgeMetresPerPixel) > 1e-6) DrawBadges();
                DrawCar();
                if (playing && Follow) FollowCar();
                map.Redraw2(tkRedrawType.RedrawSkipDataLayers);
            }
        }

        private void UpdateLeg()
        {
            int next = legEnds.Length == 0 ? 0 : Array.FindIndex(legEnds, end => travelled < end - 0.001);
            if (next < 0) next = legEnds.Length - 1;
            if (next == legIndex) return;
            legIndex = next;
            LegChanged?.Invoke(this, legIndex);
        }

        private double MetresPerPixel()
        {
            Extents view = map.Extents;
            return view == null || map.Width <= 0 ? 0 : (view.xMax - view.xMin) / map.Width;
        }

        // The point and the direction of travel at a distance along the line.
        private bool Locate(double distance, out double x, out double y, out double heading)
        {
            x = y = heading = 0;
            if (points.Count == 0) return false;
            if (points.Count == 1) { x = points[0].X; y = points[0].Y; return true; }
            distance = Math.Max(0, Math.Min(total, distance));
            int i = Array.BinarySearch(cumulative, distance);
            if (i < 0) i = ~i;
            i = Math.Max(1, Math.Min(points.Count - 1, i));
            double segment = cumulative[i] - cumulative[i - 1];
            double t = segment <= 0 ? 0 : (distance - cumulative[i - 1]) / segment;
            x = points[i - 1].X + (points[i].X - points[i - 1].X) * t;
            y = points[i - 1].Y + (points[i].Y - points[i - 1].Y) * t;
            heading = Math.Atan2(points[i].Y - points[i - 1].Y, points[i].X - points[i - 1].X);
            return true;
        }

        private void DrawCar()
        {
            if (plan == null) return;
            // A fresh drawing layer per frame, the previous one removed once the new one is there (no flicker).
            int previous = carDrawing;
            carDrawing = map.NewDrawing(tkDrawReferenceList.dlSpatiallyReferencedList);
            if (previous >= 0) map.ClearDrawing(previous);
            double mpp = MetresPerPixel();
            lastMetresPerPixel = mpp;
            double cx, cy, heading;
            if (mpp <= 0 || !Locate(travelled, out cx, out cy, out heading)) return;

            // A little ahead of the car, so it turns before a corner rather than after it.
            double ax, ay, ahead;
            if (Locate(travelled + 14 * mpp, out ax, out ay, out ahead) && Distance(new MapXY(cx, cy), new MapXY(ax, ay)) > 0.5) heading = Math.Atan2(ay - cy, ax - cx);

            Color body = RoutePalette.ColorOf(legIndex);
            double length = Ui.S(34), width = Ui.S(17);
            Polygon(Rounded(length, width, width * 0.38, 2.5, 2.5), cx, cy, heading, mpp, Color.FromArgb(70, 15, 23, 42), 90);          // shadow
            Polygon(Rounded(length + 2, width + 2, width * 0.4, 0, 0), cx, cy, heading, mpp, Color.White, 255);                          // white edge
            Polygon(Rounded(length, width, width * 0.36, 0, 0), cx, cy, heading, mpp, body, 255);                                        // body
            Polygon(Scaled(length, width, P(0.32, 0.32), P(0.16, 0.40), P(-0.20, 0.40), P(-0.30, 0.30), P(-0.30, -0.30), P(-0.20, -0.40), P(0.16, -0.40), P(0.32, -0.32)),
                cx, cy, heading, mpp, Color.FromArgb(30, 41, 59), 255);   // glass
            Polygon(Scaled(length, width, P(0.12, 0.30), P(-0.14, 0.30), P(-0.14, -0.30), P(0.12, -0.30)),
                cx, cy, heading, mpp, Color.FromArgb(71, 85, 105), 255);   // roof
            foreach (int side in new[] { -1, 1 })
            {
                float x = (float)(length * 0.5), y = (float)(side * width * 0.30);
                Polygon(new[] { new PointF(x - 4, y - 2), new PointF(x - 1, y - 2), new PointF(x - 1, y + 2), new PointF(x - 4, y + 2) },
                    cx, cy, heading, mpp, Color.FromArgb(253, 224, 71), 255);   // headlights
            }
        }

        private static PointF P(double x, double y) => new PointF((float)x, (float)y);

        // Points given as fractions of the car's length and width, as pixels.
        private static PointF[] Scaled(double length, double width, params PointF[] fractions) =>
            fractions.Select(p => new PointF((float)(p.X * length), (float)(p.Y * width))).ToArray();

        // A rounded rectangle centred on the origin, pointing along +x, sized in pixels (shifted by an offset in pixels).
        private static PointF[] Rounded(double length, double width, double radius, double offsetX, double offsetY)
        {
            var points = new List<PointF>();
            double hx = length / 2, hy = width / 2;
            double[][] corners = { new[] { hx - radius, hy - radius, 0 }, new[] { -hx + radius, hy - radius, 90 }, new[] { -hx + radius, -hy + radius, 180 }, new[] { hx - radius, -hy + radius, 270 } };
            foreach (double[] c in corners)
            {
                for (int step = 0; step <= 4; step++)
                {
                    double angle = (c[2] + step * 22.5) * Math.PI / 180;
                    points.Add(new PointF((float)(c[0] + radius * Math.Cos(angle) + offsetX), (float)(c[1] + radius * Math.Sin(angle) + offsetY)));
                }
            }
            return points.ToArray();
        }

        // Draws a polygon given in pixels around the car (+x forward) rotated by the heading, in map metres.
        private void Polygon(PointF[] pixels, double cx, double cy, double heading, double metresPerPixel, Color color, int alpha) =>
            Polygon(carDrawing, pixels, cx, cy, heading, metresPerPixel, color, alpha);

        private void Polygon(int layer, PointF[] pixels, double cx, double cy, double heading, double metresPerPixel, Color color, int alpha)
        {
            double cos = Math.Cos(heading), sin = Math.Sin(heading);
            var xs = new double[pixels.Length];
            var ys = new double[pixels.Length];
            for (int i = 0; i < pixels.Length; i++)
            {
                xs[i] = cx + (pixels[i].X * cos - pixels[i].Y * sin) * metresPerPixel;
                ys[i] = cy + (pixels[i].X * sin + pixels[i].Y * cos) * metresPerPixel;
            }
            object x = xs, y = ys;
            map.DrawPolygonEx(layer, ref x, ref y, pixels.Length, Ole(color), true, (byte)alpha);
        }

        // Keeps the car in the middle part of the window: once it nears an edge the view moves so the car is centred again.
        private void FollowCar()
        {
            double cx, cy, heading;
            if (!Locate(travelled, out cx, out cy, out heading)) return;
            double px = 0, py = 0;
            map.ProjToPixel(cx, cy, ref px, ref py);
            double mx = map.Width * FollowMargin, my = map.Height * FollowMargin;
            if (px > mx && px < map.Width - mx && py > my && py < map.Height - my) return;
            Extents view = map.Extents;
            double halfW = (view.xMax - view.xMin) / 2, halfH = (view.yMax - view.yMin) / 2;
            var moved = new Extents();
            moved.SetBounds(cx - halfW, cy - halfH, 0, cx + halfW, cy + halfH, 0);
            map.Extents = moved;
        }

        private static double Distance(MapXY a, MapXY b) => Math.Sqrt((a.X - b.X) * (a.X - b.X) + (a.Y - b.Y) * (a.Y - b.Y));

        private static uint Ole(Color color) => (uint)ColorTranslator.ToOle(color);
    }
}
