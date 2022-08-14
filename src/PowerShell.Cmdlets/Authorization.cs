using System;
using System.Management.Automation;
using System.Management.Automation.Host;

namespace Xylab.Management.Automation
{
    [Flags]
    public enum Role
    {
        ViewOnly = 0,
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public sealed class AuthorizationAttribute : Attribute
    {
        public Role Roles { get; }

        public AuthorizationAttribute(Role roles)
        {
            this.Roles = roles;
        }
    }

    public class RoleBasedAuthroizationManager : AuthorizationManager
    {
        public RoleBasedAuthroizationManager(string shellId) : base(shellId)
        {
        }

        protected override bool ShouldRun(CommandInfo commandInfo, CommandOrigin origin, PSHost host, out Exception? reason)
        {
            if (origin == CommandOrigin.Internal)
            {
                reason = null;
                return true;
            }
            else
            {
                return base.ShouldRun(commandInfo, origin, host, out reason);
            }
        }
    }
}
