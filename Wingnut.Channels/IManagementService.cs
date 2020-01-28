namespace Wingnut.Channels
{
    using System.Collections.Generic;
    using System.ServiceModel;
    using System.Threading.Tasks;

    using Wingnut.Data.Models;

    [ServiceContract(SessionMode = SessionMode.Required,
        CallbackContract = typeof(IManagementCallback))]
    public interface IManagementService
    {
        [OperationContract]
        List<Server> GetServers(string name);

        [OperationContract]
        Server AddServer(
            Server server, 
            string password, 
            bool ignoreConnectionFailure);

        [OperationContract]
        void UpdateServer(Server server);

        [OperationContract]
        void DeleteServer(string name);

        [OperationContract]
        Task<List<Ups>> GetUpsFromServer(
            string serverName, 
            string upsName);

        [OperationContract]
        Task<Ups> AddUps(
            string serverName, 
            string upsName, 
            bool monitorOnly,
            bool force);
    }

    public interface IManagementCallback
    {
        [OperationContract(IsOneWay = true)]
        void SendCallbackMessage(string message);
    }

    public enum CommunicationFaultType
    {
        Undefined,

    }
}
