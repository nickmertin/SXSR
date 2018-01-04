using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Project
{
    /// <summary>
    /// Interaction logic for SignatureDisplayWindow.xaml
    /// </summary>
    public partial class SignatureDisplayWindow : Window
    {
        public SignatureDisplayWindow(string title, Signature signature)
        {
            InitializeComponent();
            Title = title;
            foreach (SignaturePart part in signature.Parts)
            {
                PathFigure figure = new PathFigure();
                figure.StartPoint = new Point(part[0].X, part[0].Y);
                foreach (SignaturePoint point in part)
                {
                    figure.Segments.Add(new LineSegment(new Point(point.X, point.Y), true));
                    Ellipse _ = new Ellipse { Width = (double)point.Pressure * 20, Height = (double)point.Pressure * 20, Fill = new SolidColorBrush(Colors.Black) };
                    canvas.Children.Add(_);
                    Canvas.SetLeft(_, point.X - (double)point.Pressure * 10);
                    Canvas.SetTop(_, point.Y - (double)point.Pressure * 10);
                }
                path.Figures.Add(figure);
            }
        }
    }
}
