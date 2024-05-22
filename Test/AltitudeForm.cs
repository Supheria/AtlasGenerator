﻿using AtlasGenerator.Layout;
using LocalUtilities.SimpleScript.Serialization;
using LocalUtilities.TypeGeneral;
using LocalUtilities.TypeToolKit.Mathematic;

namespace AtlasGenerator.Test;

internal class AltiteudeForm : ResizeableForm
{
    Atlas? Atlas { get; set; } = null;

    public override string LocalName { get; set; } = nameof(AltiteudeForm);

    Button OpenFile { get; } = new()
    {
        Location = new Point(22, 108),
        Size = new Size(75, 23),
        Text = "open atlas",
    };

    Button GenerateNew { get; } = new()
    {
        Location = new Point(22, 200),
        Size = new Size(75, 23),
        Text = "generate new",
    };

    PictureBox AltitudeMap { get; } = new()
    {
        Location = new Point(146, 40),
        Size = new Size(541, 394),
    };

    public AltiteudeForm()
    {
        OnDrawingClient += DrawCilent;
    }

    private void DrawCilent()
    {
        AltitudeMap.Bounds = new(Left + 100, Top, ClientSize.Width - 100, ClientSize.Height);
        MakeMap();
    }

    protected override void InitializeComponent()
    {
        OpenFile.Click += OpenFile_Click;
        GenerateNew.Click += GenerateNew_Click;

        ClientSize = new Size(739, 499);
        Controls.AddRange([
            OpenFile,
            AltitudeMap,
            GenerateNew
            ]);
        Name = "AltiteudeForm";
    }

    private void GenerateNew_Click(object? sender, EventArgs e)
    {
        var data = new AtlasData(new(300, 300), new(2, 2), new(6, 6), RiverLayout.Type.BackwardSlash, 2.25, 50000, 0.66f);
        Atlas = new(data);
        Atlas.SaveToSimpleScript(false);
        MakeMap();
    }

    private void OpenFile_Click(object? sender, EventArgs e)
    {
        var openFile = new OpenFileDialog();
        if (openFile.ShowDialog() == DialogResult.Cancel)
            return;
        Atlas = new Atlas().LoadFromSimpleScript(openFile.FileName);
        MakeMap();
    }

    private void MakeMap()
    {
        if (Atlas is null)
            return;
        var altitudeCount = new Dictionary<double, int>();
        foreach (var points in Atlas.AltitudePoints)
        {
            var alt = points.Altitude;
            if (altitudeCount.ContainsKey(alt))
                altitudeCount[alt]++;
            else
                altitudeCount[alt] = 1;
        }
        altitudeCount = altitudeCount.OrderBy(x => x.Key).ToDictionary();
        var mapHeight = AltitudeMap.Height - 100;
        var heightRatio = mapHeight / (double)altitudeCount.Values.Max();

        AltitudeMap.Image = new Bitmap(AltitudeMap.Width, AltitudeMap.Height);
        var colWidth = AltitudeMap.Width / altitudeCount.Keys.Count;
        var g = Graphics.FromImage(AltitudeMap.Image);
        g.Clear(Color.White);
        var i = 0;
        double lastAltiudeHeight = 0;
        foreach (var pair in altitudeCount)
        {
            var left = colWidth * i;
            var height = (pair.Value * heightRatio).ToInt();
            var top = mapHeight - height + 50;
            g.FillRectangle(new SolidBrush(Color.Green), new(left, top, colWidth, height));
            g.DrawString($"{pair.Value}\n{Math.Round(pair.Value / (double)Atlas.AltitudePoints.Count * 100, 2)}%", LabelFont, new SolidBrush(Color.Red), new RectangleF(left, top - 50, colWidth, 50));
            var altitudeRatio = pair.Key / (double)Atlas.AltitudeMax;

            var altitudeHeight = mapHeight - altitudeRatio * mapHeight + 50;
            if (i is not 0)
                g.DrawLine(Pens.Red, new PointF(colWidth * (i - 1), (float)lastAltiudeHeight), new(left, (float)altitudeHeight));
            lastAltiudeHeight = altitudeHeight;
            g.DrawString(pair.Key.ToString(), LabelFont, new SolidBrush(Color.Black), new RectangleF(left, mapHeight + 50, colWidth, 50));
            i++;
        }
        g.Flush();
        g.Dispose();
    }
}
