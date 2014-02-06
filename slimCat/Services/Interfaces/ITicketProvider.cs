namespace slimCat.Services
{
    using System.Threading.Tasks;
    using Models;

    public interface ITicketProvider
    {
        Task<string> GetTicketAsync();

        Task<IAccount> GetAccountAsync();
    }
}
