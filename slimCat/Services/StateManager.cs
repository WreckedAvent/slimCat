using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Slimcat.Services
{
    using Microsoft.Practices.Prism.Regions;

    public class StateManager : IStateManager
    {
        private ApplicationState applicationState;

        private IRegionManager regionManager;

        public StateManager(IRegionManager regionManager)
        {
            this.regionManager = regionManager;
        }

        public event EventHandler OnStateChanged;

        public ApplicationState ApplicationState
        {
            get
            {
                return this.applicationState;
            }

            set
            {
                this.applicationState = value;
                if (this.OnStateChanged != null)
                {
                    this.OnStateChanged(this, new EventArgs());
                }
                this.UpdateView();
            }
        }

        private void UpdateView()
        {
            
        }
    }
}
