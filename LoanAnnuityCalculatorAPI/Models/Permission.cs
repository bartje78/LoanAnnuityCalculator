namespace LoanAnnuityCalculatorAPI.Models
{
    /// <summary>
    /// System-wide permissions
    /// </summary>
    public static class Permissions
    {
        // Tenant-level permissions
        public const string ManageTenant = "manage_tenant";
        public const string ManageUsers = "manage_users";
        public const string ManageFunds = "manage_funds";
        public const string ViewSettings = "view_settings";
        public const string EditSettings = "edit_settings";

        // Fund-level permissions (combined with fund access)
        public const string ViewFundData = "view_fund_data";
        public const string EditFundData = "edit_fund_data";
        public const string DeleteFundData = "delete_fund_data";
        public const string RunSimulations = "run_simulations";
        public const string GenerateReports = "generate_reports";
        public const string ExportData = "export_data";
        public const string ImportData = "import_data";
    }

    /// <summary>
    /// Tenant-level roles (in addition to Identity roles)
    /// </summary>
    public static class TenantRoles
    {
        public const string TenantAdmin = "TenantAdmin";       // Full control within tenant
        public const string FundManager = "FundManager";       // Manages assigned funds
        public const string Analyst = "Analyst";               // Read/analyze data
        public const string DataEntry = "DataEntry";           // Basic data entry only
    }
}
