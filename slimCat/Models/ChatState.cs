namespace slimCat.Services
{
    using Microsoft.Practices.Prism.Events;
    using Microsoft.Practices.Prism.Regions;
    using Microsoft.Practices.Unity;
    using Models;

    public class ChatState : IChatState
    {
        public ChatState(
            IUnityContainer container,
            IRegionManager regionManager,
            IEventAggregator eventAggregator,
            IChatModel chatModel,
            ICharacterManager characterManager,
            IHandleChatConnection chatConnection,
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

        public IHandleChatConnection ChatConnection { get; set; }
        public IUnityContainer Container { get; set; }
        public IRegionManager RegionManager { get; set; }
        public IEventAggregator EventAggregator { get; set; }
        public IChatModel ChatModel { get; set; }
        public IAccount Account { get; set; }
        public ICharacterManager CharacterManager { get; set; }
    }
}