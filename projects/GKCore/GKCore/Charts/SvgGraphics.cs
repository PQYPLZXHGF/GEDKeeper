/*
 *  "GEDKeeper", the personal genealogical database editor.
 *  Copyright (C) 2009-2018 by Sergey V. Zhdanovskih.
 *
 *  This file is part of "GEDKeeper".
 *
 *  This program is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 *
 *  This program is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 *
 *  You should have received a copy of the GNU General Public License
 *  along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

using BSLib;
using GKCore.Interfaces;

namespace GKCore.Charts
{
    public enum LineBreakMode
    {
        None,
        Clip,
        WordWrap,
    }

    public enum TextAlignment
    {
        Left,
        Center,
        Right,
        Justified
    }

    public class SvgGraphics
    {
        private class State
        {
            public const float STD_Rotate = 0;
            public ExtPointF STD_Scale = new ExtPointF(1, 1);
            public ExtPointF STD_Translation = new ExtPointF(0, 0);

            public float Rotate;
            public ExtPointF Scale;
            public ExtPointF Translation;
            public ExtRectF ClippingRect;

            public State()
            {
                Rotate = 0;
                Scale = new ExtPointF(1, 1);
                Translation = new ExtPointF(0, 0);
            }

            public bool IsChanged()
            {
                return (Rotate != STD_Rotate) || !Scale.Equals(STD_Scale) || !Translation.Equals(STD_Translation);
            }

            public string ToString(IFormatProvider fmt)
            {
                string transform = "";
                if (Rotate != 0.0f) {
                    transform += string.Format(fmt, " rotate({0})", Rotate);
                }
                if (Scale.X != 1.0f || Scale.Y != 1.0f) {
                    transform += string.Format(fmt, " scale({0} {1})", Scale.X, Scale.Y);
                }
                if (Translation.X != 0.0f || Translation.Y != 0.0f) {
                    transform += string.Format(fmt, " translate({0} {1})", Translation.X, Translation.Y);
                }
                string result = (string.IsNullOrEmpty(transform)) ? "" : string.Format("transform='{0}'", transform);
                return result;
            }
        }

        private bool fInGroup = false;
        private bool fInPolyline = false;
        private string fLastColor = null;
        private string fLastColorOpacity = null;
        private IFont fLastFont = null;
        private bool fStartedPolyline = false;
        private State fState = new State();

        private readonly IFormatProvider fFmt;
        private readonly Stack<State> fStates;
        private readonly ExtRectF fViewBox;
        private readonly TextWriter fWriter;

        public bool IncludeXmlAndDoctype { get; set; }

        public SvgGraphics(TextWriter tw, ExtRectF viewBox)
        {
            fFmt = CultureInfo.InvariantCulture;
            fViewBox = viewBox;
            fWriter = tw;

            fStates = new Stack<State>();
            fStates.Push(fState);

            IncludeXmlAndDoctype = true;
        }

        public SvgGraphics(Stream s, ExtRectF viewBox)
            : this(new StreamWriter(s, System.Text.Encoding.UTF8), viewBox)
        {
        }

        #region Private methods

        private void WriteLine(string s)
        {
            fWriter.WriteLine(s);
        }

        private void WriteLine(string format, params object[] args)
        {
            WriteLine(string.Format(fFmt, format, args));
        }

        private void Write(string s)
        {
            fWriter.Write(s);
        }

        private void Write(string format, params object[] args)
        {
            Write(string.Format(fFmt, format, args));
        }

        private static string FormatColor(IColor c)
        {
            return string.Format("#{0:X2}{1:X2}{2:X2}", c.GetR(), c.GetG(), c.GetB());
        }

        private string GetTransform()
        {
            return fState.IsChanged() ? fState.ToString(fFmt) : "";
        }

        private string GetStroke(IPen pen)
        {
            string stroke;
            if (pen != null) {
                SetColor(pen.Color);
                stroke = string.Format("stroke=\"{0}\" stroke-opacity=\"{1}\" stroke-width=\"{2}\"",
                                       fLastColor, fLastColorOpacity, pen.Width);
            } else {
                stroke = "stroke=\"none\"";
            }
            return stroke;
        }

        private string GetFill(IBrush brush)
        {
            string fill;
            if (brush != null) {
                SetColor(brush.Color);
                fill = string.Format("fill=\"{0}\" fill-opacity=\"{1}\"",
                                     fLastColor, fLastColorOpacity);
            } else {
                fill = "fill=\"none\"";
            }
            return fill;
        }

        #endregion

        public void BeginDrawing()
        {
            if (IncludeXmlAndDoctype) {
                WriteLine(@"<?xml version=""1.0""?>");
                WriteLine(@"<!DOCTYPE svg PUBLIC ""-//W3C//DTD SVG 1.1//EN"" ""http://www.w3.org/Graphics/SVG/1.1/DTD/svg11.dtd"">");
            }
            WriteLine(@"<svg viewBox=""{0} {1} {2} {3}"" preserveAspectRatio=""xMinYMin meet"" version=""1.1"" xmlns=""http://www.w3.org/2000/svg"">",
                fViewBox.Left, fViewBox.Top, fViewBox.GetWidth(), fViewBox.GetHeight());

            fInGroup = false;
        }

        public void EndDrawing()
        {
            if (fInGroup) {
                WriteLine("</g>");
                fInGroup = false;
            }
            WriteLine("</svg>");
            fWriter.Flush();
        }

        public void BeginEntity(object entity)
        {
            if (fInGroup) {
                WriteLine("</g>");
            }
            var klass = (entity != null) ? entity.ToString() : "";
            WriteLine("<g class=\"{0}\">", klass);
            fInGroup = true;
        }

        public void EndEntity()
        {
            if (fInGroup) {
                WriteLine("</g>");
            }
            fInGroup = false;
        }

        public void Clear(IColor clearColor)
        {
            WriteLine("<g id=\"background\"><rect x=\"{0}\" y=\"{1}\" width=\"{2}\" height=\"{3}\" fill=\"{4}\" stroke=\"none\"/></g>",
                fViewBox.Left, fViewBox.Top, fViewBox.GetWidth(), fViewBox.GetHeight(), FormatColor(clearColor));
        }

        public void SaveState()
        {
            var ns = new State() {
                Rotate = fState.Rotate,
                Scale = fState.Scale,
                Translation = fState.Translation,
            };
            fStates.Push(ns);
            fState = ns;
        }

        public void RestoreState()
        {
            if (fStates.Count > 1) {
                fState = fStates.Pop();
            }
        }

        public void ResetState()
        {
            fStates.Clear();
            fStates.Push(new State());
        }

        public void Scale(float sx, float sy)
        {
            fState.Scale.X *= sx;
            fState.Scale.Y *= sy;
        }

        public void Translate(float dx, float dy)
        {
            fState.Translation.X += dx;
            fState.Translation.Y += dy;
        }

        public void Rotate(float angle)
        {
            fState.Rotate += angle;
        }

        public void SetClippingRect(float x, float y, float width, float height)
        {
            fState.ClippingRect = ExtRectF.CreateBounds(x, y, width, height);
        }

        public void SetFont(IFont f)
        {
            fLastFont = f;
        }

        public void SetColor(IColor c)
        {
            fLastColor = FormatColor(c);
            fLastColorOpacity = string.Format(fFmt, "{0}", c.GetA() / 255.0);
        }

        /*public void FillPolygon(Polygon poly)
        {
            Write("<polygon fill=\"{0}\" fill-opacity=\"{1}\" stroke=\"none\" points=\"", _lastColor, _lastColorOpacity);
            foreach (var p in poly.Points) {
                Write("{0}", p.X);
                Write(",");
                Write("{0}", p.Y);
                Write(" ");
            }
            WriteLine("\" />");
        }

        public void DrawPolygon(Polygon poly, float w)
        {
            Write("<polygon stroke=\"{0}\" stroke-opacity=\"{1}\" stroke-width=\"{2}\" fill=\"none\" points=\"", _lastColor, _lastColorOpacity, w);
            foreach (var p in poly.Points) {
                Write("{0}", p.X);
                Write(",");
                Write("{0}", p.Y);
                Write(" ");
            }
            WriteLine("\" />");
        }*/

        public void DrawEllipse(float x, float y, float width, float height, IPen pen, IBrush brush)
        {
            string transform = GetTransform();
            string stroke = GetStroke(pen);
            string fill = GetFill(brush);

            var rx = width / 2;
            var ry = height / 2;
            var cx = x + rx;
            var cy = y + ry;

            WriteLine("<ellipse cx=\"{0}\" cy=\"{1}\" rx=\"{2}\" ry=\"{3}\" {4} {5} {6} />",
                cx, cy, rx, ry, stroke, fill, transform);
        }

        public void FillArc(float cx, float cy, float radius, float startAngle, float endAngle)
        {
            WriteArc(cx, cy, radius, startAngle, endAngle, 0, "none", "0", fLastColor, fLastColorOpacity);
        }

        public void DrawArc(float cx, float cy, float radius, float startAngle, float endAngle, float w)
        {
            WriteArc(cx, cy, radius, startAngle, endAngle, w, fLastColor, fLastColorOpacity, "none", "0");
        }

        public void WriteArc(float cx, float cy, float radius, float startAngle, float endAngle, float w, string stroke, string strokeOp, string fill, string fillOp)
        {
            string transform = GetTransform();

            var sa = startAngle * (Math.PI / 180.0d);
            var ea = endAngle * (Math.PI / 180.0d);

            var sx = cx + radius * Math.Cos(sa);
            var sy = cy + radius * Math.Sin(sa);
            var ex = cx + radius * Math.Cos(ea);
            var ey = cy + radius * Math.Sin(ea);

            WriteLine("<path d=\"M {0} {1} A {2} {3} 0 0 1 {4} {5}\" stroke=\"{6}\" stroke-opacity=\"{7}\" stroke-width=\"{8}\" fill=\"{9}\" fill-opacity=\"{10}\" {11} />",
                sx, sy, radius, radius, ex, ey,
                stroke, strokeOp, w, fill, fillOp, transform);
        }

        public void FillRoundedRect(float x, float y, float width, float height, float radius)
        {
            WriteLine("<rect x=\"{0}\" y=\"{1}\" width=\"{2}\" height=\"{3}\" rx=\"{6}\" ry=\"{6}\" fill=\"{4}\" fill-opacity=\"{5}\" stroke=\"none\" />",
                x, y, width, height, fLastColor, fLastColorOpacity, radius);
        }

        public void DrawRoundedRect(float x, float y, float width, float height, float radius, float w)
        {
            WriteLine("<rect x=\"{0}\" y=\"{1}\" width=\"{2}\" height=\"{3}\" rx=\"{7}\" ry=\"{7}\" stroke=\"{4}\" stroke-opacity=\"{5}\" stroke-width=\"{6}\" fill=\"none\" />",
                x, y, width, height, fLastColor, fLastColorOpacity, w, radius);
        }

        public void FillRect(float x, float y, float width, float height)
        {
            WriteLine("<rect x=\"{0}\" y=\"{1}\" width=\"{2}\" height=\"{3}\" fill=\"{4}\" fill-opacity=\"{5}\" stroke=\"none\" />",
                x, y, width, height, fLastColor, fLastColorOpacity);
        }

        public void DrawRect(float x, float y, float width, float height, float w)
        {
            WriteLine("<rect x=\"{0}\" y=\"{1}\" width=\"{2}\" height=\"{3}\" stroke=\"{4}\" stroke-opacity=\"{5}\" stroke-width=\"{6}\" fill=\"none\" />",
                x, y, width, height, fLastColor, fLastColorOpacity, w);
        }

        public void BeginLines(bool rounded)
        {
            fInPolyline = true;
        }

        public void DrawLine(float sx, float sy, float ex, float ey, float w)
        {
            string transform = GetTransform();

            if (fInPolyline) {
                if (!fStartedPolyline) {
                    Write("<polyline stroke=\"{0}\" stroke-opacity=\"{1}\" stroke-linecap=\"round\" stroke-linejoin=\"round\" stroke-width=\"{2}\" fill=\"none\" points=\"", fLastColor, fLastColorOpacity, w);
                    Write("{0},{1} ", sx, sy);
                    fStartedPolyline = true;
                }
                Write("{0},{1} ", ex, ey);
            } else {
                WriteLine("<line x1=\"{0}\" y1=\"{1}\" x2=\"{2}\" y2=\"{3}\" stroke=\"{4}\" stroke-opacity=\"{5}\" stroke-width=\"{6}\" stroke-linecap=\"round\" fill=\"none\" {7} />",
                          sx, sy, ex, ey, fLastColor, fLastColorOpacity, w, transform);
            }
        }

        public void EndLines()
        {
            if (fInPolyline) {
                WriteLine("\" />");
                fInPolyline = false;
                fStartedPolyline = false;
            }
        }

        public void DrawString(string s, float x, float y, float width, float height, LineBreakMode lineBreak, TextAlignment align)
        {
            WriteLine("<text x=\"{0}\" y=\"{1}\" font-family=\"{2}\" font-size=\"{3}\">{4}</text>",
                x, y + fLastFont.Size, fLastFont.FontFamilyName, 
                fLastFont.Size * 3 / 2,
                s.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;"));
        }

        public void DrawString(string s, float x, float y)
        {
            string transform = GetTransform();

            WriteLine("<text x=\"{0}\" y=\"{1}\" font-family=\"{2}\" font-size=\"{3}\" {4}>{5}</text>",
                x, y + fLastFont.Size, fLastFont.FontFamilyName,
                fLastFont.Size * 3 / 2,
                transform,
                s.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;"));
        }

        public void DrawImage(IImage img, float x, float y, float width, float height)
        {
        }
    }
}
