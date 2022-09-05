using MetaFrm.MVVM;
using Microsoft.AspNetCore.Components;

namespace MetaFrm.Razor.Menu.ViewModels
{
    /// <summary>
    /// DefaultNavMenuViewModel
    /// </summary>
    public partial class NavMenuViewModel : BaseViewModel
    {
        /// <summary>
        /// CollapseNavMenu
        /// </summary>
        public bool CollapseNavMenu { get; set; } = true;
        /// <summary>
        /// Collapse
        /// </summary>
        public string Collapse { get; set; } = "collapse";
        /// <summary>
        /// NotCollapse
        /// </summary>
        public string NotCollapse { get; set; } = "";
        /// <summary>
        /// MenuItems
        /// </summary>
        public List<MenuItem> MenuItems { get; set; } = new();
    }
}