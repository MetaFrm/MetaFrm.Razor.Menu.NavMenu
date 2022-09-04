using MetaFrm.Control;
using MetaFrm.Database;
using MetaFrm.Maui.ApplicationModel;
using MetaFrm.Razor.Menu.ViewModels;
using MetaFrm.Service;
using MetaFrm.Web.Bootstrap;
using Microsoft.AspNetCore.Components;
using System.Drawing;

namespace MetaFrm.Razor.Menu
{
    /// <summary>
    /// MetaFrm.Razor.Menu
    /// </summary>
    public partial class NavMenu
    {
        internal NavMenuViewModel NavMenuViewModel { get; set; } = Factory.CreateViewModel<NavMenuViewModel>();

        private bool isFirstLoad = true;

        [Inject] IBrowser? Browser { get; set; }

        private string? LogoImageUrl { get; set; }
        private Size? LogoImageSize { get; set; }
        private string? LogoText { get; set; }

        /// <summary>
        /// OnInitialized
        /// </summary>
        protected override void OnInitialized()
        {
            base.OnInitialized();

            this.LogoImageUrl = this.GetAttribute("LogoImageUrl");

            string? tmp = this.GetAttribute("LogoImageSize");
            string[]? tmps;
            if (!tmp.IsNullOrEmpty())
            {
                tmps = tmp.Split(',');
                this.LogoImageSize = new Size(tmps[0].ToInt(), tmps[1].ToInt());
            }
            this.LogoText = this.GetAttribute("LogoText");
        }

        /// <summary>
        /// OnAfterRender
        /// </summary>
        /// <param name="firstRender"></param>
        protected override void OnAfterRender(bool firstRender)
        {
            base.OnAfterRender(firstRender);

            if (firstRender)
            {
                if (this.NavMenuViewModel.LoginDisplay != null && this.NavMenuViewModel.LoginDisplay.Instance != null && this.NavMenuViewModel.LoginDisplay.Instance is IAction action)
                {
                    action.Action -= LoginDisplay_Begin;
                    action.Action += LoginDisplay_Begin;
                }

                if (Factory.Platform != Maui.Devices.DevicePlatform.Web)
                    this.HomeMenu();
            }
            else
            {
                if (Factory.Platform == Maui.Devices.DevicePlatform.Web)
                    this.HomeMenu();
            }
        }


