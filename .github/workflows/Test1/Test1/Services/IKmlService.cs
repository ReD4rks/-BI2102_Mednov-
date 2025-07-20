using Test1.Models;

namespace Test1.Services;

public interface IKmlService
{
    List<Field> GetAllFields();
    double? GetFieldSize(int id);
    double? CalculateDistance(int fieldId, double pointLat, double pointLng);
    (int id, string name)? FindFieldContainingPoint(double lat, double lng);
}