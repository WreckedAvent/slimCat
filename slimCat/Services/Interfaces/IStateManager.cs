using System;

namespace Slimcat.Services
{
    public interface IStateManager
    {
        event EventHandler OnStateChanged;

        ApplicationState ApplicationState { get; set; }
    }

    public enum ApplicationState
    {
        Login,
        CharacterSelect,
        Connect,
        Reconnect,
        Main,
        UnrecoverableError
    }
}
