namespace slimCat.Services
{
    using System.Threading.Tasks;
    using Models;

    class TicketProvider : ITicketProvider
    {
        public Task<string> GetTicketAsync()
        {
            throw new System.NotImplementedException();
        }

        public Task<IAccount> GetAccountAsync()
        {
            throw new System.NotImplementedException();
        }
    }
}
