using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using HdbscanSharp.Distance;
using HdbscanSharp.Runner;

namespace Tests;

public class DemoTests
{
    [Fact]
    public void GroupingCoordinates()
    {
        string expected = @"Group: (-500, 1000) (500, 1000)
Group: (100, 97) (98, 100) (98, 95) (97, 94)
Group: (0, 0) (0, 1) (1, 0) (2, 3)
Group: (-100, 97) (-98, 100) (-98, 95) (-97, 94) (-90, 93)";
        
        List<(int X, int Y)> points =
        [
            (0, 0), (0, 1), (1, 0), (2, 3), // A group near coordinate (0, 0)
            (100, 97), (98, 100), (98, 95), (97, 94), // A group near coordinate (100, 100)
            (-100, 97), (-98, 100), (-98, 95), (-97, 94), (-90, 93), // A group near coordinate (-100, 100)
            (-500, 1000), (500, 1000) // Two outliers
        ];

        var result = HdbscanRunner.Run(points, point => new float[] {point.X, point.Y}, 3, 3, GenericEuclideanDistance.GetFunc);

        var lines = result.Groups.Select(group => "Group: " + string.Join(" ", group.Value.Select(x => "(" + x.X + ", " + x.Y + ")")));
        var actual = string.Join(Environment.NewLine, lines);
        Assert.Equal(expected, actual);
    }
}