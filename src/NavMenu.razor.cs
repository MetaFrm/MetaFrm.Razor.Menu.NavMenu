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
    public partial class NavMenu
    {
        internal NavMenuViewModel NavMenuViewModel { get; set; } = Factory.CreateViewModel<NavMenuViewModel>();

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

        [Inject] IBrowser? Browser { get; set; }

        private string? LogoImageUrl { get; set; }
        private Size? LogoImageSize { get; set; }
        private string? LogoText { get; set; }

        [Inject]
        internal IDeviceInfo? DeviceInfo { get; set; }

        [Inject]
        internal IDeviceToken? DeviceToken { get; set; }

        private int? ActiveMenuID { get; set; } = null;

        private int? ActiveAssemblyID { get; set; } = null;

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
                if (Factory.Platform != Maui.Devices.DevicePlatform.Web)
                {
                    this.HomeMenu();
                }
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
        private async void SelectMenu()
        {
            Response response;

            try
            {
                if (this.NavMenuViewModel.IsBusy) return;

                this.NavMenuViewModel.IsBusy = true;

                ServiceData data;

                if (this.AuthState.IsLogin())
                {
                    data = new()
                    {
                        Token = this.AuthState.Token()
                    };

                    data["1"].CommandText = this.GetAttribute("Select.Menu");
                    data["1"].AddParameter("START_MENU_ID", DbType.Int, 3, null);
                    data["1"].AddParameter("ONLY_PARENT_MENU_ID", DbType.Int, 3, null);
                    data["1"].AddParameter("USER_ID", DbType.Int, 3, this.AuthState.UserID());

                    if (this.DeviceToken != null)
                    {
                        string? tmp = await this.DeviceToken.GetToken();

                        if (Config.Client.GetAttribute("IsSaveToken") == null && this.DeviceInfo != null && this.DeviceInfo.Model != null && !tmp.IsNullOrEmpty())
                            this.SaveToken(tmp);
                    }
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
                            {
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
                            }
                            else
                            {
                                this.FindParent(this.NavMenuViewModel.MenuItems, dataRow.Int("PARENT_MENU_ID"))?.Child.Add(new()
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
                            this.ProfileImage = dataRow.String("PROFILE_IMAGE");

                            if (dataRow.Values.ContainsKey("MEMBER_INACTIVE_DATE"))
                            {
                                DateTime? dateTime = dataRow.DateTime("MEMBER_INACTIVE_DATE");
                                if (dateTime != null && dateTime < DateTime.Now)
                                    dateTime = null;

                                this.DisplayInfo = $"{dataRow.Decimal("POINT"):N0}P | {dataRow.Decimal("AKT"):N0}AK | Lv{dataRow.Int("LEVEL")} | {dataRow.Decimal("POINT_RATE"):P}{(dateTime == null ? "" : $" | {dateTime:MM-dd HH:mm}")} | {dataRow.Int("RUN_CURRENT")}/{dataRow.Int("RUN_TOTAL")}R";
                            }

                            this.OnAction(this, new MetaFrmEventArgs { Action = "ProfileImage", Value = this.ProfileImage });
                            this.OnAction(this, new MetaFrmEventArgs { Action = "DisplayInfo", Value = this.DisplayInfo });
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
            catch (Exception ex)
            {
                this.ModalShow("LoadMenu", $"{ex}", new() { { "Ok", Btn.Warning } }, EventCallback.Factory.Create<string>(this, OnClickFunction));
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
                        this.ModalShow("SaveToken", response.Message, new() { { "Ok", Btn.Warning } }, EventCallback.Factory.Create<string>(this, OnClickFunction));
                }
            }
            catch (Exception ex)
            {
                this.ModalShow("SaveToken", $"{ex}", new() { { "Ok", Btn.Warning } }, EventCallback.Factory.Create<string>(this, OnClickFunction));
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

        private async void ToggleNavMenu()
        {
            this.NavMenuViewModel.CollapseNavMenu = !this.NavMenuViewModel.CollapseNavMenu;

            if ((Factory.ProjectService.GetAttributeValue("Template.Name") ?? "") == "sneat")
                if (this.JSRuntime != null)
                    await this.JSRuntime.InvokeVoidAsync("LayoutMenuInit");
        }

        private void OnMenuHomeClick()
        {
            try
            {
                if (this.NavMenuViewModel.IsBusy) return;

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
            try
            {
                if (this.NavMenuViewModel.IsBusy) return;

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
            try
            {
                if (this.NavMenuViewModel.IsBusy) return;

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
            try
            {
                if (this.NavMenuViewModel.IsBusy) return;

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
            try
            {
                if (this.NavMenuViewModel.IsBusy) return;

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
            try
            {
                if (this.NavMenuViewModel.IsBusy) return;

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
            try
            {
                if (this.NavMenuViewModel.IsBusy) return;

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
            try
            {
                if (this.NavMenuViewModel.IsBusy) return;

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
            try
            {
                if (this.NavMenuViewModel.IsBusy) return;

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
            if ((Factory.ProjectService.GetAttributeValue("Template.Name") ?? "") == "sneat")
                if (this.JSRuntime != null)
                    await this.JSRuntime.InvokeVoidAsync("LayoutMenuExpande");
        }
    }
}