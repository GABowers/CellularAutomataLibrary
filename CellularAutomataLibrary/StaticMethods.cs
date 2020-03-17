using System;
//using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Drawing;
using System.Threading;

namespace CellularAutomataLibrary
{
    public class StaticMethods
    {
        //public static bool CompareState(GraphAgent a, GraphAgent b)
        //{
        //    return a.State == b.State;
        //}

        //public static CAProperty Operate(CAProperty property, CAOperator Operator, dynamic value)
        //{
        //    dynamic self = property.value;
        //    switch(Operator)
        //    {
        //        case CAOperator.Equal:
        //            self = value;
        //            break;
        //        case CAOperator.Addition:
        //            self = self + value;
        //            break;
        //        case CAOperator.Subtraction:
        //            self = self - value;
        //            break;
        //        case CAOperator.Division:
        //            self = self / value;
        //            break;
        //        case CAOperator.Multiplication:
        //            self = self * value;
        //            break;
        //        case CAOperator.Power:
        //            self = Math.Pow(self, value);
        //            break;
        //        case CAOperator.Logarithm:
        //            self = Math.Log(self, value);
        //            break;
        //    }
        //    //CAProperty newProperty = new CAProperty(property.name, self);
        //    return newProperty;
        //}

        //public static CAProperty Operate(dynamic value, CAOperator Operator, CAProperty otherProperty)
        //{
        //    dynamic self = value;
        //    dynamic other = otherProperty.value;
        //    switch (Operator)
        //    {
        //        case CAOperator.Equal:
        //            self = other;
        //            break;
        //        case CAOperator.Addition:
        //            self = self + other;
        //            break;
        //        case CAOperator.Subtraction:
        //            self = self - other;
        //            break;
        //        case CAOperator.Division:
        //            self = self / other;
        //            break;
        //        case CAOperator.Multiplication:
        //            self = self * other;
        //            break;
        //        case CAOperator.Power:
        //            self = Math.Pow(self, other);
        //            break;
        //        case CAOperator.Logarithm:
        //            self = Math.Log(self, other);
        //            break;
        //    }
        //    return self;
        //}


        public static dynamic Operate(dynamic a, CAOperator _operator, dynamic b)
        {
            switch (_operator)
            {
                case CAOperator.Equal:
                    return b;
                case CAOperator.Addition:
                    return a + b;
                case CAOperator.Subtraction:
                    return a - b;
                case CAOperator.Division:
                    return a / b;
                case CAOperator.Multiplication:
                    return a * b;
                case CAOperator.Power:
                    return Math.Pow(a, b);
                case CAOperator.Logarithm:
                    return Math.Log(a, b);
                default:
                    return 0;
            }
        }

        public static List<ValueTuple<ushort, ushort, ushort>> AddEllipsoid(ValueTuple<ushort, ushort, ushort> dimensions, double scale)
        {
            // problem with ellipsoid is that if we don't have a cube of reasonable height, it'll cover everything. The top and bottom involve it coming to the center from the outside, and if it's squashed, that encompasses a lot of those center cells.
            List<ValueTuple<ushort, ushort, ushort>> locations = new List<ValueTuple<ushort, ushort, ushort>>();
            var a = dimensions.Item1;
            var b = dimensions.Item2;
            var c = dimensions.Item3;
            ValueTuple<double, double> increments = Subdivide(a, b, c, scale, Math.PI, (Math.PI * 2), 0, 0, 0);
            int totalInclination = (int)Math.Ceiling(Math.PI / increments.Item1);
            int totalAzimuth = (int)Math.Ceiling((Math.PI * 2) / increments.Item2);
            for (double i = 0; i < totalInclination; i += increments.Item1)
            {
                for (double j = 0; j < totalAzimuth; j += increments.Item2)
                {
                    ushort x = (ushort)Math.Round(scale * Math.Sin(i) * Math.Cos(j));
                    ushort y = (ushort)Math.Round(scale * Math.Sin(i) * Math.Sin(j));
                    ushort z = (ushort)Math.Round(scale * Math.Cos(i));
                    ValueTuple<ushort, ushort, ushort> current = new ValueTuple<ushort, ushort, ushort>(x, y, z);
                    locations.Add(current);
                }
            }
            locations = locations.Distinct().ToList();
            return locations;
            // divide by 2 and get coordinates. If the same as the previous set, don't need to subdivide any more.
        }