        private void HomeMenu()
        {
            if (this.isFirstLoad)
            {
                this.isFirstLoad = false;
                this.SelectMenu();
                this.StateHasChanged();
            }
        }
        private void SelectMenu()
        {
            Response response;

            try
            {
                if (this.NavMenuViewModel.IsBusy) return;

                this.NavMenuViewModel.IsBusy = true;

                ServiceData data;

                if (this.IsLogin())
                {
                    data = new()
                    {
                        Token = this.UserClaim("Token")
                    };

                    data["1"].CommandText = this.GetAttribute("Select.Menu");
                    data["1"].AddParameter("START_MENU_ID", DbType.Int, 3, null);
                    data["1"].AddParameter("ONLY_PARENT_MENU_ID", DbType.Int, 3, null);
                    data["1"].AddParameter("USER_ID", DbType.Int, 3, this.UserClaim("Account.USER_ID").ToInt());
                }
                else
                {
                    data = new()
                    {
                        Token = Factory.AccessKey
                    };
                    data["1"].CommandText = this.GetAttribute("Select.MenuDefault");
                    data["1"].AddParameter("USER_ID", DbType.Int, 3, null);
                }

                response = this.ServiceRequest(data);

                if (response.Status == Status.OK)
                {
                    if (response.DataSet != null && response.DataSet.DataTables.Count > 0 && response.DataSet.DataTables[0].DataRows.Count > 0)
                    {
                        this.NavMenuViewModel.MenuItems.Clear();
                        foreach (Data.DataRow dataRow in response.DataSet.DataTables[0].DataRows)
                        {
                            if (dataRow.Int("PARENT_MENU_ID") == 0)
                                this.NavMenuViewModel.MenuItems.Add(new()
                                {
                                    MenuID = dataRow.Int("MENU_ID"),
                                    ParentMenuID = dataRow.Int("PARENT_MENU_ID"),
                                    Name = dataRow.String("NAME"),
                                    Description = dataRow.String("DESCRIPTION"),
                                    Icon = dataRow.String("ICON"),
                                    AssemblyID = dataRow.Int("ASSEMBLY_ID"),
                                    Sort = dataRow.Int("SORT"),
                                });
                            else
                            {
                                MenuItem? menuItem = this.FindParent(this.NavMenuViewModel.MenuItems, dataRow.Int("PARENT_MENU_ID"));

                                if (menuItem != null)
                                    menuItem.Child.Add(new()
                                    {
                                        MenuID = dataRow.Int("MENU_ID"),
                                        ParentMenuID = dataRow.Int("PARENT_MENU_ID"),
                                        Name = dataRow.String("NAME"),
                                        Description = dataRow.String("DESCRIPTION"),
                                        Icon = dataRow.String("ICON"),
                                        AssemblyID = dataRow.Int("ASSEMBLY_ID"),
                                        Sort = dataRow.Int("SORT"),
                                    });
                            }
                        }
                    }
                }
                else
                {
                    if (response.Message != null)
                    {
                        this.ModalShow("LoadMenu", response.Message, new() { { "Ok", Btn.Warning } }, EventCallback.Factory.Create<string>(this, OnClickFunction));
                    }
                }
            }
            finally
            {
                this.NavMenuViewModel.IsBusy = false;
            }
        }
        private void OnClickFunction(string action)
        {
        }
        private MenuItem? FindParent(List<MenuItem> menuItems, int? parentMenuID)
        {
            MenuItem? item;

            foreach (MenuItem menuItem in menuItems)
            {
                if (menuItem.MenuID == parentMenuID)
                    return menuItem;

                if (menuItem.Child.Count > 0)
                {
                    item = this.FindParent(menuItem.Child, parentMenuID);

                    if (item != null)
                        return item;
                }
            }

            return null;
        }


        private void LoginDisplay_Begin(ICore sender, MetaFrmEventArgs e)
        {
            if (e.Action == "CollapseNavMenu")
            {
                this.ToggleNavMenu();
                this.StateHasChanged();
            }
            else
                this.OnAction(sender, e);
        }

        private void ToggleNavMenu()
        {
            this.NavMenuViewModel.CollapseNavMenu = !this.NavMenuViewModel.CollapseNavMenu;
        }

        private void OnMenuHomeClick()
        {
            try
            {
                if (this.NavMenuViewModel.IsBusy) return;

                this.NavMenuViewModel.IsBusy = true;
                this.OnAction(this, new MetaFrmEventArgs { Action = "Menu", Value = new List<int> { 0, 0 } });
            }
            finally
            {
                this.NavMenuViewModel.IsBusy = false;
            }
        }
        private async void OnMenuClick(string? url)
        {
            try
            {
                if (this.NavMenuViewModel.IsBusy) return;

                this.NavMenuViewModel.IsBusy = true;

                if (url == null)
                    return;

                Uri uri = new Uri(url);

                if (this.Browser != null)
                    await this.Browser.OpenAsync(uri, BrowserLaunchMode.SystemPreferred);

                this.ToggleNavMenu();
            }
            catch (Exception)
            {
            }
            finally
            {
                this.NavMenuViewModel.IsBusy = false;
            }
        }
        private void OnMenuClick(int? menuID, int? assemblyID)
        {
            try
            {
                if (this.NavMenuViewModel.IsBusy) return;

                this.NavMenuViewModel.IsBusy = true;
                if (menuID != null && assemblyID != null)
                    this.OnAction(this, new MetaFrmEventArgs { Action = "Menu", Value = new List<int> { (int)menuID, (int)assemblyID } });

                this.ToggleNavMenu();
            }
            finally
            {
                this.NavMenuViewModel.IsBusy = false;
            }
        }
        private void OnStyleChangeClick()
        {
            try
            {
                if (this.NavMenuViewModel.IsBusy) return;

                this.NavMenuViewModel.IsBusy = true;
                this.OnAction(this, new MetaFrmEventArgs { Action = "StyleChange" });
            }
            finally
            {
                this.NavMenuViewModel.IsBusy = false;
            }
        }
    }
}