using System;
using System.IO;
namespace Ttf
{
    partial class OpenFont
    {
        internal enum GlyphPointKind
        {
            Normal = 0,
            Curve = 1,
            Marker = 2
        }
        internal struct GlyphPoint
        {
            public PointF Location { get; set; }
            public GlyphPointKind Kind { get; set; }

            public static GlyphPoint Marker
            {
                get
                {
                    GlyphPoint result=default(GlyphPoint);
                    result.Location = new PointF(0, 0);
                    result.Kind = GlyphPointKind.Marker;
                    return result;
                }
            }
        }
        internal struct Glyph
        {
            public Int16 Index { get; set; }
            public GlyphPoint[] Points { get; set; }
            public UInt16[] ContourEndpoints { get; set; }
            public SRect16 Bounds { get; set; }
            public Byte[] Instructions { get; set; }

            public static readonly Glyph Empty = default(Glyph);

            public Glyph Clone()
            {
                if(this.Points==null)
                {
                    return Empty;
                }
                Glyph result=new Glyph();
                result.Index = Index;
                result.Points = new GlyphPoint[Points.Length];
                Points.CopyTo(result.Points, 0);
                result.ContourEndpoints = new UInt16[ContourEndpoints.Length];
                ContourEndpoints.CopyTo(result.ContourEndpoints, 0);
                result.Bounds = Bounds;
                result.Instructions = new Byte[Instructions.Length];
                Instructions.CopyTo(result.Instructions, 0);
                return result; 
            }
            internal void Transform2x2(float m00,float m01,float m10,float m11)
            {
                if (null == Points)
                    return;
                //http://stackoverflow.com/questions/13188156/whats-the-different-between-vector2-transform-and-vector2-transformnormal-i
                //http://www.technologicalutopia.com/sourcecode/xnageometry/vector2.cs.htm

                //change data on current glyph
                float newXMin = 0;
                float newYMin = 0;
                float newXMax = 0;
                float newYMax = 0;

                for (int i = 0; i < Points.Length; ++i)
                {
                    float x = Points[i].Location.X;
                    float y = Points[i].Location.Y;
                    float newX, newY;
                    //please note that this is transform normal***
                    Points[i].Location = new PointF(
                        newX = (float)(Math.Round((x * m00) + (y * m10))),
                    newY = (float)(Math.Round((x * m01) + (y * m11)))
                    );
                    
                    //short newX = xs[i] = (short)Math.Round((x * m00) + (y * m10));
                    //short newY = ys[i] = (short)Math.Round((x * m01) + (y * m11));
                    //------
                    if (newX < newXMin)
                    {
                        newXMin = newX;
                    }
                    if (newX > newXMax)
                    {
                        newXMax = newX;
                    }
                    //------
                    if (newY < newYMin)
                    {
                        newYMin = newY;
                    }
                    if (newY > newYMax)
                    {
                        newYMax = newY;
                    }
                }
                Bounds = new SRect16(
                   (Int16)newXMin, (Int16)newYMin,
                   (Int16)newXMax, (Int16)newYMax);
            }
            internal void Offset(Int16 dx, Int16 dy)
            {
                if (null == Points)
                    return;
                for (int i = 0; i < Points.Length; ++i)
                {
                    Points[i].Location = new PointF(Points[i].Location.X + dx,
                                                Points[i].Location.Y + dy);
                }

                Bounds = new SRect16((Int16)(Bounds.X1 + dx), (Int16)(Bounds.Y1 + dy), (Int16)(Bounds.X2 + dx), (Int16)(Bounds.X2 + dy));
            }
            internal void Append(Glyph src)
            {
                int org_dest_len = ContourEndpoints.Length;

                if (org_dest_len == 0)
                {
                    Points = src.Points;
                    ContourEndpoints = src.ContourEndpoints;
                }
                else
                {

                    UInt16 org_last_point = (UInt16)(ContourEndpoints[org_dest_len - 1] + 1); 
                    int new_points_size = src.Points.Length+ Points.Length;
                    GlyphPoint[] new_points = new GlyphPoint[new_points_size];
                    Points.CopyTo(new_points, 0);
                    src.Points.CopyTo(new_points, Points.Length);
                    int new_contour_endpoints_size = ContourEndpoints.Length + src.ContourEndpoints.Length;
                    UInt16[] new_contour_endpoints = new UInt16[new_contour_endpoints_size];
                    ContourEndpoints.CopyTo(new_contour_endpoints, 0);
                    src.ContourEndpoints.CopyTo(new_contour_endpoints, ContourEndpoints.Length);
                    Points = new_points;
                    ContourEndpoints = new_contour_endpoints;
                    //offset latest append contour  end points
                    int newlen = ContourEndpoints.Length;
                    for (int i = org_dest_len; i < newlen; ++i)
                    {
                        ContourEndpoints[i] += (UInt16)org_last_point;
                    }
                }

                //calculate new bounds
                Bounds = new SRect16(Bounds.X1 < src.Bounds.X1 ? Bounds.X1 : src.Bounds.X1,
                                    Bounds.Y1 < src.Bounds.Y1 ? Bounds.Y1 : src.Bounds.Y1,
                                    Bounds.X2 > src.Bounds.X2 ? Bounds.X2 : src.Bounds.X2,
                                    Bounds.Y2 > src.Bounds.Y2 ? Bounds.Y2 : src.Bounds.Y2);
            }

        }
    }
}