        public static List<ValueTuple<ushort, ushort, ushort>> AddCircle(ValueTuple<ushort, ushort> dimensions, double scale)
        {
            double a = (double)dimensions.Item1 / 2;
            double b = (double)dimensions.Item2 / 2;
            int a_floor = (int)Math.Round(a);
            int b_floor = (int)Math.Round(b);
            List<Tuple<ushort, ushort>> locations = new List<Tuple<ushort, ushort>>();
            int total = dimensions.Item1 * dimensions.Item2;
            double val = 360.0 / total;
            for (int i = 0; i < total; i++)
            {
                double use = val * i;
                double rad = (use * Math.PI) / 180.0;
                double x = (a_floor * Math.Cos(rad)) * scale;
                double y = (b_floor * Math.Sin(rad)) * scale;
                var x_floor = (int)Math.Round(x);
                var y_floor = (int)Math.Round(y);
                var loc_x = x_floor + a_floor;
                var loc_y = y_floor + b_floor;

                Tuple<ushort, ushort> loc = new Tuple<ushort, ushort>((ushort)loc_x, (ushort)loc_y);
                if (loc.Item1 > dimensions.Item1 || loc.Item2 > dimensions.Item2)
                {
                    throw new Exception("How did this happen?");
                }
                locations.Add(loc);
            }
            var result = locations.Distinct().ToList();
            return result.Select(x => new ValueTuple<ushort, ushort, ushort>(x.Item1, x.Item2, 0)).ToList();
        }

        static ValueTuple<double, double> Subdivide(int a, int b, int c, double r, double inclination, double azimuth, int xResult, int yResult, int zResult)
        {
            //Console.WriteLine("Inclination: {0} | Azimuth: {1}", inclination, azimuth);
            int xNew = (int)Math.Round(r * Math.Sin(inclination) * Math.Cos(azimuth));
            int yNew = (int)Math.Round(r * Math.Sin(inclination) * Math.Sin(azimuth));
            int zNew = (int)Math.Round(r * Math.Cos(inclination));
            if (xNew == xResult && yNew == yResult && zNew == zResult)
            {
                return new ValueTuple<double, double>(inclination, azimuth);
            }
            else
            {
                var newInclination = inclination;
                var newAzimuth = azimuth;
                if (xNew != xResult || yNew != yResult)
                {
                    newAzimuth /= 2;
                }
                if (zNew != zResult)
                {
                    newInclination /= 2;
                }
                return Subdivide(a, b, c, r, newInclination, newAzimuth, xNew, yNew, zNew);
            }
        }
        // add location based on shapes - 

        public static double GetRandomNumber()
        {
            var rng = new RNGCryptoServiceProvider();
            var bytes = new Byte[8];
            rng.GetBytes(bytes);
            var ul = BitConverter.ToUInt64(bytes, 0) / (1 << 11);
            Double randomDouble = ul / (Double)(1UL << 53);
            return randomDouble;

        }

        public static bool CheckEquality(dynamic a_value, CAEquality equality, dynamic b_value)
        {
            switch (equality)
            {
                case CAEquality.Less_than:
                    return a_value < b_value;
                case CAEquality.Less_than_or_equal_to:
                    return a_value <= b_value;
                case CAEquality.Equal_to:
                    return a_value == b_value;
                case CAEquality.Greater_than_or_equal_to:
                    return a_value >= b_value;
                case CAEquality.Greater_than:
                    return a_value > b_value;
                default:
                    return false;
            }

        }

        //public static T GetTypedValue<T>(CAProperty property)
        //{
        //    dynamic value = property.value;
        //    return value;
        //}

