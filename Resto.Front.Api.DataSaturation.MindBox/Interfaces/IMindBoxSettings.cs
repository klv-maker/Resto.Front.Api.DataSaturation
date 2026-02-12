namespace Resto.Front.Api.DataSaturation.MindBox.Interfaces
{
    public interface IMindBoxSettings
    {
        string AddressApi {  get; }
        string Key { get; }
        void Update(string address, string key);
    }
}
