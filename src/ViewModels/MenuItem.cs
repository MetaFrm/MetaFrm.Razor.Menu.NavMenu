namespace MetaFrm.Razor.Menu.ViewModels
{
    /// <summary>
    /// MenuItem
    /// </summary>
    public class MenuItem 
    {
        /// <summary>
        /// MenuID
        /// </summary>
        public int? MenuID { get; set; }
        /// <summary>
        /// ParentMenuID
        /// </summary>
        public int? ParentMenuID { get; set; }
        /// <summary>
        /// Name
        /// </summary>
        public string? Name { get; set; }
        /// <summary>
        /// Description
        /// </summary>
        public string? Description { get; set; }
        /// <summary>
        /// Icon
        /// </summary>
        public string? Icon { get; set; }
        /// <summary>
        /// AssemblyID
        /// </summary>
        public int? AssemblyID { get; set; }
        /// <summary>
        /// Sort
        /// </summary>
        public int? Sort { get; set; }
        /// <summary>
        /// Child
        /// </summary>
        public List<MenuItem> Child { get; set; } = new();
    }
}