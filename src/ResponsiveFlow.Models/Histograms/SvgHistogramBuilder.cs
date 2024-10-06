using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Xml.Linq;

namespace ResponsiveFlow;

internal sealed class SvgHistogramBuilder<TBin>
{
    private IReadOnlyList<string> _colors = SvgHistogramColors.Blue;

    internal IReadOnlyList<string> Colors
    {
        get => _colors;
        set
        {
            Debug.Assert(value.Count >= 2);
            _colors = value;
        }
    }

    internal XElement Build<TBinPolicy>(IReadOnlyList<TBin> bins, TBinPolicy binPolicy)
        where TBinPolicy : IBinPolicy<TBin>
    {
        const double margin = 8.0;
        const double pivotX = margin;
        const double pivotY = margin;

        const double maxRectWidthPx = 512.0;
        const double minRectHeightPx = 24.0;
        double minRectHeightMs = bins.Min(it => binPolicy.Gap(it));
        double maxRectHeightMs = bins.Max(it => binPolicy.Gap(it));
        double maxRectHeightPx = minRectHeightPx * maxRectHeightMs / minRectHeightMs;
        double maxRectWidthUnits = bins.Max(binPolicy.Height);

        double widthPixelsByUnits = maxRectWidthPx / maxRectWidthUnits;
        double heightPixelsByMs = maxRectHeightPx / maxRectHeightMs;

        XNamespace ns = "http://www.w3.org/2000/svg";
        double plotHeight = bins.Sum(it => heightPixelsByMs * binPolicy.Gap(it));
        XElement root = new(ns + "svg",
            new XAttribute("version", "2"),
            new XAttribute("width", maxRectWidthPx + 2 * margin),
            new XAttribute("height", plotHeight + 2 * margin));

        double y = pivotY;
        for (int i = 0; i < bins.Count; ++i)
        {
            var bin = bins[i];
            double widthPx = Math.Max(1.0, widthPixelsByUnits * binPolicy.Height(bin));
            double heightPx = heightPixelsByMs * binPolicy.Gap(bin);

            XElement rect = new(ns + "rect",
                new XAttribute("width", widthPx),
                new XAttribute("height", heightPx),
                new XAttribute("x", pivotX),
                new XAttribute("y", y),
                new XAttribute("fill", Colors[i & 1]));
            root.Add(rect);

            y += heightPx;
        }

        return root;
    }
}
