using CsvHelper.Configuration.Attributes;

namespace IrisDatasetExample;

public class Flower
{
    [Name("sepal_length")]
    public float SepalLength { get; set; }

    [Name("sepal_width")]
    public float SepalWidth { get; set; }
	
    [Name("petal_length")]
    public float PetalLength { get; set; }
	
    [Name("petal_width")]
    public float PetalWidth { get; set; }

    [Name("species")]
    public int Species { get; set; }
	
    public double[] Vector => [SepalLength, SepalWidth, PetalLength, PetalWidth];
}
