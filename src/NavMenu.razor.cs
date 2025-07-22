using MetaFrm.Control;
using MetaFrm.Database;
using MetaFrm.Maui.ApplicationModel;
using MetaFrm.Maui.Devices;
using MetaFrm.Razor.Menu.ViewModels;
using MetaFrm.Service;
using MetaFrm.Web.Bootstrap;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System.Drawing;

namespace MetaFrm.Razor.Menu
{
    /// <summary>
    /// MetaFrm.Razor.Menu
    /// </summary>
    public partial class NavMenu : IDisposable
    {
        private NavMenuViewModel NavMenuViewModel { get; set; } = new(null);

        private bool isFirstLoad = true;
        private string DisplayInfo { get; set; } = string.Empty;
        private string DisplayName
        {
            get
            {
                if (this.AuthState != null)
                    return this.AuthState.Nickname();

                return "";
            }
        }
        private string? ProfileImage { get; set; }

        [Inject]
        private IBrowser? Browser { get; set; }

        private bool IsLogoView { get; set; } = true;
        private string? LogoImageUrl { get; set; }
        private Size? LogoImageSize { get; set; }
        private string? LogoText { get; set; }
        private bool IsLoginView { get; set; } = true;

        [Inject]
        private IDeviceInfo? DeviceInfo { get; set; }

        [Inject]
        private IDeviceToken? DeviceToken { get; set; }

        private int? ActiveMenuID { get; set; } = null;
        private int? ActiveAssemblyID { get; set; } = null;
        private string TemplateName { get; set; } = string.Empty;

        /// <summary>
        /// OnInitialized
        /// </summary>
        protected override void OnInitialized()
        {
            base.OnInitialized();

            this.NavMenuViewModel = this.CreateViewModel<NavMenuViewModel>();

            try
            {
                if (this.Layout != null)
                {
                    this.Layout.Action -= Layout_Action;
                    this.Layout.Action += Layout_Action;
                }
            }
            catch (Exception)
            {
            }

            this.IsLogoView = this.GetAttributeBool(nameof(this.IsLogoView));
            this.LogoImageUrl = this.GetAttribute(nameof(this.LogoImageUrl));

            string? tmp = this.GetAttribute(nameof(this.LogoImageSize));
            string[]? tmps;
            if (!tmp.IsNullOrEmpty())
            {
                tmps = tmp.Split(',');
                this.LogoImageSize = new Size(tmps[0].ToInt(), tmps[1].ToInt());
            }

            this.LogoText = this.GetAttribute(nameof(this.LogoText));
            this.IsLoginView = this.GetAttributeBool(nameof(this.IsLoginView));
            this.TemplateName = this.GetAttribute(nameof(this.TemplateName));
        }

        private void Layout_Action(ICore sender, MetaFrmEventArgs e)
        {
            switch (e.Action)
            {
                case "Menu.Active":
                    if (e.Value != null && e.Value is List<int> ints && ints.Count > 1)
                    {
                        this.ActiveMenuID = ints[0];
                        this.ActiveAssemblyID = ints[1];
                    }
                    break;
            }
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
                if (Factory.Platform != Maui.Devices.DevicePlatform.Web)
                    this.HomeMenu();
            }
            else
            {
                if (Factory.Platform == Maui.Devices.DevicePlatform.Web)
                    this.HomeMenu();
            }
        }

