﻿using AtlasGenerator.VoronoiDiagram.BorderDisposal;
using AtlasGenerator.VoronoiDiagram.Data;
using AtlasGenerator.VoronoiDiagram.Model;
using LocalUtilities.GraphUtilities;
using LocalUtilities.MathBundle;

namespace AtlasGenerator.VoronoiDiagram;

/// <summary>
/// An Euclidean plane where a Voronoi diagram can be constructed from <see cref="VoronoiCell"/>s
/// producing a tesselation of cells with <see cref="VoronoiEdge"/> line segments and <see cref="VoronoiVertex"/> vertices.
/// </summary>
public class VoronoiPlane(int width, int height)
{
    List<VoronoiCell> Cells { get; set; } = [];

    List<VoronoiEdge> Edges { get; set; } = [];

    int Width { get; set; } = width;

    int Height { get; set; } = height;

    /// <summary>
    /// The generated sites are guaranteed not to lie on the border of the plane (although they may be very close).
    /// </summary>
    public List<VoronoiCell> Generate(int widthSegmentNumber, int heightSegmentNumber, IPointsGeneration pointsGeneration)
    {
        var coordinates = new List<Coordinate>();
        var widthSegment = Width / widthSegmentNumber;
        var heightSegment = Height / heightSegmentNumber;
        for (int i = 0; i < widthSegmentNumber; i++)
        {
            for (int j = 0; j < heightSegmentNumber; j++)
            {
                var (X, Y) = pointsGeneration.Generate(widthSegment * i, heightSegment * j, widthSegment * (i + 1), heightSegment * (j + 1), 1).First();
                coordinates.Add(new(X, Y));
            }
        }
        Cells = UniquePoints(coordinates).Select(c => new VoronoiCell(c)).ToList();
        Edges.Clear();
        Generate();
        return Cells;
    }

    private static List<Coordinate> UniquePoints(List<Coordinate> coordinates)
    {
        coordinates.Sort((p1, p2) =>
        {
            if (p1.X.ApproxEqual(p2.X))
            {
                if (p1.Y.ApproxEqual(p2.Y))
                    return 0;
                if (p1.Y < p2.Y)
                    return -1;
                return 1;
            }
            if (p1.X < p2.X)
                return -1;
            return 1;
        });
        var unique = new List<Coordinate>();
        var last = coordinates.First();
        unique.Add(last);
        for (var index = 1; index < coordinates.Count; index++)
        {
            var coordiante = coordinates[index];
            if (!last.X.ApproxEqual(coordiante.X) ||
                !last.Y.ApproxEqual(coordiante.Y))
            {
                unique.Add(coordiante);
                last = coordiante;
            }
        }
        return unique;
    }

    private void Generate()
    {
        var eventQueue = new MinHeap<IFortuneEvent>(5 * Cells.Count);
        foreach (var site in Cells)
            eventQueue.Insert(new FortuneSiteEvent(site));
        //init tree
        var beachLine = new BeachLine();
        var edges = new LinkedList<VoronoiEdge>();
        var deleted = new HashSet<FortuneCircleEvent>();
        //init edge list
        while (eventQueue.Count != 0)
        {
            IFortuneEvent fEvent = eventQueue.Pop();
            if (fEvent is FortuneSiteEvent fsEvent)
                beachLine.AddBeachSection(fsEvent, eventQueue, deleted, edges);
            else
            {
                if (deleted.Contains((FortuneCircleEvent)fEvent))
                    deleted.Remove((FortuneCircleEvent)fEvent);
                else
                    beachLine.RemoveBeachSection((FortuneCircleEvent)fEvent, eventQueue, deleted, edges);
            }
        }
        Edges = edges.ToList();
        Edges = BorderClipping.Clip(Edges, 0, 0, Width, Height);
        Edges = BorderClosing.Close(Edges, 0, 0, Width, Height, Cells);
    }
}