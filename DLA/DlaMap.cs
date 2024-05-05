using AtlasGenerator.Test;
using AtlasGenerator.VoronoiDiagram.Data;
using LocalUtilities.MathBundle;

namespace AtlasGenerator.DLA;

public class DlaMap(VoronoiCell cell)
{
    Dictionary<(int X, int Y), DlaPixel> PixelMap { get; } = [];

    VoronoiCell Cell { get; set; } = cell;

    Rectangle Bounds { get; set; } = cell.GetBounds();

#if DEBUG
    public static TestForm TestForm { get; } = new();
#endif

    public DlaPixel[] Generate(int pixelCount)
    {
        PixelMap.Clear();
        (int X, int Y) root = ((int)Cell.Site.X, (int)Cell.Site.Y);
        //var root = Region.Site;
        PixelMap[root] = new(root);
        bool innerFilter(int x, int y) => Cell.ContainPoint(x, y);
        for (int i = 0; PixelMap.Count < (int)(pixelCount * 0.5f); i++)
        {
            var pixel = AddWalker(innerFilter);
            PixelMap[(pixel.X, pixel.Y)] = pixel;
            TestForm.Now++;
            TestForm.Progress();
        }
        bool outerFilter(int x, int y) => Bounds.Contains(x, y);
        for (int i = 0; PixelMap.Count < pixelCount; i++)
        {
            var pixel = AddWalker(outerFilter);
            PixelMap[(pixel.X, pixel.Y)] = pixel;
            TestForm.Now++;
            TestForm.Progress();
        }
        ComputeHeight();
        return PixelMap.Values.ToArray();
    }

    private DlaPixel AddWalker(Func<int, int, bool> pixelFilter)
    {
        var pixel = new DlaPixel((
                new Random().Next(Bounds.Left, Bounds.Right + 1),
                new Random().Next(Bounds.Top, Bounds.Bottom + 1)
                ));
        while (!CheckStuck(pixel))
        {
            int x = pixel.X, y = pixel.Y;
            switch (new Random().Next(0, 8))
            {
                case 0: // left
                    x--;
                    break;
                case 1: // right
                    x++;
                    break;
                case 2: // up
                    y--;
                    break;
                case 3: // down
                    y++;
                    break;
                case 4: // left up
                    x--;
                    y--;
                    break;
                case 5: // up right
                    x++;
                    y--;
                    break;
                case 6: // bottom right
                    x++;
                    y++;
                    break;
                case 7: // left bottom
                    x--;
                    y++;
                    break;
            }
            if (pixelFilter(x, y))
                pixel = new((x, y));
            else
                pixel = new((
                    new Random().Next(Bounds.Left, Bounds.Right + 1),
                    new Random().Next(Bounds.Top, Bounds.Bottom + 1)));
        }
        return pixel;
    }

    private bool CheckStuck(DlaPixel pixel)
    {
        var X = pixel.X;
        var Y = pixel.Y;
        var left = X - 1;//Math.Max(x - 1, Bounds.Left);
        var top = Y - 1;//Math.Max(y - 1, Bounds.Top);
        var right = X + 1;//Math.Min(x + 1, Bounds.Right);
        var bottom = Y + 1; //Math.Min(y + 1, Bounds.Bottom);
        bool isStucked = false;
        if (PixelMap.ContainsKey((X, Y)))
            return false;
        if (PixelMap.TryGetValue((left, Y), out var stucked))
        {
            pixel.Neighbor[Direction.Left] = (left, Y);
            stucked.Neighbor[Direction.Right] = (X, Y);
            isStucked = true;
        }
        if (PixelMap.TryGetValue((right, Y), out stucked))
        {
            pixel.Neighbor[Direction.Right] = (right, Y);
            stucked.Neighbor[Direction.Left] = (X, Y);
            isStucked = true;
        }
        if (PixelMap.TryGetValue((X, top), out stucked))
        {
            pixel.Neighbor[Direction.Top] = (X, top);
            stucked.Neighbor[Direction.Bottom] = (X, Y);
            isStucked = true;
        }
        if (PixelMap.TryGetValue((X, bottom), out stucked))
        {
            pixel.Neighbor[Direction.Bottom] = (X, bottom);
            stucked.Neighbor[Direction.Top] = (X, Y);
            isStucked = true;
        }
        if (PixelMap.TryGetValue((left, top), out stucked))
        {
            pixel.Neighbor[Direction.LeftTop] = (left, top);
            stucked.Neighbor[Direction.BottomRight] = (X, Y);
            isStucked = true;
        }
        if (PixelMap.TryGetValue((left, bottom), out stucked))
        {
            pixel.Neighbor[Direction.LeftBottom] = (left, bottom);
            stucked.Neighbor[Direction.TopRight] = (X, Y);
            isStucked = true;
        }
        if (PixelMap.TryGetValue((right, top), out stucked))
        {
            pixel.Neighbor[Direction.TopRight] = (right, top);
            stucked.Neighbor[Direction.LeftBottom] = (X, Y);
            isStucked = true;
        }
        if (PixelMap.TryGetValue((right, bottom), out stucked))
        {
            pixel.Neighbor[Direction.BottomRight] = (right, bottom);
            stucked.Neighbor[Direction.LeftTop] = (X, Y);
            isStucked = true;
        }

        return isStucked;
    }
    /// <summary>
    /// 
    /// </summary>
    /// <param name="pixelMap"></param>
    /// <returns>the max of heights</returns>
    private void ComputeHeight()
    {
        foreach (var pair in PixelMap)
        {
            var pixel = pair.Value;
            CheckDirection(Direction.Left, pixel);
            CheckDirection(Direction.Top, pixel);
            CheckDirection(Direction.Right, pixel);
            CheckDirection(Direction.Bottom, pixel);
            CheckDirection(Direction.LeftTop, pixel);
            CheckDirection(Direction.TopRight, pixel);
            CheckDirection(Direction.LeftBottom, pixel);
            CheckDirection(Direction.BottomRight, pixel);
        }
    }

