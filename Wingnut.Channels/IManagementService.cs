namespace Wingnut.Channels
{
    using System.Collections.Generic;
    using System.ServiceModel;

    using Wingnut.Data.Configuration;
    using Wingnut.Data.Models;

    /// <summary>
    /// Management interface for controlling the Wingnut service.
    /// </summary>
    /// <remarks>
    /// All calls in this interface need to be synchronous. See:
    /// https://aloiskraus.wordpress.com/2018/07/01/why-does-my-synchronous-wcf-call-hang/
    /// </remarks>
    [ServiceContract(
        SessionMode = SessionMode.Required,
        CallbackContract = typeof(IManagementCallback))]
    public interface IManagementService
    {
        [OperationContract(IsOneWay = true)]
        void Register();

        [OperationContract(IsOneWay = true)]
        void Unregister();

        [OperationContract]
        WingnutServiceConfiguration GetServiceConfiguration();

        [OperationContract(IsOneWay = true)]
        void UpdateServiceConfiguration(
            WingnutServiceConfiguration configuration);

        [OperationContract]
        UpsConfiguration GetUpsConfiguration(
            string serverName,
            string upsName);

        [OperationContract(IsOneWay = true)]
        void UpdateUpsConfiguration(
            UpsConfiguration configuration);

        [OperationContract]
        List<Ups> GetUpsFromServer(
            Server server,
            string password,
            string upsName);

        [OperationContract]
        Ups AddUps(
            Server server,
            string password,
            string upsName, 
            int numPowerSupplies,
            bool monitorOnly,
            bool force);

        [OperationContract]
        bool RemoveUps(
            string serverName,
            string upsName);

        [OperationContract]
        List<Ups> GetUps(
            string serverName, 
            string upsName);
    }

    [ServiceContract]
    public interface IManagementCallback
    {
        [OperationContract(IsOneWay = true)]
        void UpsDeviceAdded(Ups ups);

        [OperationContract(IsOneWay = true)]
        void UpsDeviceChanged(Ups ups);

        [OperationContract(IsOneWay = true)]
        void UpsDeviceRemoved(string serverName, string upsName);
    }
}
