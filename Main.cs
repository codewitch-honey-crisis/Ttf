using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Ttf
{
    public partial class Main : Form
    {
        OpenFont _font = null;
        const string _fontPath = @"..\..\Shangar.ttf";
        Point[] _poly;
        
        public Main()
        {
            InitializeComponent();
            
            using(var stream = File.Open(_fontPath,FileMode.Open,FileAccess.Read))
            {
                _font = OpenFont.Read(stream);
                
            }
            bool skipNext;
            var g = _font.GetGlyph('B', 0, out skipNext);
            _poly = new Point[g.Points.Length + 1];
            for(int i = 0;i<g.Points.Length;++i)
            {
                _poly[i]=new Point((int)g.Points[i].Location.X / 10, (int)g.Points[i].Location.Y / 10);
                
            }
            // close the polygon
            _poly[_poly.Length - 1] = _poly[0];
        }
        static Rectangle PolyBounds(Point[] V)
        {
            int x1=V[0].X, y1=V[0].Y, x2=x1, y2=y1;
            for(int i = 1;i<V.Length;++i)
            {
                Point p = V[i];
                if (p.X < x1) x1 = p.X;
                if (p.Y < y1) y1 = p.Y;
                if (p.X > x2) x2 = p.X;
                if (p.Y > y2) y2 = p.Y;
            }
            return new Rectangle(x1, y1, x2 - x1 + 1, y2 - y1 + 1); 
        }
        static int IsLeft(Point P0, Point P1, Point P2)
        {
            return ((P1.X - P0.X) * (P2.Y - P0.Y)
                    - (P2.X - P0.X) * (P1.Y - P0.Y));
        }
        static bool IsInsidePolygon(Point P, Point[] V)
        {
            int wn = 0;    // the  winding number counter
            int n = V.Length - 1;
            // loop through all edges of the polygon
            for (int i = 0; i < n; i++)
            {   // edge from V[i] to  V[i+1]
                if (V[i].Y <= P.Y)
                {          // start y <= P.y
                    if (V[i + 1].Y > P.Y)      // an upward crossing
                        if (IsLeft(V[i], V[i + 1], P) > 0)  // P left of  edge
                            ++wn;            // have  a valid up intersect
                }
                else
                {                        // start y > P.y (no test needed)
                    if (V[i + 1].Y <= P.Y)     // a downward crossing
                        if (IsLeft(V[i], V[i + 1], P) < 0)  // P right of  edge
                            --wn;            // have  a valid down intersect
                }
            }
            return wn!=0;
        }
        static bool IsInsidePolygon2(Point P, Point[] V)
        {
            int i, j;
            bool c = false;
            for (i = 0, j = V.Length - 1; i < V.Length; j = i++)
            {
                Point pti = V[i];
                Point ptj = V[j];
                if (((pti.Y > P.Y) != (ptj.Y > P.Y)) &&
                 (P.X < (ptj.X - pti.X) * (P.Y - pti.Y) / (ptj.Y - pti.Y) + pti.X))
                    c = !c;
            }
            return c;
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            using (Brush brush = new SolidBrush(Color.Black))
            {
                Rectangle b = PolyBounds(_poly);
                for(int y = 0;y<b.Height;++y)
                {
                    for(int x = 0;x<b.Width;++x)
                    {
                        Point pt = new Point(x + b.X, y + b.Y);
                        if (IsInsidePolygon2(pt,_poly))
                        {
                            e.Graphics.FillRectangle(brush, new Rectangle(pt, new Size(1, 1)));
                        }
                    }
                }
            }
        }
    }
}
