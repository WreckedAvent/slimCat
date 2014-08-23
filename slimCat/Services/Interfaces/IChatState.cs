namespace slimCat.Services
{
    using Microsoft.Practices.Prism.Events;
    using Microsoft.Practices.Prism.Regions;
    using Microsoft.Practices.Unity;
    using Models;

    public interface IChatState
    {
        IChatConnection ChatConnection { get; set; }

        IUnityContainer Container { get; set; }
        
        IRegionManager RegionManager { get; set; }
        
        IEventAggregator EventAggregator { get; set; }
        
        IChatModel ChatModel { get; set; }

        IAccount Account { get; set; }

        ICharacterManager CharacterManager { get; set; }
    }

    public class ChatState : IChatState
    {
        public ChatState(
            IUnityContainer container,
            IRegionManager regionManager, 
            IEventAggregator eventAggregator,
            IChatModel chatModel,
            ICharacterManager characterManager, 
            IChatConnection chatConnection,
            IAccount account)
        {
            ChatConnection = chatConnection;
            Container = container;
            RegionManager = regionManager;
            EventAggregator = eventAggregator;
            ChatModel = chatModel;
            CharacterManager = characterManager;
            Account = account;
        }

        public IChatConnection ChatConnection { get; set; }
        public IUnityContainer Container { get; set; }
        public IRegionManager RegionManager { get; set; }
        public IEventAggregator EventAggregator { get; set; }
        public IChatModel ChatModel { get; set; }
        public IAccount Account { get; set; }
        public ICharacterManager CharacterManager { get; set; }
    }
}
