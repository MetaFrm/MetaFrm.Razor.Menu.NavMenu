using MetaFrm.Control;
using MetaFrm.Razor.Menu.ViewModels;

namespace MetaFrm.Razor.Menu
{
    /// <summary>
    /// LoginDisplay
    /// </summary>
    public partial class LoginDisplay : IAction
    {
        internal LoginDisplayViewModel LoginDisplayViewModel { get; set; } = Factory.CreateViewModel<LoginDisplayViewModel>();

        private string DisplayName
        {
            get
            {
                if (this.AuthenticationState != null)
                {
                    var auth = this.AuthenticationState.Result;

                    if (auth.User.Identity != null && auth.User.Identity.IsAuthenticated)
                        return auth.User.Claims.Single(x => x.Type == "Account.NICKNAME").Value;
                }

                return "";
            }
        }

        private void ToggleNavMenu()
        {
            this.OnAction(this, new MetaFrmEventArgs { Action = "CollapseNavMenu" });
        }

        private void OnLoginClick()
        {
            try
            {
                if (this.LoginDisplayViewModel.IsBusy) return;

                this.LoginDisplayViewModel.IsBusy = true;
                this.OnAction(this, new MetaFrmEventArgs { Action = "Login" });
            }
            finally
            {
                this.LoginDisplayViewModel.IsBusy = false;
            }
        }
        private void OnLogoutClick()
        {
            try
            {
                if (this.LoginDisplayViewModel.IsBusy) return;

                this.LoginDisplayViewModel.IsBusy = true;
                this.OnAction(this, new MetaFrmEventArgs { Action = "Logout" });
            }
            finally
            {
                this.LoginDisplayViewModel.IsBusy = false;
            }
        }
        private void OnRegisterClick()
        {
            try
            {
                if (this.LoginDisplayViewModel.IsBusy) return;

                this.LoginDisplayViewModel.IsBusy = true;
                this.OnAction(this, new MetaFrmEventArgs { Action = "Register" });
            }
            finally
            {
                this.LoginDisplayViewModel.IsBusy = false;
            }
        }
        private void OnPasswordResetClick()
        {
            try
            {
                if (this.LoginDisplayViewModel.IsBusy) return;

                this.LoginDisplayViewModel.IsBusy = true;
                this.OnAction(this, new MetaFrmEventArgs { Action = "PasswordReset" });
            }
            finally
            {
                this.LoginDisplayViewModel.IsBusy = false;
            }
        }
    }
}