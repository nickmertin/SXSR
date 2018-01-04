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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Project
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// SXSR Copyright (c) 2014 Nicholas Mertin
    /// </summary>
    public partial class MainWindow : Window
    {
        // Constants
        const double at = 7; // Angle threshold for recognizing point
        const double tt = 2; // Time difference threshold
        const double lt = 2; // Length difference threshold
        const double dst = 100; // Detail skip threshold
        const double act = 2; // Angle change threshold
        const double mact = 120; // Minimum angle change threshold
        const double adt = 1.8; // Angle direction toggle quantity threshold
        const double sat = 60; // Starting angle difference threshold
        const double pct = 2; // Pressure change threshold
        const double mpct = 5; // Minimum pressure change threshold
        const double pdt = 2.5; // Pressure direction toggle quantity threshold
        const double spt = .2; // Starting pressure difference threshold
        const double set = 100; // Starting/ending point difference threshold
        const double edt = 2.5; // Endpoint difference ratio threshold
        // Variables
        Signature original;
        Signature signature;
        int attempt = 0;
        List<SignaturePart> parts = new List<SignaturePart>();
        DateTime time;
        public MainWindow()
        {
            InitializeComponent();
        }
        private void collected(object sender, InkCanvasStrokeCollectedEventArgs e)
        {
            SignaturePart part = new SignaturePart();
            part.Offset = DateTime.Now - time;
            foreach (StylusPoint point in e.Stroke.StylusPoints)
                part.Add(new SignaturePoint(point.X, point.Y, point.PressureFactor));
            parts.Add(part);
        }
        private void save(object sender, RoutedEventArgs e)
        {
            // Simplify
            for (int n = 0; n < parts.Count; ++n)
                if (parts[n].Count < 3) parts.RemoveAt(n--);
            foreach (SignaturePart part in parts)
            {
                Console.WriteLine("--- START NEW PART ---\nOriginal Points: " + part.Count);
            // If first 2 points are identical, remove
            chk1stptssame:
                if (part[0].X == part[1].X && part[0].Y == part[1].Y)
                {
                    part.RemoveAt(0);
                    goto chk1stptssame;
                }
                // Include first 2 points and set starting point for comparison
                int j = 2;
                double x = part[1].X;
                double y = part[1].Y;
                // Set starting angle for comparison to the angle between first 2 points
                double a = angle(part[0].X, part[0].Y, part[1].X, part[1].Y);
                for (; j < part.Count - 1; )
                {
                    // If angle difference is less than threshold or 2 points are identical, remove
                    if (Math.Abs(angle(x, y, part[j].X, part[j].Y) - a) < at || (part[j].X == part[j - 1].X && part[j].Y == part[j - 1].Y))
                        part.RemoveAt(j);
                    else
                    {
                        Console.WriteLine("Point {0}: ({1},{2}) Angle = {3}", j, part[j].X, part[j].Y, angle(x, y, part[j].X, part[j].Y));
                        a = angle(x, y, part[j].X, part[j].Y);
                        x = part[j].X;
                        y = part[j].Y;
                        ++j;
                    }
                }
            }
            // Set signature object
            signature = new Signature(parts);
            // Compare unless this is the original
            byte code = 0;
            int i = -1;
            if (attempt > 0)
            {
                // Compare number of parts
                if (original.Parts.Count != signature.Parts.Count)
                {
                    code = 1;
                    Console.WriteLine("--- FAIL ---\nCode 1\nOriginal: {0}\nAttempt:  {1}", original.Parts.Count, signature.Parts.Count);
                    goto cleanup;
                }
                // Cycle through parts
                for (i = 0; i < original.Parts.Count; ++i)
                {
                    // Compare time offsets
                    double x = original.Parts[i].Offset.TotalMilliseconds / signature.Parts[i].Offset.TotalMilliseconds;
                    if (x < 1 / tt || x > tt)
                    {
                        code = 2;
                        Console.WriteLine("--- FAIL ---\nCode 2\nOriginal: {0}\nAttempt:  {1}\nRatio:    {2}", original.Parts[i].Offset.TotalMilliseconds, signature.Parts[i].Offset.TotalMilliseconds, x);
                        goto cleanup;
                    }
                    // Compare linear length
                    double o = 0;
                    double a = 0;
                    for (int j = 1; j < original.Parts[i].Count; ++j)
                        o += Math.Sqrt(Math.Pow(original.Parts[i][j - 1].X - original.Parts[i][j].X, 2) + Math.Pow(original.Parts[i][j - 1].Y - original.Parts[i][j].Y, 2));
                    for (int j = 1; j < signature.Parts[i].Count; ++j)
                        a += Math.Sqrt(Math.Pow(signature.Parts[i][j - 1].X - signature.Parts[i][j].X, 2) + Math.Pow(signature.Parts[i][j - 1].Y - signature.Parts[i][j].Y, 2));
                    // If part is too short, skip detailed tests
                    if (Math.Max(o, a) < dst) goto skip;
                    // Continue comparing length
                    x = o / a;
                    if (x < 1 / lt || x > lt)
                    {
                        code = 3;
                        Console.WriteLine("--- FAIL ---\nCode 3\nOriginal: {0}\nAttempt:  {1}\nRatio:    {2}", o, a, x);
                        goto cleanup;
                    }
                    // Compare total angle change
                    double ao = o = angle(original.Parts[i][0].X, original.Parts[i][0].Y, original.Parts[i][1].X, original.Parts[i][1].Y);
                    double aa = a = angle(signature.Parts[i][0].X, signature.Parts[i][0].Y, signature.Parts[i][1].X, signature.Parts[i][1].Y);
                    for (int j = 2; j < original.Parts[i].Count; ++j)
                    {
                        o += Math.Abs(angle(original.Parts[i][j - 1].X, original.Parts[i][j - 1].Y, original.Parts[i][j].X, original.Parts[i][j].Y) - ao);
                        ao = angle(original.Parts[i][j - 1].X, original.Parts[i][j - 1].Y, original.Parts[i][j].X, original.Parts[i][j].Y);
                    }
                    for (int j = 2; j < signature.Parts[i].Count; ++j)
                    {
                        a += Math.Abs(angle(signature.Parts[i][j - 1].X, signature.Parts[i][j - 1].Y, signature.Parts[i][j].X, signature.Parts[i][j].Y) - aa);
                        aa = angle(signature.Parts[i][j - 1].X, signature.Parts[i][j - 1].Y, signature.Parts[i][j].X, signature.Parts[i][j].Y);
                    }
                    // Skip angle change test if necessary
                    if (Math.Max(a, o) < mact) goto sac;
                    x = a / o;
                    if (x < 1 / act || x > act)
                    {
                        code = 4;
                        Console.WriteLine("--- FAIL ---\nCode 4\nOriginal: {0}\nAttempt:  {1}\nRatio:    {2}", o, a, x);
                        goto cleanup;
                    }
                    // Compare +/- of angle change
                    bool co = angle(original.Parts[i][0].X, original.Parts[i][0].Y, original.Parts[i][1].X, original.Parts[i][1].Y) > angle(original.Parts[i][1].X, original.Parts[i][1].Y, original.Parts[i][2].X, original.Parts[i][2].Y);
                    bool ca = angle(signature.Parts[i][0].X, signature.Parts[i][0].Y, signature.Parts[i][1].X, signature.Parts[i][1].Y) > angle(signature.Parts[i][1].X, signature.Parts[i][1].Y, signature.Parts[i][2].X, signature.Parts[i][2].Y);
                    bool so = co;
                    bool sa = ca;
                    List<int> to = new List<int>();
                    List<int> ta = new List<int>();
                    for (int j = 2; j < original.Parts[i].Count; ++j)
                        if ((angle(original.Parts[i][j - 2].X, original.Parts[i][j - 2].Y, original.Parts[i][j - 1].X, original.Parts[i][j - 1].Y) > angle(original.Parts[i][j - 1].X, original.Parts[i][j - 1].Y, original.Parts[i][j].X, original.Parts[i][j].Y)) != co)
                        {
                            to.Add(j);
                            co = !co;
                        }
                    for (int j = 2; j < signature.Parts[i].Count; ++j)
                        if ((angle(signature.Parts[i][j - 2].X, signature.Parts[i][j - 2].Y, signature.Parts[i][j - 1].X, signature.Parts[i][j - 1].Y) > angle(signature.Parts[i][j - 1].X, signature.Parts[i][j - 1].Y, signature.Parts[i][j].X, signature.Parts[i][j].Y)) != ca)
                        {
                            ta.Add(j);
                            ca = !ca;
                        }
                    x = to.Count * 1.0 / ta.Count;
                    if (x > adt || x < 1 / adt)
                    {
                        code = 5;
                        Console.WriteLine("--- FAIL ---\nCode 5\nOriginal: {0}\nAttempt:  {1}\nRatio:    {2}", to.Count, ta.Count, x);
                        goto cleanup;
                    }
                sac:
                    // Compare starting angle
                    o = angle(original.Parts[i][0].X, original.Parts[i][0].Y, original.Parts[i][1].X, original.Parts[i][1].Y);
                    a = angle(signature.Parts[i][0].X, signature.Parts[i][0].Y, signature.Parts[i][1].X, signature.Parts[i][1].Y);
                    x = Math.Abs(o - a);
                    if (x > sat)
                    {
                        code = 6;
                        Console.WriteLine("--- FAIL ---\nCode 6\nOriginal:   {0}\nAttempt:    {1}\nDifference: {2}", o, a, x);
                        goto cleanup;
                    }
                    goto next;
                skip:
                    Console.WriteLine("Skipped detailed tests");
                next:
                    // Compare total pressure change
                    double po = o = (double)original.Parts[i][0].Pressure;
                    double pa = a = (double)signature.Parts[i][0].Pressure;
                    for (int j = 1; j < original.Parts[i].Count; ++j)
                    {
                        o += Math.Abs((double)original.Parts[i][j].Pressure - po);
                        po = (double)original.Parts[i][j].Pressure;
                    }
                    for (int j = 1; j < signature.Parts[i].Count; ++j)
                    {
                        a += Math.Abs((double)signature.Parts[i][j].Pressure - pa);
                        pa = (double)signature.Parts[i][j].Pressure;
                    }
                    x = a / o;
                    if (Math.Max(a, o) < mpct) goto spc;
                    if (x < 1 / pct || x > pct)
                    {
                        code = 7;
                        Console.WriteLine("--- FAIL ---\nCode 7\nOriginal: {0}\nAttempt:  {1}\nRatio:    {2}", o, a, x);
                        goto cleanup;
                    }
                    // Compare +/- of pressure change
                    co = original.Parts[i][0].Pressure < original.Parts[i][1].Pressure;
                    ca = signature.Parts[i][0].Pressure < signature.Parts[i][1].Pressure;
                    so = co;
                    sa = ca;
                    to = new List<int>();
                    ta = new List<int>();
                    for (int j = 2; j < original.Parts.Count; ++j)
                        if (original.Parts[i][j].Pressure > original.Parts[i][j - 1].Pressure)
                        {
                            to.Add(j);
                            co = !co;
                        }
                    for (int j = 2; j < signature.Parts.Count; ++j)
                        if (signature.Parts[i][j].Pressure > signature.Parts[i][j - 1].Pressure)
                        {
                            ta.Add(j);
                            ca = !ca;
                        }
                    x = to.Count * 1.0 / ta.Count;
                    if (x > pdt || x < 1 / pdt)
                    {
                        code = 8;
                        Console.WriteLine("--- FAIL ---\nCode 8\nOriginal: {0}\nAttempt:  {1}\nRatio:    {2}", to.Count, ta.Count, x);
                        goto cleanup;
                    }
                spc:
                    // Compare starting pressure
                    x = Math.Abs((double)(original.Parts[i][0].Pressure - signature.Parts[i][0].Pressure));
                    if (x > spt)
                    {
                        code = 9;
                        Console.WriteLine("--- FAIL ---\nCode 9\nOriginal: {0}\nAttempt:  {1}\nRatio:    {2}", original.Parts[i][0].Pressure, signature.Parts[i][0].Pressure, x);
                        goto cleanup;
                    }
                    // Compare starting position
                    x = Math.Sqrt(Math.Pow(Math.Abs(original.Parts[i][0].X - signature.Parts[i][0].X), 2) + Math.Pow(Math.Abs(original.Parts[i][0].Y - signature.Parts[i][0].Y), 2));
                    if (x > set)
                    {
                        code = 10;
                        Console.WriteLine("--- FAIL ---\nCode 10\nOriginal: ({0},{1})\nAttempt:  ({2},{3})\nDistance: {4}", original.Parts[i][0].X, original.Parts[i][0].Y, signature.Parts[i][0].X, signature.Parts[i][0].Y, x);
                        goto cleanup;
                    }
                    // Compare ending position
                    double y = Math.Sqrt(Math.Pow(Math.Abs(original.Parts[i].Last().X - signature.Parts[i].Last().X), 2) + Math.Pow(Math.Abs(original.Parts[i].Last().Y - signature.Parts[i].Last().Y), 2));
                    if (y > set)
                    {
                        code = 11;
                        Console.WriteLine("--- FAIL ---\nCode 11\nOriginal: ({0},{1})\nAttempt:  ({2},{3})\nDistance: {4}", original.Parts[i].Last().X, original.Parts[i].Last().Y, signature.Parts[i].Last().X, signature.Parts[i].Last().Y, x);
                        goto cleanup;
                    }
                    // Compare endpoint difference ratio
                    if (x / y > edt || x / y < 1 / edt)
                    {
                        code = 12;
                        Console.WriteLine("--- FAIL ---\nCode 12\nStart: {0}\nEnd:   {1}\nRatio: {2}", x, y, x / y);
                        goto cleanup;
                    }
                }
            }
            else original = signature;
            // Clean up
        cleanup:
            canvas.Strokes.Clear();
            new SignatureDisplayWindow(attempt == 0 ? "Original" : ("Attempt #" + attempt) + " - Code " + code + (i < 0 ? "" : (" on part " + i)) + ": Access " + (code > 0 ? "Denied" : "Granted"), signature).Show();
            ++attempt;
            parts = new List<SignaturePart>();
            go.IsEnabled = false;
            canvas.IsEnabled = false;
            ts.Visibility = Visibility.Visible;
        }
        double angle(double x1, double y1, double x2, double y2)
        {
            double x = Math.Atan2(Math.Abs(y1 - y2), Math.Abs(x1 - x2)) * 180 / Math.PI;
            return x == double.NaN ? 0 : x;
        }
        private void start(object sender, RoutedEventArgs e)
        {
            go.IsEnabled = true;
            canvas.IsEnabled = true;
            time = DateTime.Now;
            ts.Visibility = Visibility.Collapsed;
        }
    }
}