        /// <summary>
        /// Dispose
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        /// <summary>
        /// Dispose
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (this.Layout != null)
                    this.Layout.Action -= Layout_Action;
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
        private async void SelectMenu()
        {
            ServiceData serviceData;
            Response response;

            if (this.NavMenuViewModel.IsBusy) return;

            try
            {
                this.NavMenuViewModel.IsBusy = true;

                if (this.AuthState.IsLogin())
                {
                    serviceData = new()
                    {
                        Token = this.AuthState.Token()
                    };

                    serviceData["1"].CommandText = this.GetAttribute("Select.Menu");
                    serviceData["1"].AddParameter("START_MENU_ID", DbType.Int, 3, null);
                    serviceData["1"].AddParameter("ONLY_PARENT_MENU_ID", DbType.Int, 3, null);
                    serviceData["1"].AddParameter("USER_ID", DbType.Int, 3, this.AuthState.UserID());

                    if (this.DeviceToken != null)
                    {
                        string? tmp = await this.DeviceToken.GetToken();

                        if (Config.Client.GetAttribute("IsSaveToken") == null && this.DeviceInfo != null && this.DeviceInfo.Model != null && !tmp.IsNullOrEmpty())
                            this.SaveToken(tmp);
                    }
                }
                else
                {
                    serviceData = new()
                    {
                        Token = Factory.AccessKey
                    };
                    serviceData["1"].CommandText = this.GetAttribute("Select.MenuDefault");
                    serviceData["1"].AddParameter("USER_ID", DbType.Int, 3, null);
                }

                response = this.ServiceRequest(serviceData);

                if (response.Status == Status.OK)
                {
                    if (response.DataSet != null && response.DataSet.DataTables.Count > 0 && response.DataSet.DataTables[0].DataRows.Count > 0)
                    {
                        this.NavMenuViewModel.MenuItems.Clear();
                        foreach (Data.DataRow dataRow in response.DataSet.DataTables[0].DataRows)
                        {
                            if (dataRow.Int("PARENT_MENU_ID") == 0)
                            {
                                this.NavMenuViewModel.MenuItems.Add(new(this.Localization)
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
                            else
                            {
                                FindParent(this.NavMenuViewModel.MenuItems, dataRow.Int("PARENT_MENU_ID"))?.Child.Add(new(this.Localization)
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

                            this.ActiveMenuID ??= dataRow.Int("MENU_ID");
                            this.ActiveAssemblyID ??= dataRow.Int("ASSEMBLY_ID");
                        }
                    }

                    if (response.DataSet != null && response.DataSet.DataTables.Count > 1 && response.DataSet.DataTables[1].DataRows.Count > 0)
                    {
                        foreach (Data.DataRow dataRow in response.DataSet.DataTables[1].DataRows)
                        {
                            if (dataRow.Values.ContainsKey("PROFILE_IMAGE"))
                            {
                                this.ProfileImage = dataRow.String("PROFILE_IMAGE");
                                this.OnAction(this, new MetaFrmEventArgs { Action = "ProfileImage", Value = this.ProfileImage });
                            }
                            else
                                this.OnAction(this, new MetaFrmEventArgs { Action = "ProfileImage", Value = null });

                            if (dataRow.Values.ContainsKey("DISPLAY_INFO"))
                            {
                                this.DisplayInfo = dataRow.String("DISPLAY_INFO") ?? "";
                                this.OnAction(this, new MetaFrmEventArgs { Action = "DisplayInfo", Value = this.DisplayInfo });
                            }
                            else
                                this.OnAction(this, new MetaFrmEventArgs { Action = "DisplayInfo", Value = null });
                        }
                    }
                }
                else
                {
                    if (response.Message != null)
                    {
                        this.ModalShow("메뉴", response.Message, new() { { "Ok", Btn.Warning } }, null);
                    }
                }
            }
            catch (Exception ex)
            {
                this.ModalShow("메뉴", $"{ex}", new() { { "Ok", Btn.Warning } }, null);
            }
            finally
            {
                this.NavMenuViewModel.IsBusy = false;
            }
        }
        private void SaveToken(string? Token)
        {
            Response? response;

            try
            {
                ServiceData serviceData = new()
                {
                    TransactionScope = true,
                    Token = this.AuthState.Token()
                };
                serviceData["1"].CommandText = this.GetAttribute("SaveToken");
                serviceData["1"].AddParameter("TOKEN_TYPE", DbType.NVarChar, 50, "Firebase.FCM");
                serviceData["1"].AddParameter("USER_ID", DbType.Int, 3, this.AuthState.UserID());
                if (this.DeviceInfo != null)
                {
                    serviceData["1"].AddParameter("DEVICE_MODEL", DbType.NVarChar, 50, this.DeviceInfo.Model);
                    serviceData["1"].AddParameter("DEVICE_NAME", DbType.NVarChar, 50, this.DeviceInfo.Name);
                }
                serviceData["1"].AddParameter("TOKEN_STR", DbType.NVarChar, 200, Token);

                response = serviceData.ServiceRequest(serviceData);

                if (response.Status == Status.OK)
                {
                    Config.Client.SetAttribute("IsSaveToken", "Y");
                }
                else
                {
                    if (response != null && response.Message != null)
                        this.ModalShow("메뉴", response.Message, new() { { "Ok", Btn.Warning } }, null);
                }
            }
            catch (Exception ex)
            {
                this.ModalShow("메뉴", $"{ex}", new() { { "Ok", Btn.Warning } }, null);
            }
        }

        private static MenuItemModel? FindParent(List<MenuItemModel> menuItems, int? parentMenuID)
        {
            MenuItemModel? item;

            foreach (MenuItemModel menuItem in menuItems)
            {
                if (menuItem.MenuID == parentMenuID)
                    return menuItem;

                if (menuItem.Child.Count > 0)
                {
                    item = FindParent(menuItem.Child, parentMenuID);

                    if (item != null)
                        return item;
                }
            }

            return null;
        }

        private async void ToggleNavMenu()
        {
            this.NavMenuViewModel.CollapseNavMenu = !this.NavMenuViewModel.CollapseNavMenu;

            if (this.TemplateName == "sneat")
                if (this.JSRuntime != null)
                    await this.JSRuntime.InvokeVoidAsync("LayoutMenuInit");
        }

        private void OnMenuHomeClick()
        {
            if (this.NavMenuViewModel.IsBusy) return;

            try
            {
                this.NavMenuViewModel.IsBusy = true;
                this.OnAction(this, new MetaFrmEventArgs { Action = "Menu", Value = new List<int> { 0, 0 } });

                this.ToggleNavMenu();
            }
            finally
            {
                this.NavMenuViewModel.IsBusy = false;
            }
        }
        private void OnOpenMenuClick(int? menuID)
        {
            if (this.NavMenuViewModel.IsBusy) return;

            try
            {
                this.NavMenuViewModel.IsBusy = true;

                this.ActiveMenuID = menuID;
            }
            catch (Exception)
            {
            }
            finally
            {
                this.NavMenuViewModel.IsBusy = false;
            }
        }
        private async void OnMenuClick(string? url)
        {
            if (this.NavMenuViewModel.IsBusy) return;

            try
            {
                this.NavMenuViewModel.IsBusy = true;

                if (url == null)
                    return;

                Uri uri = new(url);

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
        private void OnMenuClick(int? menuID, int? assemblyID, bool isActiveMenuID)
        {
            if (this.NavMenuViewModel.IsBusy) return;

            try
            {
                this.NavMenuViewModel.IsBusy = true;
                if (menuID != null && assemblyID != null)
                    this.OnAction(this, new MetaFrmEventArgs { Action = "Menu", Value = new List<int> { (int)menuID, (int)assemblyID } });

                if (isActiveMenuID)
                    this.ActiveMenuID = menuID;

                this.ActiveAssemblyID = assemblyID;

                this.ToggleNavMenu();
            }
            finally
            {
                this.NavMenuViewModel.IsBusy = false;
            }
        }
        private void OnProfileClick()
        {
            if (this.NavMenuViewModel.IsBusy) return;

            try
            {
                this.NavMenuViewModel.IsBusy = true;
                this.OnAction(this, new MetaFrmEventArgs { Action = "Profile" });

                this.ToggleNavMenu();
            }
            finally
            {
                this.NavMenuViewModel.IsBusy = false;
            }
        }
        private void OnLoginClick()
        {
            if (this.NavMenuViewModel.IsBusy) return;

            try
            {
                this.NavMenuViewModel.IsBusy = true;
                this.OnAction(this, new MetaFrmEventArgs { Action = "Login" });

                this.ToggleNavMenu();
            }
            finally
            {
                this.NavMenuViewModel.IsBusy = false;
            }
        }
        private void OnLogoutClick()
        {
            if (this.NavMenuViewModel.IsBusy) return;

            try
            {
                this.NavMenuViewModel.IsBusy = true;
                this.OnAction(this, new MetaFrmEventArgs { Action = "Logout" });

                this.ToggleNavMenu();
            }
            finally
            {
                this.NavMenuViewModel.IsBusy = false;
            }
        }
        private void OnRegisterClick()
        {
            if (this.NavMenuViewModel.IsBusy) return;

            try
            {
                this.NavMenuViewModel.IsBusy = true;
                this.OnAction(this, new MetaFrmEventArgs { Action = "Register" });

                this.ToggleNavMenu();
            }
            finally
            {
                this.NavMenuViewModel.IsBusy = false;
            }
        }
        private void OnPasswordResetClick()
        {
            if (this.NavMenuViewModel.IsBusy) return;

            try
            {
                this.NavMenuViewModel.IsBusy = true;
                this.OnAction(this, new MetaFrmEventArgs { Action = "PasswordReset" });

                this.ToggleNavMenu();
            }
            finally
            {
                this.NavMenuViewModel.IsBusy = false;
            }
        }

        private async void OnLayoutMenuExpandeClick()
        {
            if (this.TemplateName == "sneat")
                if (this.JSRuntime != null)
                    await this.JSRuntime.InvokeVoidAsync("LayoutMenuExpande");
        }
    }
}