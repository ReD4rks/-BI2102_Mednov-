using System.Globalization;
using System.Xml.Linq;
using Test1.Models;

namespace Test1.Services;

public class KmlService : IKmlService
{
    private readonly List<Field> _fields;

    public KmlService()
    {
        _fields = LoadFieldsFromKml();
    }

    private List<Field> LoadFieldsFromKml()
    {
        var fields = new List<Field>();
        var centroids = new Dictionary<int, Location>();

        // Load centroids
        var centroidsDoc = XDocument.Load("Data/centroids.kml");
        var centroidPlacemarks = centroidsDoc.Descendants("{http://www.opengis.net/kml/2.2}Placemark");

        foreach (var placemark in centroidPlacemarks)
        {
            var name = placemark.Element("{http://www.opengis.net/kml/2.2}name")?.Value;
            var extendedData = placemark.Element("{http://www.opengis.net/kml/2.2}ExtendedData");
            var fid = int.Parse(extendedData.Descendants("{http://www.opengis.net/kml/2.2}SimpleData")
                .First(x => x.Attribute("name")?.Value == "fid").Value);
            var size = double.Parse(extendedData.Descendants("{http://www.opengis.net/kml/2.2}SimpleData")
                .First(x => x.Attribute("name")?.Value == "size").Value);
            var coords = placemark.Descendants("{http://www.opengis.net/kml/2.2}coordinates").First().Value;
            var coordParts = coords.Split(',');

            centroids[fid] = new Location
            {
                Longitude = double.Parse(coordParts[0], CultureInfo.InvariantCulture),
                Latitude = double.Parse(coordParts[1], CultureInfo.InvariantCulture)
            };
        }

        // Load fields
        var fieldsDoc = XDocument.Load("Data/fields.kml");
        var fieldPlacemarks = fieldsDoc.Descendants("{http://www.opengis.net/kml/2.2}Placemark");

        foreach (var placemark in fieldPlacemarks)
        {
            var name = placemark.Element("{http://www.opengis.net/kml/2.2}name")?.Value;
            var extendedData = placemark.Element("{http://www.opengis.net/kml/2.2}ExtendedData");
            var fid = int.Parse(extendedData.Descendants("{http://www.opengis.net/kml/2.2}SimpleData")
                .First(x => x.Attribute("name")?.Value == "fid").Value);
            var size = double.Parse(extendedData.Descendants("{http://www.opengis.net/kml/2.2}SimpleData")
                .First(x => x.Attribute("name")?.Value == "size").Value);
            var coords = placemark.Descendants("{http://www.opengis.net/kml/2.2}coordinates").First().Value;

            var polygon = coords.Split(new[] { ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(coord =>
                {
                    var parts = coord.Split(',');
                    return new Location
                    {
                        Longitude = double.Parse(parts[0], CultureInfo.InvariantCulture),
                        Latitude = double.Parse(parts[1], CultureInfo.InvariantCulture)
                    };
                }).ToList();

            fields.Add(new Field
            {
                Id = fid,
                Name = name,
                Size = size,
                Center = centroids.GetValueOrDefault(fid),
                Polygon = polygon
            });
        }

        return fields;
    }

    public List<Field> GetAllFields() => _fields;

    public double? GetFieldSize(int id) => _fields.FirstOrDefault(f => f.Id == id)?.Size;

    public double? CalculateDistance(int fieldId, double pointLat, double pointLng)
    {
        var field = _fields.FirstOrDefault(f => f.Id == fieldId);
        if (field?.Center == null) return null;

        return CalculateDistanceInMeters(field.Center.Latitude, field.Center.Longitude, pointLat, pointLng);
    }

    public (int id, string name)? FindFieldContainingPoint(double lat, double lng)
    {
        var point = new Location { Latitude = lat, Longitude = lng };
        foreach (var field in _fields)
        {
            if (IsPointInPolygon(point, field.Polygon))
            {
                return (field.Id, field.Name);
            }
        }
        return null;
    }

    private bool IsPointInPolygon(Location point, List<Location> polygon)
    {
        bool inside = false;
        for (int i = 0, j = polygon.Count - 1; i < polygon.Count; j = i++)
        {
            var pi = polygon[i];
            var pj = polygon[j];

            if (((pi.Latitude > point.Latitude) != (pj.Latitude > point.Latitude)) &&
                (point.Longitude < (pj.Longitude - pi.Longitude) * (point.Latitude - pi.Latitude) /
                (pj.Latitude - pi.Latitude) + pi.Longitude))
            {
                inside = !inside;
            }
        }
        return inside;
    }

    private double CalculateDistanceInMeters(double lat1, double lon1, double lat2, double lon2)
    {
        const double R = 6371e3; // Earth radius in meters
        var φ1 = lat1 * Math.PI / 180;
        var φ2 = lat2 * Math.PI / 180;
        var Δφ = (lat2 - lat1) * Math.PI / 180;
        var Δλ = (lon2 - lon1) * Math.PI / 180;

        var a = Math.Sin(Δφ / 2) * Math.Sin(Δφ / 2) +
                Math.Cos(φ1) * Math.Cos(φ2) *
                Math.Sin(Δλ / 2) * Math.Sin(Δλ / 2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

        return R * c;
    }
}