        public static Bitmap MakeImage(CA ca)
        {
            CAGraph graph = ca.Graph;
            CASettings settings = ca.Settings;
            // needs to be SVG that returns various types
            if (graph.Shape == GridShape.Square)
            {
                int x = graph.Cells.GetLength(0);
                int y = graph.Cells.GetLength(1);
                int z = graph.Cells.GetLength(2);

                Bitmap bmp = new Bitmap(x, y);
                using (Graphics g = Graphics.FromImage(bmp))
                {
                    for (ushort i = 0; i < z; i++)
                    {
                        Bitmap layer = new Bitmap(x, y);
                        for (ushort j = 0; j < x; j++)
                        {
                            for (ushort k = 0; k < y; k++)
                            {
                                var cell = graph.GetCell(new ValueTuple<ushort, ushort, ushort>(j, k, i));
                                Color color = Color.Black;
                                if (cell != null)
                                {
                                    if(cell.ContainsAgent())
                                    {
                                        int state = cell.Agent.GetStateProperty();
                                        if(settings.StateColorMap.Count >= state)
                                        {
                                            color = settings.StateColorMap[state];
                                        }
                                    }
                                }
                                layer.SetPixel(j, k, color);
                            }
                        }
                        g.DrawImage(layer, new Point(0, 0));
                    }
                }
                return bmp;
            }
            else
            {
                throw new Exception("Graph shape not expected.");
            }
        }

        /// <summary>
        /// Get a (user-defined) number of colors designed to be as visually distinct as possible (Kenneth Kelly's colors - .
        /// </summary>
        /// <param name="total">The number of colors to use.</param>
        /// <returns></returns>
        public static List<Color> GetColors(int total)
        {
            Color[] kelly_colors = new Color[] { System.Drawing.ColorTranslator.FromHtml("#F2F3F4"), System.Drawing.ColorTranslator.FromHtml("#222222"),
                System.Drawing.ColorTranslator.FromHtml("#F3C300"), System.Drawing.ColorTranslator.FromHtml("#875692"),
                System.Drawing.ColorTranslator.FromHtml("#F38400"), System.Drawing.ColorTranslator.FromHtml("#A1CAF1"),
                System.Drawing.ColorTranslator.FromHtml("#BE0032"), System.Drawing.ColorTranslator.FromHtml("#C2B280"),
                System.Drawing.ColorTranslator.FromHtml("#848482"), System.Drawing.ColorTranslator.FromHtml("#008856"),
                System.Drawing.ColorTranslator.FromHtml("#E68FAC"), System.Drawing.ColorTranslator.FromHtml("#0067A5"),
                System.Drawing.ColorTranslator.FromHtml("#F99379"), System.Drawing.ColorTranslator.FromHtml("#604E97"),
                System.Drawing.ColorTranslator.FromHtml("#F6A600"), System.Drawing.ColorTranslator.FromHtml("#B3446C"),
                System.Drawing.ColorTranslator.FromHtml("#DCD300"), System.Drawing.ColorTranslator.FromHtml("#882D17"),
                System.Drawing.ColorTranslator.FromHtml("#8DB600"), System.Drawing.ColorTranslator.FromHtml("#654522"),
                System.Drawing.ColorTranslator.FromHtml("#E25822"), System.Drawing.ColorTranslator.FromHtml("#2B3D26") };

            if(total > 22)
            {
                throw new Exception("That's too many colors. You'll need to pick your colors yourself.");
            }
            return kelly_colors.ToList().GetRange(0, total);
        }
    }

    static class MyExtensions
    {
        public static void Shuffle<T>(this IList<T> list)
        {
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = ThreadSafeRandom.ThisThreadsRandom.Next(n + 1);
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }
    }


    public static class ThreadSafeRandom
    {
        [ThreadStatic] private static Random Local;

        public static Random ThisThreadsRandom
        {
            get { return Local ?? (Local = new Random(unchecked(Environment.TickCount * 31 + Thread.CurrentThread.ManagedThreadId))); }
        }
    }

}
