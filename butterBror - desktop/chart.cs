using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;

namespace butterBror___desktop
{
    public class SmoothLineChart : Panel
    {
        private List<PointData> pointsData = new List<PointData>();
        private List<int> targetValues = new List<int>();
        private float currentMax;
        private int targetMax;
        private System.Windows.Forms.Timer animationTimer;
        private float animationSpeed = 0.3f;

        public Color LineColor { get; set; } = Color.FromArgb(245, 129, 66);
        public int LineThickness { get; set; } = 1;
        public int PointRadius { get; set; } = 4;
        public bool AnimationEnabled { get; set; } = true;

        public int FPS
        {
            get => 1000 / animationTimer.Interval;
            set => animationTimer.Interval = Math.Clamp(1000 / value, 1, 1000);
        }

        public SmoothLineChart()
        {
            DoubleBuffered = true;
            Resize += (s, e) => Invalidate();
            Padding = new Padding(20);

            animationTimer = new System.Windows.Forms.Timer { Interval = 16 };
            animationTimer.Tick += AnimationTimer_Tick;
            animationTimer.Start();
        }

        public void UpdateValues(List<int> newValues)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => UpdateValues(newValues)));
                return;
            }

            targetValues = newValues.ToList();
            targetMax = targetValues.DefaultIfEmpty(1).Max();
            if (targetMax == 0) targetMax = 1;

            SyncPointsStructure();
            UpdateTargetPositions();

            if (!AnimationEnabled)
                ForceImmediateUpdate();
        }

        private void SyncPointsStructure()
        {
            // Добавляем новые точки В КОНЕЦ списка
            while (pointsData.Count < targetValues.Count)
            {
                pointsData.Add(new PointData
                {
                    Value = 0,
                    TargetValue = 0,
                    Position = Width - Padding.Right, // Начальная позиция справа за экраном
                    TargetPosition = Width - Padding.Right
                });
            }

            // Удаляем лишние точки С НАЧАЛА списка
            while (pointsData.Count > targetValues.Count)
            {
                pointsData.RemoveAt(0);
            }
        }

        private void UpdateTargetPositions()
        {
            float availableWidth = Width - Padding.Horizontal;
            int count = pointsData.Count;
            float step = count > 1 ? availableWidth / (count - 1) : 0;

            for (int i = 0; i < count; i++)
            {
                pointsData[i].TargetValue = targetValues[i];
                pointsData[i].TargetPosition = Padding.Left + i * step;
            }
        }

        private void ForceImmediateUpdate()
        {
            foreach (var p in pointsData)
            {
                p.Value = p.TargetValue;
                p.Position = p.TargetPosition;
            }
            currentMax = targetMax;
            Invalidate();
        }

        private void AnimationTimer_Tick(object sender, EventArgs e)
        {
            if (!AnimationEnabled) return;

            bool needsUpdate = false;

            // Анимация масштаба
            currentMax += (targetMax - currentMax) * animationSpeed;
            if (Math.Abs(currentMax - targetMax) > 0.1f) needsUpdate = true;
            else currentMax = targetMax;

            // Анимация значений и позиций
            foreach (var point in pointsData)
            {
                // Анимация значения
                point.Value += (point.TargetValue - point.Value) * animationSpeed;

                // Анимация позиции
                point.Position += (point.TargetPosition - point.Position) * animationSpeed * 2; // Ускоренная анимация позиции

                if (Math.Abs(point.Value - point.TargetValue) > 0.1f ||
                    Math.Abs(point.Position - point.TargetPosition) > 0.1f)
                {
                    needsUpdate = true;
                }
            }

            if (needsUpdate) Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            if (pointsData.Count < 2) return;

            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            DrawAxes(g);
            DrawChart(g);
        }

        private void DrawAxes(Graphics g)
        {
            using (var axisPen = new Pen(Color.FromArgb(100, Color.White)))
            {
                g.DrawLine(axisPen, Padding.Left, Padding.Top,
                         Padding.Left, Height - Padding.Bottom);

                g.DrawLine(axisPen, Padding.Left, Height - Padding.Bottom,
                         Width - Padding.Right, Height - Padding.Bottom);
            }
        }

        private void DrawChart(Graphics g)
        {
            var points = CalculatePoints();

            // Рисуем область с градиентом
            using (var path = new GraphicsPath())
            {
                path.AddCurve(points.ToArray());
                path.AddLine(points.Last(),
                           new PointF(Width - Padding.Right, Height - Padding.Bottom));
                path.AddLine(new PointF(Width - Padding.Right, Height - Padding.Bottom),
                           new PointF(points.First().X, Height - Padding.Bottom));

                using (var brush = new LinearGradientBrush(
                    ClientRectangle,
                    Color.FromArgb(100, LineColor),
                    Color.Transparent,
                    LinearGradientMode.Vertical))
                {
                    g.FillPath(brush, path);
                }
            }

            // Рисуем линию
            using (var linePen = new Pen(LineColor, LineThickness))
            {
                g.DrawCurve(linePen, points.ToArray(), 0.5f);
            }

            // Рисуем точки
            int index = 0;
            foreach (var point in points)
            {
                using (var brush = new SolidBrush(Color.White))
                using (var pen = new Pen(LineColor, 2))
                {
                    g.FillEllipse(brush, point.X - PointRadius,
                                point.Y - PointRadius,
                                PointRadius * 2, PointRadius * 2);
                    g.DrawEllipse(pen, point.X - PointRadius,
                                point.Y - PointRadius,
                                PointRadius * 2, PointRadius * 2);
                    if (index < targetValues.Count)
                    {
                        string label = targetValues[index].ToString();
                        var size = g.MeasureString(label, Font);
                        g.DrawString(label, Font, new SolidBrush(Color.White),
                            points[index].X - size.Width / 2,
                            points[index].Y - size.Height - 5);
                    }
                }
                index++;
            }
        }

        private List<PointF> CalculatePoints()
        {
            var points = new List<PointF>();
            if (pointsData.Count < 2) return points;

            foreach (var p in pointsData)
            {
                float y = Height - Padding.Bottom -
                         (Height - Padding.Vertical) * (p.Value / currentMax);
                points.Add(new PointF(p.Position, y));
            }

            return points;
        }

        private class PointData
        {
            public float Value { get; set; }
            public float TargetValue { get; set; }
            public float Position { get; set; }
            public float TargetPosition { get; set; }
        }
    }
}