    private int CheckDirection(Direction direction, DlaPixel walker)
    {
        if (!walker.ConnetNumber.ContainsKey(direction))
        {
            if (walker.Neighbor.TryGetValue(direction, out var neighbor))

                walker.ConnetNumber[direction] = CheckDirection(direction, PixelMap[neighbor]) + 1;
            else
                walker.ConnetNumber[direction] = 0;
        }
        return walker.ConnetNumber[direction];
    }

    public static Dictionary<(int X, int Y), DlaPixel> RelocatePixels(ICollection<DlaPixel> pixels)
    {
        var pixelMap = new Dictionary<(int X, int Y), DlaPixel>();
        foreach (var pixel in pixels)
            pixelMap[(pixel.X, pixel.Y)] = pixel;
        foreach (var pixel in pixelMap.Values)
        {
            var x = pixel.X;
            var y = pixel.Y;
            var left = x - 1;
            var top = y - 1;
            var right = x + 1;
            var bottom = y + 1;
            if (pixelMap.TryGetValue((left, y), out var other) && !other.Neighbor.ContainsKey(Direction.Right))
            {
                pixel.Neighbor[Direction.Left] = (left, y);
                other.Neighbor[Direction.Right] = (x, y);
            }
            if (pixelMap.TryGetValue((right, y), out other) && !other.Neighbor.ContainsKey(Direction.Left))
            {
                pixel.Neighbor[Direction.Right] = (right, y);
                other.Neighbor[Direction.Left] = (x, y);
            }
            if (pixelMap.TryGetValue((x, top), out other) && !other.Neighbor.ContainsKey(Direction.Bottom))
            {
                pixel.Neighbor[Direction.Top] = (x, top);
                other.Neighbor[Direction.Bottom] = (x, y);
            }
            if (pixelMap.TryGetValue((x, bottom), out other) && !other.Neighbor.ContainsKey(Direction.Top))
            {
                pixel.Neighbor[Direction.Bottom] = (x, bottom);
                other.Neighbor[Direction.Top] = (x, y);
            }
            if (pixelMap.TryGetValue((left, top), out other) && !other.Neighbor.ContainsKey(Direction.BottomRight))
            {
                pixel.Neighbor[Direction.LeftTop] = (left, top);
                other.Neighbor[Direction.BottomRight] = (x, y);
            }
            if (pixelMap.TryGetValue((left, bottom), out other) && !other.Neighbor.ContainsKey(Direction.TopRight))
            {
                pixel.Neighbor[Direction.LeftBottom] = (left, bottom);
                other.Neighbor[Direction.TopRight] = (x, y);
            }
            if (pixelMap.TryGetValue((right, top), out other) && !other.Neighbor.ContainsKey(Direction.LeftBottom))
            {
                pixel.Neighbor[Direction.TopRight] = (right, top);
                other.Neighbor[Direction.LeftBottom] = (x, y);
            }
            if (pixelMap.TryGetValue((right, bottom), out other) && !other.Neighbor.ContainsKey(Direction.LeftTop))
            {
                pixel.Neighbor[Direction.BottomRight] = (right, bottom);
                other.Neighbor[Direction.LeftTop] = (x, y);
            }
        }
        return pixelMap;
    }
}