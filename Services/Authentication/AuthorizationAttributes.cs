using System;

namespace HospitalManagementSystem.Services.Authentication
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class RequireRoleAttribute : Attribute
    {
        public string[] Roles { get; }

        public RequireRoleAttribute(params string[] roles)
        {
            Roles = roles;
        }
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class RequirePermissionAttribute : Attribute
    {
        public string Permission { get; }

        public RequirePermissionAttribute(string permission)
        {
            Permission = permission;
        }
    }
}