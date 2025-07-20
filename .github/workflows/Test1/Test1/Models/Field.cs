namespace Test1.Models;

public class Field
{
    public int Id { get; set; }
    public string Name { get; set; }
    public double Size { get; set; }
    public Location Center { get; set; }
    public List<Location> Polygon { get; set; }
}