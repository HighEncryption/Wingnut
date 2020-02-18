namespace Wingnut.Channels
{
    using System.Collections.Generic;
    using System.ServiceModel;
    using System.Threading.Tasks;

    using Wingnut.Data.Configuration;
    using Wingnut.Data.Models;

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

        [OperationContract]
        void UpdateServiceConfiguration(
            WingnutServiceConfiguration configuration);

        [OperationContract]
        UpsConfiguration GetUpsConfiguration(
            string serverName,
            string upsName);

        [OperationContract]
        void UpdateUpsConfiguration(
            UpsConfiguration configuration);

        [OperationContract]
        Task<List<Ups>> GetUpsFromServer(
            Server server,
            string password,
            string upsName);

        [OperationContract]
        Task<Ups> AddUps(
            Server server,
            string password,
            string upsName, 
            int numPowerSupplies,
            bool monitorOnly,
            bool force);

        [OperationContract]
        Task<List<Ups>> GetUps(
            string serverName, 
            string upsName);
    }

    public interface IManagementCallback
    {
        [OperationContract(IsOneWay = true)]
        void UpsDeviceChanged(Ups ups);
    }
}
